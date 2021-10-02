using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor.UI;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public static TileType GrassFull = new TileType('o');
    public static TileType Grass4 = new TileType('4');
    public static TileType Grass3 = new TileType('3');
    public static TileType Grass2 = new TileType('2');
    public static TileType Grass1 = new TileType('1');
    
    public TextAsset[] levelTextAssets;
    public GameObject grassFullTilePrefab;
    public GameObject grass4TilePrefab;
    public GameObject grass3TilePrefab;
    public GameObject grass2TilePrefab;
    public GameObject grass1TilePrefab;
    private Dictionary<TileType, GameObject> tilePrefabs = new Dictionary<TileType, GameObject>();

    public GameObject playerPrefab;
    
    public class TileType
    {
        private static readonly Dictionary<char, TileType> byCode = new Dictionary<char, TileType>();
        public readonly char Code;

        public TileType(char code)
        {
            this.Code = code;
            byCode.Add(code, this);
        }

        public static TileType FromCode(char code)
        {
            return byCode[code];
        }
    }
    
    public class Tile
    {
        public readonly TileType Type;
        public GameObject Object;

        public Tile(TileType type)
        {
            this.Type = type;
            this.Object = null;
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

        public Vector3 ToTransformPosition()
        {
            return new Vector3(0.5f * this.X + 0.5f * this.Y, -0.25f * this.X + 0.25f * this.Y, this.Y - this.X);
        }
    }

    public class Level
    {
        public readonly Dictionary<TilePos, Tile> Tiles;
        private TileManager Manager;
        private GameObject player;

        public Level(Dictionary<TilePos, Tile> tiles, TilePos playerPos, TileManager manager)
        {
            this.Tiles = tiles;
            this.Manager = manager;
            
            // make tiles
            foreach (var entry in tiles)
            {
                var obj = Instantiate(manager.tilePrefabs[entry.Value.Type], entry.Key.ToTransformPosition(), Quaternion.identity);
                entry.Value.Object = obj;
            }
            // make player
            this.player = Instantiate(manager.playerPrefab, playerPos.ToTransformPosition(), Quaternion.identity);
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

        LoadLevelFromTextAsset(this.levelTextAssets[0]);
    }
}
