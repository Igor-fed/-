using UnityEngine;

/// <summary>
/// Данные о типе блока. Хранится в ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "New Block", menuName = "Game/Block Data")]
public class BlockData : ScriptableObject
{
    [Header("Основное")]
    public string blockName = "Блок";
    public BlockType blockType;

    [Header("Текстуры — номер тайла в атласе (колонка, строка)")]
    public Vector2Int tileTop = Vector2Int.zero;
    public Vector2Int tileSide = Vector2Int.zero;
    public Vector2Int tileBottom = Vector2Int.zero;

    [Header("Физика блока")]
    public bool isSolid = true;
    public bool isBreakable = true;
    public float breakTime = 1f;       // секунд до разрушения
    public int minToolLevel = 0;       // 0=руки, 1=дерево, 2=камень...

    [Header("Анимация ломания")]
    public Vector2Int[] breakStages;   // тайлы трещин, обычно 8 штук

    [Header("Дроп")]
    public ItemData dropItem;          // что выпадает
    public int minDrop = 1;
    public int maxDrop = 1;

    [Header("Рендер")]
    public BlockRenderType renderType = BlockRenderType.Cube;
    public Mesh customMesh;            // только для CustomMesh

    [Header("Поведение")]
    public BlockBehaviour behaviour = BlockBehaviour.None;

    [Header("Визуал")]
    public Color emissionColor = Color.black;
    [Range(0f, 2f)]
    public float emissionIntensity = 0f;
    public float lightLevel = 0f;

    public Vector2Int GetTileForFace(FaceDirection face) => face switch
    {
        FaceDirection.Up => tileTop,
        FaceDirection.Down => tileBottom,
        _ => tileSide
    };
}

public enum BlockRenderType { Cube, CustomMesh, None }
public enum BlockBehaviour { None, SpikeTrap, Door, Chest }

public enum FaceDirection
{
    Up, Down, Left, Right, Forward, Back
}

public enum BlockType
{
    Air,
    Grass,
    Dirt,
    Stone,
    CoalOre,
    IronOre,
    GoldOre,
    DiamondOre,
    AncientRock,
    AncientArtifact,
    Lava,
    Water,
    Bedrock,
    WoodPlank,
    StoneBrick,
    IronWall,
    DiamondWall,
    CrystalBlock,
    MushroomBlock,
    AncientBrick,
    BossAltar
}