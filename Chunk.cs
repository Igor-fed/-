using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// „анк содержит 16x16x16 блоков
/// </summary>
public class Chunk : MonoBehaviour
{
    public const int Size = 16;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    public Vector3Int chunkPosition;
    private BlockType[,,] blocks;

    public bool isDirty = true;
    public bool isGenerated = false;

    private World world;
    private BlockTypeRegistry blockRegistry;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
        if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();

        blocks = new BlockType[Size, Size, Size];
    }

    public void Initialize(Vector3Int pos, World worldRef)
    {
        chunkPosition = pos;
        world = worldRef;
        blockRegistry = world.blockRegistry;

        transform.position = new Vector3(pos.x * Size, pos.y * Size, pos.z * Size);
        gameObject.name = $"Chunk_{pos.x}_{pos.y}_{pos.z}";
    }

    public void SetBlock(int x, int y, int z, BlockType type)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
            return;
        blocks[x, y, z] = type;
        isDirty = true;
    }

    public BlockType GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
            return BlockType.Air;
        return blocks[x, y, z];
    }

    public void FillWithBlock(BlockType type)
    {
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                for (int z = 0; z < Size; z++)
                    blocks[x, y, z] = type;
        isDirty = true;
    }

    public void RebuildMesh()
    {
        if (!isDirty) return;

        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int z = 0; z < Size; z++)
                {
                    BlockType type = blocks[x, y, z];
                    if (type == BlockType.Air) continue;

                    BlockData data = blockRegistry.GetBlockData(type);
                    if (data == null) continue;

                    if (IsFaceVisible(x, y + 1, z)) AddFace(x, y, z, FaceDirection.Up, data);
                    if (IsFaceVisible(x, y - 1, z)) AddFace(x, y, z, FaceDirection.Down, data);
                    if (IsFaceVisible(x + 1, y, z)) AddFace(x, y, z, FaceDirection.Right, data);
                    if (IsFaceVisible(x - 1, y, z)) AddFace(x, y, z, FaceDirection.Left, data);
                    if (IsFaceVisible(x, y, z + 1)) AddFace(x, y, z, FaceDirection.Forward, data);
                    if (IsFaceVisible(x, y, z - 1)) AddFace(x, y, z, FaceDirection.Back, data);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        isDirty = false;
    }

    private bool IsFaceVisible(int x, int y, int z)
    {
        BlockType neighbor = GetBlockTypeFromWorld(x, y, z);
        if (neighbor == BlockType.Air) return true;

        BlockData data = blockRegistry.GetBlockData(neighbor);
        if (data != null && !data.isSolid) return true;

        return false;
    }

    private BlockType GetBlockTypeFromWorld(int x, int y, int z)
    {
        // ¬нутри чанка Ч берЄм из локального массива
        if (x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size)
            return blocks[x, y, z];

        // «а границей Ч запрашиваем мир (в World.GetBlockType тоже есть фикс модул€)
        Vector3Int worldPos = new Vector3Int(
            chunkPosition.x * Size + x,
            chunkPosition.y * Size + y,
            chunkPosition.z * Size + z
        );
        return world.GetBlockType(worldPos);
    }

    // ?????????????????????????????????????????????????????????????????????????
    // ƒобавление грани.
    //
    // ¬се грани используют ≈ƒ»Ќџ… пор€док треугольников: 0-1-2, 0-2-3
    // ¬ершины каждой грани нумеруютс€ так, чтобы при взгл€де снаружи
    // блока пор€док был по часовой стрелке (CW Ч стандарт Unity):
    //
    //   3???2
    //   ??  ?
    //   ? ? ?
    //   0???1
    //
    // tri0: 0,1,2   tri1: 0,2,3
    // ?????????????????????????????????????????????????????????????????????????
    private void AddFace(int x, int y, int z, FaceDirection face, BlockData data)
    {
        int i = vertices.Count;

        float tileSize = 1f / world.textureAtlas.tilesPerRow;
        Vector2 uv = world.textureAtlas.GetUV(data, face);

        switch (face)
        {
            // +Y Ч верхн€€ грань. Ќормаль смотрит вверх.
            case FaceDirection.Up:
                vertices.Add(new Vector3(x + 1, y + 1, z));  // 0
                vertices.Add(new Vector3(x, y + 1, z));  // 1
                vertices.Add(new Vector3(x, y + 1, z + 1));  // 2
                vertices.Add(new Vector3(x + 1, y + 1, z + 1));  // 3
                break;

            // -Y Ч нижн€€ грань. Ќормаль смотрит вниз.
            case FaceDirection.Down:
                vertices.Add(new Vector3(x, y, z));  // 0
                vertices.Add(new Vector3(x + 1, y, z));  // 1
                vertices.Add(new Vector3(x + 1, y, z + 1));  // 2
                vertices.Add(new Vector3(x, y, z + 1));  // 3
                break;

            // +X Ч права€ грань. Ќормаль смотрит в +X.
            case FaceDirection.Right:
                vertices.Add(new Vector3(x + 1, y, z + 1));  // 0
                vertices.Add(new Vector3(x + 1, y, z));  // 1
                vertices.Add(new Vector3(x + 1, y + 1, z));  // 2
                vertices.Add(new Vector3(x + 1, y + 1, z + 1));  // 3
                break;

            // -X Ч лева€ грань. Ќормаль смотрит в -X.
            case FaceDirection.Left:
                vertices.Add(new Vector3(x, y, z));  // 0
                vertices.Add(new Vector3(x, y, z + 1));  // 1
                vertices.Add(new Vector3(x, y + 1, z + 1));  // 2
                vertices.Add(new Vector3(x, y + 1, z));  // 3
                break;

            // +Z Ч передн€€ грань. —мотрим сзади вперЄд. CW: X+, Y пор€док
            case FaceDirection.Forward:
                vertices.Add(new Vector3(x, y, z + 1));  // 0
                vertices.Add(new Vector3(x + 1, y, z + 1));  // 1
                vertices.Add(new Vector3(x + 1, y + 1, z + 1));  // 2
                vertices.Add(new Vector3(x, y + 1, z + 1));  // 3
                break;

            // -Z Ч задн€€ грань. —мотрим спереди назад. CW: X-, Y пор€док
            case FaceDirection.Back:
                vertices.Add(new Vector3(x + 1, y, z));  // 0
                vertices.Add(new Vector3(x, y, z));  // 1
                vertices.Add(new Vector3(x, y + 1, z));  // 2
                vertices.Add(new Vector3(x + 1, y + 1, z));  // 3
                break;
        }

        // UV: 0=левый-нижний, 1=правый-нижний, 2=правый-верхний, 3=левый-верхний
        uvs.Add(uv);
        uvs.Add(uv + new Vector2(tileSize, 0));
        uvs.Add(uv + new Vector2(tileSize, tileSize));
        uvs.Add(uv + new Vector2(0, tileSize));

        // ≈диный пор€док треугольников дл€ всех граней
        triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
        triangles.Add(i); triangles.Add(i + 2); triangles.Add(i + 3);
    }
}