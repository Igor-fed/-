using UnityEngine;

[CreateAssetMenu(fileName = "TextureAtlas", menuName = "Game/Texture Atlas")]
public class TextureAtlas : ScriptableObject
{
    public Texture2D atlasTexture;
    public int tilesPerRow = 16;

    public float TileSize => 1f / tilesPerRow;

    public void Initialize() { }  // оставляем для совместимости

    // Принимает координату тайла (col, row) ? UV левого-нижнего угла
    public Vector2 GetUV(Vector2Int tile)
    {
        return new Vector2(tile.x * TileSize, tile.y * TileSize);
    }

    public Vector2 GetUV(BlockData data, FaceDirection face)
    {
        return GetUV(data.GetTileForFace(face));
    }
}