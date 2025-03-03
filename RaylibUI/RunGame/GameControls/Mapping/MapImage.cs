using Civ2engine;
using Civ2engine.Enums;
using Civ2engine.MapObjects;
using Model.Images;
using Model.ImageSets;
using Raylib_cs;

namespace RaylibUI.RunGame.GameControls.Mapping;

public static class MapImage
{
    private static Color _replacementColour = new (255, 0, 0, 255);
    
    private static readonly (int, int)[][] CoastMap = {
        new[]{ (0,4), (3,1) }, 
        new[]{ (3,2) }, 
        new[]{ (3,4), (1,1) }, 
        new[]{ (1,2) }, 
        new[]{ (1,4), (2,1) },
        new[]{ (2,2) },
        new[]{ (2,4), (0,1) },
        new[]{ (0,2) }
    };

    public static Rectangle TileRec = new (0, 0, 64, 32);

    internal static TileDetails MakeTileGraphic(Tile tile, Map map,
        TerrainSet terrainSet, Game game, int civilizationId)
    {
        // Define base bitmap for drawing
        var tilePic = Raylib.ImageCopy(terrainSet.BaseTiles[(int)tile.Type]);

        var directNeighbours = map.DirectNeighbours(tile, true).ToArray();

        var neighbours = map.Neighbours(tile, true).ToArray();

        // Dither
        if (tile.Type != TerrainType.Ocean)
        {
            for (var index = 0; index < directNeighbours.Length; index++)
            {
                var neighbour = directNeighbours[index];
                if (neighbour != null)
                {
                    if (neighbour.IsVisible(civilizationId))
                    {
                        ApplyDither(tilePic, neighbour.Type, tile.Type, terrainSet.DitherMaps[index]);
                    }
                }
            }
        }
        else
        {
            //drawCoasts
            var coastIndex = new[] { 0, 0, 0, 0 };
            foreach (var (neighbour, ind) in neighbours.Zip(CoastMap))
            {
                if (neighbour != null && neighbour.Type != TerrainType.Ocean)
                {
                    foreach (var (index, valueVariable) in ind)
                    {
                        coastIndex[index] += valueVariable;
                    }
                }
            }

            // NW+N+NE tiles
            Raylib.ImageDraw(ref tilePic, terrainSet.Coast[coastIndex[0], 0], new Rectangle(0, 0, 32, 16),
                new Rectangle(16, 0, 32, 16), Color.White);

            // SW+S+SE tiles
            Raylib.ImageDraw(ref tilePic, terrainSet.Coast[coastIndex[1], 1], new Rectangle(0, 0, 32, 16),
                new Rectangle(16, 16, 32, 16), Color.White);

            // SW+W+NW tiles
            Raylib.ImageDraw(ref tilePic, terrainSet.Coast[coastIndex[2], 2], new Rectangle(0, 0, 32, 16),
                new Rectangle(0, 8, 32, 16), Color.White);

            // NE+E+SE tiles
            Raylib.ImageDraw(ref tilePic, terrainSet.Coast[coastIndex[3], 3], new Rectangle(0, 0, 32, 16),
                new Rectangle(32, 8, 32, 16), Color.White);

            // River mouth
            // If river is next to ocean, draw river mouth on this tile.
            for (var index = 0; index < directNeighbours.Length; index++)
            {
                var neighbour = directNeighbours[index];
                if (neighbour is { River: true })
                {
                    Raylib.ImageDraw(ref tilePic, terrainSet.RiverMouth[index], TileRec, TileRec, Color.White);
                }
            }
        }

        if (tile.Type is TerrainType.Forest or TerrainType.Hills or TerrainType.Mountains)
        {
            var index = 0;
            var increment = 1;
            foreach (var neighbour in directNeighbours)
            {
                if (neighbour != null && neighbour.Type == tile.Type)
                {
                    index += increment;
                }

                increment *= 2;
            }

            Raylib.ImageDraw(ref tilePic, terrainSet.ImagesFor(tile.Type)[index], TileRec,
                TileRec, Color.White);
        }

        // Draw rivers
        if (tile.River)
        {
            var index = 0;
            var increment = 1;
            foreach (var neighbour in directNeighbours)
            {
                if (neighbour != null && (neighbour.River || neighbour.Type == TerrainType.Ocean))
                {
                    index += increment;
                }

                increment *= 2;
            }

            Raylib.ImageDraw(ref tilePic, terrainSet.River[index], TileRec, TileRec, Color.White);
        }

        // Draw shield for grasslands
        if (tile.Type == TerrainType.Grassland)
        {
            if (tile.HasShield)
            {
                Raylib.ImageDraw(ref tilePic, terrainSet.GrasslandShield, TileRec, TileRec, Color.White);
            }
        }
        else if (tile.Special != -1)
        {
            // Draw special resources if they exist
            Raylib.ImageDraw(ref tilePic, terrainSet.Specials[tile.Special][(int)tile.Type], TileRec, TileRec,
                Color.White);
        }

        
        var tileDetails = new TileDetails { Image = tilePic };
        if (tile.Map.MapRevealed || (tile.PlayerKnowledge != null && tile.PlayerKnowledge.Length > civilizationId &&
            tile.PlayerKnowledge[civilizationId] != null))
        {
            var improvements =
                (tile.Map.MapRevealed ? tile.Improvements : tile.PlayerKnowledge[civilizationId].Improvements)
                .Where(ci => game.TerrainImprovements.ContainsKey(ci.Improvement))
                .OrderBy(ci => game.TerrainImprovements[ci.Improvement].Layer).ToList();

            foreach (var construct in improvements)
            {
                var improvement = game.TerrainImprovements[construct.Improvement];
                var graphics = terrainSet.ImprovementsMap[construct.Improvement];

                if (improvement.HasMultiTile)
                {
                    bool hasNeighbours = false;

                    for (int i = 0; i < neighbours.Length; i++)
                    {
                        var neighbour = neighbours[i];

                        var neighboringImprovement =
                            neighbour?.Improvements.FirstOrDefault(i =>
                                i.Improvement == construct.Improvement);
                        if (neighboringImprovement != null)
                        {
                            var index = i + 1;
                            if (index != -1)
                            {
                                if (neighboringImprovement.Level < construct.Level)
                                {
                                    Raylib.ImageDraw(ref tilePic, graphics.Levels[neighboringImprovement.Level, index],
                                        TileRec,
                                        TileRec, Color.White);
                                }
                                else
                                {
                                    hasNeighbours = true;
                                    Raylib.ImageDraw(ref tilePic, graphics.Levels[construct.Level, index], TileRec,
                                        TileRec,
                                        Color.White);
                                }
                            }
                        }
                    }

                    if (!hasNeighbours)
                    {
                        if (tile.CityHere is null)
                        {
                            Raylib.ImageDraw(ref tilePic, graphics.Levels[construct.Level, 0], TileRec, TileRec,
                                Color.White);
                        }
                    }
                }
                else if (tile.PlayerKnowledge[civilizationId].CityHere is not null)
                {
                    if (tile.Map.DirectNeighbours(tile)
                        .Any(t => t.Improvements.Any(i => i.Improvement == construct.Improvement)))
                    {
                        Raylib.ImageDraw(ref tilePic, graphics.Levels[construct.Level, 0], TileRec, TileRec,
                            Color.White);
                    }
                }
                else
                {
                    if (improvement.HideUnits != -1)
                    {
                        tileDetails.ForegroundElement = new UnitHidingImprovement
                        {
                            UnitDomain = (UnitGas)improvement.HideUnits,
                            UnitImage = new MemoryStorage(graphics.UnitLevels[construct.Level, 0], improvement.Name,
                                _replacementColour),
                            Image = new MemoryStorage(graphics.Levels[construct.Level, 0], improvement.Name,
                                _replacementColour)
                        };
                    }
                    else if (improvement.Foreground)
                    {
                        tileDetails.ForegroundElement = new ForegroundImprovement
                        {
                            Image = new MemoryStorage(graphics.Levels[construct.Level, 0], improvement.Name)
                        };
                    }
                    else
                    {
                        Raylib.ImageDraw(ref tilePic, graphics.Levels[construct.Level, 0], TileRec, TileRec,
                            Color.White);
                    }
                }
            }
        }

        for (var index = 0; index < directNeighbours.Length; index++)
        {
            var directNeighbour = directNeighbours[index];
            if (directNeighbour == null || !directNeighbour.IsVisible(civilizationId))
            {
                var ditherMap = terrainSet.DitherMaps[index];
                Raylib.ImageDraw(ref tilePic, ditherMap.Images[^1], new Rectangle(0, 0, 32, 16),
                    new Rectangle(ditherMap.X, ditherMap.Y, 32, 16), Color.White);
            }
        }

        return tileDetails;
    }

    private static void ApplyDither(Image origImg, TerrainType neighbourType, TerrainType tileType,
        DitherMap ditherMap)
    {
        if (neighbourType == TerrainType.Ocean)
        {
            neighbourType = TerrainType.Grassland;
        }

        if (neighbourType == tileType) return;
        Raylib.ImageDraw(ref origImg, ditherMap.Images[(int)neighbourType], new Rectangle(0, 0, 32, 16),
            new Rectangle(ditherMap.X, ditherMap.Y, 32, 16), Color.White);
    }
}