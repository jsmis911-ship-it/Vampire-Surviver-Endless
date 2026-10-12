using System.Collections.Generic;
using UnityEngine;

/// <summary>A fixed four-lane dungeon with physical walls and a shared flow field toward the core.</summary>
public sealed class EndlessDungeon : MonoBehaviour
{
    public static EndlessDungeon Active { get; private set; }

    const int MinX = -44, MaxX = 44, MinY = -36, MaxY = 36;
    const int Width = MaxX - MinX + 1, Height = MaxY - MinY + 1;
    const int TilePixels = 16;
    const byte Wall = 0, Floor = 1, Dais = 2, Stairs = 3, Gate = 4, Void = 5;
    static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    static readonly Vector2Int[] GateCells = { new Vector2Int(-33, 27), new Vector2Int(33, 27), new Vector2Int(-33, -27), new Vector2Int(33, -27) };

    readonly byte[,] tiles = new byte[Width, Height];
    readonly bool[,] blocked = new bool[Width, Height];
    readonly int[,] distance = new int[Width, Height];
    Transform wallRoot;
    static Sprite torchSprite, portalSprite;
    public int FlowVersion { get; private set; }
    public int GateCount { get { return GateCells.Length; } }
    public Vector2 GatePosition(int index) { return GateCells[index]; }
    public Vector2 Snap(Vector2 world) { return Cell(world); }
    public float ElevationAt(Vector2 world)
    {
        Vector2Int cell = Cell(world);
        if (!Inside(cell.x, cell.y)) return 0;
        byte tile = tiles[cell.x - MinX, cell.y - MinY];
        if (tile == Dais) return 1.2f;
        if (tile != Stairs) return 0;
        int inward = Mathf.Abs(cell.x) > 3
            ? 5 - Mathf.Abs(cell.x) : 4 - Mathf.Abs(cell.y);
        return Mathf.Clamp01(inward / 2f) * 1.2f;
    }
    public static Vector2 ScreenToFloor(Vector3 screen)
    {
        Ray ray = Camera.main.ScreenPointToRay(screen);
        float distance = -ray.origin.z / ray.direction.z;
        Vector3 hit = ray.origin + ray.direction * distance;
        return hit;
    }

    void Awake()
    {
        Active = this;
        BuildLayout();
        BuildVisual();
        BuildColliders();
        BuildDecorations();
        RebuildFlow();
    }

    void OnDestroy() { if (Active == this) Active = null; }

    void Carve(int left, int right, int bottom, int top, byte type = Floor)
    {
        for (int y = bottom; y <= top; y++)
        for (int x = left; x <= right; x++)
            if (Inside(x, y)) tiles[x - MinX, y - MinY] = type;
    }

    void BuildLayout()
    {
        // Central hall, with four side chambers and long dogleg corridors.
        Carve(-11, 11, -9, 9);
        foreach (int sx in new[] { -1, 1 })
        foreach (int sy in new[] { -1, 1 })
        {
            int roomL = sx < 0 ? -39 : 29;
            int roomR = sx < 0 ? -29 : 39;
            int roomB = sy < 0 ? -32 : 22;
            int roomT = sy < 0 ? -22 : 32;
            Carve(roomL, roomR, roomB, roomT);
            int corridorL = sx < 0 ? -35 : 31;
            int corridorR = sx < 0 ? -31 : 35;
            Carve(corridorL, corridorR, sy < 0 ? -24 : 8, sy < 0 ? -8 : 24);
            Carve(sx < 0 ? -33 : 9, sx < 0 ? -9 : 33,
                sy < 0 ? -10 : 6, sy < 0 ? -6 : 10);
            // The widened bend forms a small defensive platform before each lane.
            Carve(sx < 0 ? -27 : 17, sx < 0 ? -17 : 27,
                sy < 0 ? -13 : 4, sy < 0 ? -4 : 13);
        }

        // A raised stone dais and its four stair approaches. The surrounding
        // parapet is solid, so every unit must enter through one of the stairs.
        Carve(-4, 4, -3, 3, Dais);
        for (int x = -5; x <= 5; x++)
        {
            if (Mathf.Abs(x) <= 1) continue;
            SetWall(x, -4); SetWall(x, 4);
        }
        for (int y = -4; y <= 4; y++)
        {
            if (Mathf.Abs(y) <= 1) continue;
            SetWall(-5, y); SetWall(5, y);
        }
        Carve(-1, 1, 3, 4, Stairs);
        Carve(-1, 1, -4, -3, Stairs);
        Carve(-5, -4, -1, 1, Stairs);
        Carve(4, 5, -1, 1, Stairs);

        // Low stone columns divide the hall without closing any entrance.
        foreach (int sx in new[] { -1, 1 })
        foreach (int sy in new[] { -1, 1 })
        {
            SetWall(sx * 8, sy * 5);
            SetWall(sx * 8, sy * 6);
        }
        foreach (Vector2Int gate in GateCells) tiles[gate.x - MinX, gate.y - MinY] = Gate;
        MarkOuterVoid();
    }

