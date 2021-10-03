using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class TileManager : MonoBehaviour
{
    public static TileType Unmovable = new TileType('x', false, false, null);
    public static TileType Grass1 = new TileType('1', true, true, null);
    public static TileType Grass2 = new TileType('2', true, true, Grass1);
    public static TileType Grass3 = new TileType('3', true, true, Grass2);
    public static TileType Grass4 = new TileType('4', true, true, Grass3);
    public static TileType GrassFull = new TileType('o', true, true, Grass4);

    public TextAsset[] levelTextAssets;
    public GameObject tilePrefab;
    public Sprite unmovableTileSprite;
    public Sprite grassFullTileSprite;
    public Sprite grass4TileSprite;
    public Sprite grass3TileSprite;
    public Sprite grass2TileSprite;
    public Sprite grass1TileSprite;
    public Dictionary<TileType, Sprite> tileSprites = new Dictionary<TileType, Sprite>();

    public GameObject[] firstLevelExplanations;

    public GameObject levelPrefab;
    public GameObject playerPrefab;
    public GameObject flagPrefab;
    public GameObject rockPrefab;

    public Text currentLevelText;

    public int currentLevelId = 0;
    public Level currentLevel = null;
    private Vector3 currentLevelOffset = Vector3.zero;
    public Camera myCamera;

    private float levelFadeDelay = 5f;
    private float fadingOutTime = 0f;
    public Level fadingOutLevel = null;
    
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
            float offsetScale = 1.4f;
            return new Vector3(0.5f * offsetScale * x + 0.5f * offsetScale * y, -0.25f * offsetScale * x + 0.25f * offsetScale * y, y - x);   
        }

        public static Vector2 TransformToTileCoords(float x, float y)
		{
            float offsetScale = 1.4f;
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
        private PlayerController playerComp;

        public GameObject obj;
        public bool StepActive = false;
        public float CurrentStepDelta = 0.0f;
        public float StepLength = 2.0f;  // in seconds

        public Level(Dictionary<TilePos, Tile> tiles, TilePos playerPos, Vector3 levelPos, TileManager manager)
        {
            this.Tiles = tiles;
            this.Manager = manager;

            this.obj = Instantiate(manager.levelPrefab, levelPos, Quaternion.identity);
            // make tiles
            foreach (var entry in tiles)
            {
                var pos = entry.Key;
                var tileObj = Instantiate(manager.tilePrefab, pos.ToTransformPosition(), Quaternion.identity, this.obj.transform);
                var comp = tileObj.GetComponent<TileComponent>();
                comp.Init(this, manager.tileSprites[entry.Value.Type], pos);
                entry.Value.Comp = comp;
                if (entry.Value.HasFlag)
                {
                    var flagObj = Instantiate(manager.flagPrefab, Vector3.zero, Quaternion.identity, tileObj.transform);
                    flagObj.transform.localPosition = new Vector3(0f, 0f, -2f);
                }
                if (entry.Value.HasRock)
				{
                    var rockObj = Instantiate(manager.rockPrefab, Vector3.zero, Quaternion.identity, tileObj.transform);
                    rockObj.transform.localPosition = new Vector3(0f, 0f, -2f);
                }
            }
            // make player
            var player = Instantiate(manager.playerPrefab, this.obj.transform.position + playerPos.ToTransformPosition(), Quaternion.identity, this.obj.transform);
            this.playerComp = player.GetComponent<PlayerController>();
            this.playerComp.myLevel = this;
            this.playerComp.pos = playerPos;
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
                        tile = new Tile(GrassFull);
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

        return new Level(tiles, playerPos, this.currentLevelOffset, this);
    }

    public void RestartCurrentLevel()
    {
        if (this.currentLevel != null)
            this.currentLevel.Cleanup();
        this.currentLevelText.text = "Level " + (currentLevelId + 1);
        this.currentLevel = LoadLevelFromTextAsset(this.levelTextAssets[currentLevelId]);

        foreach (var obj in firstLevelExplanations)
            obj.SetActive(false);

        var center = this.currentLevel.GetGlobalCenterPos();
        this.myCamera.transform.localPosition =
            new Vector3(center.x, center.y, this.myCamera.transform.localPosition.z);
    }

    public void ProgressToNextLevel()
    {
        this.fadingOutLevel = this.currentLevel;
        this.currentLevel = null;
        this.fadingOutTime = 0f;
        this.currentLevelId++;
        
        var verticalSize = myCamera.orthographicSize * 2.0f;
        var horizontalSize = verticalSize * Screen.width / Screen.height;
        var levelOffset = new Vector3(horizontalSize, -verticalSize, 0);
        this.currentLevelOffset += levelOffset;

        this.RestartCurrentLevel();
    }

    private void Start()
    {
        this.tileSprites[Unmovable] = unmovableTileSprite;
        this.tileSprites[GrassFull] = grassFullTileSprite;
        this.tileSprites[Grass4] = grass4TileSprite;
        this.tileSprites[Grass3] = grass3TileSprite;
        this.tileSprites[Grass2] = grass2TileSprite;
        this.tileSprites[Grass1] = grass1TileSprite;
        
        RestartCurrentLevel();
        if (currentLevelId < firstLevelExplanations.Length)
            firstLevelExplanations[currentLevelId].SetActive(true);
    }

    private void Update()
    {
        if (this.fadingOutLevel != null)
        {
            this.fadingOutTime += Time.deltaTime;
            var newCenter = Vector3.Lerp(this.fadingOutLevel.GetGlobalCenterPos(),
                this.currentLevel.GetGlobalCenterPos(), this.fadingOutTime / this.levelFadeDelay);
            this.myCamera.transform.position = new Vector3(newCenter.x, newCenter.y, this.myCamera.transform.position.z);
            if (this.fadingOutTime >= this.levelFadeDelay)
            {
                // done.
                this.fadingOutLevel.Cleanup();
                this.fadingOutLevel = null;
                this.fadingOutTime = 0;
                if (currentLevelId < firstLevelExplanations.Length)
                    firstLevelExplanations[currentLevelId].SetActive(true);
            }
            return;
        }
        if (Input.GetKeyDown("r"))
        {
            this.RestartCurrentLevel();
            if (currentLevelId < firstLevelExplanations.Length)
                firstLevelExplanations[currentLevelId].SetActive(true);
        }
        if (this.currentLevel != null)
            this.currentLevel.Update();
    }
}
