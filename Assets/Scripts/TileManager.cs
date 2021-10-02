using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public static TileType GrassFull = new TileType('o', true);
    public static TileType Grass4 = new TileType('4', true);
    public static TileType Grass3 = new TileType('3', true);
    public static TileType Grass2 = new TileType('2', true);
    public static TileType Grass1 = new TileType('1', true);
    
    public TextAsset[] levelTextAssets;
    public GameObject grassFullTilePrefab;
    public GameObject grass4TilePrefab;
    public GameObject grass3TilePrefab;
    public GameObject grass2TilePrefab;
    public GameObject grass1TilePrefab;
    private Dictionary<TileType, GameObject> tilePrefabs = new Dictionary<TileType, GameObject>();

    public GameObject playerPrefab;

    public Level currentLevel = null;
    
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

        public Vector3 ToTransformPosition()
        {
            return new Vector3(0.5f * this.X + 0.5f * this.Y, -0.25f * this.X + 0.25f * this.Y, this.Y - this.X);
        }
    }


    public class Level
    {
        public readonly Dictionary<TilePos, Tile> Tiles;
        private TileManager Manager;
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
                comp.Level = this;
                comp.TilePos = pos;
                comp.PrevPos = pos;
                entry.Value.Comp = comp;
            }
            // make player
            var player = Instantiate(manager.playerPrefab, playerPos.ToTransformPosition(), Quaternion.identity);
            this.playerComp = player.GetComponent<PlayerController>();
            this.playerComp.myLevel = this;
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
                    TileType tileType;
                    if (code == 'A')
                    {
                        tileType = GrassFull;
                        playerPos = pos;
                    }
                    else
                    {
                        tileType = TileType.FromCode(code);
                    }
                    tiles.Add(pos, new Tile(tileType));
                }
            }
        }

        return new Level(tiles, playerPos, this);
    }

    private void Start()
    {
        this.tilePrefabs[GrassFull] = grassFullTilePrefab;
        this.tilePrefabs[Grass4] = grass4TilePrefab;
        this.tilePrefabs[Grass3] = grass3TilePrefab;
        this.tilePrefabs[Grass2] = grass2TilePrefab;
        this.tilePrefabs[Grass1] = grass1TilePrefab;

        this.currentLevel = LoadLevelFromTextAsset(this.levelTextAssets[0]);

        if (this.currentLevel.CanShiftTiles(new TilePos(0, 0), new TilePos(1, 0)))
        {
            this.currentLevel.ShiftTiles(new TilePos(0, 0), new TilePos(1, 0));
        }
    }

    private void Update()
    {
        if (this.currentLevel != null)
            this.currentLevel.Update();
    }
}
