﻿#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;

using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Map
{
    public sealed class MapChunk : IDisposable
    {
        public static readonly MapChunk Invalid = new MapChunk(0xFFFF, 0xFFFF);

        public MapChunk(ushort x, ushort y)
        {
            X = x;
            Y = y;
            Tiles = new Tile[8][];

            for (int i = 0; i < 8; i++)
            {
                Tiles[i] = new Tile[8];
                for (int j = 0; j < 8; j++) Tiles[i][j] = new Tile((ushort) (i + x * 8), (ushort) (j + y * 8));
            }

            LastAccessTime = CoreGame.Ticks + 5000;
        }

        //private ushort? _x, _y;

        public ushort X { get; }
        public ushort Y { get; }

        //public ushort X
        //{
        //    get => _x ?? 0xFFFF;
        //    set => _x = value;
        //}

        //public ushort Y
        //{
        //    get => _y ?? 0xFFFF;
        //    set => _y = value;
        //}

        public Tile[][] Tiles { get; private set; }

        public long LastAccessTime { get; set; }

        //public static bool operator ==(MapChunk p1, MapChunk p2)
        //{
        //    return p1.X == p2.X && p1.Y == p2.Y;
        //}

        //public static bool operator !=(MapChunk p1, MapChunk p2)
        //{
        //    return p1.X != p2.X || p1.Y != p2.Y;
        //}

        //public override int GetHashCode()
        //{
        //    return X ^ Y;
        //}

        //public override bool Equals(object obj)
        //{
        //    return obj is MapChunk mapChunk && this == mapChunk;
        //}

        public unsafe void Load(int map)
        {
            IndexMap im = GetIndex(map);

            if (im.MapAddress != 0)
            {
                MapBlock* block = (MapBlock*) im.MapAddress;
                MapCells* cells = (MapCells*) &block->Cells;
                int bx = X * 8;
                int by = Y * 8;

                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        int pos = y * 8 + x;
                        ushort tileID = (ushort) (cells[pos].TileID & 0x3FFF);
                        sbyte z = cells[pos].Z;
                        LandTiles info = TileData.LandData[tileID];

                        Land land = new Land(tileID)
                        {
                            Graphic = tileID,
                            AverageZ = z,
                            MinZ = z,
                            IsStretched = info.TexID == 0 && TileData.IsWet(info.Flags),
                        };
                        ushort tileX = (ushort) (bx + x);
                        ushort tileY = (ushort) (by + y);
                        land.Calculate(tileX, tileY, z);
                        land.Position = new Position(tileX, tileY, z);
                    }
                }

                if (im.StaticAddress != 0)
                {
                    StaticsBlock* sb = (StaticsBlock*) im.StaticAddress;

                    if (sb != null)
                    {
                        int count = (int) im.StaticCount;

                        for (int i = 0; i < count; i++, sb++)
                        {
                            if (sb->Color > 0 && sb->Color != 0xFFFF)
                            {
                                ushort x = sb->X;
                                ushort y = sb->Y;
                                int pos = y * 8 + x;

                                if (pos >= 64)
                                    continue;
                                sbyte z = sb->Z;

                                Static staticObject = new Static(sb->Color, sb->Hue, pos)
                                {
                                    Position = new Position((ushort) (bx + x), (ushort) (by + y), z)
                                };

                                if (TileData.IsAnimated(staticObject.ItemData.Flags))
                                    staticObject.Effect = new AnimatedItemEffect(staticObject, staticObject.Graphic, staticObject.Hue, -1);
                            }
                        }
                    }
                }
            }
        }

        private IndexMap GetIndex(int map) => GetIndex(map, X, Y);

        private static IndexMap GetIndex(int map, int x, int y) => IO.Resources.Map.GetIndex(map, x, y);

        public void Dispose()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    var tile = Tiles[i][j];

                    for (GameObject obj = tile.FirstNode; obj != null; obj = obj.Right)
                    //for (int k = 0; k < Tiles[i][j].ObjectsOnTiles.Count; k++)
                    {
                        //var obj = Tiles[i][j].ObjectsOnTiles[k];
                        if (obj != World.Player)
                        {

                           tile.RemoveGameObject(obj);


                            //int count = Tiles[i][j].ObjectsOnTiles.Count;
                            //Tiles[i][j].ObjectsOnTiles[k].Dispose();

                            //if (count == Tiles[i][j].ObjectsOnTiles.Count)
                            //{
                            //    Tiles[i][j].ObjectsOnTiles.RemoveAt(k);
                            //}

                            //k--;
                        }
                    }

                    //Tiles[i][j].Dispose();
                    Tiles[i][j] = null;// Tile.Invalid;
                }
            }

            Tiles = null;
        }

        public bool HasNoExternalData()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Tile tile = Tiles[i][j];

                    for (GameObject obj = tile.FirstNode; obj != null; obj = obj.Right)
                    //List<GameObject> list = tile.ObjectsOnTiles;

                    //for (int k = 0; k < list.Count; k++)
                    {
                        //GameObject obj = list[k];
                        if (obj is Static st && st.Effect != null) st.Effect = null;

                        if (!(obj is Land) && !(obj is Static))
                            return false;
                    }
                }
            }

            return true;
        }
    }
}