    void MarkOuterVoid()
    {
        // Keep a two-cell masonry boundary around each room and corridor.
        // The remaining solid mass is dark void, not another walkable-looking floor.
        for (int y = MinY; y <= MaxY; y++)
        for (int x = MinX; x <= MaxX; x++)
        {
            if (tiles[x - MinX, y - MinY] != Wall) continue;
            bool nearFloor = false;
            for (int dy = -2; dy <= 2 && !nearFloor; dy++)
            for (int dx = -2; dx <= 2; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (Inside(nx, ny) && tiles[nx - MinX, ny - MinY] != Wall
                    && tiles[nx - MinX, ny - MinY] != Void)
                { nearFloor = true; break; }
            }
            if (!nearFloor) tiles[x - MinX, y - MinY] = Void;
        }
    }

    void SetWall(int x, int y) { tiles[x - MinX, y - MinY] = Wall; }
    static bool Inside(int x, int y) { return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY; }
    static Vector2Int Cell(Vector2 world) { return new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y)); }
    bool Walkable(int x, int y)
    {
        return Inside(x, y) && tiles[x - MinX, y - MinY] != Wall
            && tiles[x - MinX, y - MinY] != Void && !blocked[x - MinX, y - MinY];
    }

    void RebuildFlow()
    {
        FlowVersion++;
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++) distance[x, y] = int.MaxValue;
        var queue = new Queue<Vector2Int>();
        distance[-MinX, -MinY] = 0;
        queue.Enqueue(Vector2Int.zero);
        while (queue.Count > 0)
        {
            Vector2Int at = queue.Dequeue();
            int cost = distance[at.x - MinX, at.y - MinY] + 1;
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int next = at + direction;
                if (!Walkable(next.x, next.y) || distance[next.x - MinX, next.y - MinY] <= cost) continue;
                distance[next.x - MinX, next.y - MinY] = cost;
                queue.Enqueue(next);
            }
        }
    }

    public bool CanBuild(Vector2 world, bool wall)
    {
        Vector2Int cell = Cell(world);
        if (!Walkable(cell.x, cell.y) || tiles[cell.x - MinX, cell.y - MinY] != Floor) return false;
        if (!wall) return true;
        // Preview checks run frequently. Test the candidate against a local flood fill
        // without changing the live flow field or forcing every enemy to repath.
        var seen = new bool[Width, Height];
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(Vector2Int.zero);
        seen[-MinX, -MinY] = true;
        while (queue.Count > 0)
        {
            Vector2Int at = queue.Dequeue();
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int next = at + direction;
                if (!Inside(next.x, next.y) || next == cell || seen[next.x - MinX, next.y - MinY]
                    || !Walkable(next.x, next.y)) continue;
                seen[next.x - MinX, next.y - MinY] = true;
                queue.Enqueue(next);
            }
        }
        foreach (Vector2Int gate in GateCells)
            if (!seen[gate.x - MinX, gate.y - MinY]) return false;
        return true;
    }

    public void SetBuildBlock(Vector2 world, bool value)
    {
        Vector2Int cell = Cell(world);
        if (!Inside(cell.x, cell.y)) return;
        blocked[cell.x - MinX, cell.y - MinY] = value;
        RebuildFlow();
    }

    public Vector2 NextWaypoint(Vector2 current)
    {
        Vector2Int at = Cell(current);
        if (!Inside(at.x, at.y)) return Vector2.zero;
        int best = distance[at.x - MinX, at.y - MinY];
        Vector2Int chosen = at;
        foreach (Vector2Int direction in Directions)
        {
            Vector2Int next = at + direction;
            if (!Walkable(next.x, next.y)) continue;
            int score = distance[next.x - MinX, next.y - MinY];
            if (score < best) { best = score; chosen = next; }
        }
        // Move through tile centers before turning. This prevents corner cuts.
        return chosen;
    }

    void BuildColliders()
    {
        wallRoot = new GameObject("DungeonWalls").transform;
        wallRoot.SetParent(transform, false);
        var faceMaterial = new Material(Shader.Find("Sprites/Default"));
        faceMaterial.mainTexture = StoneTexture(false);
        var capMaterial = new Material(Shader.Find("Sprites/Default"));
        capMaterial.mainTexture = StoneTexture(true);
        // The sanctuary is a walkable raised slab, not merely a light floor tile.
        PlaceBlock(transform, "SanctuaryPlatform", new Vector3(0, 0, -.6f),
            new Vector3(9f, 7f, 1.2f), faceMaterial);
        PlaceBlock(transform, "SanctuaryTread", new Vector3(0, 0, -1.22f),
            new Vector3(8.86f, 6.86f, .055f), capMaterial);
        for (int y = MinY; y <= MaxY; y++)
        {
            int x = MinX;
            while (x <= MaxX)
            {
                if (tiles[x - MinX, y - MinY] != Wall && tiles[x - MinX, y - MinY] != Void) { x++; continue; }
                int first = x;
                byte solidType = tiles[x - MinX, y - MinY];
                while (x <= MaxX && tiles[x - MinX, y - MinY] == solidType) x++;
                var segment = new GameObject("WallCollider");
                segment.transform.SetParent(wallRoot, false);
                segment.transform.localPosition = new Vector3((first + x - 1) * .5f, y, 0);
                var collider = segment.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(x - first, 1f);
                // Extruded masonry: the 2D collider marks the footprint while
                // the 3D block supplies a top, front, and visible thickness.
                if (solidType == Wall)
                {
                    PlaceBlock(segment.transform, "RaisedMasonry", new Vector3(0, 0, -.72f),
                        new Vector3(x - first, 1f, 1.44f), faceMaterial);
                    PlaceBlock(segment.transform, "MasonryCap", new Vector3(0, 0, -1.44f),
                        new Vector3(x - first - .12f, .88f, .08f), capMaterial);
                }
            }
        }
        // The platform has actual risers at each approach. Their short 2D
        // footprint leaves the stairs and the dais fully traversable.
        for (int step = 0; step < 4; step++)
        {
            float height = .3f + step * .25f;
            PlaceRiser(new Vector2(0, -4.4f + step * .8f), new Vector2(3.3f, .19f), height, faceMaterial, capMaterial);
            PlaceRiser(new Vector2(0, 4.4f - step * .8f), new Vector2(3.3f, .19f), height, faceMaterial, capMaterial);
            PlaceRiser(new Vector2(-5.4f + step * .8f, 0), new Vector2(.19f, 3.3f), height, faceMaterial, capMaterial);
            PlaceRiser(new Vector2(5.4f - step * .8f, 0), new Vector2(.19f, 3.3f), height, faceMaterial, capMaterial);
        }
    }

    void PlaceBlock(Transform parent, string name, Vector3 offset, Vector3 size, Material material)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = offset;
        block.transform.localScale = size;
        Destroy(block.GetComponent<BoxCollider>());
        // Repeat the masonry pattern by world size instead of stretching one
        // enormous brick across an entire platform or corridor wall.
        MeshFilter filter = block.GetComponent<MeshFilter>();
        Mesh mesh = Instantiate(filter.sharedMesh);
        Vector2[] uv = mesh.uv;
        for (int i = 0; i < uv.Length; i++)
            uv[i] = new Vector2(uv[i].x * Mathf.Max(1f, size.x),
                uv[i].y * Mathf.Max(1f, size.y));
        mesh.uv = uv;
        filter.sharedMesh = mesh;
        var renderer = block.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = -5;
    }

    static Texture2D StoneTexture(bool cap)
    {
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Repeat;
        var colors = new Color32[32 * 32];
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            bool joint = y % 8 == 0 || (x + (y / 8 % 2) * 8) % 16 == 0;
            int chip = ((x * 13 + y * 29) & 31) == 0 ? 14 : 0;
            int shade = cap ? 83 : 37;
            shade += ((x * 7 + y * 11) & 3) + chip;
            if (joint) shade = cap ? 36 : 19;
            colors[y * 32 + x] = new Color32((byte)shade,
                (byte)(shade + (cap ? 8 : 5)), (byte)(shade + (cap ? 20 : 11)), 255);
        }
        texture.SetPixels32(colors);
        texture.Apply();
        return texture;
    }

    void PlaceRiser(Vector2 position, Vector2 footprint, float height, Material side, Material cap)
    {
        var root = new GameObject("StoneStepRiser").transform;
        root.SetParent(transform, false);
        root.localPosition = new Vector3(position.x, position.y, -height * .5f);
        PlaceBlock(root, "RiserFace", Vector3.zero,
            new Vector3(footprint.x, footprint.y, height), side);
        PlaceBlock(root, "StoneTread", new Vector3(0, 0, -height * .5f),
            new Vector3(footprint.x, footprint.y, .035f), cap);
    }

    void BuildVisual()
    {
        var texture = new Texture2D(Width * TilePixels, Height * TilePixels, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[texture.width * texture.height];
        for (int gy = 0; gy < Height; gy++)
        for (int gx = 0; gx < Width; gx++)
        {
            byte type = tiles[gx, gy];
            int seed = (gx * 73856093) ^ (gy * 19349663);
            for (int py = 0; py < TilePixels; py++)
            for (int px = 0; px < TilePixels; px++)
            {
                int textureX = gx * TilePixels + px;
                int textureY = gy * TilePixels + py;
                pixels[textureY * texture.width + textureX] = Pixel(type, px, py, seed, gx, gy);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), TilePixels);
        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -30;
        if (Camera.main != null) Camera.main.backgroundColor = new Color(.055f, .065f, .09f);
    }

    void BuildDecorations()
    {
        if (torchSprite == null) torchSprite = MakeDecoration(false);
        if (portalSprite == null) portalSprite = MakeDecoration(true);
        foreach (int sx in new[] { -1, 1 })
        foreach (int sy in new[] { -1, 1 })
        {
            PlaceSprite("WallTorch", new Vector2(sx * 6, sy * 10), torchSprite, 1.45f, -18);
            PlaceSprite("WallTorch", new Vector2(sx * 30, sy * 16), torchSprite, 1.45f, -18);
            PlaceSprite("SpawnPortal", new Vector2(sx * 33, sy * 27), portalSprite, 1.4f, -12);
        }
    }

    void PlaceSprite(string name, Vector2 position, Sprite sprite, float scale, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = position;
        go.transform.localScale = Vector3.one * scale;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
    }

    static Sprite MakeDecoration(bool portal)
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Color32 color = new Color32(0, 0, 0, 0);
            if (portal)
            {
                float dx = (x - 15.5f) / 12f, dy = (y - 15.5f) / 9f;
                float d = dx * dx + dy * dy;
                if (d < 1.35f && d > .55f) color = new Color32(106, 60, 170, 150);
                if (d < 1.07f && d > .77f) color = new Color32(191, 129, 245, 255);
                if (d < .55f) color = new Color32(39, 25, 68, 185);
            }
            else
            {
                if (x >= 13 && x <= 18 && y >= 3 && y <= 13) color = new Color32(64, 70, 82, 255);
                if (x >= 10 && x <= 21 && y >= 9 && y <= 13) color = new Color32(122, 135, 147, 255);
                float dx = x - 15.5f, dy = y - 21f;
                if (dx * dx / 20f + dy * dy / 45f < 1f) color = new Color32(255, 132, 44, 255);
                if (dx * dx / 9f + dy * dy / 25f < 1f) color = new Color32(255, 221, 112, 255);
            }
            pixels[y * size + x] = color;
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 16f);
    }

    static Color32 Pixel(byte type, int x, int y, int seed, int gx, int gy)
    {
        int variation = (seed & 7) - 3;
        bool mortar = ((x + ((gy & 1) * 8)) % 16 == 0) || y == 0;
        bool grain = ((x * 17 + y * 31 + seed) & 31) == 0;
        if (type == Void)
        {
            byte shade = (byte)(17 + ((seed >> 5) & 3));
            return new Color32(shade, (byte)(shade + 3), (byte)(shade + 9), 255);
        }
        if (type == Wall)
        {
            bool brickJoint = ((x + ((gy & 1) * 8)) % 8 == 0) || y == 7;
            if (brickJoint) return new Color32(24, 28, 38, 255);
            if (y >= 13) return new Color32(51, 60, 77, 255);
            byte shade = (byte)(54 + variation + (grain ? 13 : 0));
            return new Color32(shade, (byte)(shade + 7), (byte)(shade + 20), 255);
        }
        if (type == Gate)
        {
            bool rim = x < 2 || x > 13 || y < 2 || y > 13;
            return rim ? new Color32(41, 47, 68, 255) :
                ((x + y) % 4 == 0 ? new Color32(119, 83, 171, 255) : new Color32(65, 47, 106, 255));
        }
        if (type == Stairs)
        {
            if (y % 4 == 0) return new Color32(36, 48, 63, 255);
            return new Color32(107, 121, 139, 255);
        }
        if (type == Dais)
        {
            if (mortar) return new Color32(41, 59, 73, 255);
            return grain ? new Color32(125, 149, 168, 255) : new Color32(86, 107, 128, 255);
        }
        if (mortar) return new Color32(32, 40, 52, 255);
        byte baseShade = (byte)(65 + variation + (grain ? 13 : 0));
        return new Color32(baseShade, (byte)(baseShade + 9), (byte)(baseShade + 19), 255);
    }
}
