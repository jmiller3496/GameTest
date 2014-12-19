﻿using Destruct.Entities.Items;
using Destruct.GameStates;
using Destruct.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Destruct.Entities.TileMaps
{
    public class TileMap
    {
        public int xOffset;
        public int yOffset;
        public List<TileLayer> layers;
        public List<TileCell> cells;
        public List<TileCell> cellsToAdd;
        List<Entities.Items.Item> items = new List<Item>();
        bool first;
        int cellId;
        public MainState state;

        public TileMap(int[][][] tiles, MainState state)
        {
            Init();
            Create(tiles);
            this.state = state;
        }

        public void Create(int[][][] tiles)
        {
            cellId++;
            for (int i = 0; i < tiles.Length; i++)
            {
                layers.Add(new TileLayer(tiles[i], Globals.defaultTileSize, 0, 0, this, cellId, 0, new List<Items.Item>()));
            }
        }

        public void Init()
        {
            cells = new List<TileCell>();
            layers = new List<TileLayer>();
            SaveWriter.SavedTiles = new List<string>();
            Thread tileGen = new Thread(GenTiles);
            tileGen.Start();
        }
        
        public void Update(int x, int y, List<Entities.Items.Item> items)
        {
            this.xOffset = x;
            this.yOffset = y;
            for (int i = 0; i < layers.Count; i++)
                layers[i].Update(items);
            this.items = items;
        }
        public void Draw(Graphics g)
        {
            for (int i = 0; i < layers.Count; i++)
                if(!layers[i].isRoof)
                    layers[i].Draw(g);
        }
        public void DrawRoof(Graphics g)
        {
            for (int i = 0; i < layers.Count; i++)
                if (layers[i].isRoof)
                    layers[i].Draw(g);
        }
        public bool CheckForHit(Rectangle rect, int hits)
        {
            foreach (TileLayer l in layers)
            {
                if (l.CheckForHit(rect, hits))
                    return true;
            }
            return false;
        }
        public bool IsColAtRect(Rectangle rect, int xAdd, int yAdd)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                var l = layers[i];
                if (l.IsColAtRect(rect, xAdd, yAdd))
                    return true;
            }
            return false;
        }

        public void GenTiles()
        {
            while (true)
            {
                cellId = 0;
                int layId = layers.Count;
                List<TileLayer> newLayers = new List<TileLayer>();
                TileLayer l = layers.OrderBy(i => i.xMapOffset).First(i => (i.yMapOffset * i.size * Globals.scale) + yOffset <= Globals.renderArea && (i.yMapOffset * i.size * Globals.scale) + yOffset >= -Globals.screenSize);
                TileLayer r = layers.OrderBy(i => i.xMapOffset).Last();
                TileLayer d;
                TileLayer u;
                int[][][] iTiles = TileCreator.GetRandomTiles();
                var lll = layers.Select(i => new { i.xMapOffset, i.yMapOffset, i.cellId });
                cellsToAdd = new List<TileCell>();
                int index = 0;
                while ((r.xMapOffset * r.size * Globals.scale) + xOffset <= Globals.renderArea)
                {
                    int xOff = r.xMapOffset + iTiles[0].Count();
                    cellsToAdd.Add(new TileCell());
                    cellId++;
                    foreach (int[][] singleLayer in iTiles)
                    {
                        r = new TileLayer(singleLayer, Globals.defaultTileSize, xOff, l.yMapOffset, this, index, layId, items);
                        cellsToAdd[index].layers.Add(r);
                        if (r.tiles.Any(i => i.Any(j => j.roof)))
                            r.isRoof = true;
                        layId++;
                    }
                    d = r;
                    u = r;
                    first = false;
                    index++;
                }
                while ((l.xMapOffset * l.size * Globals.scale) + xOffset >= -Globals.screenSize)
                {
                    int xOff = l.xMapOffset - iTiles[0].Count();
                    cellsToAdd.Add(new TileCell());
                    cellId++;
                    foreach (int[][] singleLayer in iTiles)
                    {
                        l = new TileLayer(singleLayer, Globals.defaultTileSize, xOff, l.yMapOffset, this, index, layId, items);
                        if (l.tiles.Any(i => i.Any(j => j.roof)))
                            l.isRoof = true;
                        cellsToAdd[index].layers.Add(l);
                        layId++;
                    }
                    d = l;
                    u = l;
                    index++;
                }
                List<TileLayer> newVLayers = new List<TileLayer>();
                for (int z = 0; z < layers.Count; z++)
                {
                    var layer = layers[z];
                    Rectangle rect = new Rectangle((layer.xMapOffset * layer.size * Globals.scale) + xOffset, (layer.yMapOffset * layer.size * Globals.scale) + yOffset, (layer.tiles[0].Count() * layer.size * Globals.scale), (layer.tiles[0].Count() * layer.size * Globals.scale));
                    if (rect.IntersectsWith(new Rectangle(-Globals.screenSize, -Globals.screenSize, Globals.renderArea, Globals.renderArea)) || new Rectangle(-Globals.screenSize, -Globals.screenSize, Globals.renderArea, Globals.renderArea).Contains(rect))
                    {
                        d = layers.OrderBy(i => i.yMapOffset).Last(j => j.xMapOffset == layer.xMapOffset);
                        u = layers.OrderBy(i => i.yMapOffset).First(j => j.xMapOffset == layer.xMapOffset);
                        int yOffD = d.yMapOffset;
                        while ((yOffD * d.size * 2) + yOffset <= Globals.renderArea)
                        {
                            yOffD = d.yMapOffset;
                            iTiles = TileCreator.GetRandomTiles();
                            yOffD += iTiles[0].Count();
                            cellsToAdd.Add(new TileCell());
                            cellId++;
                            foreach (int[][] singleLayer in iTiles)
                            {
                                d = new TileLayer(singleLayer, Globals.defaultTileSize, d.xMapOffset, yOffD, this, layId, index, items);
                                cellsToAdd[index].layers.Add(d);
                                if (d.tiles.Any(i => i.Any(j => j.roof)))
                                    d.isRoof = true;
                                layId++;
                            }
                            first = false;
                            index++;
                        }
                        int yOffU = u.yMapOffset;
                        while ((yOffU * u.size * 2) + yOffset >= -Globals.screenSize)
                        {
                            yOffU = u.yMapOffset;
                            iTiles = TileCreator.GetRandomTiles();
                            yOffU -= iTiles[0].Count();
                            cellsToAdd.Add(new TileCell());
                            cellId++;
                            foreach (int[][] singleLayer in iTiles)
                            {
                                u = new TileLayer(singleLayer, Globals.defaultTileSize, u.xMapOffset, yOffU, this, layId, index, items);
                                cellsToAdd[index].layers.Add(u);
                                if (u.tiles.Any(i => i.Any(j => j.roof)))
                                    u.isRoof = true;
                                layId++;
                            }
                            first = false;
                            index++;
                        }
                    }
                }
                newLayers.AddRange(newVLayers);
                List<TileLayer> layersNotIn = new List<TileLayer>();
                cellId++;
                cellId = cells.Count;
                foreach (TileCell cell in cellsToAdd)
                {
                    cells.Add(cell);
                    cell.health = Globals.defaultTileSize * Globals.defaultTileSize;
                    int health = 100;
                    foreach (TileLayer cellLayer in cell.layers)
                        foreach (Tile[] tileArray in cellLayer.tiles)
                            health += (int)(tileArray.Where(i => i.solid).Count() * Globals.defaultTileSize * 4);
                    cell.health = health;
                    if (!isAlreadyLayer(cell.layers[0]) && !DoesExistInSavedLayers(cell.layers[0]))
                    {
                        string[] saved = cell.layers.Select(i => i.XmlSerialize<TileLayer>()).ToArray();
                        SaveWriter.SavedTiles.AddRange(saved);
                        foreach (TileLayer layer in cell.layers)
                        {
                            layer.layId = layers.Count;
                            layer.cellId = cellId;
                            layers.Add(layer);
                        }
                    }
                    else
                    {
                        foreach (TileLayer cellLayer in cell.layers)
                            foreach (Item i in cellLayer.items)
                                items.Remove(i);
                    }
                    cellId++;
                }
                for (int i = 0; i < layers.Count; i++)
                {
                    TileLayer layer = layers[i];
                    Rectangle rect = new Rectangle((layer.xMapOffset * layer.size * Globals.scale) + xOffset, (layer.yMapOffset * layer.size * Globals.scale) + yOffset, (layer.tiles[0].Count() * layer.size * Globals.scale), (layer.tiles[0].Count() * layer.size * Globals.scale));
                    //if (!rect.IntersectsWith(new Rectangle(-Globals.screenSize, -Globals.screenSize, Globals.renderArea, Globals.renderArea)))
                    bool inter = rect.IntersectsWith(new Rectangle(-Globals.screenSize, -Globals.screenSize, Globals.renderArea, Globals.renderArea);
                    bool cont = rect.Contains(new Rectangle(-Globals.screenSize, -Globals.screenSize, Globals.renderArea, Globals.renderArea));
                    if (!rect.IntersectsWith(new Rectangle(-Globals.screenSize, -Globals.screenSize, Globals.renderArea, Globals.renderArea)) && !rect.Contains(new Rectangle(-Globals.screenSize, -Globals.screenSize, Globals.renderArea, Globals.renderArea)))
                    {
                        layers.Remove(layer);
                    }
                }
            }
        }

        public bool DoesExistInSavedLayers(TileLayer tl)
        {
            List<TileLayer> saveLayers = new List<TileLayer>();
            bool isAdded = false;
            for (int i = 0; i < SaveWriter.SavedTiles.Count; i++ )
            {
                TileLayer sl = SaveWriter.SavedTiles[i].XmlDeserialize<TileLayer>();
                if(sl.xMapOffset == tl.xMapOffset && sl.yMapOffset == tl.yMapOffset)
                {
                    foreach (Tile[] tiles in sl.tiles)
                        foreach (Tile t in tiles)
                            t.layer = sl;
                    sl.map = this;
                    int health = 100;
                    var cell = cells.First(z => z.layers[0].xMapOffset == sl.xMapOffset && z.layers[0].yMapOffset == sl.yMapOffset);
                    foreach (TileLayer cellLayer in cell.layers)
                        foreach (Tile[] tileArray in cellLayer.tiles)
                            health += (int)(tileArray.Where(c => c.solid).Count() * Globals.defaultTileSize * 4);
                    cell.health = health;
                    if(!(cell.layers.Any(w => w.isRoof) && cell.layers.Count < 2))
                        layers.AddRange(cell.layers);
                    //layers.Add(sl);
                    isAdded = true;
                }
            }
            return isAdded;
        }

        public bool isAlreadyLayer(TileLayer tl)
        {
            if (tl.isRoof)
                return false;
            for(int i = 0; i < layers.Count; i++) 
            {
                TileLayer lay = layers[i];
                if (!(tl.xMapOffset == lay.xMapOffset && tl.yMapOffset == lay.yMapOffset && tl.cellId == i))
                {
                        if ((tl.xMapOffset == lay.xMapOffset && tl.yMapOffset == lay.yMapOffset && tl.cellId != i))
                        {
                            if(!tl.isRoof)
                                return true;
                        }
                }
            }
                return false;
        }
    }
}
