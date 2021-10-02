using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

public class TileManager : MonoBehaviour
{
    public static TileType Unmovable = new TileType('x', false);
    public static TileType GrassFull = new TileType('o', true);
    public static TileType Grass4 = new TileType('4', true);
    public static TileType Grass3 = new TileType('3', true);
    public static TileType Grass2 = new TileType('2', true);
    public static TileType Grass1 = new TileType('1', true);
    
    public TextAsset[] levelTextAssets;
    public GameObject unmovableTilePrefab;
    public GameObject grassFullTilePrefab;
    public GameObject grass4TilePrefab;
    public GameObject grass3TilePrefab;
    public GameObject grass2TilePrefab;
    public GameObject grass1TilePrefab;
    private Dictionary<TileType, GameObject> tilePrefabs = new Dictionary<TileType, GameObject>();

    public GameObject playerPrefab;
    public GameObject flagPrefab;
    public GameObject rockPrefab;
    
    public Text currentLevelText;

    public int currentLevelId = 0;
    public Level currentLevel = null;
    public Camera myCamera;
    
    public class TileType
    {
        private static readonly Dictionary<char, TileType> byCode = new Dictionary<char, TileType>();
        public readonly char Code;
        public readonly bool Movable;

        public TileType(char code, bool movable)
        {
            this.Code = code;
            byCode.Add(code, this);
            this.Movable = movable;
        }

        public static TileType FromCode(char code)
        {
            return byCode[code];
        }
    }
    
    public class Tile
    {
        public readonly TileType Type;
        public TileComponent Comp;
        public bool HasFlag = false;
        public bool Walkable = true;

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
            return new Vector3(0.5f * x + 0.5f * y, -0.25f * x + 0.25f * y, y - x);   
        }

        public Vector3 ToTransformPosition()
        {
            return CoordsToTransformPosition(this.X, this.Y);
        }
    }


    public class Level
    {
        public readonly Dictionary<TilePos, Tile> Tiles;
        public TileManager Manager;
        private PlayerController playerComp;

        public bool StepActive = false;
        public float CurrentStepDelta = 0.0f;
        public float StepLength = 2.0f;  // in seconds

        public Level(Dictionary<TilePos, Tile> tiles, TilePos playerPos, TileManager manager)
        {
            this.Tiles = tiles;
            this.Manager = manager;
            
            // make tiles
            foreach (var entry in tiles)
            {
                var pos = entry.Key;
                var obj = Instantiate(manager.tilePrefabs[entry.Value.Type], pos.ToTransformPosition(), Quaternion.identity);
                var comp = obj.GetComponent<TileComponent>();
                comp.Init(this, pos);
                entry.Value.Comp = comp;
                if (entry.Value.HasFlag)
                {
                    var flagObj = Instantiate(manager.flagPrefab, Vector3.zero, Quaternion.identity, obj.transform);
                    flagObj.transform.localPosition = new Vector3(0f, 0.5f, -2f);
                }
            }
            // make player
            var player = Instantiate(manager.playerPrefab, playerPos.ToTransformPosition(), Quaternion.identity);
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
            {
                entry.Value.Comp.UpdateStep();
            }
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
                this.Set(pos, tileBefore);
                tileBefore = tile;
                pos = pos + direction;
            }
            this.Set(pos, tileBefore);
            this.StepActive = true;
        }

        public void Cleanup()
        {
            foreach (var tile in this.Tiles.Values)
            {
                Destroy(tile.Comp.gameObject);
            }
            Destroy(playerComp.gameObject);
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
                        tile = new Tile(GrassFull);
                        playerPos = pos;
                    }
                    else if (code == 'F')
                    {
                        tile = new Tile(GrassFull);
                        tile.HasFlag = true;
                    }
                    else
                    {
                        tile = new Tile(TileType.FromCode(code));
                    }
                    tiles.Add(pos, tile);
                }
            }
        }

        return new Level(tiles, playerPos, this);
    }

    public void RestartCurrentLevel()
    {
        if (this.currentLevel != null)
            this.currentLevel.Cleanup();
        this.currentLevelText.text = "Level " + (currentLevelId + 1);
        this.currentLevel = LoadLevelFromTextAsset(this.levelTextAssets[currentLevelId]);

        TilePos smallPos = new TilePos(Int32.MaxValue, Int32.MaxValue);
        TilePos largePos = new TilePos(Int32.MinValue, Int32.MinValue);
        foreach (var pos in this.currentLevel.Tiles.Keys)
        {
            smallPos = new TilePos(Math.Min(pos.X, smallPos.X), Math.Min(pos.Y, smallPos.Y));
            largePos = new TilePos(Math.Max(pos.X, largePos.X), Math.Max(pos.Y, largePos.Y));
        }

        var center = TilePos.CoordsToTransformPosition(0.5f * (largePos.X - smallPos.X + 1),
            0.5f * (largePos.Y - smallPos.Y + 1));
        this.myCamera.transform.localPosition =
            new Vector3(center.x, center.y, this.myCamera.transform.localPosition.z);
    }

    private void Start()
    {
        this.tilePrefabs[Unmovable] = unmovableTilePrefab;
        this.tilePrefabs[GrassFull] = grassFullTilePrefab;
        this.tilePrefabs[Grass4] = grass4TilePrefab;
        this.tilePrefabs[Grass3] = grass3TilePrefab;
        this.tilePrefabs[Grass2] = grass2TilePrefab;
        this.tilePrefabs[Grass1] = grass1TilePrefab;
        
        RestartCurrentLevel();

        if (this.currentLevel.CanShiftTiles(new TilePos(0, 0), new TilePos(1, 0)))
        {
            this.currentLevel.ShiftTiles(new TilePos(0, 0), new TilePos(1, 0));
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown("r"))
        {
            this.RestartCurrentLevel();
        }
        if (this.currentLevel != null)
            this.currentLevel.Update();
    }
}
