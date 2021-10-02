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
    public TextAsset[] levelTextAssets;
    public GameObject tilePrefab;
    
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
    
    public static TileType Grass = new TileType('o');

    public class Tile
    {
        public readonly TileType Type;

        public Tile(TileType type)
        {
            this.Type = type;
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
            return new Vector3(this.X, this.Y, 0);
        }
    }

    public class Level
    {
        public readonly Dictionary<TilePos, Tile> Tiles;
        private GameObject TilePrefab;

        public Level(Dictionary<TilePos, Tile> tiles, GameObject tilePrefab)
        {
            this.Tiles = tiles;
            this.TilePrefab = tilePrefab;

            foreach (var entry in tiles)
            {
                Instantiate(tilePrefab, entry.Key.ToTransformPosition(), Quaternion.identity);
            }
        }
    }
    
        
    public Level LoadLevelFromTextAsset(TextAsset textAsset)
    {
        var lines = Regex.Split(textAsset.text, "\r\n|\r|\n");
        var tiles = new Dictionary<TilePos, Tile>();
        for (int y = 0; y < lines.Length; y++)
        {
            for (int x = 0; x < lines[y].Length; x++)
            {
                var code = lines[y][x];
                if (code != ' ')
                {
                    tiles.Add(new TilePos(x, y), new Tile(TileType.FromCode(code)));
                }
            }
        }

        return new Level(tiles, tilePrefab: this.tilePrefab);
    }

    private void Start()
    {
        Debug.Log("test");
        LoadLevelFromTextAsset(this.levelTextAssets[0]);
    }
}
