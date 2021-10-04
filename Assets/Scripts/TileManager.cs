using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TileManager : MonoBehaviour
{
    public static TileType Unmovable = new TileType('x', false, false, null);
    public static TileType Grass1 = new TileType('1', true, true, null);
    public static TileType Grass2 = new TileType('2', true, true, Grass1);
    public static TileType Grass3 = new TileType('3', true, true, Grass2);
    public static TileType Grass4 = new TileType('4', true, true, Grass3);
    public static TileType GrassFull = new TileType('o', true, true, Grass4);
    public static TileType FlagTile = new TileType('F', false, false, null);
    public static TileType Replanted = new TileType('v', true, false, null);
    public static TileType Replanted2 = new TileType('w', true, false, null);
    public static TileType Replanted3 = new TileType('u', true, false, null);

    public TextAsset[] levelTextAssets;
    public GameObject tilePrefab;
    public Sprite unmovableTileSprite;
    public Sprite grassFullTileSprite;
    public Sprite grass4TileSprite;
    public Sprite grass3TileSprite;
    public Sprite grass2TileSprite;
    public Sprite grass1TileSprite;
    public Sprite flagTileSprite;
    public Sprite replantedSprite;
    public Sprite replanted2Sprite;
    public Sprite replanted3Sprite;
    public Dictionary<TileType, Sprite> tileSprites = new Dictionary<TileType, Sprite>();

    public GameObject[] firstLevelExplanations;
    public GameObject finalLevelExplanation;

    public GameObject levelPrefab;
    public GameObject playerPrefab;
    public GameObject flagPrefab;
    public GameObject rockPrefab;

    public Text currentLevelText;

    public int currentLevelId = 1;
    public Level currentLevel = null;
    private Vector3 currentLevelOffset = Vector3.zero;
    public Camera myCamera;

    private float levelFadeDelay = 5f;
    private float fadingOutTime = 0f;
    public Level fadingOutLevel = null;
    private Vector3 fromLeafPos = Vector3.zero;
    private GameObject currentLeaf = null;
    public GameObject leafPrefab;
    public AnimationCurve leafSpeedCurve;
    public AnimationCurve leafHeightCurve;

    public bool replantsEverything = false;
    public float replantingTime = 0f;
    public float pushTogetherDelay = 10f;
    public bool startedReplant = false;

    private float fadingBackTime = 0f;
    public List<Level> pastLevels = new List<Level>();

    private float levelStartingOrEndingTime = 0f;
    public bool levelStarting = false;
    public bool levelEnding = false;
    private float levelEndAndStartDelay = 1f;

    private float changeWaterColorTime = 0f;
    public float changeWaterColorDelay = 2f;
    
    public Color initialWaterColor = new Color(57, 75, 80);
    public Color replantedWaterColor = new Color(115, 190, 211);

    public class TileType
    {
        private static readonly Dictionary<char, TileType> byCode = new Dictionary<char, TileType>();
        public readonly char Code;
        public readonly bool Movable;
        public readonly bool Decays;
        public readonly TileType DecaysTo;

        public TileType(char code, bool movable, bool decays, TileType decaysTo)
        {
            this.Code = code;
            byCode.Add(code, this);
            this.Movable = movable;
            this.Decays = decays;
            this.DecaysTo = decaysTo;
        }

        public static TileType FromCode(char code)
        {
            return byCode[code];
        }
    }
    
    public class Tile
    {
        public TileType Type;
        public TileComponent Comp;
        public bool HasFlag = false;
        public bool HasRock = false;

        public Tile(TileType type)
        {
            this.Type = type;
            this.Comp = null;
        }
    }

    public readonly struct TilePos
    {
        public readonly int X;
        public readonly int Y;
        
        public static float offsetScale = 1.4f;

        public TilePos(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
        
        public static TilePos operator +(TilePos a, TilePos b) => new TilePos(a.X + b.X, a.Y + b.Y);

        public override string ToString()
        {
            return "(" + this.X + "," + this.Y + ")";
        }

        public static Vector3 CoordsToTransformPosition(float x, float y)
        {
            return new Vector3(0.5f * offsetScale * x + 0.5f * offsetScale * y, -0.25f * offsetScale * x + 0.25f * offsetScale * y, y - x);   
        }

        public static Vector2 TransformToTileCoords(float x, float y)
		{
            return 1.0f / offsetScale * (new Vector2(x - 2.0f * y, x + 2.0f * y));
		}

        public Vector3 ToTransformPosition()
        {
            return CoordsToTransformPosition(this.X, this.Y);
        }

        public bool IsUpDir()
		{
            return this.X == -1 || this.Y == 1;
		}
    }


    public class Level
    {
        public readonly Dictionary<TilePos, Tile> Tiles;
        public readonly HashSet<Tile> DeletedTiles = new HashSet<Tile>();
        public TileManager Manager;
        public PlayerController playerComp;

        public GameObject obj;
        public bool StepActive = false;
        public float CurrentStepDelta = 0.0f;
        public float StepLength = 1.0f;  // in seconds

        public Level(Dictionary<TilePos, Tile> tiles, TilePos playerPos, Vector3 levelPos, TileManager manager)
        {
            this.Tiles = tiles;
            this.Manager = manager;

            this.obj = Instantiate(manager.levelPrefab);
            this.obj.transform.position = levelPos;
            // make tiles
            foreach (var entry in tiles)
            {
                var pos = entry.Key;
                var tileObj = Instantiate(manager.tilePrefab, levelPos + pos.ToTransformPosition(), Quaternion.identity, this.obj.transform);
                var comp = tileObj.GetComponent<TileComponent>();
                comp.Init(this, manager.tileSprites[entry.Value.Type], pos);
                entry.Value.Comp = comp;
                if (entry.Value.HasFlag)
                    comp.topEntity = Instantiate(manager.flagPrefab, comp.blockTransform.position + new Vector3(0f, 0f, -0.9f), Quaternion.identity, comp.blockTransform);
                if (entry.Value.HasRock)
                    comp.topEntity = Instantiate(manager.rockPrefab, comp.blockTransform.position + new Vector3(0f, 0f, -0.9f), Quaternion.identity, comp.blockTransform);
            }
            // make player
            var player = Instantiate(manager.playerPrefab, this.obj.transform);
            this.playerComp = player.GetComponent<PlayerController>();
            this.playerComp.Init(this, playerPos);
        }

        public Tile Get(TilePos pos)
        {
            if (this.Tiles.ContainsKey(pos))
                return this.Tiles[pos];
            return null;
        }

        public void Set(TilePos pos, Tile newTile)
        {
            if (newTile == null)
                this.Tiles.Remove(pos);
            else
                this.Tiles[pos] = newTile;
        }

        public void Update()
        {
            if (!StepActive)
                return;
            CurrentStepDelta += Time.deltaTime / StepLength;
            if (CurrentStepDelta > 1.0f)
                this.UpdateStep();
        }

        private void UpdateStep()
        {
            this.StepActive = false;
            this.CurrentStepDelta = 0.0f;
            foreach (var entry in this.Tiles)
                entry.Value.Comp.UpdateStep();
            foreach (var tile in this.DeletedTiles)
                tile.Comp.UpdateStep();
            this.DeletedTiles.Clear();
        }

        public bool CanShiftTiles(TilePos pos, TilePos direction)
        {
            var new_pos = pos + direction;
            if (this.Get(new_pos) == null) return false;
            while (this.Get(new_pos) != null)
            {
                if (this.Get(new_pos) != null && !this.Get(new_pos).Type.Movable)
                    return false;
                new_pos = new_pos + direction;
            }
            return true;
        }

        public void ShiftTiles(TilePos pos, TilePos direction)
        {
            if (this.StepActive)
                this.UpdateStep();
            pos = pos + direction;
            Tile tileBefore = null;
            while (this.Get(pos) != null)
            {
                Tile tile = this.Get(pos);
                tile.Comp.DoMoveTo(pos + direction);
                if (tile.Type.Decays)
                {
                    tile.Comp.DoChangeTo(tile.Type.DecaysTo);
                    if (tile.Type.DecaysTo != null)
                        tile.Type = tile.Type.DecaysTo;
                    else
                    {
                        this.DeletedTiles.Add(tile);
                        tile = null;
                    }
                }
                this.Set(pos, tileBefore);
                tileBefore = tile;
                pos = pos + direction;
            }
            this.Set(pos, tileBefore);
            this.StepActive = true;
        }

        public void Cleanup()
        {
            Destroy(this.obj);
        }

        public TilePos GetDimensions()
        {
            TilePos smallPos = new TilePos(Int32.MaxValue, Int32.MaxValue);
            TilePos largePos = new TilePos(Int32.MinValue, Int32.MinValue);
            foreach (var pos in this.Tiles.Keys)
            {
                smallPos = new TilePos(Math.Min(pos.X, smallPos.X), Math.Min(pos.Y, smallPos.Y));
                largePos = new TilePos(Math.Max(pos.X, largePos.X), Math.Max(pos.Y, largePos.Y));
            }
            return new TilePos(largePos.X - smallPos.X + 1, largePos.Y - smallPos.Y + 1);
        }

        public Vector3 GetGlobalCenterPos()
        {
            TilePos smallPos = new TilePos(Int32.MaxValue, Int32.MaxValue);
            TilePos largePos = new TilePos(Int32.MinValue, Int32.MinValue);
            foreach (var pos in this.Tiles.Keys)
            {
                smallPos = new TilePos(Math.Min(pos.X, smallPos.X), Math.Min(pos.Y, smallPos.Y));
                largePos = new TilePos(Math.Max(pos.X, largePos.X), Math.Max(pos.Y, largePos.Y));
            }
            return this.obj.transform.position + 0.5f * (smallPos + largePos + new TilePos(-2, 2)).ToTransformPosition();
        }

        public void MaybeReplantTile(TilePos pos)
        {
            if (Get(pos) == null)
                return;
            var replanted = new[] { Replanted, Replanted2, Replanted3, FlagTile};
            var tile = replanted[Random.Range(0, replanted.Length)];
            Get(pos).Comp.DoChangeTo(tile);
            Get(pos).Comp.myOcean.MakeWave(new Vector2(pos.X, pos.Y), 1, 0.1f, 0.5f);
            if (this.playerComp.pos.X == pos.X && this.playerComp.pos.Y == pos.Y)
            {
                this.playerComp.myAnimator.Play("GainLeaves");
            }

            if (Get(pos).HasRock)
            {
                Destroy(Get(pos).Comp.topEntity);
                Get(pos).HasRock = false;
                var fullTree = Instantiate(this.playerComp, this.obj.transform.position + pos.ToTransformPosition(),
                    Quaternion.identity, this.obj.transform);
                var fullTreeComp = fullTree.GetComponent<PlayerController>();
                fullTreeComp.Init(this, pos);
                fullTreeComp.unmovable = true;
                fullTreeComp.myAnimator.Play("GainLeaves");
            }
        }
    
        public IEnumerator ReplantFromPos(TilePos startPos)
        {
            var dims = this.GetDimensions();
            var radius = Math.Max(dims.X, dims.Y) * 2;  // *2 because we use manhatten distance
            var dirs = new[]
                { new TilePos(0, 1), new TilePos(0, -1), new TilePos(-1, 0), new TilePos(1, 0) };

            var replantDelay = 0.15f * 10f / this.Tiles.Count;

            HashSet<TilePos> replanted = new HashSet<TilePos>();
            replanted.Add(startPos);
            this.MaybeReplantTile(startPos);
            for (int rad = 1; rad < radius; rad++)
            {
                HashSet<TilePos> nexts = new HashSet<TilePos>();
                foreach (var old in replanted)
                {
                    foreach (var dir in dirs)
                    {
                        var next = old + dir;
                        if (replanted.Contains(next))
                            continue;
                        nexts.Add(next);
                        if (Get(next) == null)
                            continue;
                        this.MaybeReplantTile(next);
                        yield return new WaitForSeconds(replantDelay);
                    }
                }
                replanted.UnionWith(nexts);
            }
        }

        public TilePos GetBottomRight()
        {
            TilePos smallPos = new TilePos(Int32.MaxValue, Int32.MaxValue);
            TilePos largePos = new TilePos(Int32.MinValue, Int32.MinValue);
            foreach (var pos in this.Tiles.Keys)
            {
                smallPos = new TilePos(Math.Min(pos.X, smallPos.X), Math.Min(pos.Y, smallPos.Y));
                largePos = new TilePos(Math.Max(pos.X, largePos.X), Math.Max(pos.Y, largePos.Y));
            }
            return new TilePos(Math.Max(smallPos.X, largePos.X), Math.Min(smallPos.Y, largePos.Y));
        }
    }
    
    public Level LoadLevelFromTextAsset(TextAsset textAsset)
    {
        var lines = Regex.Split(textAsset.text, "\r\n|\r|\n");
        var tiles = new Dictionary<TilePos, Tile>();
        TilePos playerPos = new TilePos(0, 0);
        for (int y = 0; y < lines.Length; y++)
        {
            for (int x = 0; x < lines[y].Length; x++)
            {
                var code = lines[y][x];
                if (code != ' ')
                {
                    var pos = new TilePos(x, y);
                    Tile tile;
                    if (code == 'A')
                    {
                        tile = new Tile(Unmovable);
                        playerPos = pos;
                    }
                    else if (code == 'F')
                    {
                        tile = new Tile(FlagTile);
                        tile.HasFlag = true;
                    }
                    else if (code == 'X')
					{
                        tile = new Tile(Unmovable);
                        tile.HasRock = true;
					}
                    else if (code == 'O')
					{
                        tile = new Tile(GrassFull);
                        tile.HasRock = true;
					}
                    else
                    {
                        tile = new Tile(TileType.FromCode(code));
                    }
                    tiles.Add(pos, tile);
                }
            }
        }
        
        // danke anton ..., shift everything by player pos
        var shiftedTiles = new Dictionary<TilePos, Tile>();
        foreach (var entry in tiles)
        {
            shiftedTiles.Add(new TilePos(entry.Key.X - playerPos.X, entry.Key.Y - playerPos.Y), entry.Value);
        }

        return new Level(shiftedTiles, new TilePos(0, 0), this.currentLevelOffset, this);
    }

    public void RestartCurrentLevel()
    {
        if (this.currentLevel != null)
            this.currentLevel.Cleanup();
        this.currentLevelText.text = "Level " + currentLevelId;
        this.currentLevel = LoadLevelFromTextAsset(this.levelTextAssets[currentLevelId]);

        foreach (var obj in firstLevelExplanations)
            obj.SetActive(false);
        this.finalLevelExplanation.SetActive(false);
    }

    public void ProgressToNextLevel()
    {
        this.levelEnding = true;
        this.currentLevel.playerComp.myAnimator.Play("LooseLeaves");
        this.levelStartingOrEndingTime = 0f;

        var oldPos = this.currentLevel.playerComp.pos;
        this.fadingOutLevel = this.currentLevel;
        this.currentLevel = null;
        this.fadingOutTime = 0f;
        this.currentLevelId++;

        this.IncreaseLevelOffset();

        this.RestartCurrentLevel();

        // spawn leaf
        this.fromLeafPos = this.fadingOutLevel.obj.transform.position + oldPos.ToTransformPosition();
    }

    private void Start()
    {
        this.tileSprites[Unmovable] = unmovableTileSprite;
        this.tileSprites[GrassFull] = grassFullTileSprite;
        this.tileSprites[Grass4] = grass4TileSprite;
        this.tileSprites[Grass3] = grass3TileSprite;
        this.tileSprites[Grass2] = grass2TileSprite;
        this.tileSprites[Grass1] = grass1TileSprite;
        this.tileSprites[FlagTile] = flagTileSprite;
        this.tileSprites[Replanted] = replantedSprite;
        this.tileSprites[Replanted2] = replanted2Sprite;
        this.tileSprites[Replanted3] = replanted3Sprite;

        this.myCamera.backgroundColor = initialWaterColor;

        // this is just to restore the old state.
        var numLevels = this.currentLevelId;
        this.currentLevelId = 0;
        for (int pastLevel = 0; pastLevel < numLevels; pastLevel++)
        {
            this.RestartCurrentLevel();
            this.IncreaseLevelOffset();
            this.currentLevelId++;
            this.currentLevel.playerComp.unmovable = true;
            this.currentLevel.playerComp.myAnimator.Play("NoLeaves");
            this.pastLevels.Add(this.currentLevel);
            this.currentLevel = null;
        }
        this.RestartCurrentLevel();
        this.CenterCamera();
        if (currentLevelId < firstLevelExplanations.Length)
            firstLevelExplanations[currentLevelId].SetActive(true);
        
        this.currentLevel.playerComp.myAnimator.Play("GainLeaves");
    }

    private void CenterCamera()
    {
        var center = this.currentLevel.GetGlobalCenterPos();
        this.myCamera.transform.localPosition =
            new Vector3(center.x, center.y, this.myCamera.transform.localPosition.z);
    }

    private void IncreaseLevelOffset()
    {
        var verticalSize = Math.Max(6, myCamera.orthographicSize * 2.0f);
        var horizontalSize = verticalSize * Screen.width / Screen.height;
        var levelOffset = new Vector3(horizontalSize, -verticalSize, 0);
        this.currentLevelOffset += levelOffset;
    }

    private void Update()
    {
        if (replantsEverything)
        {
            if (replantingTime < pushTogetherDelay)
            {
                replantingTime += Time.deltaTime;
                TilePos.offsetScale = Mathf.Max(1f, Mathf.Lerp(1.4f, 1f, replantingTime / pushTogetherDelay));
                this.startedReplant = false;
                var myOcean = GameObject.Find("Ocean").GetComponent<Ocean>();
                myOcean.noiseIntensity = Mathf.Lerp(0.1f, 0f, replantingTime / pushTogetherDelay);
                myOcean.waveAmplitude = Mathf.Lerp(0.1f, 0f, replantingTime / pushTogetherDelay);
                this.changeWaterColorTime = 0;
                foreach (var level in this.pastLevels)
                    foreach (var tile in level.Tiles.Values)
                        tile.Comp.breakingAnimation.Play("EndWater");
                return;
            }
            if (this.changeWaterColorTime < this.changeWaterColorDelay)
            {
                this.changeWaterColorTime += Time.deltaTime;
                myCamera.backgroundColor = Color.Lerp(this.initialWaterColor, this.replantedWaterColor,
                    changeWaterColorTime / changeWaterColorDelay);
                return;
            }
            

            if (this.pastLevels.Count > 0)
            {
                foreach (var level in this.pastLevels)
                    level.playerComp.ResetPosition();

                var fadeBackTo = this.pastLevels[this.pastLevels.Count - 1];
                var newCenter = Vector3.Lerp(this.currentLevel.GetGlobalCenterPos(),
                    fadeBackTo.GetGlobalCenterPos(), this.fadingBackTime / this.levelFadeDelay);
                var dist = Vector3.Distance(currentLevel.GetGlobalCenterPos(), fadeBackTo.GetGlobalCenterPos());
                this.fadingBackTime += Time.deltaTime * (20f / dist);
                this.myCamera.transform.position = new Vector3(newCenter.x, newCenter.y, this.myCamera.transform.position.z);
                if (this.fadingBackTime >= this.levelFadeDelay * 0.5f && !this.startedReplant)
                {
                    if (this.pastLevels.Count > 1)
                        this.currentLevelText.text = "Level " + (this.pastLevels.Count - 1);
                    else 
                        this.currentLevelText.text = "";
                    StartCoroutine(fadeBackTo.ReplantFromPos(fadeBackTo.GetBottomRight()));
                    this.startedReplant = true;
                }
                if (this.fadingBackTime >= this.levelFadeDelay)
                {
                    this.currentLevel = fadeBackTo;
                    this.pastLevels.RemoveAt(this.pastLevels.Count - 1);
                    this.fadingBackTime = 0f;
                    this.startedReplant = false;
                }
            }
            else
            {
                this.finalLevelExplanation.SetActive(true);
            }
            return;
        }

        if (levelEnding)
        {
            levelStartingOrEndingTime += Time.deltaTime;
            if (levelStartingOrEndingTime > levelEndAndStartDelay)
            {
                levelEnding = false;
                this.currentLeaf = Instantiate(this.leafPrefab, fromLeafPos, Quaternion.identity);
                levelStartingOrEndingTime = 0f;
            }
            else
                return;
        }
        if (this.fadingOutLevel != null)
        {
            if (this.currentLeaf != null)
            {
                var newLeafPos = Vector3.Lerp(
                    this.fromLeafPos, this.currentLevel.obj.transform.position + this.currentLevel.playerComp.pos.ToTransformPosition(),
                    leafSpeedCurve.Evaluate(this.fadingOutTime / this.levelFadeDelay));
                this.currentLeaf.transform.position =
                    new Vector3(newLeafPos.x, newLeafPos.y + 6f * leafHeightCurve.Evaluate(this.fadingOutTime / this.levelFadeDelay), this.currentLeaf.transform.position.z);
            }
            this.fadingOutTime += Time.deltaTime;
            var newCamCenter = Vector3.Lerp(this.fadingOutLevel.GetGlobalCenterPos(),
                this.currentLevel.GetGlobalCenterPos(), this.fadingOutTime / this.levelFadeDelay);
            this.myCamera.transform.position = new Vector3(newCamCenter.x, newCamCenter.y, this.myCamera.transform.position.z);
            if (this.fadingOutTime >= this.levelFadeDelay)
            {
                // done.
                this.fadingOutLevel.playerComp.unmovable = true;
                this.pastLevels.Add(this.fadingOutLevel);
                this.fadingOutLevel = null;
                this.fadingOutTime = 0;
                if (currentLevelId < firstLevelExplanations.Length)
                    firstLevelExplanations[currentLevelId].SetActive(true);

                this.levelStartingOrEndingTime = 0f;
                this.levelStarting = true;
                this.currentLevel.playerComp.myAnimator.Play("GainLeaves");
                this.currentLeaf.SetActive(false);
            }
            return;
        }
        if (levelStarting)
        {
            levelStartingOrEndingTime += Time.deltaTime;
            if (levelStartingOrEndingTime > levelEndAndStartDelay)
            {
                levelStarting = false;
                levelStartingOrEndingTime = 0f;
                Destroy(this.currentLeaf);
                this.currentLeaf = null;

                if (currentLevelId == levelTextAssets.Length - 1)
                {
                    // last level, whoo
                    // in the last level, all tiles turn healthy again
                    replantsEverything = true;
                    replantingTime = 0;
                    StartCoroutine(this.currentLevel.ReplantFromPos(this.currentLevel.playerComp.pos));
                }
            }
            else
                return;
        }
        if (Input.GetKeyDown("r"))
        {
            this.RestartCurrentLevel();
            this.currentLevel.playerComp.myAnimator.Play("GainLeaves");
            this.CenterCamera();
            if (currentLevelId < firstLevelExplanations.Length)
                firstLevelExplanations[currentLevelId].SetActive(true);
        }
        if (this.currentLevel != null)
            this.currentLevel.Update();
    }
}
