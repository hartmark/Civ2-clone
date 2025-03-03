using System.Numerics;
using Civ2engine.MapObjects;
using Raylib_cs;

namespace RaylibUI.RunGame.GameControls.Mapping.Views.ViewElements;

public class TextureElement : IViewElement
{
    public TextureElement(Texture2D texture, Vector2 location, Tile tile, bool isTerrain = false, Vector2? offset = null)
    {
        Texture = texture;
        Location = location;
        Tile = tile;
        IsTerrain = isTerrain;
        Offset = offset ?? Vector2.Zero;
    }

    /// <summary>
    /// Used for sub elements in a set of elements to scale their locations
    /// </summary>
    public Vector2 Offset { get; set; }

    public Texture2D Texture { get; init; }
    
    public Vector2 Location { get; set; }
    
    public Tile Tile { get; set; }
    public bool IsTerrain { get; }

    public void Draw(Vector2 adjustedLocation, float scale = 1f)
    {
        var loc = adjustedLocation - Offset + Offset * scale;
        Raylib.DrawTextureEx(Texture,
            loc,
            0f,
            scale,
            Color.White);
    }

    public IViewElement CloneForLocation(Vector2 newLocation)
    {
        return new TextureElement(Texture, newLocation, Tile, IsTerrain);
    }
}