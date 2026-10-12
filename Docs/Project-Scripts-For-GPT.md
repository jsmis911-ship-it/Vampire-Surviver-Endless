# 全部游戏脚本源码

包含全部 91 个 Assets/Scripts 下的 C# 游戏脚本。场景、预制体、资源、编辑器扩展和第三方包请读取仓库中对应目录。

来源版本：`18bd017afd4d75853a39f1eb7a3e5485b1b495a4`。原始文件路径保留在每段标题。

## Assets/Scripts/Contents/EndlessBuilding.cs

```csharp
using UnityEngine;

/// <summary>One placed defense. All numbers are local to endless mode.</summary>
public sealed class EndlessBuilding : MonoBehaviour
{
    EndlessMode mode;
    SpriteRenderer sprite;
    float nextAttack;
    public int Kind { get; private set; }
    public int Level { get; private set; } = 1;
    public int Health { get; private set; }
    public int Cost { get; private set; }
    public int UpgradeCost { get { return 20 + Level * 15; } }
    public bool Selected { get; set; }

    public void Initialize(EndlessMode owner, int kind, Color color)
    {
        mode = owner;
        Kind = kind;
        Cost = new[] { 35, 20, 40, 55 }[kind];
        Health = kind == 1 ? 140 : 80;
        sprite = gameObject.AddComponent<SpriteRenderer>();
        sprite.sprite = EndlessVisual.Tower;
        sprite.color = color;
        sprite.sortingOrder = 4;
        transform.localScale = Vector3.one * 1.4f;
        if (kind == 1)
        {
            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * .65f;
        }
    }

    void Update()
    {
        if (mode == null || mode.IsDefeated || Managers.gameStop || Time.time < nextAttack) return;
        if (Kind == 1) { nextAttack = Time.time + .7f; DamageNearbyWall(); return; }
        float range = Kind == 0 ? 5.8f : 4.2f;
        EnemyController target = mode.NearestEnemy(transform.position, range);
        if (target == null) return;
        int strength = Level + mode.TowerBonus;
        if (Kind == 0)
        {
            target.OnDamaged(8 + strength * 5);
            nextAttack = Time.time + Mathf.Max(.15f, .55f - strength * .025f);
        }
        else if (Kind == 2)
        {
            mode.DamageEnemies(transform.position, 3.1f, 2 + strength * 2, true);
            nextAttack = Time.time + 1.1f;
        }
        else
        {
            mode.DamageEnemies(transform.position, 3.8f, 11 + strength * 7);
            nextAttack = Time.time + 1.25f;
        }
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 5f) * 5f);
    }

    void DamageNearbyWall()
    {
        EnemyController target = mode.NearestEnemy(transform.position, .85f);
        if (target == null) return;
        Health -= Mathf.Max(1, target.GetComponent<EnemyStat>().Damage);
        if (Health <= 0) mode.SellDestroyed(this);
    }

    public void Upgrade()
    {
        if (Level >= 5 || !mode.Spend(UpgradeCost)) return;
        Level++;
        Health += 30;
        transform.localScale = Vector3.one * (1.4f + Level * .06f);
    }

    void OnDestroy()
    {
        if (Kind == 1 && EndlessDungeon.Active != null)
            EndlessDungeon.Active.SetBuildBlock(transform.position, false);
    }
}

/// <summary>Temporary slow without permanently changing the enemy's base speed.</summary>
public sealed class EndlessSlow : MonoBehaviour
{
    EnemyStat stat;
    float originalSpeed, expires;
    void Awake() { stat = GetComponent<EnemyStat>(); }
    public void Apply()
    {
        if (stat == null) return;
        if (expires < Time.time) originalSpeed = stat.MoveSpeed;
        stat.MoveSpeed = originalSpeed * .55f;
        expires = Time.time + 1.5f;
    }
    void Update()
    {
        if (expires <= 0 || Time.time < expires || stat == null) return;
        stat.MoveSpeed = originalSpeed;
        expires = 0;
    }
    void OnDisable()
    {
        if (expires > 0 && stat != null) stat.MoveSpeed = originalSpeed;
        expires = 0;
    }
}

/// <summary>Small pixel sprites generated once so the mode runs with the assets already in the project.</summary>
public static class EndlessVisual
{
    static Sprite crystal, tower, foundation;
    public static Sprite Crystal { get { return crystal ?? (crystal = Make(true)); } }
    public static Sprite Tower { get { return tower ?? (tower = Make(false)); } }
    public static Sprite Foundation { get { return foundation ?? (foundation = MakeFoundation()); } }

    static Sprite MakeFoundation()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - 15.5f) / 14f, dy = (y - 13f) / 9f;
            float d = dx * dx + dy * dy;
            pixels[y * size + x] = d > 1.1f ? new Color32(0,0,0,0) :
                d > .83f ? new Color32(24,31,43,255) :
                d > .57f ? new Color32(95,112,132,255) :
                new Color32(41,57,75,255);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f,.5f), 32f);
    }

    static Sprite Make(bool gem)
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Color32 color = new Color32(0,0,0,0);
            if (gem)
            {
                bool baseRing = y < 9 && (x > 2 && x < 29);
                int half = y < 9 ? 0 : y < 24 ? 8 : Mathf.Max(1, 8 - (y - 24));
                bool body = y >= 9 && Mathf.Abs(x - 16) < half;
                if (baseRing) color = y > 5 ? new Color32(55,72,95,255) : new Color32(123,146,161,255);
                if (body) color = x < 16 ? new Color32(34,206,245,255) : new Color32(137,247,255,255);
                if (body && (x == 16 || y == 30)) color = new Color32(231,255,255,255);
            }
            else
            {
                bool baseBody = y < 8 && x > 2 && x < 29;
                bool turret = x > 8 && x < 24 && y >= 8 && y < 23;
                bool barrel = x > 16 && x < 31 && y >= 16 && y < 20;
                if (baseBody) color = new Color32(45,55,70,255);
                if (turret) color = new Color32(102,124,146,255);
                if (barrel) color = new Color32(39,47,62,255);
                if (turret && x > 12 && x < 19 && y > 12 && y < 20) color = new Color32(165,236,255,255);
            }
            pixels[y * size + x] = color;
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f,.5f), 32f);
    }
}
```

## Assets/Scripts/Contents/EndlessDungeon.cs

```csharp
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
```

## Assets/Scripts/Contents/EndlessMode.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>Single-player core-defense rules. The normal game and LAN session never create this component.</summary>
public sealed class EndlessMode : MonoBehaviour
{
    public static bool Selected;
    public static EndlessMode Active { get; private set; }

    const float CoreRadius = 1.45f;
    static readonly int[] Costs = { 35, 20, 40, 55 };
    static readonly string[] Names = { "机枪塔", "路障", "减速塔", "电击塔" };
    static readonly Color[] Colors = { new Color(.35f,.75f,1f), new Color(.64f,.43f,.25f), new Color(.25f,.9f,1f), new Color(1f,.83f,.2f) };

    readonly List<EndlessBuilding> buildings = new List<EndlessBuilding>();
    readonly List<EnemyController> enemies = new List<EnemyController>();
    Rigidbody2D core;
    GameObject player;
    int coreHp = 600, coreMaxHp = 600, materials = 120, wave, toSpawn, selectedBuild = -1;
    int towerBonus, weaponBonus;
    float nextSpawn, nextWave, nextCoreHit;
    bool rewardOpen, waitingChest, defeated, waveRunning;
    string defeatReason = "核心已被攻破";
    string message = "守护核心：每波结束拾取宝箱，选择一项强化";
    float messageUntil;
    GUIStyle label, button, title;
    SpriteRenderer buildPreview;
    LineRenderer rangePreview;
    string placementHint;

    public int TowerBonus { get { return towerBonus; } }
    public bool IsDefeated { get { return defeated; } }

    void Awake()
    {
        if (LanSession.Active || !Selected) { Destroy(this); return; }
        Active = this;
    }

    void Start()
    {
        player = Managers.Game.getPlayer();
        CreateCore();
        CreateBuildPreview();
        nextWave = Time.time + 15f;
        messageUntil = Time.time + 12f;
    }

    void CreateCore()
    {
        var go = new GameObject("CoreCrystal");
        go.transform.position = new Vector3(0, 0, -1.2f);
        go.AddComponent<SpriteRenderer>().sprite = EndlessVisual.Crystal;
        go.GetComponent<SpriteRenderer>().sortingOrder = -2;
        go.transform.localScale = Vector3.one * 2.2f;
        var foundation = new GameObject("CoreFoundation");
        foundation.transform.position = new Vector3(0, -.12f, -1.21f);
        foundation.transform.localScale = Vector3.one * 3.3f;
        var foundationSprite = foundation.AddComponent<SpriteRenderer>();
        foundationSprite.sprite = EndlessVisual.Foundation;
        foundationSprite.sortingOrder = -3;
        core = go.AddComponent<Rigidbody2D>();
        core.bodyType = RigidbodyType2D.Kinematic;
        core.constraints = RigidbodyConstraints2D.FreezeAll;
        var body = go.AddComponent<CircleCollider2D>();
        body.radius = .43f;
    }

    void Update()
    {
        if (defeated || player == null || core == null || Managers.gameStop) return;
        if (Input.GetKeyDown(KeyCode.P) && Managers.UI.getSceneUI() is UI_Player inventoryUI)
            inventoryUI.ToggleInventory();
        if (!rewardOpen)
        {
            if (!waveRunning && !waitingChest && Time.time >= nextWave) BeginWave();
            if (waveRunning && toSpawn > 0 && Time.time >= nextSpawn) SpawnEnemy();
            if (waveRunning && toSpawn == 0 && enemies.Count == 0) FinishWave();
        }
        AttackCore();
        HandlePlacement();
        UpdateBuildPreview();
        RaiseOccupants();
    }

    void RaiseOccupants()
    {
        if (EndlessDungeon.Active == null) return;
        Vector3 position = player.transform.position;
        position.z = -EndlessDungeon.Active.ElevationAt(position);
        player.transform.position = position;
        foreach (EnemyController enemy in enemies)
        {
            if (enemy == null) continue;
            position = enemy.transform.position;
            position.z = -EndlessDungeon.Active.ElevationAt(position);
            enemy.transform.position = position;
        }
    }

    void CreateBuildPreview()
    {
        var preview = new GameObject("BuildPreview");
        buildPreview = preview.AddComponent<SpriteRenderer>();
        buildPreview.sprite = EndlessVisual.Tower;
        buildPreview.sortingOrder = 20;
        preview.transform.localScale = Vector3.one * 1.4f;
        buildPreview.enabled = false;
        rangePreview = preview.AddComponent<LineRenderer>();
        rangePreview.material = new Material(Shader.Find("Sprites/Default"));
        rangePreview.useWorldSpace = false;
        rangePreview.loop = true;
        rangePreview.positionCount = 64;
        rangePreview.widthMultiplier = .045f;
        rangePreview.sortingOrder = 19;
        rangePreview.enabled = false;
    }

    bool CanPlaceAt(Vector2 point, out string reason)
    {
        reason = null;
        if (selectedBuild < 0 || EndlessDungeon.Active == null) return false;
        if (materials < Costs[selectedBuild]) { reason = "材料不足"; return false; }
        if (Vector2.Distance(point, core.position) < 2f || Vector2.Distance(point, player.transform.position) < 1f)
        { reason = "离核心或玩家太近"; return false; }
        if (!EndlessDungeon.Active.CanBuild(point, selectedBuild == 1))
        { reason = "只能在地板上建造，路障不能封死通路"; return false; }
        if (NearestEnemy(point, 1.1f) != null) { reason = "不能在敌人脚下建造"; return false; }
        foreach (EndlessBuilding building in buildings)
            if (building != null && Vector2.Distance(building.transform.position, point) < 1.4f)
            { reason = "建筑之间需要留出空间"; return false; }
        return true;
    }

    void UpdateBuildPreview()
    {
        if (buildPreview == null) return;
        bool visible = selectedBuild >= 0 && !rewardOpen && !defeated && Camera.main != null;
        Vector2 uiPoint = new Vector2(Input.mousePosition.x / Screen.width * 1280f,
            (Screen.height - Input.mousePosition.y) / Screen.height * 720f);
        visible &= uiPoint.y >= 115 && uiPoint.y <= 630;
        buildPreview.enabled = visible;
        rangePreview.enabled = visible && selectedBuild != 1;
        placementHint = null;
        if (!visible) return;
        Vector2 point = EndlessDungeon.Active.Snap(EndlessDungeon.ScreenToFloor(Input.mousePosition));
        bool valid = CanPlaceAt(point, out placementHint);
        buildPreview.transform.position = point;
        Color tint = valid ? Colors[selectedBuild] : new Color(1f, .25f, .25f);
        tint.a = .65f;
        buildPreview.color = tint;
        float radius = selectedBuild == 0 ? 5.8f : selectedBuild == 2 ? 4.2f : 3.8f;
        rangePreview.startColor = rangePreview.endColor = new Color(tint.r, tint.g, tint.b, .55f);
        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64f;
            rangePreview.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius / 1.4f,
                Mathf.Sin(angle) * radius / 1.4f, 0));
        }
    }

    void BeginWave()
    {
        wave++;
        waveRunning = true;
        toSpawn = 3 + wave * 3;
        nextSpawn = Time.time + 1f;
        Say("第 " + wave + " 波开始！", 3f);
    }

    void SpawnEnemy()
    {
        toSpawn--;
        nextSpawn = Time.time + Mathf.Max(.35f, 1.25f - wave * .035f);
        int type = Random.Range(1, Mathf.Min(5, 2 + wave / 2) + 1);
        bool elite = wave % 5 == 0 && toSpawn == 0;
        if (elite) type = Mathf.Clamp(1 + wave / 5, 1, 5);
        if (!Managers.Data.MonsterData.TryGetValue(type, out Data.Monster data)) return;
        // Four fixed portal tiles, each over forty tiles away by walking route.
        Vector2 portal = EndlessDungeon.Active.GatePosition((wave + toSpawn) % EndlessDungeon.Active.GateCount);
        GameObject go = Managers.Game.Spawn(Define.WorldObject.Enemy, "Monster/Enemy");
        if (go == null) return;
        // ResourceManager's pooled Instantiate overload does not apply its position argument.
        go.transform.position = portal;
        EnemyController enemy = go.GetComponent<EnemyController>();
        enemy.Init(data, Mathf.Min(100, wave * 2), elite ? Define.MonsterType.middleBoss : Define.MonsterType.Enemy);
        enemy.SetCrystalTarget(core);
        enemies.Add(enemy);
    }

    public void EnemyKilled(EnemyController enemy)
    {
        if (!enemies.Remove(enemy)) return;
        materials += enemy.GetComponent<EnemyStat>().MonsterType == Define.MonsterType.middleBoss ? 15 : 2;
    }

    void AttackCore()
    {
        if (Time.time < nextCoreHit) return;
        nextCoreHit = Time.time + 1.2f;
        int damageThisTick = 0;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = enemies[i];
            if (enemy == null) { enemies.RemoveAt(i); continue; }
            if (Vector2.Distance(enemy.transform.position, core.position) > CoreRadius) continue;
            damageThisTick += Mathf.Max(1, enemy.GetComponent<EnemyStat>().Damage);
            core.GetComponent<SpriteRenderer>().color = new Color(1f, .45f, .55f);
        }
        coreHp -= Mathf.Min(8 + wave * 2, damageThisTick);
        if (coreHp > 0) StartCoroutine(ResetCoreColor());
        if (coreHp <= 0) Defeat();
    }

    System.Collections.IEnumerator ResetCoreColor()
    {
        yield return new WaitForSeconds(.18f);
        if (core != null) core.GetComponent<SpriteRenderer>().color = Color.white;
    }

    void FinishWave()
    {
        waveRunning = false;
        waitingChest = true;
        materials += 15 + wave * 2;
        coreHp = Mathf.Min(coreMaxHp, coreHp + Mathf.CeilToInt(coreMaxHp * .08f));
        GameObject chest = Managers.Resource.Instantiate("Content/Box");
        if (chest == null) OpenReward();
        else
        {
            chest.transform.position = core.position + Vector2.right * 2.5f;
            Say("第 " + wave + " 波结束！拾取宝箱，选择奖励", 6f);
        }
    }

    public void OpenReward()
    {
        if (defeated || rewardOpen) return;
        rewardOpen = true;
        waitingChest = false;
        selectedBuild = -1;
        Managers.GamePause();
    }

    void ChooseReward(int choice)
    {
        PlayerStat stat = player.GetComponent<PlayerStat>();
        if (choice == 0) { weaponBonus++; stat.Damage += 10; stat.RefreshWeapons(); Say("武器强化：伤害 +10", 3f); }
        if (choice == 1) { towerBonus++; Say("建造强化：塔等级效果 +1", 3f); }
        if (choice == 2) { coreHp = Mathf.Min(coreMaxHp, coreHp + Mathf.CeilToInt(coreMaxHp * .3f)); Say("核心修复：恢复 30% 生命", 3f); }
        rewardOpen = false;
        nextWave = Time.time + 10f;
        Managers.GamePlay();
    }

    void HandlePlacement()
    {
        if (Input.GetMouseButtonDown(1)) { selectedBuild = -1; return; }
        if (!Input.GetMouseButtonDown(0) || rewardOpen || Camera.main == null) return;
        Vector2 uiPoint = new Vector2(Input.mousePosition.x / Screen.width * 1280f, (Screen.height - Input.mousePosition.y) / Screen.height * 720f);
        if (uiPoint.y < 115 || uiPoint.y > 630) return;
        Vector3 point = EndlessDungeon.Active.Snap(EndlessDungeon.ScreenToFloor(Input.mousePosition));
        foreach (EndlessBuilding building in buildings)
            if (building != null && Vector2.Distance(building.transform.position, point) < .9f)
            {
                selectedBuild = -1;
                building.Selected = !building.Selected;
                return;
            }
        if (selectedBuild < 0) return;
        if (!CanPlaceAt(point, out string reason)) { Say(reason, 2f); return; }
        materials -= Costs[selectedBuild];
        var go = new GameObject(Names[selectedBuild]);
        go.transform.position = point;
        var buildingPart = go.AddComponent<EndlessBuilding>();
        buildingPart.Initialize(this, selectedBuild, Colors[selectedBuild]);
        if (selectedBuild == 1) EndlessDungeon.Active.SetBuildBlock(point, true);
        buildings.Add(buildingPart);
        selectedBuild = -1;
    }

    public EnemyController NearestEnemy(Vector2 position, float radius)
    {
        EnemyController nearest = null;
        float best = radius * radius;
        foreach (EnemyController enemy in enemies)
        {
            if (enemy == null) continue;
            float d = ((Vector2)enemy.transform.position - position).sqrMagnitude;
            if (d < best) { best = d; nearest = enemy; }
        }
        return nearest;
    }

    public void DamageEnemies(Vector2 position, float radius, int damage, bool slow = false)
    {
        // OnDamaged can remove its target from enemies immediately through OnDead.
        foreach (EnemyController enemy in enemies.ToArray())
        {
            if (enemy == null || Vector2.Distance(enemy.transform.position, position) > radius) continue;
            enemy.OnDamaged(damage);
            if (slow && enemy != null) enemy.gameObject.GetOrAddComponent<EndlessSlow>().Apply();
        }
    }

    public void Sell(EndlessBuilding building)
    {
        materials += building.Cost / 2;
        buildings.Remove(building);
        Destroy(building.gameObject);
    }

    public void SellDestroyed(EndlessBuilding building)
    {
        buildings.Remove(building);
        Destroy(building.gameObject);
    }

    public bool Spend(int amount)
    {
        if (materials < amount) { Say("材料不足", 2f); return false; }
        materials -= amount;
        return true;
    }

    void Defeat()
    {
        coreHp = 0;
        defeated = true;
        Managers.GamePause();
    }

    public void PlayerDefeated()
    {
        if (defeated) return;
        defeatReason = "守护者倒下";
        defeated = true;
        Managers.GamePause();
    }

    void Say(string value, float seconds) { message = value; messageUntil = Time.time + seconds; }

    void OnGUI()
    {
        if (Active != this) return;
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleLeft, richText = true };
            title = new GUIStyle(GUI.skin.box) { fontSize = 30, alignment = TextAnchor.MiddleCenter, richText = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        }
        GUI.Box(new Rect(12, 10, 1260, 94), "");
        GUI.Label(new Rect(22, 18, 390, 75), $"核心 {coreHp}/{coreMaxHp}\n材料 {materials}    击杀强化 {weaponBonus}", label);
        string phase = waveRunning ? "战斗中" : rewardOpen ? "选择奖励" : waitingChest ? "拾取宝箱" : "准备中";
        string detail = waveRunning ? $"剩余敌人 {enemies.Count + toSpawn}" :
            waitingChest ? "到核心旁拾取宝箱" : $"下一波 {Mathf.CeilToInt(Mathf.Max(0, nextWave - Time.time))} 秒";
        GUI.Label(new Rect(415, 18, 450, 75), $"第 {wave} 波   {phase}\n{detail}", label);
        PlayerStat playerStat = player != null ? player.GetComponent<PlayerStat>() : null;
        GUI.Label(new Rect(870, 18, 390, 75), playerStat == null ? "" :
            $"玩家 {playerStat.HP}/{playerStat.MaxHP}  Lv {playerStat.Level}\n塔强化 {towerBonus}  {Mathf.FloorToInt(Managers.GameTime / 60):00}:{Mathf.FloorToInt(Managers.GameTime % 60):00}", label);
        if (Time.time < messageUntil) GUI.Label(new Rect(340, 112, 620, 52), message, title);
        if (selectedBuild >= 0 && placementHint != null && Time.time >= messageUntil)
            GUI.Label(new Rect(340, 112, 620, 52), placementHint, title);
        if (!defeated && !rewardOpen)
        {
            for (int i = 0; i < 4; i++)
                if (GUI.Button(new Rect(180 + i * 230, 645, 215, 62), $"{Names[i]}  {Costs[i]}材料" + (selectedBuild == i ? " ✓" : ""), button)) selectedBuild = i;
            EndlessBuilding chosen = null;
            foreach (EndlessBuilding building in buildings) if (building != null && building.Selected) { chosen = building; break; }
            if (chosen != null)
            {
                GUI.Box(new Rect(450, 525, 380, 110), $"{Names[chosen.Kind]} 等级 {chosen.Level}  耐久 {chosen.Health}");
                if (GUI.Button(new Rect(460, 565, 170, 55), "升级 " + chosen.UpgradeCost, button)) chosen.Upgrade();
                if (GUI.Button(new Rect(640, 565, 170, 55), "出售 +" + chosen.Cost / 2, button)) Sell(chosen);
            }
        }
        if (rewardOpen)
        {
            GUI.Box(new Rect(340, 220, 600, 310), "选择一个宝箱奖励");
            if (GUI.Button(new Rect(380, 280, 520, 65), "武器强化 · 伤害 +10", button)) ChooseReward(0);
            if (GUI.Button(new Rect(380, 355, 520, 65), "建造升级 · 所有塔伤害强化", button)) ChooseReward(1);
            if (GUI.Button(new Rect(380, 430, 520, 65), "核心修复 · 恢复 30% 血量", button)) ChooseReward(2);
        }
        if (defeated)
        {
            GUI.Box(new Rect(390, 245, 500, 230), defeatReason + " · 第 " + wave + " 波");
            if (GUI.Button(new Rect(460, 370, 360, 65), "返回主菜单", button))
            {
                Selected = false;
                Managers.GamePlay();
                Managers.Scene.LoadScene(Define.SceneType.MainMenuScene);
            }
        }
        GUI.matrix = previousMatrix;
    }

    void OnDestroy()
    {
        if (Active == this)
        {
            Active = null;
            Selected = false;
        }
    }
}
```

## Assets/Scripts/Contents/Items/Base_Item.cs

```csharp
using UnityEngine;

public abstract class Base_Item : MonoBehaviour
{
    public Transform target { get; set; } = null;
    protected bool claimed;

    protected virtual void OnEnable() { claimed = false; target = null; }

    protected virtual void OnTriggerStay2D(Collider2D col)
    {
        // A pickup deferred by a shared pause must still work if the player did
        // not leave/re-enter its trigger after both upgrade choices completed.
        if (LanSession.Active) OnTriggerEnter2D(col);
    }

    void Update()
    {
        if (!LanSession.Active || !LanSession.Instance.CanSimulate || target == null) return;
        var player = target.GetComponent<PlayerController>();
        if (player == null || LanSession.Instance.IsDown(player)) { target = null; return; }
        transform.position = Vector3.MoveTowards(transform.position, target.position, 10f * Time.deltaTime);
    }

    public abstract void OnItemEvent(PlayerStat player);

    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        if (col.transform.tag == "Player")
        {
            if (LanSession.Active && (claimed || !LanSession.Instance.CanSimulate || LanSession.Instance.IsDown(col.GetComponent<PlayerController>()))) return;
            claimed = true;
            PlayerStat playerStat = col.GetComponent<PlayerStat>();
            OnItemEvent(playerStat);
            target = null;
            Managers.Resource.Destroy(gameObject);
        }
    }
}
```

## Assets/Scripts/Contents/Items/Box_Item.cs

```csharp
using UnityEngine;

public class Box_Item : Base_Item
{
    public override void OnItemEvent(PlayerStat player)
    {
        if (LanSession.Active) { LanSession.Instance.OpenBox(player); return; }
        // 单机：直接随机给道具 + Toast，不再弹选择 UI
        Managers.Event.GiveRandomItemFromBox(player);
        Managers.Resource.Destroy(gameObject);
    }
}
```

## Assets/Scripts/Contents/Items/Exp_Item.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exp_Item : Base_Item
{
    public Sprite[] _sprite;
    public long _exp;
    public int _expMul= 1;

    public override void OnItemEvent(PlayerStat player)
    {
        // 联机/单机统一：每个玩家独立经验池，谁拾取加给谁
        long percentExp = (long)Math.Truncate((double)player.MaxExp / 100);
        if (percentExp > _exp)
            _exp = percentExp;
        player.Exp += _exp * _expMul;
    }

    public void SetExp(long exp, int mul)
    {
        _exp = exp;
        _expMul = mul;
    }

}
```

## Assets/Scripts/Contents/Items/Health_Item.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health_Item : Base_Item
{
    int _healthHp = 15;
    public override void OnItemEvent(PlayerStat player)
    {
        Managers.Sound.Play("GetHealth");
        player.HP = Mathf.Min(player.HP + _healthHp, player.MaxHP);
    }

}
```

## Assets/Scripts/Contents/Items/Magnet_Item.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magnet_Item : Base_Item
{
    public override void OnItemEvent(PlayerStat player)
    {
        if (LanSession.Active)
        {
            foreach (var exp in FindObjectsOfType<Exp_Item>()) exp.target = player.transform;
            return;
        }
        StartCoroutine(MagnetExp(player));
    }

    IEnumerator MagnetExp(PlayerStat player)
    {
        ItemGetter itemGetter = player.transform.Find("GetItemRange").GetComponent<ItemGetter>();
        float size = itemGetter._size;
        itemGetter.transform.GetComponent<CircleCollider2D>().radius = size * 100;
        yield return new WaitForSeconds(0.1f);
        itemGetter.transform.GetComponent<CircleCollider2D>().radius = size;

    }

    protected override void OnTriggerEnter2D(Collider2D col)
    {
        if (col.transform.tag == "Player")
        {
            if (LanSession.Active && (claimed || !LanSession.Instance.CanSimulate || LanSession.Instance.IsDown(col.GetComponent<PlayerController>()))) return;
            claimed = true;
            PlayerStat playerStat = col.GetComponent<PlayerStat>();
            OnItemEvent(playerStat);
            target = null;
            Managers.Resource.Destroy(gameObject, 0.2f);
        }
    }
}
```

## Assets/Scripts/Contents/RePosition.cs

```csharp
using UnityEngine;

public class RePosition : MonoBehaviour
{
    Collider2D _coll;
    [SerializeField]

    private void Awake()
    {
        _coll = GetComponent<Collider2D>();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Multiplayer recycling considers both players and runs once on the host.
        if (LanSession.Active) return;
        if (!collision.CompareTag("Area"))
            return;

        float areaSize = collision.transform.GetComponent<BoxCollider2D>().size.x;

        GameObject player = Managers.Game.getPlayer();
        if (player == null)
            return;
        Vector3 playerPos = player.transform.position;
        Vector3 myPos = transform.position;


        float dirX = playerPos.x - myPos.x;
        float dirY = playerPos.y - myPos.y;

        float diffx = Mathf.Abs(dirX);
        float diffy = Mathf.Abs(dirY);

        dirX = dirX > 0 ? 1 : -1;
        dirY = dirY > 0 ? 1 : -1;

        if (_coll.enabled)
        {
            transform.Translate(new Vector3(dirX, dirY, 0f).normalized * areaSize + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0));
        }
    }
}
```

## Assets/Scripts/Contents/Spawner.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    Dictionary<int, Data.Monster> _monsterStat = new Dictionary<int, Data.Monster>();
    public Transform[] _spawnPoint;
    float _spawnTime = 0.5f;
    bool _isSpawning = false;
    [SerializeField]
    GameObject[] _spawnUnit;

    [SerializeField]
    int _maxSpawnUnit = 50;

    public int enemyCount = 0;
    int timeLevel = 0;
    int TimeLevel { get { return timeLevel; } set { timeLevel = value; if (timeLevel >= 3) _spawnTime = 0.1f; } }
    public void AddEnemyCount(int value) { enemyCount += value; }
    private void Start()
    {
        _monsterStat = Managers.Data.MonsterData;
        _spawnPoint = GetComponentsInChildren<Transform>();
        Managers.Game._OnSpawnEvent -= AddEnemyCount;
        Managers.Game._OnSpawnEvent += AddEnemyCount;
    }

    private void Update()
    {
        if (LanSession.Active && !LanSession.Instance.CanSimulate) return;
        if ((timeLevel + 1) * 60 < Managers.GameTime)
        {
            timeLevel = (int)Managers.GameTime / 60;
            if (timeLevel <= 5)
            {
                Debug.Log($"{timeLevel}Boss Spawn!");
                SpawnBoss(timeLevel);
            }

        }
        if (!_isSpawning)
            StartCoroutine(SpawnMonster());
    }

    void SpawnBoss(int timeLevel)
    {
        GameObject Boss = null;
        if (timeLevel < 5)
        {
            int level = LanSession.Active ? LanSession.Instance.State.level : Managers.Game.getPlayer().GetComponent<PlayerStat>().Level;
            Boss = Managers.Game.Spawn(Define.WorldObject.Enemy, "Monster/Enemy");
            Boss.GetOrAddComponent<EnemyController>().Init(_monsterStat[timeLevel], level, Define.MonsterType.middleBoss);
        }
        else
        {
            Boss = Managers.Game.Spawn(Define.WorldObject.Enemy, "Monster/Boss");
        }
        if (Boss == null)
        {
            Debug.Log($"Boss Load Failed! level : {timeLevel}");
            return;
        }

        Boss.transform.position = LanSession.Active ? LanSession.Instance.SpawnPosition() : _spawnPoint[Random.Range(1, _spawnPoint.Length)].position;


    }


    IEnumerator SpawnMonster()
    {
        _isSpawning = true;
        if (enemyCount < _maxSpawnUnit)
        {
            int monsterType = SetRandomMonster(timeLevel);

            int level = LanSession.Active ? LanSession.Instance.State.level : Managers.Game.getPlayer().GetComponent<PlayerStat>().Level;
            GameObject enemy = Managers.Game.Spawn(Define.WorldObject.Enemy, "Monster/Enemy");
            enemy.transform.position = LanSession.Active ? LanSession.Instance.SpawnPosition() : _spawnPoint[Random.Range(1, _spawnPoint.Length)].position;
            enemy.GetOrAddComponent<EnemyController>().Init(_monsterStat[monsterType], level, Define.MonsterType.Enemy);
        }
        yield return new WaitForSeconds(_spawnTime);
        _isSpawning = false;
    }


    int SetRandomMonster(int timeLevel)
    {
        float rand1 = Random.Range(0, 100);
        float rand2 = Random.Range(0, 100);
        int rd = 1;
        if (rand1 < 50)
        {
            if (rand2 < 90 - (20 * timeLevel))
                rd = 1;
            else
                rd = 2;
        }
        else if (rand1 < 90)
        {
            if (rand2 < 90 - (20 * timeLevel))
                rd = 3;
            else
                rd = 4;
        }
        else
            rd = 5;

        return rd;
    }

}
```

## Assets/Scripts/Contents/WorldScrolling.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldScrolling : MonoBehaviour
{
    public void SetFollowTarget(Transform target) { _playerTransform = target; }
    [SerializeField]
    private Transform _playerTransform;
    private Vector2Int _currentTilePosition = new Vector2Int(0,0);
    [SerializeField ]
    private Vector2Int _playerTilePosition;
    private Vector2Int _onTileGridPlayerPosition;

    [SerializeField]
    private float _tileSize = 25f;
    private GameObject[,] _terrainTiles;


    [SerializeField]
    private int terrainTileHorizontalCount;
    [SerializeField]
    private int terrainTileVerticalCount;

    [SerializeField]
    private int fieldOfVisionHeight = 3;
    [SerializeField]
    private int fieldOfVisionWidth = 3;

    private void Awake()
    {
        // 不再在 Awake 依赖玩家生成时序，改为在 Update 中延迟获取
        _terrainTiles = new GameObject[terrainTileHorizontalCount, terrainTileVerticalCount];
    }

    private void Update()
    {
        // 玩家尚未生成或已销毁时，延迟重新获取
        if (_playerTransform == null)
        {
            GameObject player = Managers.Game.getPlayer();
            if (player == null)
                return;
            _playerTransform = player.transform;
        }

        _playerTilePosition.x = (int)(_playerTransform.position.x / _tileSize);
        _playerTilePosition.y = (int)(_playerTransform.position.y / _tileSize);

        _playerTilePosition.x -= _playerTransform.position.x < 0 ? 1 : 0;
        _playerTilePosition.y -= _playerTransform.position.y < 0 ? 1 : 0;

        if (_currentTilePosition != _playerTilePosition)
        {
            _currentTilePosition = _playerTilePosition;

            _currentTilePosition.x = CalculatePositionOnAxis(_currentTilePosition.x, true);
            _currentTilePosition.y = CalculatePositionOnAxis(_currentTilePosition.y, false);
            UpdateTileInScreen();
        }
    }

    private void UpdateTileInScreen()
    {
        if (_terrainTiles == null)
            return;

        for(int pov_x= -(fieldOfVisionWidth/2); pov_x <= fieldOfVisionWidth/2; pov_x++)
        {
            for (int pov_y = -(fieldOfVisionHeight/2); pov_y <= fieldOfVisionWidth/2; pov_y++)
            {
                int tileToUpdate_x = CalculatePositionOnAxis(_playerTilePosition.x + pov_x, true);
                int tileToUpdate_y = CalculatePositionOnAxis(_playerTilePosition.y + pov_y, false);

                // 瓦片尚未注册时跳过，避免空引用
                if (tileToUpdate_x < 0 || tileToUpdate_x >= terrainTileHorizontalCount
                    || tileToUpdate_y < 0 || tileToUpdate_y >= terrainTileVerticalCount)
                    continue;

                GameObject tile = _terrainTiles[tileToUpdate_x, tileToUpdate_y];
                if (tile == null)
                    continue;

                tile.transform.position = CalculateTilePosition(
                    _playerTilePosition.x + pov_x,
                    _playerTilePosition.y + pov_y
                    );
            }
        }
    }

    private Vector3 CalculateTilePosition(int x, int y)
    {
        return new Vector3(x * _tileSize, y * _tileSize, 0f);
    }

    private int CalculatePositionOnAxis(float currentValue, bool horizontal)
    {
        if (horizontal)
        {
            if(currentValue >= 0)
                currentValue = currentValue % terrainTileHorizontalCount;
            else
            {
                currentValue += 1;
                currentValue = (terrainTileHorizontalCount - 1) + currentValue % terrainTileHorizontalCount;
            }

        }
        else
        {
            if (currentValue >= 0)
                currentValue = currentValue % terrainTileVerticalCount;
            else
            {
                currentValue += 1;
                currentValue = (terrainTileVerticalCount - 1) + currentValue % terrainTileVerticalCount;
            }

        }

        return (int)currentValue;
    }

    internal void Add(GameObject tileGameObject, Vector2Int tilePosition)
    {
        _terrainTiles[tilePosition.x, tilePosition.y] = tileGameObject;
    }
}
```

## Assets/Scripts/Contents/tileReRosition.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tileReRosition : MonoBehaviour
{
    [SerializeField] Vector2Int _tilePosition;


    private void Start()
    {
        GetComponentInParent<WorldScrolling>().Add(gameObject, _tilePosition);
    }
}
```

## Assets/Scripts/Controller/BaseController.cs

```csharp
using UnityEngine;

public abstract class BaseController : MonoBehaviour
{
    protected Rigidbody2D _rigid;
    protected SpriteRenderer _sprite;
    public Animator _anime;
    public Define.WorldObject _type = Define.WorldObject.Unknown;

    private void Awake()
    {
        Init();
    }

    public abstract void OnDead();

    public abstract void OnDamaged(int damage, float force = 0);

    protected abstract void Init();
}
```

## Assets/Scripts/Controller/BossController.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : BaseController
{
    public GameObject _hudDamageText;

    protected EnemyStat _stat;
    public RuntimeAnimatorController[] _animeCon;
    public Rigidbody2D _target;
    bool _isLive = true;
    bool _isAttack = false;
    public float _skillcool = 8f;
    public int _randStat = 50;
    bool _useSkill = false;

    protected override void Init()
    {
        _stat = GetComponent<EnemyStat>();
        _rigid = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
        _anime = GetComponent<Animator>();
        _type = Define.WorldObject.Enemy;
        _target = Managers.Game.getPlayer().GetComponent<Rigidbody2D>();
        _stat.MonsterStyle = Define.MonsterStyle.unknown;
        _stat.MonsterType = Define.MonsterType.Boss;

        // 参照角色上方小血条逻辑给Boss加头顶血条
        if (gameObject.GetComponentInChildren<UI_HPBar>() == null)
            Managers.UI.MakeWorldSpaceUI<UI_HPBar>(transform, "UI_HPBar");

        // 伤害数字预制体未在 Inspector 赋值时，运行时自动加载
        if (_hudDamageText == null)
            _hudDamageText = Managers.Resource.Load<GameObject>("Prefabs/UI/WorldSpace/Damage Text");
    }

    private void FixedUpdate()
    {
        if (LanSession.Active)
        {
            if (!LanSession.Instance.CanSimulate) return;
            _target = LanSession.Instance.NearestTarget(_rigid.position);
        }
        if (_target == null) return;
        if (!_isLive)
            return;
        Vector2 dirVec = _target.position - _rigid.position;
        if(!_useSkill && dirVec.magnitude < 10)
        {
            float rd = Random.Range(0, 100f);

            if (rd < _randStat)
            {
                Debug.Log("Play Skill1");
                Skill1();
            }

            else
            {
                Debug.Log("Play Skill2");
                Skill2();
            }

        }
        else if(_useSkill && _isAttack)
        {
            _rigid.velocity = Vector2.zero;
        }
        else if (!_isAttack)
        {
            Vector2 nextVec = dirVec.normalized * (_stat.MoveSpeed * Time.fixedDeltaTime);
            _rigid.MovePosition(_rigid.position + nextVec);
            _rigid.velocity = Vector2.zero;
        }
    }

    public void SetExp(int level)
    {
        _stat.ExpPoint = 10 * level;
        _stat.ExpMul = 10;

    }

    private void LateUpdate()
    {
        // 防御：死亡后或目标/自身刚体已被销毁（通关切场景）时跳过，避免 MissingReferenceException
        if (!_isLive || _target == null || _rigid == null)
            return;
        _sprite.flipX = (_target.position.x - _rigid.position.x < 0) ? true : false;
    }

    private void OnEnable()
    {
        _isLive = true;
        _stat.HP = _stat.MaxHP;
    }



    void StartSkill1()
    {
        _anime.Play("Mushromm_Attack1", -1, 0);
    }
    void StartTimeStop()
    {
        Debug.Log("StartTimeStop!");

        StartCoroutine(TimePlay());
    }

    IEnumerator TimePlay()
    {
        if (LanSession.Active)
        {
            LanSession.Instance.PauseForBoss(2f);
            yield return new WaitForSecondsRealtime(2f);
            while (LanSession.Active && LanSession.Instance.State.phase == LanPhase.Upgrade) yield return null;
            if (!LanSession.Active || LanSession.Instance.State.phase != LanPhase.Playing) yield break;
            _target = LanSession.Instance.NearestTarget(_rigid.position);
            if (_target == null) yield break;
            _rigid.position = _target.position + Vector2.one;
            _rigid.velocity = Vector2.zero;
            _anime.Play("Mushromm_Run");
            _isAttack = false;
            yield return new WaitForSeconds(_skillcool);
            _useSkill = false;
            yield break;
        }
        UI_TimeStop TimeStopUI = Managers.UI.ShowPopupUI<UI_TimeStop>(name:null,true);
        TimeStopUI.gameObject.GetOrAddComponent<Canvas>().sortingOrder = 0;
        Managers.GamePause();
        float time = 0;
        while(time < 2f)
        {
            if(Managers.UI.GetPopupUICount() < 2)
            {
                time += Time.fixedDeltaTime;
            }
            yield return new WaitForSecondsRealtime(Time.fixedDeltaTime);
        }


        Debug.Log("Play!");
        Managers.UI.CloseAllGroupPopupUI(TimeStopUI.PopupID);
        int[] num = new int[] { -1, 1 };
        int wRand = Random.Range(0, num.Length);
        int hRand = Random.Range(0, num.Length);

        _rigid.position = _target.position + new Vector2(num[wRand], num[hRand]);
        _rigid.velocity = Vector2.zero;
        _anime.Play("Mushromm_Run");

        yield return new WaitForSecondsRealtime(Time.fixedDeltaTime);

        _isAttack = false;
        yield return new WaitForSeconds(_skillcool);
        _useSkill = false;
    }
    void Skill1()
    {
        _useSkill = true;
        _isAttack = true;
        _anime.Play("Mushromm_AttackReady1", -1, 0);
    }


    void PlayRush()
    {
        Debug.Log("Rush!");
        _anime.SetBool("doRush", true);
        StartCoroutine(Rush());
    }

    IEnumerator Rush()
    {
        for(int i = 0; i<3; i++)
        {
            Vector3 targetPosition = _target.position;
            Debug.Log($"player Position : {targetPosition}");
            Vector3 dirVec = (targetPosition - transform.position).normalized;
            float time = 0;
            float RushSpeed = ((targetPosition + (dirVec * 2)) - transform.position).magnitude * Time.fixedDeltaTime;
            while (time < 1)
            {
                Vector2 nextVec = dirVec * RushSpeed;
                _rigid.MovePosition(_rigid.position + nextVec);
                _rigid.velocity = Vector2.zero;
                yield return new WaitForFixedUpdate();
                time += Time.fixedDeltaTime;
            }
            if(i < 2)
                yield return new WaitForSeconds(0.5f);
        }
        _anime.SetBool("doRush", false);
        _anime.Play("Mushromm_Run");
        _isAttack = false;
        yield return new WaitForSeconds(_skillcool);
        _useSkill = false;


    }

    void Skill2()
    {
        _useSkill = true;
        _isAttack = true;
        _anime.Play("Mushromm_AttackReady2", -1, 0);
    }

    public override void OnDamaged(int damage, float force = 0)
    {
        if (!_isLive || (LanSession.Active && !LanSession.Instance.CanSimulate)) return;
        Managers.Event.PlayHitEnemyEffectSound();
        int calculateDamage = Mathf.Max(damage - _stat.Defense, 1);
        _stat.HP -= calculateDamage;
        _rigid.AddForce((_rigid.position - _target.position).normalized * (force * 500f));
        FloatDamageText(calculateDamage);

        OnDead();
    }


    void FloatDamageText(int damage)
    {
        if (_hudDamageText == null)
            return;
        GameObject hudText = Instantiate(_hudDamageText); // ������ �ؽ�Ʈ ������Ʈ
        hudText.transform.position = transform.position + Vector3.up * 1.5f; // ǥ�õ� ��ġ
        hudText.GetComponent<UI_DamageText>()._damage = damage; // ������ ����
    }

    public override void OnDead()
    {
        if (_isLive && _stat.HP <= 0)
        {
            _isLive = false;
            _stat.HP = 0;

            SpawnExp();
            _anime.Play("Mushromm_Death");
            if (LanSession.Active) { LanSession.Instance.Finish(true); return; }
            Managers.UI.ShowPopupUI<UI_GameVictory>();
            Managers.GamePause();
        }
    }

    void SpawnExp()
    {
        for(int i = 0; i<5; i++)
        {
            GameObject expGo = Managers.Game.Spawn(Define.WorldObject.Unknown, "Content/Exp");
            Exp_Item expPoint = expGo.GetComponent<Exp_Item>();
            expGo.GetComponent<SpriteRenderer>().sprite = expPoint._sprite[2];
            expPoint.SetExp(_stat.ExpPoint, _stat.ExpMul);
            expGo.transform.position = transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
        }
    }
}
```

## Assets/Scripts/Controller/CameraController.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    static readonly float[] ViewHeights = { 7.5f, 10f, 15f };
    public const int MinViewLevel = 0;
    public const int MaxViewLevel = 2;
    public static int ViewLevel { get; private set; } = 1;
    public static float ViewHeight => ViewHeights[ViewLevel];
    Camera _camera;
    void Awake()
    {
        _camera = GetComponent<Camera>();
        ApplyViewHeight(_camera);
    }
    public static float HeightForLevel(float level)
    {
        return ViewHeights[Mathf.Clamp(Mathf.RoundToInt(level), MinViewLevel, MaxViewLevel)];
    }
    public static void SetViewLevel(float level)
    {
        ViewLevel = Mathf.Clamp(Mathf.RoundToInt(level), MinViewLevel, MaxViewLevel);
        Camera camera = Camera.main;
        if (camera != null) ApplyViewHeight(camera);
    }
    static void ApplyViewHeight(Camera camera)
    {
        PixelPerfectCamera pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
        camera.orthographicSize = pixelPerfect == null
            ? ViewHeight
            : pixelPerfect.CorrectCinemachineOrthoSize(ViewHeight);
    }

    GameObject _player = null;

    Vector3 _delta = new Vector3(0, 0, -10f);

    public void SetPlayer(GameObject player)
    {
        _player = player;
        bool dungeon = EndlessMode.Selected && !LanSession.Active;
        transform.rotation = dungeon ? Quaternion.Euler(24f, 0, 0) : Quaternion.identity;
        _delta = dungeon ? new Vector3(0, 4.45f, -10f) : new Vector3(0, 0, -10f);
    }
    void Update()
    {
        if (object.ReferenceEquals(_player, null))
            return;

        transform.position = Vector3.Slerp(transform.position, _player.transform.position + _delta, 0.1f);
    }
}
```

## Assets/Scripts/Controller/EnemyController.cs

```csharp
using System.Collections;
using UnityEngine;

public class EnemyController : BaseController
{
    public GameObject hudDamageText;

    protected EnemyStat _stat;
    public RuntimeAnimatorController[] _animeCon;
    public Rigidbody2D _target;
    bool _isLive = true;
    bool _isRange = false;
    bool _isAttack = false;
    Vector2 _pathWaypoint;
    bool _hasPathWaypoint;
    int _pathVersion;
    Vector2 _lastProgressPosition;
    float _lastProgressTime;
    public bool TargetsCrystal { get; private set; }
    public void SetCrystalTarget(Rigidbody2D crystal)
    {
        _target = crystal;
        TargetsCrystal = crystal != null;
        _hasPathWaypoint = false;
        _lastProgressPosition = transform.position;
        _lastProgressTime = Time.time;
    }


    protected override void Init()
    {
        _stat = GetComponent<EnemyStat>();
        _rigid = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
        _anime = GetComponent<Animator>();
        _type = Define.WorldObject.Enemy;
        _target = Managers.Game.getPlayer().GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (LanSession.Active)
        {
            if (!LanSession.Instance.CanSimulate) return;
            _target = LanSession.Instance.NearestTarget(_rigid.position);
        }
        if (_target == null) return;
        if (!_isLive)
            return;

        OnMove();

        if (_isRange && !_isAttack)
        {
            StartCoroutine(RangeAttack());
        }
    }

    void OnMove()
    {
        Vector2 destination = _target.position;
        if (TargetsCrystal && EndlessDungeon.Active != null)
        {
            if (Vector2.Distance(_rigid.position, _lastProgressPosition) > .5f)
            {
                _lastProgressPosition = _rigid.position;
                _lastProgressTime = Time.time;
            }
            else if (Time.time - _lastProgressTime > 4f)
            {
                // Physics can pin a pooled enemy against a wall at a dogleg.
                // Advance exactly one reachable flow cell, never through a wall.
                Vector2 recovery = EndlessDungeon.Active.NextWaypoint(_rigid.position);
                if (Vector2.Distance(recovery, _rigid.position) > .3f)
                {
                    Debug.LogWarning($"Endless enemy unstuck from {_rigid.position} to {recovery}");
                    _rigid.position = recovery;
                }
                _hasPathWaypoint = false;
                _lastProgressPosition = _rigid.position;
                _lastProgressTime = Time.time;
            }
            if (!_hasPathWaypoint || _pathVersion != EndlessDungeon.Active.FlowVersion
                || Vector2.Distance(_rigid.position, _pathWaypoint) < .12f)
            {
                Vector2 from = _hasPathWaypoint && _pathVersion == EndlessDungeon.Active.FlowVersion
                    ? _pathWaypoint : EndlessDungeon.Active.Snap(_rigid.position);
                _pathWaypoint = EndlessDungeon.Active.NextWaypoint(from);
                _hasPathWaypoint = true;
                _pathVersion = EndlessDungeon.Active.FlowVersion;
            }
            destination = _pathWaypoint;
        }
        Vector2 dirVec = destination - _rigid.position;
        Vector2 nextVec = Vector2.ClampMagnitude(dirVec, _stat.MoveSpeed * Time.fixedDeltaTime);

        _rigid.MovePosition(_rigid.position + nextVec);
        _rigid.velocity = Vector2.zero;
    }

    IEnumerator RangeAttack()
    {
        _isAttack = true;
        SpawnBullet();
        yield return new WaitForSeconds(2f);
        _isAttack = false;
    }

    void SpawnBullet()
    {
        EnemyBullet bullet = Managers.Resource.Instantiate("Weapon/EnemyBullet",_rigid.position).GetOrAddComponent<EnemyBullet>();
        bullet._damage = _stat.Damage;
        bullet._speed = 5f;
        bullet._dir = (_target.position - _rigid.position).normalized;
    }
    private void LateUpdate()
    {
        // 防御：死亡后或目标/自身刚体已被销毁（通关切场景）时跳过，避免 MissingReferenceException
        if (!_isLive || _target == null || _rigid == null)
            return;
        _sprite.flipX = (_target.position.x - _rigid.position.x < 0) ? true : false;
    }

    private void OnEnable()
    {
        _isLive = true;
    }


    public void Init(Data.Monster monsterStat, int level, Define.MonsterType type)
    {
        _isRange = false;
        _isAttack = false;
        TargetsCrystal = false;
        _hasPathWaypoint = false;
        transform.localScale = Vector3.one;
        int mul = 1;
        switch (type)
        {
            case Define.MonsterType.Enemy:
                mul = 1;
                break;
            case Define.MonsterType.middleBoss:
                mul = 50;
                break;
        }
        _anime.runtimeAnimatorController = _animeCon[monsterStat.id-1];

        if (monsterStat.id == 5)
            _isRange = true;

        if (type == Define.MonsterType.middleBoss)
        {
            transform.localScale = Vector3.one * 2;
            // 参照角色上方小血条逻辑给中Boss加头顶血条
            if (gameObject.GetComponentInChildren<UI_HPBar>() == null)
                Managers.UI.MakeWorldSpaceUI<UI_HPBar>(transform, "UI_HPBar");
        }
        _stat.MonsterStyle = (Define.MonsterStyle)System.Enum.Parse(typeof(Define.MonsterStyle), monsterStat.name);
        _stat.MonsterType = type;
        _stat.MoveSpeed = monsterStat.moveSpeed *((float)(100f+ level)/100f);
        _stat.MaxHP = SetRandomStat((int)(monsterStat.maxHp * ((100f + 10f*level)/ 100f))) * mul;
        _stat.HP = _stat.MaxHP;
        _stat.Damage = SetRandomStat((int)(monsterStat.damage * ((100f + level) / 100f)));
        _stat.Defense = SetRandomStat((int)(monsterStat.defense * ((100f + level) / 100f)));
        _rigid.mass = 3;
        _stat.ExpPoint = 10*level;
        _stat.ExpMul = monsterStat.expMul;
        if(type == Define.MonsterType.middleBoss)
        {
            Debug.Log("Boss Spawn! ");
            Debug.Log($"MaxHp : {_stat.MaxHP}");
            Debug.Log($"Hp : {_stat.HP}");
            Debug.Log($"Damage : {_stat.Damage}");
            Debug.Log($"Defense : {_stat.Defense}");
            Debug.Log($"Weight : {_rigid.mass}");
        }
    }

    int SetRandomStat(int value)
    {
        value = (int)(value * Random.Range(0.9f, 1.1f));
        return value;
    }



    public override void OnDamaged(int damage, float force = 0)
    {
        if (!_isLive || (LanSession.Active && !LanSession.Instance.CanSimulate)) return;
        Managers.Event.PlayHitEnemyEffectSound();
        _anime.SetTrigger("Hit");
        int calculateDamage = Mathf.Max(damage - _stat.Defense, 1);
        _stat.HP -= calculateDamage;
        _rigid.AddForce((_rigid.position - _target.position).normalized * (force * 200f));
        FloatDamageText(calculateDamage);


        OnDead();
    }


    void FloatDamageText(int damage)
    {
        GameObject hudText = Instantiate(hudDamageText); // ?�성???�스???�브?�트
        hudText.transform.position = transform.position + Vector3.up*1.5f; // ?�시???�치
        hudText.GetComponent<UI_DamageText>()._damage = damage; // ?��?지 ?�달
    }

    public override void OnDead()
    {
        if(_isLive && _stat.HP <= 0)
        {
            _isLive = false;
            _stat.HP = 0;

            SpawnExp();
            Managers.Event.DropItem(_stat,transform);
            if (EndlessMode.Active != null) EndlessMode.Active.EnemyKilled(this);
            transform.localScale = Vector3.one;
            Managers.Game.Despawn(gameObject);
        }
    }

    void SpawnExp()
    {
        GameObject expGo = Managers.Game.Spawn(Define.WorldObject.Unknown, "Content/Exp");
        expGo.transform.position = transform.position;
        Exp_Item expPoint = expGo.GetComponent<Exp_Item>();
        expPoint.SetExp(_stat.ExpPoint, _stat.ExpMul);

        if (expPoint._expMul == 1)
            expGo.GetComponent<SpriteRenderer>().sprite = expPoint._sprite[0];
        else if(expPoint._expMul == 2)
            expGo.GetComponent<SpriteRenderer>().sprite = expPoint._sprite[1];
        else
            expGo.GetComponent<SpriteRenderer>().sprite = expPoint._sprite[2];

    }


}
```

## Assets/Scripts/Controller/ItemGetter.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ItemGetter : MonoBehaviour
{
    public float _size;
    private float _movSpeed = 10f;
    private Transform _player;

    private void Awake()
    {
        transform.GetComponent<CircleCollider2D>().radius = _size;
        _player = transform.parent;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Item")
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("ItemBox"))
                return;
            if (LanSession.Active)
            {
                if (!LanSession.Instance.CanSimulate || LanSession.Instance.IsDown(_player.GetComponent<PlayerController>())) return;
                var item = other.GetComponent<Base_Item>();
                if (item != null && item.target == null) item.target = _player;
                return;
            }
            StartCoroutine(GetItemInField(other.gameObject));
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (LanSession.Active) OnTriggerEnter2D(other);
    }

    IEnumerator GetItemInField(GameObject item)
    {
        if (item.GetOrAddComponent<Base_Item>().target != _player)
        {
            item.GetOrAddComponent<Base_Item>().target = _player;
            while (item != null && item.activeSelf)
            {
                item.transform.position =
                    Vector3.MoveTowards(item.transform.position, _player.transform.position, _movSpeed * Time.fixedDeltaTime);
                yield return new WaitForSeconds(Time.fixedDeltaTime);
            }
        }
    }
}
```

## Assets/Scripts/Controller/PlayerController.cs

```csharp
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : BaseController
{
    protected PlayerStat _stat;
    public RuntimeAnimatorController[] animeCon;

    [SerializeField]
    Vector2 _inputVec;
    [SerializeField]
    public Vector2 _lastDirVec = new Vector2(1, 0);

    bool _isDamaged = false;
    float _invincibility_time = 0.2f;

    // ===== 冲刺（Dash）模块 =====
    // 左Shift触发，沿最后移动方向突进一小段距离；突进期间无敌；
    // 冷却固定为常量2秒，不随攻速/冷却等参数变化。
    // dashSpeed 随 MoveSpeed 线性增长（倍率 2.4 = 12/5，保持初始手感），
    // dashDuration 自适应保证突进距离恒定（2.16 单位），无敌时间随 duration 走。
    const float DashTargetDistance = 2.16f;          // 固定突进距离（单位）
    const float DashSpeedMultiplier = 2.4f;           // dashSpeed = MoveSpeed × 2.4
    const float DASH_COOLDOWN = 2f;                   // 冲刺冷却常量（秒）
    bool _isDashing = false;                           // 是否正在突进
    bool _isInvincible = false;                       // 突进期间无敌标记
    float _nextDashTime = 0f;                         // 下一次可冲刺的时间点

    protected override void Init()
    {
        _stat = GetComponent<PlayerStat>();
        _rigid = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
        _anime = GetComponent<Animator>();
        _type = Define.WorldObject.Player;
    }


    void Update()
    {
        if (LanSession.Active)
            _inputVec = LanSession.Instance.Movement(this);
        else
        {
            _inputVec.x = Input.GetAxisRaw("Horizontal");
            _inputVec.y = Input.GetAxisRaw("Vertical");
        }

        // 左Shift触发冲刺：冷却固定2秒，不受攻速/冷却影响
        bool wantsDash = LanSession.Active ? LanSession.Instance.DashRequested(this) : Input.GetKeyDown(KeyCode.LeftShift);
        if (wantsDash && Time.time >= _nextDashTime && (!LanSession.Active || !LanSession.Instance.IsDown(this)))
        {
            _nextDashTime = Time.time + DASH_COOLDOWN;
            StartCoroutine(Dash());
        }
    }

    // 冲刺：沿最后移动方向高速突进，期间无敌
    // dashSpeed 跟随 MoveSpeed 线性增长，dashDuration 自适应保证突进距离恒定
    IEnumerator Dash()
    {
        float dashSpeed = _stat.MoveSpeed * DashSpeedMultiplier;
        float dashDuration = DashTargetDistance / dashSpeed;

        _isDashing = true;
        _isInvincible = true;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            Vector2 dir = (_lastDirVec.magnitude > 0.01f) ? _lastDirVec.normalized : Vector2.right;
            _rigid.MovePosition(_rigid.position + dir * (dashSpeed * Time.fixedDeltaTime));
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        _isDashing = false;
        _isInvincible = false;
    }

    private void FixedUpdate()
    {
        if (LanSession.Active && (!LanSession.Instance.CanSimulate || LanSession.Instance.IsDown(this))) return;
        // 冲刺期间由 Dash() 控制移动，不再叠加普通移动
        if (_isDashing)
            return;

        Vector2 nextVec = _inputVec.normalized * (_stat.MoveSpeed * Time.fixedDeltaTime);
        //Position change the player to last direction;
        _rigid.MovePosition(_rigid.position + nextVec);

        if (_inputVec.normalized.magnitude != 0)
        {
            _lastDirVec = _inputVec.normalized;
        }
    }

    private void LateUpdate()
    {
        _anime.SetFloat("speed", _inputVec.magnitude);
        if (_inputVec.x != 0)
        {
            _sprite.flipX = (_inputVec.x < 0) ? true : false;
        }
    }
    public void Init(Data.Player playerData)
    {
        int animIdx = Mathf.Clamp(playerData.id - 1, 0, animeCon.Length - 1);
        _anime.runtimeAnimatorController = animeCon[animIdx];
        _stat.MaxHP = playerData.maxHp;
        _stat.HP = playerData.maxHp;
        _stat.Damage = playerData.damage;
        _stat.Defense = playerData.defense;
        _stat.MoveSpeed = playerData.moveSpeed;
        _stat.Cooldown = playerData.coolDown;
        _stat.Amount = playerData.amount;
        // 根据角色初始数据决定是否显示弹道/冷却属性
        // Axe/LaserCannon 不显示弹道数量（光束/扇形重砍，不是多发射击）
        int wid = playerData.weaponID;
        _stat.HasAmount = wid != (int)Define.Weapons.Axe
                       && wid != (int)Define.Weapons.LaserCannon
                       && ((playerData.coolDown == 0) || (playerData.amount > 0));
        _stat.HasCooldown = playerData.coolDown != 0;
        var weaponID = (Define.Weapons)playerData.weaponID;
        if (!Enum.IsDefined(typeof(Define.Weapons), weaponID))
            weaponID = Define.Weapons.Knife;
        _stat.AddOrSetWeaponDict(weaponID, 1);
    }
    public void OnDamaged(Collision2D collision)
    {
        if (LanSession.Active && (!LanSession.Instance.CanSimulate || LanSession.Instance.IsDown(this))) return;
        _isDamaged = true;
        Stat EnemyStat = collision.transform.GetComponent<EnemyStat>();


        _stat.HP -= Mathf.Max(EnemyStat.Damage - _stat.Defense, 1);

        if (_stat.HP <= 0)
            OnDead();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.transform.tag == "Enemy")
        {
            // 冲刺无敌期间不受伤
            if (_isInvincible)
                return;

            float currentTime = Managers.GameTime;
            if (!_isDamaged)
            {
                OnDamaged(collision);
                if (LanSession.Active && LanSession.Instance.IsDown(this)) return;
                StartCoroutine(OnDamagedColor());
            }
        }
    }

    public override void OnDamaged(int damage, float force = 0)
    {
        if (LanSession.Active && (!LanSession.Instance.CanSimulate || LanSession.Instance.IsDown(this))) return;
        // 冲刺无敌期间不受伤
        if (_isInvincible)
            return;

        Managers.Event.PlayHitPlayerEffectSound();
        _stat.HP -= Mathf.Max(damage - _stat.Defense, 1);
        OnDead();
    }

    IEnumerator OnDamagedColor()
    {
        _sprite.color = Color.red;

        yield return new WaitForSeconds(_invincibility_time);

        _isDamaged = false;
        _sprite.color = Color.white;
    }


    public override void OnDead()
    {
        if (LanSession.Active)
        {
            if (_stat.HP <= 0) LanSession.Instance.Down(this);
            return;
        }
        if(_stat.HP <= 0)
        {
            _anime.SetTrigger("dead");
            _stat.HP = 0;
            if (EndlessMode.Active != null)
            {
                EndlessMode.Active.PlayerDefeated();
                return;
            }
            Managers.UI.ShowPopupUI<UI_GameOver>();
            Managers.GamePause();
        }

    }

    public void SetCoopDown(bool down)
    {
        StopAllCoroutines();
        _isDashing = false; _isInvincible = false; _isDamaged = false;
        _inputVec = Vector2.zero; _rigid.velocity = Vector2.zero;
        _sprite.color = down ? new Color(0.4f, 0.4f, 0.5f, 0.65f) : Color.white;
        _anime.SetFloat("speed", 0);
        foreach (var collider in GetComponents<Collider2D>()) collider.enabled = !down;
        if (!down) StartCoroutine(ReviveProtection());
    }

    IEnumerator ReviveProtection()
    {
        _isInvincible = true;
        yield return new WaitForSeconds(1f);
        _isInvincible = false;
    }


}
```

## Assets/Scripts/Data/Data.Contents.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
	#region Character

	[Serializable]
	public class Player
	{
		public int id;
		public string name;
		public bool selectable;
		public int weaponID;
		public int maxHp;
		public int damage;
		public int defense;
		public float moveSpeed;
		public int coolDown;
		public int amount;
	}

	[Serializable]
	public class PlayerData : ILoader<int, Player>
	{
		public List<Player> _players = new List<Player>();

		public Dictionary<int, Player> MakeDict()
		{
			Dictionary<int, Player> dict = new Dictionary<int, Player>();
			foreach (Player player in _players)
				dict.Add(player.id, player);
			return dict;
		}
	}

    #endregion

    #region Monster

	[Serializable]
	public class Monster
    {
		public int id;
		public string name;
		public int maxHp;
		public int damage;
		public int defense;
		public float moveSpeed;
		public int expMul;
	}

    public class MonsterData : ILoader<int, Monster>
    {
		public List<Monster> _monsters = new List<Monster>();
        public Dictionary<int, Monster> MakeDict()
        {
			Dictionary<int, Monster> dict = new Dictionary<int, Monster>();
			foreach (Monster monster in _monsters)
				dict.Add(monster.id, monster);
			return dict;
		}
    }


    #endregion
    #region Weapon

    [Serializable]
	public class WeaponData
    {
        public int weaponID;
        public string weaponName;
        public List<WeaponLevelData> weaponLevelData = new List<WeaponLevelData>();

	}

	[Serializable]
	public class WeaponLevelData
	{
		public int level;
		public int damage;
		public float movSpeed;
		public float force;
		public float cooldown;
		public float size;
		public float angle;
		public int penetrate;
		public int countPerCreate;
	}

	[Serializable]
	public class WeaponDataLoader : ILoader<int, WeaponData>
	{
		public List<WeaponData> _weapons = new List<WeaponData>();
        public Dictionary<int, WeaponData> MakeDict()
        {
            Dictionary<int, WeaponData> dict = new Dictionary<int, WeaponData>();
            foreach (WeaponData weapon in _weapons)
                dict.Add(weapon.weaponID, weapon);
            return dict;
        }
    }
	#endregion

}
```

## Assets/Scripts/Editor/InventoryPrefabBuilder.cs

```csharp
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>一键构建背包系统预制体。菜单: Tools/Build Inventory Prefabs</summary>
public static class InventoryPrefabBuilder
{
    const string ITEM_PATH   = "Assets/Resources/Prefabs/UI/SubItem/InventoryItem.prefab";
    const string POPUP_PATH = "Assets/Resources/Prefabs/UI/Popup/UI_Inventory.prefab";
    const string PLAYER_PATH = "Assets/Resources/Prefabs/UI/Scene/UI_Player.prefab";
    const string FONT_PATH  = "Assets/TextMesh Pro/Fonts/simhei SDF.asset";
    const string ICON_PATH  = "Assets/Resources/Prefabs/SpriteIcon/InventoryBtn.png";

    static TMP_FontAsset _zhFont;
    static Sprite _btnIcon;
    static TMP_FontAsset ZhFont { get { if (_zhFont == null) _zhFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH); return _zhFont; } }

    [MenuItem("Tools/Build Inventory Prefabs")]
    public static void BuildAll()
    {
        // 强制重新导入图标
        AssetDatabase.ImportAsset(ICON_PATH, ImportAssetOptions.ForceUpdate);
        _btnIcon = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        Debug.Log(_btnIcon != null
            ? $"[Inventory] ✔ BtnIcon loaded: {_btnIcon.name}"
            : $"[Inventory] ❌ BtnIcon NULL at {ICON_PATH}");

        BuildItem();
        BuildPopup();
        RebuildPlayer();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Inventory] All prefabs built!");
    }

    static void BuildItem()
    {
        if (File.Exists(ITEM_PATH)) { File.Delete(ITEM_PATH); File.Delete(ITEM_PATH + ".meta"); }
        var root = new GameObject("InventoryItem", typeof(RectTransform));
        root.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        root.AddComponent<InventoryItem>();
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60, 60);

        var iconGo = new GameObject("IconImage", typeof(RectTransform));
        iconGo.transform.SetParent(root.transform, false);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.raycastTarget = false;
        Stretch(iconGo.GetComponent<RectTransform>());

        var countGo = new GameObject("CountText", typeof(RectTransform));
        countGo.transform.SetParent(root.transform, false);
        var countTMP = countGo.AddComponent<TextMeshProUGUI>();
        countTMP.font = ZhFont;
        countTMP.text = "2"; countTMP.alignment = TextAlignmentOptions.BottomRight;
        countTMP.fontSize = 18; countTMP.color = Color.white; countTMP.raycastTarget = false;
        var countRT = countGo.GetComponent<RectTransform>();
        countRT.anchorMin = new Vector2(1, 0); countRT.anchorMax = new Vector2(1, 0);
        countRT.pivot = new Vector2(1, 0);
        countRT.anchoredPosition = new Vector2(-2, 2); countRT.sizeDelta = new Vector2(30, 24);

        var tipGo = new GameObject("TooltipPanel", typeof(RectTransform));
        tipGo.transform.SetParent(root.transform, false);
        tipGo.SetActive(false);
        tipGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        var tipRT = tipGo.GetComponent<RectTransform>();
        tipRT.anchorMin = new Vector2(0.5f, 1); tipRT.anchorMax = new Vector2(0.5f, 1);
        tipRT.pivot = new Vector2(0.5f, 0);
        tipRT.anchoredPosition = new Vector2(0, -4); tipRT.sizeDelta = new Vector2(200, 100);

        var nameGo = new GameObject("TooltipName", typeof(RectTransform));
        nameGo.transform.SetParent(tipGo.transform, false);
        var nameTMP = nameGo.AddComponent<TextMeshProUGUI>();
        nameTMP.font = ZhFont;
        nameTMP.text = "名称"; nameTMP.alignment = TextAlignmentOptions.TopLeft;
        nameTMP.fontSize = 16; nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = new Color(1f, 0.85f, 0.3f); nameTMP.raycastTarget = false;
        var nameRT = nameGo.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 1); nameRT.anchorMax = new Vector2(1, 1);
        nameRT.pivot = new Vector2(0.5f, 1);
        nameRT.anchoredPosition = new Vector2(0, -8); nameRT.sizeDelta = new Vector2(-16, 22);

        var descGo = new GameObject("TooltipDesc", typeof(RectTransform));
        descGo.transform.SetParent(tipGo.transform, false);
        var descTMP = descGo.AddComponent<TextMeshProUGUI>();
        descTMP.font = ZhFont;
        descTMP.text = "描述"; descTMP.alignment = TextAlignmentOptions.TopLeft;
        descTMP.fontSize = 13; descTMP.color = Color.white;
        descTMP.enableWordWrapping = true; descTMP.raycastTarget = false;
        var descRT = descGo.GetComponent<RectTransform>();
        descRT.anchorMin = Vector2.zero; descRT.anchorMax = Vector2.one;
        descRT.offsetMin = new Vector2(8, 8); descRT.offsetMax = new Vector2(-8, -30);

        SavePrefab(root, ITEM_PATH);
    }

    static void BuildPopup()
    {
        if (File.Exists(POPUP_PATH)) { File.Delete(POPUP_PATH); File.Delete(POPUP_PATH + ".meta"); }

        var root = new GameObject("UI_Inventory", typeof(RectTransform));
        var rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero; rootRT.offsetMax = Vector2.zero;
        root.AddComponent<UI_Inventory>();

        var panelGo = new GameObject("Panel", typeof(RectTransform));
        panelGo.transform.SetParent(root.transform, false);
        panelGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
        var panelRT = panelGo.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f); panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero; panelRT.sizeDelta = new Vector2(360, 180);

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(panelGo.transform, false);
        var titleTMP = titleGo.AddComponent<TextMeshProUGUI>();
        titleTMP.font = ZhFont;
        titleTMP.text = "背包 (P 键关闭)"; titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.fontSize = 16; titleTMP.color = new Color(0.1f, 0.1f, 0.1f); titleTMP.raycastTarget = false;
        var titleRT = titleGo.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -8); titleRT.sizeDelta = new Vector2(0, 24);

        var gridGo = new GameObject("GridPanel", typeof(RectTransform));
        gridGo.transform.SetParent(panelGo.transform, false);
        var gridRT = gridGo.GetComponent<RectTransform>();
        gridRT.anchorMin = Vector2.zero; gridRT.anchorMax = Vector2.one;
        gridRT.offsetMin = new Vector2(12, 12); gridRT.offsetMax = new Vector2(-12, -40);

        var glg = gridGo.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(60, 60); glg.spacing = new Vector2(8, 8);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft; glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperCenter; glg.constraint = GridLayoutGroup.Constraint.Flexible;
        var fitter = gridGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        SavePrefab(root, POPUP_PATH);
    }

    static void RebuildPlayer()
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(PLAYER_PATH);
        if (prefabRoot == null) { Debug.LogError("Cannot load " + PLAYER_PATH); return; }

        var oldBox = prefabRoot.transform.Find("WeaponImgList");
        if (oldBox != null) { Object.DestroyImmediate(oldBox.gameObject); Debug.Log("[Inventory] WeaponImgList removed"); }

        var oldBtn = prefabRoot.transform.Find("InventoryBtn");
        if (oldBtn != null) Object.DestroyImmediate(oldBtn.gameObject);

        var btnGo = new GameObject("InventoryBtn", typeof(RectTransform));
        btnGo.transform.SetParent(prefabRoot.transform, false);
        var img = btnGo.AddComponent<Image>();
        // RED = 可视诊断！如果看到红方块说明 Image 正常但 sprite 没加载
        // 如果看到 Enemy 图说明一切正常
        img.color = Color.red;
        if (_btnIcon != null) { img.sprite = _btnIcon; img.preserveAspect = true; }
        btnGo.AddComponent<Button>().targetGraphic = img;
        var btnRT = btnGo.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0, 1); btnRT.anchorMax = new Vector2(0, 1);
        btnRT.pivot = new Vector2(0, 1);
        btnRT.anchoredPosition = new Vector2(20, -20); btnRT.sizeDelta = new Vector2(60, 60);

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PLAYER_PATH);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        Debug.Log(_btnIcon != null ? "[Inventory] ✔ InventoryBtn sprite assigned!" : "[Inventory] ⚠️ InventoryBtn sprite NULL — Image.color=red");
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void SavePrefab(GameObject root, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var p = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        if (p != null) Debug.Log("[Inventory] Created: " + path);
    }
}
#endif
```

## Assets/Scripts/Managers/DataManager.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
}

public class DataManager
{
    public Dictionary<int, Data.WeaponData> WeaponData { get; private set; } = new Dictionary<int, Data.WeaponData>();
    public Dictionary<int, Data.Player> PlayerData { get; private set; } = new Dictionary<int, Data.Player>();
    public Dictionary<int, Data.Monster> MonsterData { get; private set; } = new Dictionary<int, Data.Monster>();

    public void Init()
    {
        PlayerData = LoadJson<Data.PlayerData, int, Data.Player>("PlayerData").MakeDict();
        WeaponData = LoadJson<Data.WeaponDataLoader, int, Data.WeaponData>("WeaponData").MakeDict();
        MonsterData = LoadJson<Data.MonsterData, int, Data.Monster>("MonsterData").MakeDict();

    }

    Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    {
        TextAsset textAsset = Managers.Resource.Load<TextAsset>($"Data/{path}");
        return JsonUtility.FromJson<Loader>(textAsset.text);
    }

}
```

## Assets/Scripts/Managers/EventManager.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class EventManager
{
    public enum PlayerStats
    {
        MaxHP,
        MoveSpeed,
        Damage,
        Defense,
        Cooldown,
        Amount,
        AxeRangeUp,
        AxeAngleUp,
        AxeDamageUp,
        LaserRangeUp,
        LaserDamageUp
    }


    public List<string[]> SetRandomItem(PlayerStat player, int Maxcount)
    {
        int i = 0;
        List<string[]> PoolList = new List<string[]>();
        var axeUpgrades = new List<string>();
        foreach (var name in new[] { "AxeRangeUp", "AxeAngleUp", "AxeDamageUp" })
            if (player.CanUpgradeAxe(name)) axeUpgrades.Add(name);
        var laserUpgrades = new List<string>();
        foreach (var name in new[] { "LaserRangeUp", "LaserDamageUp" })
            if (player.CanUpgradeLaser(name)) laserUpgrades.Add(name);
        while(i < Maxcount)
        {
            string[] selected = new string[2];
            float random = Random.Range(0, 100);
            int rd = 0;

            Debug.Log($"Random number for levelUp : {random}");
            if (random < 30)
                rd = 0;
            else if (random < 50)
                rd = 1;
            else
                rd = 2;


            if (i == 0 && axeUpgrades.Count > 0)
            {
                selected[0] = "1";
                selected[1] = axeUpgrades[Random.Range(0, axeUpgrades.Count)];
            }
            else if (i == 0 && laserUpgrades.Count > 0)
            {
                selected[0] = "1";
                selected[1] = laserUpgrades[Random.Range(0, laserUpgrades.Count)];
            }
            else if (rd== 0)
            {
                if (player.GetWeaponDict()[player._playerStartWeapon] >= 5)
                    continue;
                selected[0] = "0";
                selected[1] = player._playerStartWeapon.ToString();
            }
            else if (rd == 1)
            {
                selected[0] = "1";
                PlayerStats stat = SetRandomStat(player);
                selected[1] = stat.ToString();
            }
            else
            {
                selected[0] = "2";
                Define.Weapons weapon = SetRandomWeapon(player);
                if (player.GetWeaponDict().GetValueOrDefault<Define.Weapons, int>(weapon) >= 5)
                    continue;
                if (player.GetWeaponDict().Count >= 4 && !player.GetWeaponDict().ContainsKey(weapon))
                    continue;
                selected[1] = weapon.ToString();
            }

            bool isContains = false;
            foreach (string[] type in PoolList)
            {
                if(selected[1] == type[1])
                {
                    Debug.Log($"{type[1]} is already contained");
                    isContains = true;
                    break;
                }
            }
            if (isContains)
                continue;
            PoolList.Add(selected);
            i++;
        }
        return PoolList;
    }

    public PlayerStats SetRandomStat(PlayerStat player)
    {
        // Lightning/Axe/LaserCannon 升级随机池排除弹道数量
        bool excludeAmount = player != null && (player._playerStartWeapon == Define.Weapons.Lightning || player._playerStartWeapon == Define.Weapons.Axe || player._playerStartWeapon == Define.Weapons.LaserCannon);
        List<PlayerStats> stats = new List<PlayerStats>();
        foreach (PlayerStats ps in System.Enum.GetValues(typeof(PlayerStats)))
        {
            if (excludeAmount && ps == PlayerStats.Amount)
                continue;
            if ((ps == PlayerStats.AxeRangeUp || ps == PlayerStats.AxeAngleUp || ps == PlayerStats.AxeDamageUp)
                && (player == null || !player.CanUpgradeAxe(ps.ToString())))
                continue;
            // 激光炮专属增幅：只有 Aemeath 能刷到，范围/伤害均有 3 级上限
            if (ps == PlayerStats.LaserRangeUp || ps == PlayerStats.LaserDamageUp)
            {
                if (player == null || !player.CanUpgradeLaser(ps.ToString()))
                    continue;
            }
            if (ps == PlayerStats.MoveSpeed
                && (player == null || !player.CanUpgradeMoveSpeed()))
                continue;
            stats.Add(ps);
        }
        return stats[Random.Range(0, stats.Count)];
    }

    public Define.Weapons SetRandomWeapon(PlayerStat player = null)
    {
        // 和联机 BuildOffers/BuildBoxPool 对齐：
        // 通用武器所有角色都能刷，角色专属起始武器（>=100）只有对应起始武器的角色能刷
        List<Define.Weapons> weapons = new List<Define.Weapons>();
        foreach (Define.Weapons w in System.Enum.GetValues(typeof(Define.Weapons)))
        {
            int val = (int)w;
            bool isStartWeapon = val >= 100;
            if (isStartWeapon && (player == null || player._playerStartWeapon != w))
                continue;
            weapons.Add(w);
        }
        return weapons[Random.Range(0, weapons.Count)];
    }
    public void DropItem(EnemyStat stat, Transform transform)
    {
        GameObject item = null;
        float rand = Random.Range(0, 100);
        // Endless chests are awarded on wave completion, never by random kills.
        if (EndlessMode.Active != null)
            rand = rand < 5 ? 5 : 100;
        Debug.Log($"rand dropItem : {rand}");
        if ((rand < 1 || stat.MonsterType == Define.MonsterType.middleBoss) && EndlessMode.Active == null)
        {
            item = Managers.Resource.Instantiate("Content/Box");
        }
        else if (rand < 8)
        {
            int rd = Random.Range(1, 11);
            if (rd < 7)
            {
                item = Managers.Resource.Instantiate("Content/Health");
            }
            else
            {
                item = Managers.Resource.Instantiate("Content/Magnet");
            }
        }
        if (item == null)
            return;
        item.transform.position = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
    }

    public Define.Weapons SetRandomWeaponInItem()
    {
        return SetRandomWeapon();
    }

    public void LevelUpEvent()
    {
        Managers.UI.ShowPopupUI<UI_LevelUp>();
        // 单机升级不再暂停游戏（战斗继续进行，玩家可以边打怪边选升级）
    }

    public void LevelUpOverEvent(int itemType, string itemName)
    {
        //PlayerStatorWeaponUp
        PlayerStat player = Managers.Game.getPlayer().GetComponent<PlayerStat>();
        if (itemType == 1)
        {
            switch (itemName)
            {
                case "MaxHP":
                    player.MaxHP += 10;
                    player.HP = Mathf.Min(player.HP + 15, player.MaxHP);
                    break;
                case "MoveSpeed":
                    player.ApplyMoveSpeedUpgrade();
                    break;
                case "Damage":
                    player.Damage += 10;
                    break;
                case "Defense":
                    player.Defense += 1;
                    break;
                case "Cooldown":
                    player.Cooldown += 10;
                    break;
                case "Amount":
                    player.Amount += 1;
                    break;
                case "AxeRangeUp":
                case "AxeAngleUp":
                case "AxeDamageUp":
                    player.ApplyAxeUpgrade(itemName);
                    break;
                case "LaserRangeUp":
                case "LaserDamageUp":
                    player.ApplyLaserUpgrade(itemName);
                    break;
            }
            player.AddOrSetWeaponDict(player._playerStartWeapon, 0);
            player.RecordObtain("Stat", itemName);
        }

        else
        {
            Define.Weapons weaponType = (Define.Weapons)System.Enum.Parse(typeof(Define.Weapons), itemName);
            player.AddOrSetWeaponDict(weaponType, 1);
            player.RecordObtain("Weapon", weaponType.ToString());
        }


        Managers.UI.ClosePopupUI(Define.PopupUIGroup.UI_LevelUp);
    }

    public void ShowItemBoxUI()
    {
        Managers.UI.ShowPopupUI<UI_ItemBoxOpen>();
        // 单机宝箱也不暂停游戏
    }

    /// <summary>
    /// 单机宝箱：随机抽 1 个属性，直接生效 + Toast 提示。
    /// 用 LanSession.BuildBoxPool 确保和联机宝箱完全一致的角色过滤逻辑。
    /// </summary>
    public void GiveRandomItemFromBox(PlayerStat player)
    {
        if (EndlessMode.Active != null)
        {
            EndlessMode.Active.OpenReward();
            return;
        }
        var pool = LanSession.BuildBoxPool(player);
        int idx = Random.Range(0, pool.Count);
        string offer = pool[idx];

        LanSession.ApplyOffer(player, offer);

        string desc = LanSession.DescribeOffer(offer);
        ToastManager.Show(desc);
    }
    public List<Define.Weapons> SetRandomWeaponfromItemBox(PlayerStat player)
    {
        bool weaponFull = true;
        if (player.GetWeaponDict().Count >= 4)
        {
            foreach(KeyValuePair<Define.Weapons, int> weapon in player.GetWeaponDict())
            {
                if (weapon.Key == player._playerStartWeapon)
                    continue;
                if(weapon.Value < 5)
                {
                    weaponFull = false;
                    break;
                }
            }
            if (weaponFull)
            {
                return null;
            }
        }
        int maxCount = 3;
        int rd = Random.Range(1, maxCount+1);
        int i = 0;
        List<Define.Weapons> weaponList = new List<Define.Weapons>();
        while(i < rd)
        {
            Define.Weapons wp = SetRandomWeaponInItem();
            int weaponlevel = player.GetWeaponDict().GetValueOrDefault<Define.Weapons, int>(wp);
            if (weaponlevel >= 5 || (player.GetWeaponDict().Count == 4 && weaponlevel == 0))
                continue;
            weaponList.Add(wp);
            i++;
        }

        return weaponList;
    }

    public void SetLevelUpWeaponfromItemBox(List<Define.Weapons> weaponList, PlayerStat player)
    {
        if(weaponList == null)
        {
            player.HP = Mathf.Min(player.HP + 15, player.MaxHP);
            return;
        }
        foreach(Define.Weapons weaponType in weaponList)
        {
            player.AddOrSetWeaponDict(weaponType, 1);
            player.RecordObtain("Weapon", weaponType.ToString());
        }

    }

    public void PlayHitEnemyEffectSound()
    {
        int rd = Random.Range(1, 3);
        switch (rd)
        {
            case 1:
                Managers.Sound.Play("Hit0");
                break;
            case 2:
                Managers.Sound.Play("Hit1");
                break;
        }
    }

    public void PlayHitPlayerEffectSound()
    {
        Managers.Sound.Play("Hit_01");
    }

}
```

## Assets/Scripts/Managers/GameManagerEx.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class GameManagerEx
{
    GameObject _player;

    // int <-> gameobject
    //Dictionary<int, GameObject> _player = new Dictionary<int, GameObject>();
    HashSet<GameObject> _monster = new HashSet<GameObject>();
    public Action<int> _OnSpawnEvent;

    public Vector3 MousePos { get; set; }
    public Vector3 WorldMousePos { get; set; }

    public GameObject getPlayer()
    {
        if (LanSession.Active && LanSession.Instance.LocalObject != null)
            return LanSession.Instance.LocalObject;
        if (_player == null)
            _player = GameObject.FindWithTag("Player");
        return _player;
    }
    public Data.Player StartPlayer { get; set; } = new Data.Player();

    public GameObject Spawn(Define.WorldObject type, string path, Transform parent = null)
    {
        GameObject go = Managers.Resource.Instantiate(path, parent);

        switch (type)
        {
            case Define.WorldObject.Unknown:
                break;
            case Define.WorldObject.Enemy:
                _monster.Add(go);
                _OnSpawnEvent?.Invoke(1);
                break;
            case Define.WorldObject.Player:
                _player = go;
                break;
        }

        return go;

    }

    public GameObject Spawn(Define.WorldObject type, string path, Vector3 position, Transform parent = null)
    {
        GameObject go = Managers.Resource.Instantiate(path, position, parent);

        switch (type)
        {
            case Define.WorldObject.Unknown:
                break;
            case Define.WorldObject.Enemy:
                _monster.Add(go);
                _OnSpawnEvent?.Invoke(1);
                break;
            case Define.WorldObject.Player:
                _player = go;
                break;
        }

        return go;

    }

    public Define.WorldObject GetWorldObjectType(GameObject go)
    {
        Define.WorldObject type = go.GetComponent<BaseController>()._type;

        if(go == null)
            return Define.WorldObject.Unknown;

        return type;

    }

    public void Despawn(GameObject go, float time = 0)
    {
        Define.WorldObject type = GetWorldObjectType(go);

        switch (type)
        {
            case Define.WorldObject.Player:
                if (_player == go)
                {
                    _player = null;
                }
                break;
            case Define.WorldObject.Enemy:
                {
                    if (_monster.Contains(go))
                    {
                        _monster.Remove(go);
                        if (_OnSpawnEvent != null)
                            _OnSpawnEvent.Invoke(-1);
                    }
                }
                break;
        }
        Managers.Resource.Destroy(go, time);
    }

    public void Clear()
    {
        if (_player == null) { ResetPlayers(); return; }
        Spawner spanwer =  _player.GetComponentInChildren<Spawner>();
        spanwer.gameObject.SetActive(false);

        _monster.Clear();
        Despawn(_player);
    }

    public void ResetPlayers()
    {
        _player = null;
        _monster.Clear();
        _OnSpawnEvent = null;
    }

    public void SetMousePos()
    {
        MousePos = Input.mousePosition;
    }

    public void setWorldMousePos()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;
        WorldMousePos = EndlessMode.Active != null
            ? EndlessDungeon.ScreenToFloor(MousePos)
            : cam.ScreenToWorldPoint(MousePos) + new Vector3(0,0,10);
    }

}
```

## Assets/Scripts/Managers/Managers.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Managers : MonoBehaviour
{
    static Managers _instance;
    public static Managers Instance { get { Init(); return _instance; } }

    #region Contents
    GameManagerEx _game = new GameManagerEx();

    public static GameManagerEx Game { get { return Instance._game; } }
    #endregion

    #region core
    DataManager _data = new DataManager();
    PoolManager _pool = new PoolManager();
    ResourceManager _resource = new ResourceManager();
    UIManager _ui = new UIManager();
    SoundManager _sound = new SoundManager();
    private EventManager _event = new EventManager();
    private SceneManagerEx _scene = new SceneManagerEx();

    public static DataManager Data { get { return Instance._data; } }
    public static PoolManager Pool { get { return Instance._pool; } }
    public static ResourceManager Resource { get { return Instance._resource; } }
    public static UIManager UI { get { return Instance._ui; } }
    public static SoundManager Sound { get { return Instance._sound; } }
    public static EventManager Event { get { return Instance._event; } }
    public static SceneManagerEx Scene { get { return Instance._scene; } }

    #endregion

    public static float GameTime { get; set; } = 0;
    public static bool gameStop = false;

    void Awake()
    {
        Init();
    }

    static void Init()
    {
        if (_instance == null)
        {
            GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }
            DontDestroyOnLoad(go);
            _instance = go.GetComponent<Managers>();
            _instance._sound.Init();
            _instance._pool.Init();
            _instance._data.Init();
        }
    }

    private void Update()
    {
        if (!LanSession.Active || LanSession.Instance.CanSimulate)
            GameTime += Time.deltaTime;
        Game.SetMousePos();
        Game.setWorldMousePos();
    }

    public static void Clear()
    {
        Sound.Clear();
        UI.Clear();
        Pool.Clear();
        Game.Clear();
    }

    public static void ResetGameTime()
    {
        GameTime = 0;
    }
    public static void GamePause()
    {
        Time.timeScale = 0;
        Managers.gameStop = true;
    }

    public static void GamePlay()
    {
        if (LanSession.Active && LanSession.Instance.State.phase != LanPhase.Playing
            && LanSession.Instance.State.phase != LanPhase.Lobby) return;
        Time.timeScale = 1;
        Managers.gameStop = false;
    }


}
```

## Assets/Scripts/Managers/PoolManager.cs

```csharp
using System.Collections.Generic;
using UnityEngine;
public class PoolManager
{
    #region Pool
    class Pool
    {
        //원본 오브젝트
        public GameObject Original { get; private set; }
        //오브젝트 풀 역할의 Root Object
        public Transform Root { get; set; }
        //Poolable Object를 저장하는 poolStack. stack이 아니라 queue를 사용해도 된다.
        Stack<Poolable> _poolStack = new Stack<Poolable>();
        //original Object를 Pooling할 Pool이 존재하지 않을 경우 Pool을 생성한다.
        public void Init(GameObject original, int count = 5)
        {
            Original = original;
            Root = new GameObject().transform;
            Root.name = $"{Original.name}_Root";
            for (int i = 0; i < count; i++)
                Push(Create());
        }
        //Pool에 저장되는 Poolable Object를 생성한다.
        Poolable Create()
        {
            GameObject go = Object.Instantiate<GameObject>(Original);
            go.name = Original.name;
            return go.GetOrAddComponent<Poolable>();
        }
        //생성된 Poolable Object를 pool에 배치한다.
        public void Push(Poolable poolable)
        {
            //object가 존재하지 않을 시 배치하지 않는다.
            if (poolable == null)
                return;
            //pool역할을 하는 Object의 자식오브젝트로 배치하고, 비활성화하여 사용되지 않음을 확인시킨다.
            poolable.transform.parent = Root;
            poolable.gameObject.SetActive(false);
            poolable.isUsing = false;
            //poolStack에 저장
            _poolStack.Push(poolable);
        }
        //pool에 배치된 오브젝트를 꺼내온다.
        public Poolable Pop(Transform parent)
        {
            Poolable poolable = null;
            //pool에 남아있는 Poolable Object가 있는지 확인한다. 없을 경우 새로 생성.
            while (_poolStack.Count > 0)
            {
                poolable = _poolStack.Pop();
                if (!poolable.gameObject.activeSelf)
                    break;
            }
            //pool에 남아있는 Object가 없다면 원본을 통해 새로 생성하여 꺼내온다.
            if (poolable == null || poolable.gameObject.activeSelf)
                poolable = Create();
            poolable.gameObject.SetActive(true);
            //부모 오브젝트로 배치되지 않을 경우 Scene 위치에 배치된다.
            if (parent == null)
                poolable.transform.parent = Managers.Scene.CurrentScene.transform;

            poolable.transform.parent = parent;
            poolable.isUsing = true;
            return poolable;
        }
    }
    #endregion
    //전체 Pool을 담당하는 딕셔너리. 각 Pool마다 배치되는 Poolable Object가 다르기 때문에 각각의 원본마다 Pool을 따로 생성하고 관리해줘야한다.
    Dictionary<string, Pool> _pool = new Dictionary<string, Pool>();
    Transform _root;
    public void Init()
    {
        //전체 Pool Object를 부모 오브젝트 “@Pool_Root”에 배치하여 관리한다.
        if (_root == null)
        {
            _root = new GameObject { name = "@Pool_Root" }.transform;
            Object.DontDestroyOnLoad(_root);
        }
    }
    //Pool사용을 다 한 Object를 해당 원본이 존재하는 Pool에 다시 배치한다.
    public void Push(Poolable poolable, float time)
    {
        //해당 원본을 담당하는 Pool이 존재하는지 확인. 존재하지 않을 경우 Poolable 	Object가 아니라는 의미이므로 해당 오브젝트를 파괴한다.
        string name = poolable.gameObject.name;
        if (!_pool.ContainsKey(name))
        {
            GameObject.Destroy(poolable.gameObject, time);
            return;
        }
        //Pool이 존재할 경우 해당 pool에 저장한다.
        _pool[name].Push(poolable);
    }
    //해당 원본에 해당하는 오브젝트를 pool에서 꺼낸다.
    public Poolable Pop(GameObject original, Transform parent = null)
    {
        //원본에 해당되는 pool이 존재하지 않을 경우 pool을 생성
        if (!_pool.ContainsKey(original.name))
            CreatePool(original);
        //해당 pool의 Object를 꺼낸다.
        return _pool[original.name].Pop(parent);
    }
    //원본을 배치하는 pool을 생성한다.
    public void CreatePool(GameObject original, int count = 5)
    {
        //pool 객체를 생성하고 해당 원본과 지정 개수에 맞춰 pool을 초기화한다.
        Pool pool = new Pool();
        pool.Init(original, count);
        pool.Root.parent = _root;
        //해당 풀을 pool을 _pool 딕셔너리에 추가
        _pool.Add(original.name, pool);
    }
    //해당 풀의 원본을 가져온다.
    public GameObject GetOriginal(string name)
    {
        if (!_pool.ContainsKey(name))
            return null;
        return _pool[name].Original;
    }
    //모든 pool을 제거하여 초기화한다.
    public void Clear()
    {
        foreach (Transform child in _root)
        {
            GameObject.Destroy(child.gameObject);
        }
        _pool.Clear();
    }
}
```

## Assets/Scripts/Managers/Poolable.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Poolable : MonoBehaviour
{
    public bool isUsing;
}
```

## Assets/Scripts/Managers/ResourceManager.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager
{

    public T Load<T>(string path) where T:Object
    {
        if(typeof(T) == typeof(GameObject))
        {
            string name = path;
            int index = name.LastIndexOf('/');
            if (index >= 0)
                name = name.Substring(index + 1);

            GameObject go = Managers.Pool.GetOriginal(name);
            if (go != null)
                return go as T;
        }

        return Resources.Load<T>(path);
    }

    public Sprite LoadSprite(string name)
    {
        string path = $"Prefabs/SpriteIcon/{name}";

        Sprite original = Resources.Load<Sprite>(path);
        if (original == null)
        {
            Debug.Log($"Faild to sprite : {path}");
            return null;
        }
        return original;


    }
    public GameObject Instantiate(string path, Transform parent = null)
    {
        GameObject original = Load<GameObject>($"Prefabs/{path}");
        if (original == null)
        {
            Debug.Log($"Faild to load prefab : {path}");
            return null;
        }


        if (original.GetComponent<Poolable>() != null)
            return Managers.Pool.Pop(original, parent).gameObject;


        GameObject go = Object.Instantiate(original, parent);
        go.name = original.name;

        return go;
    }
    public GameObject Instantiate(string path, Vector3 position, Transform parent = null)
    {
        GameObject original = Load<GameObject>($"Prefabs/{path}");
        if(original == null)
        {
            Debug.Log($"Faild to load prefab : {path}");
            return null;
        }


        if (original.GetComponent<Poolable>() != null)
            return Managers.Pool.Pop(original, parent).gameObject;


        GameObject go = Object.Instantiate(original, position, Quaternion.identity, parent);
        go.name = original.name;

        return go;
    }

    public void Destroy(GameObject obj, float time = 0)
    {
        if(obj == null)
        {
            return;
        }


        Poolable poolable = obj.GetComponent<Poolable>();
        if(poolable != null)
        {
            Managers.Pool.Push(poolable, time);
            return;
        }

        Object.Destroy(obj, time);
    }
}
```

## Assets/Scripts/Managers/SceneManagerEx.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerEx
{

    public BaseScene CurrentScene {get{ return GameObject.FindObjectOfType<BaseScene>(); }}
   public void LoadScene(Define.SceneType type)
    {
        CurrentScene.Clear();
        SceneManager.LoadScene(GetSceneName(type));
    }

    string GetSceneName(Define.SceneType type)
    {
        string name = System.Enum.GetName(typeof(Define.SceneType), type);
        return name;
    }
    public void Clear()
    {
        CurrentScene.Clear();
    }
}
```

## Assets/Scripts/Managers/SoundManager.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager
{
    AudioSource[] _audioSources = new AudioSource[(int)Define.Sound.MaxCount];
    Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();


    public float _soundVolumn = 1f;
    public Define.BGMs _BGM = Define.BGMs.A_Bit_Of_Hope;
    // MP3 Player   -> AudioSource
    // MP3 음원     -> AudioClip
    // 관객(귀)     -> AudioListener

    public void Init()
    {
        GameObject root = GameObject.Find("@Sound");
        if (root == null)
        {
            root = new GameObject { name = "@Sound" };
            Object.DontDestroyOnLoad(root);
        }

        // 域重载后 SoundManager 实例重建但 @Sound 仍存在，需补齐 AudioSource 引用
        string[] soundNames = System.Enum.GetNames(typeof(Define.Sound));
        for (int i = 0; i < soundNames.Length - 1; i++)
        {
            if (_audioSources[i] != null)
                continue;

            Transform child = root.transform.Find(soundNames[i]);
            GameObject go;
            if (child != null)
                go = child.gameObject;
            else
            {
                go = new GameObject { name = soundNames[i] };
                go.transform.parent = root.transform;
            }

            _audioSources[i] = go.GetComponent<AudioSource>();
            if (_audioSources[i] == null)
                _audioSources[i] = go.AddComponent<AudioSource>();
        }

        _audioSources[(int)Define.Sound.Bgm].loop = true;
    }

    public void Clear()
    {
        foreach (AudioSource audioSource in _audioSources)
        {
            audioSource.clip = null;
            audioSource.Stop();
        }
        _audioClips.Clear();
    }

    public void Play(string name, Define.Sound type = Define.Sound.Effect, float pitch = 1.0f)
    {
        AudioClip audioClip = GetOrAddAudioClip(name, type);
        Play(audioClip, type, pitch);
    }

	public void Play(AudioClip audioClip, Define.Sound type = Define.Sound.Effect, float pitch = 1.0f)
	{
        if (audioClip == null)
            return;

		if (type == Define.Sound.Bgm)
		{
			AudioSource audioSource = _audioSources[(int)Define.Sound.Bgm];
			if (audioSource.isPlaying)
				audioSource.Stop();

            _BGM = (Define.BGMs)System.Enum.Parse(typeof(Define.BGMs), audioClip.name);

            audioSource.pitch = pitch;
			audioSource.clip = audioClip;
			audioSource.Play();
		}
		else
		{
			AudioSource audioSource = _audioSources[(int)Define.Sound.Effect];
			audioSource.pitch = pitch;
			audioSource.PlayOneShot(audioClip);
		}
	}

	AudioClip GetOrAddAudioClip(string name, Define.Sound type = Define.Sound.Effect)
    {
		AudioClip audioClip = null;

		if (type == Define.Sound.Bgm)
		{
            string path = $"Audio/BGM/{name}";
			audioClip = Managers.Resource.Load<AudioClip>(path);
		}
		else
		{
			if (!_audioClips.TryGetValue(name, out audioClip))
			{
                string path = $"Audio/Effect/{name}";
                audioClip = Managers.Resource.Load<AudioClip>(path);
				_audioClips.Add(name, audioClip);
			}
		}

		if (audioClip == null)
			Debug.Log($"AudioClip Missing ! {name}");

		return audioClip;
    }
    public void SetAudioVolumn(Define.Sound type = Define.Sound.Bgm, float volumn = 1)
    {
        _audioSources[(int)type].volume = volumn;
    }
    public void SetMasterVolume(float volume)
    {
        _soundVolumn = Mathf.Clamp01(volume);
        SetAudioVolumn(Define.Sound.Bgm, _soundVolumn);
        SetAudioVolumn(Define.Sound.Effect, _soundVolumn);
    }
}
```

## Assets/Scripts/Managers/ToastManager.cs

```csharp
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 简单屏幕提示系统：动态创建 Canvas + TMP_Text，淡入→停留→淡出。
/// 支持同时显示多条，垂直堆叠。
/// </summary>
public class ToastManager : MonoBehaviour
{
    static ToastManager _instance;
    static ToastManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("ToastManager");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<ToastManager>();
                _instance.Init();
            }
            return _instance;
        }
    }

    Canvas _canvas;
    Transform _toastRoot;
    static TMP_FontAsset _font;

    void Init()
    {
        // 加载 simhei SDF 字体（中文支持）
        _font = Resources.Load<TMP_FontAsset>("Fonts/simhei SDF");
        if (_font == null) _font = TMP_Settings.defaultFontAsset;

        // Canvas
        var canvasGO = new GameObject("ToastCanvas");
        canvasGO.transform.SetParent(transform);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100; // 最顶层
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();
        // EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        // Toast 容器（底部中央）
        var rootGO = new GameObject("ToastRoot");
        rootGO.transform.SetParent(canvasGO.transform, false);
        var rt = rootGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        _toastRoot = rootGO.transform;
    }

    /// <summary>显示一条 Toast。duration 总时长（淡入0.3s+停留+淡出0.5s）。</summary>
    public static void Show(string text, float duration = 2.2f)
    {
        Instance.StartCoroutine(Instance.ShowRoutine(text, duration));
    }

    IEnumerator ShowRoutine(string text, float duration)
    {
        // 创建 Toast 背景
        var toastGO = new GameObject("Toast");
        toastGO.transform.SetParent(_toastRoot, false);
        var bg = toastGO.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.75f);
        var bgRT = bg.rectTransform;
        bgRT.sizeDelta = new Vector2(380, 44);

        // 创建文本
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(toastGO.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.font = _font; // 关键！simhei SDF 字体支持中文
        txt.text = text;
        txt.color = Color.white;
        txt.fontSize = 18;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = FontStyles.Bold;
        var txtRT = txt.rectTransform;
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(10, 0); txtRT.offsetMax = new Vector2(-10, 0);

        // 位置：底部向上堆叠（y 偏移由 siblingsCount 决定）
        int siblingsCount = _toastRoot.childCount;
        var toastRT = bgRT;
        toastRT.anchoredPosition = new Vector2(0, -100 - (siblingsCount - 1) * 50);

        // 淡入
        float fadeIn = 0.3f;
        for (float t = 0; t < fadeIn; t += Time.deltaTime)
        {
            float a = t / fadeIn;
            toastRT.localScale = new Vector3(1f, 0f, 1f);
            toastRT.localScale = Vector3.Lerp(new Vector3(1f, 0f, 1f), Vector3.one, a);
            bg.color = new Color(0, 0, 0, 0.75f * a);
            txt.color = new Color(1, 1, 1, a);
            yield return null;
        }
        toastRT.localScale = Vector3.one;
        bg.color = new Color(0, 0, 0, 0.75f);
        txt.color = Color.white;

        // 停留
        float stay = Mathf.Max(0, duration - 0.3f - 0.5f);
        yield return new WaitForSeconds(stay);

        // 淡出
        float fadeOut = 0.5f;
        for (float t = 0; t < fadeOut; t += Time.deltaTime)
        {
            float a = 1f - t / fadeOut;
            bg.color = new Color(0, 0, 0, 0.75f * a);
            txt.color = new Color(1, 1, 1, a);
            toastRT.localScale = new Vector3(1f, a, 1f);
            yield return null;
        }

        Destroy(toastGO);
    }
}
```

## Assets/Scripts/Managers/UIManager.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager
{
    private int _order = 10;

    private Dictionary<Define.PopupUIGroup, Stack<UI_Popup>> _popupStackDict = new Dictionary<Define.PopupUIGroup, Stack<UI_Popup>>();
    private UI_Scene _sceneUI = null;

    public GameObject Root()
    {
        GameObject root = GameObject.Find("@UI_Root");
        if (root == null)
            root = new GameObject { name = "@UI_Root" };

        return root;
    }

    public void SetCanvas(GameObject go, bool sorting = false)
    {
        Canvas canvas = go.GetOrAddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;

        if (sorting)
            canvas.sortingOrder = _order++;
        else
            canvas.sortingOrder = 0;
    }

    public T MakeWorldSpaceUI<T>(Transform parent, string name = null) where T : UI_Base
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"UI/WorldSpace/{name}");

        if (parent != null)
            go.transform.SetParent(parent);

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        //return go.GetOrAddComponent<T>();
        return Util.GetOrAddComponent<T>(go);
    }

    public T MakeSubItem<T>(Transform parent, string name = null) where T : UI_Base
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"UI/SubItem/{name}");

        if(parent != null)
            go.transform.SetParent(parent);

        return go.GetOrAddComponent<T>();
    }

    public T ShowSceneUI<T>(string name = null) where T : UI_Scene
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;


        GameObject go = Managers.Resource.Instantiate($"UI/Scene/{name}");

        T sceneUI = Util.GetOrAddComponent<T>(go);
        _sceneUI = sceneUI;


        go.transform.SetParent(Root().transform);
        if (sceneUI is UI_MainMenu)
            go.GetOrAddComponent<FixedMenuViewport>().Configure(true);
        return sceneUI;
    }
    public UI_Scene getSceneUI()
    {
        return _sceneUI;
    }

    public T ShowPopupUI<T>(string name = null, bool forEffect = false) where T : UI_Popup
    {
        Managers.Sound.SetAudioVolumn(Define.Sound.Bgm, Managers.Sound._soundVolumn * (typeof(T) == typeof(UI_GameMenu) ? 1f : .5f));

        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"UI/Popup/{name}");

        go.transform.SetParent(Root().transform);

        T popup =  go.GetOrAddComponent<T>();
        if (popup is UI_CharacterSelect)
            go.GetOrAddComponent<FixedMenuViewport>().Configure(false);
        Define.PopupUIGroup popupType = popup.PopupID;

        if (!_popupStackDict.ContainsKey(popupType))
            _popupStackDict.Add(popupType, new Stack<UI_Popup>());

        _popupStackDict[popupType].Push(popup);

        if (forEffect)
            popup.GetComponent<Canvas>().sortingOrder = -1;


        return popup as T;
    }

    public void ClosePopupUI(Define.PopupUIGroup popupType)
    {
        if (!_popupStackDict.TryGetValue(popupType, out Stack<UI_Popup> popupStack)
            || _popupStackDict[popupType].Count == 0)
            return;

        UI_Popup popup = _popupStackDict[popupType].Pop();
        Managers.Resource.Destroy(popup.gameObject);
        _order--;
        popup = null;

        CheckPopupUICountAndRemove();
    }

    public void ClosePopupUI(UI_Popup popup)
    {
        Define.PopupUIGroup popupType = popup.PopupID;
        if (!_popupStackDict.TryGetValue(popupType, out Stack<UI_Popup> popupStack)
            || _popupStackDict[popupType].Count == 0)
            return;

        if (popup != popupStack.Peek())
        {
            Debug.Log("Close Popup Failed");
            return;
        }

        ClosePopupUI(popupType);
    }
    public void CloseAllPopupUI()
    {
        foreach (KeyValuePair<Define.PopupUIGroup, Stack<UI_Popup>> kv in _popupStackDict)
        {
            Define.PopupUIGroup popupType = kv.Key;
            Stack<UI_Popup> popupStack = kv.Value;
            while(popupStack.Count != 0)
            {
                UI_Popup popup = popupStack.Pop();
                Managers.Resource.Destroy(popup.gameObject);
                _order--;
                popup = null;
            }
        }
        CheckPopupUICountAndRemove();
    }

    public void CloseAllGroupPopupUI(Define.PopupUIGroup popupType)
    {
        if (!_popupStackDict.TryGetValue(popupType, out Stack<UI_Popup> popupStack)
            || _popupStackDict[popupType].Count == 0)
            return;

        while (popupStack.Count != 0)
        {
            UI_Popup popup = popupStack.Pop();
            Managers.Resource.Destroy(popup.gameObject);
            _order--;
            popup = null;
        }
        CheckPopupUICountAndRemove();
    }


    void CheckPopupUICountAndRemove()
    {
        List<Define.PopupUIGroup> popupType = new List<Define.PopupUIGroup>();
        foreach(Define.PopupUIGroup popupUI in _popupStackDict.Keys)
        {
            popupType.Add(popupUI);
        }
        for(int i = 0; i<_popupStackDict.Count; i++)
        {
            if (_popupStackDict.GetValueOrDefault<Define.PopupUIGroup, Stack<UI_Popup>>(popupType[i]).Count == 0)
                _popupStackDict.Remove(popupType[i]);
        }
        CheckPopupUICountInScene();
    }

    void CheckPopupUICountInScene()
    {

        Debug.Log($"popupCount : {_popupStackDict.Count}");
        foreach(Define.PopupUIGroup popupKey in _popupStackDict.Keys)
        {
            Debug.Log($"popupList : {popupKey}");
        }

        if (_popupStackDict.Count == 0)
        {
            Managers.Sound.SetAudioVolumn(Define.Sound.Bgm, Managers.Sound._soundVolumn);
            Managers.GamePlay();
        }
    }

    public int GetPopupUICount()
    {
        return _popupStackDict.Count;
    }

    /// <summary>判断指定 PopupUIGroup 是否正在打开（栈内有元素）。</summary>
    public bool IsPopupOpen(Define.PopupUIGroup group)
    {
        return _popupStackDict.TryGetValue(group, out var stack) && stack.Count > 0;
    }

    public void Clear()
    {
        CloseAllPopupUI();
        _sceneUI = null;
    }
}
```

## Assets/Scripts/Multiplayer/LanAssetCatalog.cs

```csharp
using TMPro;
using UnityEngine;

// Stable asset GUID/local-ID ordering is generated in the Editor, never instance IDs.
public sealed class LanAssetCatalog : ScriptableObject
{
    public Sprite[] sprites;
    public string contentHash;
    public TMP_FontAsset font;
}
```

## Assets/Scripts/Multiplayer/LanOverlay.cs

```csharp
using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime UI keeps the prototype independent of single-player popup stacks.</summary>
public sealed class LanOverlay : MonoBehaviour
{
    LanSession session;
    TMP_FontAsset font;
    Transform root;
    GameObject lobby, hud, upgrades, result, menu;
    TMP_Text status, roster, progress, resultTitle, resultText, upgradeTitle;
    readonly TMP_Text[] playerHeader = new TMP_Text[2];      // 标题行（你/队友 · 幸存者 N）
    readonly TMP_Text[] playerHP = new TMP_Text[2];          // 生命: hp/maxHp
    readonly TMP_Text[] playerCooldown = new TMP_Text[2];   // 攻速: X.X次/秒
    readonly TMP_Text[] playerAttack = new TMP_Text[2];     // 攻击力加成: X
    readonly TMP_Text[] playerAmount = new TMP_Text[2];     // 弹道: X（hasAmount 角色才显示）
    readonly TMP_Text[] playerSpeed = new TMP_Text[2];      // 移速: X.X
    readonly TMP_Text[] playerDefense = new TMP_Text[2];    // 防御: X
    readonly TMP_Text[] markers = new TMP_Text[2];
    readonly Image[] health = new Image[2];
    readonly Button[] characters = new Button[4];
    readonly Outline[] characterBorders = new Outline[4];
    readonly TMP_Text[] characterSelection = new TMP_Text[4];
    readonly Button[] choices = new Button[3];
    readonly Image[] choiceIcons = new Image[3];
    TMP_InputField address;
    Button host, join, ready, start;
    static readonly Color Panel = new Color(0.055f, 0.075f, 0.115f, 0.98f);
    static readonly Color Accent = new Color(0.24f, 0.74f, 0.62f);

    public void Initialize(LanSession owner, TMP_FontAsset menuFont)
    {
        session = owner;
        var catalog = Resources.Load<LanAssetCatalog>("LanAssetCatalog");
        font = catalog != null && catalog.font != null ? catalog.font : menuFont;
        var canvasObject = new GameObject("MultiplayerUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        root = canvasObject.transform;
        BuildLobby(); BuildHud(); BuildUpgrade(); BuildResult(); BuildMenu();
    }

    void BuildLobby()
    {
        lobby = Screen("LAN Lobby", true);
        var panel = Box(lobby.transform, "Lobby Panel", Vector2.zero, new Vector2(1140, 830), Panel);
        Label(panel.transform, "局域网合作", new Vector2(-360, 330), new Vector2(360, 70), 46);
        Label(panel.transform, "双人测试版 / LAN CO-OP", new Vector2(145, 330), new Vector2(400, 50), 24, Accent);
        Label(panel.transform, "独立经验 · 独立升级 · 倒地救援 · 一起击败最终首领（升级/宝箱均不暂停）", new Vector2(0, 254), new Vector2(1030, 55), 25);
        Label(panel.transform, "选择你的角色", new Vector2(0, 212), new Vector2(1000, 45), 26);
        string[] labels = { "霰弹枪", "闪电", "重斧农夫", "光束炮" };
        // 每个角色的精灵表（角色卡 Run 0 头像）。角色id与精灵表名映射：1→Farmer 0 / 2→Farmer 1 / 3→Farmer 2 / 4→Farmer 4
        string[] sheets = { "Farmer 0", "Farmer 1", "Farmer 2", "Farmer 4" };
        float spacing = 260f, left = -1.5f * spacing;
        for (int i = 0; i < characters.Length; i++)
        {
            int character = i + 1;
            var card = MakeButton(panel.transform, "Character " + character, "", new Vector2(left + i * spacing, 94), new Vector2(280, 174), () => session.SelectCharacter(character));
            characters[i] = card;
            characterBorders[i] = card.gameObject.AddComponent<Outline>();
            characterBorders[i].effectColor = Accent;
            characterBorders[i].effectDistance = new Vector2(3, -3);
            // These textures are sprite sheets. Use the exact Run 0 slice shown
            // in the character reference, rather than displaying the entire sheet.
            Sprite portrait = Array.Find(Resources.LoadAll<Sprite>("Sprites/" + sheets[i]), sprite => sprite.name == "Run 0");
            var portraitImage = Box(card.transform, "Character Portrait", new Vector2(0, 17), new Vector2(108, 120), Color.white).GetComponent<Image>();
            portraitImage.sprite = portrait;
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            Label(card.transform, labels[i], new Vector2(0, -65), new Vector2(220, 34), 23);
            characterSelection[i] = Label(card.transform, "已选", new Vector2(96, 65), new Vector2(70, 30), 18, Accent);
        }
        roster = Label(panel.transform, "", new Vector2(0, -34), new Vector2(1040, 50), 27);
        host = MakeButton(panel.transform, "Host", "创建房间", new Vector2(-360, -106), new Vector2(290, 68), session.Host);
        address = MakeInput(panel.transform, new Vector2(-12, -106), new Vector2(345, 68));
        join = MakeButton(panel.transform, "Join", "加入房间", new Vector2(360, -106), new Vector2(290, 68), () => session.Join(address.text));
        status = Label(panel.transform, "", new Vector2(0, -199), new Vector2(1020, 90), 24, Accent);
        ready = MakeButton(panel.transform, "Ready", "准备", new Vector2(-170, -300), new Vector2(290, 68), session.ToggleReady);
        start = MakeButton(panel.transform, "Start Match", "开始合作", new Vector2(170, -300), new Vector2(290, 68), session.StartMatch);
        MakeButton(panel.transform, "Close Lobby", "返回", new Vector2(463, 350), new Vector2(135, 48), session.Exit);
        Label(panel.transform, "同一局域网 · 双方使用相同版本 · TCP 7777 · 开局后暂不支持中途加入", new Vector2(0, -374), new Vector2(1060, 40), 20, new Color(0.6f, 0.66f, 0.74f));
    }

    void BuildHud()
    {
        hud = Screen("LAN HUD", false);
        for (int i = 0; i < 2; i++)
        {
            // 卡片加宽到 780×160，列宽 240px 适配较长中文 label（如 "攻击力加成"）
            var card = Box(hud.transform, "Player " + (i + 1) + " HUD", new Vector2(390, -176 - i * 160), new Vector2(780, 160), Panel);
            Anchor(card, new Vector2(0, 1));
            // 标题（卡片中心偏上）
            playerHeader[i] = Label(card.transform, "", new Vector2(0, 52), new Vector2(740, 28), 19);
            playerHeader[i].alignment = TextAlignmentOptions.Center;
            // 血条（标题下方）
            var bar = Box(card.transform, "Health Background", new Vector2(0, 10), new Vector2(700, 11), new Color(0.2f, 0.2f, 0.25f));
            health[i] = Box(bar.transform, "Health", Vector2.zero, new Vector2(700, 11), i == 0 ? Accent : new Color(0.42f, 0.65f, 1)).GetComponent<Image>();
            var rect = health[i].rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 0.5f); rect.anchoredPosition = Vector2.zero;
            // 2 行 × 3 列属性网格
            // row1 (y=-20): 生命 | 攻速/间隔 | 攻击力加成/斧头伤害
            // row2 (y=-46): 弹量/范围(°) | 移速 | 防御
            // 列 x: -250 / 0 / +250，每列宽 240px
            playerHP[i]        = Label(card.transform, "", new Vector2(-250, -20), new Vector2(240, 24), 14);
            playerCooldown[i]  = Label(card.transform, "", new Vector2(   0, -20), new Vector2(240, 24), 14);
            playerAttack[i]    = Label(card.transform, "", new Vector2(+250, -20), new Vector2(240, 24), 14);
            playerAmount[i]    = Label(card.transform, "", new Vector2(-250, -46), new Vector2(240, 24), 14);
            playerSpeed[i]     = Label(card.transform, "", new Vector2(   0, -46), new Vector2(240, 24), 14);
            playerDefense[i]   = Label(card.transform, "", new Vector2(+250, -46), new Vector2(240, 24), 14);
            // 全部左对齐 + 启用换行以防中文过长
            foreach (var lbl in new[] { playerHP[i], playerCooldown[i], playerAttack[i], playerAmount[i], playerSpeed[i], playerDefense[i] })
            {
                lbl.alignment = TextAlignmentOptions.Left;
                lbl.enableWordWrapping = true;
            }
            markers[i] = Label(hud.transform, "", Vector2.zero, new Vector2(260, 50), 21,
                i == 0 ? new Color(0.75f, 1f, 0.84f) : new Color(0.7f, 0.85f, 1f));
        }
        progress = Label(hud.transform, "", new Vector2(0, -70), new Vector2(770, 100), 29); Anchor(progress.gameObject, new Vector2(0.5f, 1));
        var help = Label(hud.transform, "WASD 移动   Shift 冲刺   鼠标瞄准 / 左键闪电   靠近倒地队友按住 E 救援", new Vector2(0, 34), new Vector2(1400, 50), 23); Anchor(help.gameObject, new Vector2(0.5f, 0));
        SettingsControls.Gear(hud.transform, () => session.MenuOpen = !session.MenuOpen);
        // 背包按钮（和单机一致，皮质背包图，P 键或点击打开）
        var invBtn = MakeButton(hud.transform, "Inventory", "", new Vector2(-350, -52), new Vector2(60, 60), ToggleInventory);
        Anchor(invBtn.gameObject, new Vector2(1, 1));
        // 替换按钮默认背景色为背包图标
        var invIcon = invBtn.GetComponent<Image>();
        if (invIcon != null)
        {
            invIcon.sprite = Managers.Resource.Load<Sprite>("Prefabs/SpriteIcon/InventoryBtn");
            invIcon.preserveAspect = true;
            invIcon.color = Color.white; // 关键：让图标原图颜色显示，不然被 MakeButton 的深色背景色盖住
            invIcon.type = Image.Type.Simple;
        }
        // 隐藏 MakeButton 创建的 Label 文字（图标按钮不需要文字）
        var invLabel = invBtn.GetComponentInChildren<TMP_Text>();
        if (invLabel != null) invLabel.gameObject.SetActive(false);
    }

    /// <summary>联机背包开关：和单机 UI_Player.ToggleInventory 逻辑一致。</summary>
    void ToggleInventory()
    {
        if (session.MenuOpen) return;
        Define.PopupUIGroup group = Define.PopupUIGroup.UI_Inventory;
        if (Managers.UI.IsPopupOpen(group))
            Managers.UI.ClosePopupUI(group);
        else
            Managers.UI.ShowPopupUI<UI_Inventory>();
    }

    void BuildUpgrade()
    {
        upgrades = Screen("Coop Upgrade", true);
        var panel = Box(upgrades.transform, "Upgrade Panel", Vector2.zero, new Vector2(1050, 450), Panel);
        upgradeTitle = Label(panel.transform, "", new Vector2(0, 142), new Vector2(990, 80), 38, Accent);
        Label(panel.transform, "战斗已暂停。双方各自选择一项强化后继续。", new Vector2(0, 60), new Vector2(990, 60), 26);
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            choices[i] = MakeButton(panel.transform, "Upgrade " + i, "", new Vector2(-330 + i * 330, -64), new Vector2(300, 120), () => session.SelectUpgrade(index));
            // 在按钮内左侧加一个图标 Image 组件（用于显示道具/武器图标）
            var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(choices[i].transform, false);
            var iconRT = (RectTransform)iconObj.transform;
            iconRT.anchorMin = iconRT.anchorMax = iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = new Vector2(-95, 0);
            iconRT.sizeDelta = new Vector2(72, 72);
            choiceIcons[i] = iconObj.GetComponent<Image>();
            choiceIcons[i].raycastTarget = false;
            // 把按钮自带的 Label 文字右移，避开左侧 Icon（Icon 区域宽 72，间距 8，共 80）
            var btnLabel = choices[i].GetComponentInChildren<TMP_Text>();
            if (btnLabel != null)
            {
                var lr = btnLabel.rectTransform;
                lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f);
                lr.pivot = new Vector2(0, 0.5f);
                lr.anchoredPosition = new Vector2(-20, 0);   // 文字左边缘对齐到按钮中心偏左 20px
                lr.sizeDelta = new Vector2(200, 120);          // 缩小宽度，只占右半部分
                btnLabel.alignment = TextAlignmentOptions.MidlineLeft;
            }
        }
    }

    void BuildResult()
    {
        result = Screen("Coop Result", true);
        var panel = Box(result.transform, "Result Panel", Vector2.zero, new Vector2(1030, 500), Panel);
        resultTitle = Label(panel.transform, "", new Vector2(0, 145), new Vector2(930, 90), 48, Accent);
        resultText = Label(panel.transform, "", new Vector2(0, 5), new Vector2(930, 170), 28);
        MakeButton(panel.transform, "Return Main Menu", "返回主菜单", new Vector2(0, -156), new Vector2(340, 72), session.Exit);
    }

    void BuildMenu()
    {
        menu = Screen("Coop Menu", true);
        SettingsControls.Build(menu.transform, font, () => session.MenuOpen = false, session.Exit, true);
    }

    void Update()
    {
        if (session == null || lobby == null) return;
        // 联机背包 P 键（和单机一致）
        if (Input.GetKeyDown(KeyCode.P) && session.InMatch)
            ToggleInventory();
        var state = session.State;
        bool ended = state.phase == LanPhase.Victory || state.phase == LanPhase.Defeat || state.phase == LanPhase.Disconnected;
        lobby.SetActive(state.phase == LanPhase.Lobby);
        hud.SetActive(session.InMatch && !ended);
        // 升级弹窗 per-player：自己的 upgradePending 才弹，不暂停游戏
        upgrades.SetActive(session.InMatch && !ended && state.players[session.LocalSlot].upgradePending);
        result.SetActive(ended);
        menu.SetActive(session.MenuOpen && state.phase == LanPhase.Playing);
        status.text = session.Status;
        int slot = session.HasConnection ? session.LocalSlot : 0;
        for (int i = 0; i < characters.Length; i++)
        {
            bool selected = state.players[slot].character == i + 1;
            characters[i].image.color = selected ? new Color(0.12f, 0.30f, 0.28f) : new Color(0.14f, 0.18f, 0.25f);
            characterBorders[i].enabled = selected;
            characterSelection[i].gameObject.SetActive(selected);
            characters[i].interactable = !state.players[slot].ready && (!session.HasConnection || session.IsHost || session.PeerVerified);
        }
        roster.text = session.HasConnection
            ? $"房主：幸存者 {state.players[0].character} · {(state.players[0].ready ? "已准备" : "未准备")}     队友：{(session.PeerVerified ? "幸存者 " + state.players[1].character + (state.players[1].ready ? " · 已准备" : " · 未准备") : "等待加入")}" : "创建房间成为房主，或输入房主 IP 加入";
        host.interactable = join.interactable = address.interactable = !session.HasConnection;
        ready.interactable = session.PeerVerified;
        ready.GetComponentInChildren<TMP_Text>().text = state.players[slot].ready ? "取消准备" : "准备";
        start.interactable = session.IsHost && session.PeerVerified && state.players[0].ready && state.players[1].ready;
        for (int i = 0; i < 2; i++)
        {
            var player = state.players[i];
            string header = $"{(i == session.LocalSlot ? "你" : "队友")} · 幸存者 {player.character}";
            // 先把所有属性 label 隐藏，按需显示
            SetAttrVisible(i, false);
            if (player.down)
            {
                playerHeader[i].text = header + $" — 倒地 · 救援 {player.rescue:F1}/3秒";
            }
            else if (!session.InMatch || player.maxHp == 0)
            {
                // Lobby/Loading 阶段：属性还没初始化，只显示角色和准备状态
                playerHeader[i].text = header + (player.ready ? " · 已准备" : " · 未准备");
            }
            else
            {
                // Playing/Upgrade 阶段：按角色类型显示不同 label，和单机 UI_Player 完全对齐
                playerHeader[i].text = header;
                SetAttrVisible(i, true);
                // 公共属性（所有角色都显示）
                playerHP[i].text      = $"生命: {player.hp}/{player.maxHp}";
                playerSpeed[i].text   = $"移速: {player.moveSpeed:F1}";
                playerDefense[i].text = $"防御: {player.defense}";
                // 角色差异属性
                if (player.isAxe)
                {
                    // Axe 专属：斧头伤害 / 间隔(秒) / 范围
                    playerAttack[i].text   = $"斧头伤害: {player.axeDamage}";
                    playerCooldown[i].text = $"间隔: {player.axeCooldown:F2}秒";
                    playerAmount[i].text   = $"范围: {player.axeRange:F1}/{player.axeAngle:0}°";
                    playerAmount[i].gameObject.SetActive(true);
                }
                else if (player.isLaser)
                {
                    // 光束炮（角色4）：伤害 / 攻速 / 范围，和单机 UI_Player 对齐
                    playerAttack[i].text   = $"伤害: {player.weaponDamage}";
                    playerCooldown[i].text = $"攻速: {player.attackPerSecond:F1}次/秒";
                    playerAmount[i].text   = $"范围: {player.laserWidth:F1}";
                    playerAmount[i].gameObject.SetActive(true);
                }
                else
                {
                    // 通用角色：伤害 / 攻速 / 弹量(仅 hasAmount)
                    playerAttack[i].text   = $"伤害: {player.weaponDamage}";
                    playerCooldown[i].text = $"攻速: {player.attackPerSecond:F1}次/秒";
                    playerAmount[i].text   = $"弹量: {player.amount}";
                    playerAmount[i].gameObject.SetActive(player.hasAmount);
                }
            }
            health[i].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 700 * (player.maxHp > 0 ? Mathf.Clamp01((float)player.hp / player.maxHp) : 0));
            if (Camera.main != null && session.LocalObject != null)
            {
                // Opposite sides keep labels readable while standing together to revive.
                float labelOffset = i == session.LocalSlot ? -1.1f : 1.3f;
                Vector3 position = Camera.main.WorldToViewportPoint(new Vector3(player.x, player.y + labelOffset, 0));
                bool outside = position.x < .08f || position.x > .92f || position.y < .12f || position.y > .86f;
                var size = ((RectTransform)root).rect.size;
                markers[i].rectTransform.anchoredPosition = new Vector2((Mathf.Clamp(position.x, .08f, .92f) - .5f) * size.x,
                    (Mathf.Clamp(position.y, .12f, .86f) - .5f) * size.y);
                float distance = Vector2.Distance(session.LocalObject.transform.position, new Vector2(player.x, player.y));
                markers[i].text = (i == session.LocalSlot ? "你" : "队友") + (player.down ? " · 倒地 [E 救援]" : "")
                    + (outside ? $" · {distance:F0}米" : "");
            }
        }
        // 进度条：时间 + 自己独立的等级/经验，不再是团队共享
        var me = state.players[session.LocalSlot];
        string myExpBar = me.maxExp > 0 ? $"{me.exp}/{me.maxExp}" : "0";
        progress.text = $"{(int)state.time / 60:00}:{(int)state.time % 60:00}   等级 {me.level}   经验 {myExpBar}\n"
            + (state.phase == LanPhase.Loading ? "正在等待双方加载…" : state.paused && state.phase == LanPhase.Playing ? "首领时停"
                : state.bossMaxHp > 0 ? $"最终首领   {state.bossHp}/{state.bossMaxHp}" : "击败最终首领，与队友一起通关");
        upgradeTitle.text = me.chosen ? "已选择" : $"幸存者 {me.character} 升级 · 选择你的强化";
        for (int i = 0; i < 3; i++)
        {
            choices[i].interactable = !me.chosen && i < me.choices.Length;
            choices[i].GetComponentInChildren<TMP_Text>().text = i < me.choices.Length ? me.choices[i] : "";
            // 根据原始 offer key 加载对应道具/武器图标
            if (choiceIcons[i] != null)
            {
                if (session.offers != null && session.offers.Length > session.LocalSlot
                    && i < session.offers[session.LocalSlot].Count)
                {
                    string rawKey = session.offers[session.LocalSlot][i];
                    string iconKey = rawKey.StartsWith("weapon:") ? rawKey.Substring(7) : rawKey;
                    choiceIcons[i].enabled = true;
                    var sprite = Managers.Resource.Load<Sprite>($"Prefabs/SpriteIcon/{iconKey}");
                    choiceIcons[i].sprite = sprite;
                    choiceIcons[i].preserveAspect = true;
                }
                else choiceIcons[i].enabled = false;
            }
        }
        resultTitle.text = state.phase == LanPhase.Victory ? "合作通关！" : state.phase == LanPhase.Defeat ? "全队倒地" : "连接已结束";
        resultText.text = state.phase == LanPhase.Disconnected ? session.Status
            : $"共同坚持了 {(int)state.time / 60:00}:{(int)state.time % 60:00}。\n" + (state.phase == LanPhase.Victory ? "最终首领已被击败。" : "两位玩家均已倒地，本次合作结束。");
    }

    GameObject Screen(string name, bool block)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(root, false);
        var r = (RectTransform)go.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        if (block) { var image = go.AddComponent<Image>(); image.color = new Color(0, 0, 0, 0.76f); }
        go.SetActive(false); return go;
    }
    static void Anchor(GameObject go, Vector2 anchor) { var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = anchor; }
    GameObject Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = size; rect.anchoredPosition = position;
        go.GetComponent<Image>().color = color; return go;
    }
    TMP_Text Label(Transform parent, string text, Vector2 position, Vector2 size, float fontSize, Color? color = null)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>(); label.font = font; label.text = text; label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center; label.color = color ?? new Color(0.91f, 0.94f, 0.97f);
        label.raycastTarget = false; label.enableWordWrapping = true;
        var r = label.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f); r.sizeDelta = size; r.anchoredPosition = position;
        return label;
    }
    Button MakeButton(Transform parent, string name, string text, Vector2 position, Vector2 size, Action action)
    {
        var go = Box(parent, name, position, size, new Color(0.14f, 0.18f, 0.25f));
        var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); button.onClick.AddListener(() => action());
        var colors = button.colors; colors.highlightedColor = new Color(0.7f, 1, 0.9f); colors.disabledColor = new Color(0.43f, 0.43f, 0.43f); button.colors = colors;
        Label(go.transform, text, Vector2.zero, size - new Vector2(14, 8), 27);
        return button;
    }
    TMP_InputField MakeInput(Transform parent, Vector2 position, Vector2 size)
    {
        var go = Box(parent, "Host IP", position, size, new Color(0.03f, 0.045f, 0.07f));
        var input = go.AddComponent<TMP_InputField>();
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); viewport.transform.SetParent(go.transform, false);
        var rect = (RectTransform)viewport.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(15, 6); rect.offsetMax = new Vector2(-15, -6);
        var label = Label(viewport.transform, "", Vector2.zero, size - new Vector2(30, 12), 28);
        var placeholder = Label(viewport.transform, "输入房主局域网 IP", Vector2.zero, size - new Vector2(30, 12), 23, new Color(0.48f, 0.54f, 0.62f));
        input.textViewport = rect; input.textComponent = (TextMeshProUGUI)label; input.placeholder = placeholder;
        input.characterLimit = 15; input.lineType = TMP_InputField.LineType.SingleLine;
        input.onValidateInput = (text, index, ch) => (ch >= '0' && ch <= '9') || ch == '.' ? ch : '\0';
        return input;
    }

    /// <summary>批量切换 6 个属性 label 可见性（Lobby/倒地时隐藏，Playing 时显示）。</summary>
    void SetAttrVisible(int i, bool visible)
    {
        playerHP[i].gameObject.SetActive(visible);
        playerCooldown[i].gameObject.SetActive(visible);
        playerAttack[i].gameObject.SetActive(visible);
        playerAmount[i].gameObject.SetActive(visible);
        playerSpeed[i].gameObject.SetActive(visible);
        playerDefense[i].gameObject.SetActive(visible);
    }

}
```

## Assets/Scripts/Multiplayer/LanProtocol.cs

```csharp
using System;
using UnityEngine;

[Serializable]
public class LanMessage
{
    public string kind;
    public string version;
    public int character;
    public bool ready;
    public int sequence;
    public float x, y, aimX, aimY;
    public int dash, fire;
    public bool revive;
    public int choice, round;
}

public enum LanPhase { Lobby, Loading, Playing, Upgrade, Victory, Defeat, Disconnected }

[Serializable]
public class LanState
{
    public LanPhase phase;
    public float time;
    public int level = 1;
    public long exp, maxExp = 10;
    public bool paused;
    public int round;
    public int bossHp, bossMaxHp;
    public LanPlayerState[] players = { new LanPlayerState(), new LanPlayerState() };
    public LanVisualState[] visuals = new LanVisualState[0];
}

[Serializable]
public class LanPlayerState
{
    public int character = 1;
    public bool ready, down, chosen;
    // 每玩家独立升级弹窗待打开（不暂停游戏，per-player）
    public bool upgradePending;
    public int hp, maxHp;
    // 独立经验/等级（不再队伍共享）
    public long exp, maxExp;
    public int level;
    // 属性加成（和单机右上角格式对齐）
    public int damage, defense, cooldown, amount;
    public float moveSpeed, attackPerSecond;
    public bool hasAmount;
    // 起始武器实际伤害（属性面板"伤害"槽位显示，含攻击力加成重算值）
    public int weaponDamage;
    // 激光炮（character == 4）角色专属
    public bool isLaser;
    public float laserWidth;
    // Axe 角色专属（character == 3）
    public bool isAxe;
    public float axeRange, axeAngle, axeCooldown;
    public int axeDamage;
    public float x, y, rescue;
    public string[] choices = new string[0];
    public string weapons;
}

[Serializable]
public struct LanVisualState
{
    public int id, sprite, layer, order;
    public float x, y, z, rotation, sx, sy;
    public bool flipX, flipY;
    public Color color;
}
```

## Assets/Scripts/Multiplayer/LanSession.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>Separate cooperative match rules. Existing combat runs once, on the host.
/// The guest scene contains a camera target, terrain and passive world renderers.</summary>
[DefaultExecutionOrder(-100)]
public sealed class LanSession : MonoBehaviour
{
    public static LanSession Instance { get; private set; }
    public static bool Active => Instance != null;
    public bool IsHost { get; private set; }
    public int LocalSlot => IsHost ? 0 : 1;
    public LanState State { get; private set; } = new LanState();
    public PlayerController[] Players { get; } = new PlayerController[2];
    public GameObject LocalObject { get; private set; }
    public string Status { get; private set; } = "选择角色，然后创建或加入房间";
    public bool HasConnection => transport != null;
    public bool PeerVerified { get; private set; }
    public bool InMatch => State.phase != LanPhase.Lobby && State.phase != LanPhase.Disconnected;
    public bool CanSimulate => IsHost && State.phase == LanPhase.Playing && !Managers.gameStop;
    LanTransport transport;
    LanWorldView world;
    LanOverlay overlay;
    LanMessage localInput = new LanMessage { kind = "input" };
    LanMessage remoteInput = new LanMessage { kind = "input" };
    int consumedDash, consumedFire;
    bool helloSent; internal bool hostLoaded, guestLoaded; bool exiting;
    float lastReceive, lastInput, nextSend, nextRecycle;
    // 经验/等级已迁移到各玩家 PlayerStat 独立管理，不再有队伍共享池
    // 每个玩家自己满经验时 BeginUpgradeFor 只给自己开升级弹窗
    internal readonly List<string>[] offers = { new List<string>(), new List<string>() };
    // 每个玩家的升级弹窗是否待打开（per-player 触发，不暂停游戏）
    internal readonly bool[] upgradePending = { false, false };
    internal readonly bool[] upgradeChosen = { false, false };
    public bool MenuOpen { get; set; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    internal Func<LanMessage> TestInputSource;
#endif

    public static void Open(TMP_FontAsset font)
    {
        if (Active) return;
        var go = new GameObject("LAN Cooperative Session");
        DontDestroyOnLoad(go);
        var session = go.AddComponent<LanSession>();
        session.overlay = go.AddComponent<LanOverlay>();
        session.overlay.Initialize(session, font);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器调试用：一键跳过 Lobby、PeerVerified 和玩家2 ready，直接进入联机战斗场景。
    /// 打包时 #if UNITY_EDITOR 自动屏蔽，不影响 Release 产物。
    /// 在 Unity 菜单 Tools → LAN Debug → 一键进入联机战斗 触发。
    /// </summary>
    public static void Editor_QuickStartTest()
    {
        // 确保 LanSession 存在（绕过 Open 里的 DontDestroyOnLoad —— MCP script-execute 视为 Editor 代码）
        if (Instance == null)
        {
            var go = new GameObject("LAN Cooperative Session");
            UnityEngine.Object.DontDestroyOnLoad(go); // 立即标记，防止 LoadBattle 切场景时被销毁
            Instance = go.AddComponent<LanSession>();
            // 手动初始化 overlay（跳过 Open 里的 DontDestroyOnLoad）
            var font = Resources.Load<TMP_FontAsset>("simhei SDF");
            Instance.overlay = go.AddComponent<LanOverlay>();
            Instance.overlay.Initialize(Instance, font);
        }

        // 清理旧状态
        Instance.IsHost = false;
        Instance.Players[0] = null; Instance.Players[1] = null;

        Instance.Host();

        // 绕过 PeerVerified 门槛（Editor-only）
        Instance.PeerVerified = true;

        // 自动完成 Lobby：选角色 + ready
        Instance.SelectCharacter(1);
        Instance.ToggleReady();

        // 补全玩家2（默认角色2）
        if (!IsCharacterAvailable(Instance.State.players[1].character))
            Instance.State.players[1].character = 2;
        Instance.State.players[1].ready = true;

        // 手动把 guestLoaded 设为 true（没有真实 Guest，不让 TryBegin 卡住）
        Instance.guestLoaded = true;

        // 直接开始
        Instance.StartMatch();
    }
#endif

    void Awake() { Instance = this; DontDestroyOnLoad(gameObject); Application.runInBackground = true; }

    public void Host()
    {
        if (HasConnection) return;
        if (!Prepare()) return;
        IsHost = true;
        transport = new LanTransport(); transport.Host();
        lastReceive = Time.realtimeSinceStartup;
        Status = "等待队友加入 · " + LocalAddresses() + " : " + LanTransport.Port;
    }

    public void Join(string address)
    {
        if (HasConnection) return;
        if (!IPAddress.TryParse(address.Trim(), out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        { Status = "请输入有效的 IPv4 地址，例如 192.168.1.10"; return; }
        if (!Prepare()) return;
        IsHost = false;
        State.players[1].character = State.players[0].character;
        transport = new LanTransport(); transport.Join(ip.ToString());
        Status = "正在连接房主…"; lastReceive = Time.realtimeSinceStartup;
    }

    bool Prepare()
    {
        try { world = new LanWorldView(); return true; }
        catch (Exception ex) { Status = "无法准备联机资源：" + ex.Message; return false; }
    }

    public static string LocalAddresses()
    {
        try { return string.Join(" / ", Dns.GetHostAddresses(Dns.GetHostName())
            .Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(x)).Select(x => x.ToString())); }
        catch { return "请在系统网络设置中查看 IPv4"; }
    }

    public static bool IsCharacterAvailable(int character)
        => Managers.Data.PlayerData.ContainsKey(character);

    public void SelectCharacter(int character)
    {
        if (State.phase != LanPhase.Lobby || !IsCharacterAvailable(character)) return;
        int slot = HasConnection ? LocalSlot : 0;
        State.players[slot].character = character; State.players[slot].ready = false;
        if (HasConnection && !IsHost && PeerVerified) SendLobby();
    }

    public void ToggleReady()
    {
        if (!PeerVerified || State.phase != LanPhase.Lobby) return;
        State.players[LocalSlot].ready = !State.players[LocalSlot].ready;
        if (!IsHost) SendLobby();
    }

    void SendLobby() => Send(new LanMessage { kind = "lobby", character = State.players[1].character, ready = State.players[1].ready });
    void Send(LanMessage message) => transport?.Send(JsonUtility.ToJson(message));

    public void StartMatch()
    {
        if (!IsHost || !PeerVerified || State.phase != LanPhase.Lobby || !State.players.All(x => x.ready && IsCharacterAvailable(x.character))) return;
        State.phase = LanPhase.Loading; Status = "等待双方加载战场…";
        Send(new LanMessage { kind = "start" }); LoadBattle();
    }

    void LoadBattle()
    {
        MenuOpen = false;
        Managers.UI.Clear(); Managers.ResetGameTime(); Managers.GamePause();
        SceneManager.LoadScene("GameScene");
    }

    public void InitializeBattle()
    {
        Managers.Game._OnSpawnEvent = null;
        if (IsHost)
        {
            for (int i = 0; i < 2; i++)
            {
                var go = Managers.Game.Spawn(Define.WorldObject.Player, "Player/Player");
                Players[i] = go.GetComponent<PlayerController>();
                if (i == 0) LocalObject = go;
                go.name = "Coop Player " + (i + 1);
                go.transform.position = new Vector3(i == 0 ? -1.5f : 1.5f, 0, 0);
                Players[i].Init(Managers.Data.PlayerData[State.players[i].character]);
                foreach (var spawner in go.GetComponentsInChildren<Spawner>()) spawner.enabled = i == 0;
            }
            foreach (var a in Players[0].GetComponents<Collider2D>())
                foreach (var b in Players[1].GetComponents<Collider2D>()) Physics2D.IgnoreCollision(a, b);
            hostLoaded = true;
        }
        else LocalObject = new GameObject("Local camera target");
        Camera.main.gameObject.GetOrAddComponent<CameraController>().SetPlayer(LocalObject);
        var terrain = Managers.Resource.Instantiate("Content/Grid");
        if (IsHost)
        {
            // Keep authoritative terrain around BOTH players. The second copy only
            // supplies colliders; its periodic tile layout matches the visible grid.
            var remoteTerrain = Managers.Resource.Instantiate("Content/Grid");
            remoteTerrain.name = "Remote Player Terrain";
            remoteTerrain.GetComponent<WorldScrolling>().SetFollowTarget(Players[1].transform);
            foreach (var renderer in remoteTerrain.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        }
        else foreach (var collider in terrain.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        Managers.Sound.Play(Define.BGMs.BGM_01.ToString(), Define.Sound.Bgm);
        if (!IsHost) Send(new LanMessage { kind = "loaded" });
        TryBegin();
    }

    void TryBegin()
    {
        if (!IsHost || !hostLoaded || !guestLoaded || State.phase != LanPhase.Loading) return;
        Managers.ResetGameTime(); State.phase = LanPhase.Playing; Managers.GamePlay();
        Status = "WASD 移动 · Shift 冲刺 · 鼠标瞄准/左键落雷 · 靠近队友按住 E 救援";
    }

    void Update()
    {
        if (exiting || transport == null) return;
        if (transport.Error != null && State.phase != LanPhase.Disconnected
            && State.phase != LanPhase.Victory && State.phase != LanPhase.Defeat) Disconnect(transport.Error);
        if (State.phase == LanPhase.Disconnected) return;
        if (transport.Connected && !helloSent)
        {
            helloSent = true; lastReceive = Time.realtimeSinceStartup;
            if (!IsHost) Send(new LanMessage { kind = "hello", version = world.Fingerprint, character = State.players[1].character });
        }
        try
        {
            while (transport.TryReceive(out var json)) { lastReceive = Time.realtimeSinceStartup; Handle(JsonUtility.FromJson<LanMessage>(json)); }
            string state = transport.TakeState();
            if (state != null)
            {
                lastReceive = Time.realtimeSinceStartup;
                if (IsHost) Handle(JsonUtility.FromJson<LanMessage>(state));
                else ReceiveState(JsonUtility.FromJson<LanState>(state));
            }
        }
        catch (Exception ex) { Disconnect("联机数据异常：" + ex.Message); }
        if (transport.Connected && Time.realtimeSinceStartup - lastReceive > 15f) Disconnect("队友连接超时，请返回菜单重新连接。");
        if (State.phase == LanPhase.Disconnected) return;
        if (hostLoaded || (!IsHost && LocalObject != null))
        {
            ReadInput();
            if (IsHost && CanSimulate) UpdateRescue();
            if (!IsHost) world.Interpolate(Time.unscaledDeltaTime);
            if (IsHost && Time.unscaledTime >= nextRecycle && CanSimulate)
            { nextRecycle = Time.unscaledTime + 1f; RecycleDistantEnemies(); }
        }
    }

    void LateUpdate()
    {
        if (transport == null || Time.unscaledTime < nextSend || State.phase == LanPhase.Disconnected) return;
        // Host 自己是权威服务器，不需要 Guest Connected 也能同步状态
        // Guest 则需要 transport.Connected 和 PeerVerified
        if (!IsHost && (!transport.Connected || !PeerVerified)) return;
        nextSend = Time.unscaledTime + (IsHost ? 0.05f : 0.033f);
        if (IsHost)
        {
            RefreshState();
            State.visuals = hostLoaded ? world.Capture() : new LanVisualState[0];
            transport.Send(JsonUtility.ToJson(State), true);
        }
        else
        {
            localInput.sequence++;
            transport.Send(JsonUtility.ToJson(localInput), true);
        }
    }

    void Handle(LanMessage message)
    {
        if (message == null) throw new InvalidOperationException("Empty message");
        if (IsHost)
        {
            if (message.kind == "hello" && !PeerVerified && State.phase == LanPhase.Lobby)
            {
                if (message.version != world.Fingerprint || !IsCharacterAvailable(message.character))
                { Disconnect("双方游戏版本或资源不一致，请使用同一份构建。"); return; }
                PeerVerified = true; State.players[1].character = message.character;
                Send(new LanMessage { kind = "welcome", version = world.Fingerprint });
                Status = "队友已加入。双方准备后，由房主开始。";
            }
            else if (PeerVerified)
            {
                switch (message.kind)
                {
                    case "lobby":
                        if (State.phase == LanPhase.Lobby && IsCharacterAvailable(message.character))
                        { State.players[1].character = message.character; State.players[1].ready = message.ready; }
                        break;
                    case "loaded": guestLoaded = true; TryBegin(); break;
                    case "input":
                        if (message.sequence <= remoteInput.sequence || !ValidInput(message)) break;
                        remoteInput = message; lastInput = Time.unscaledTime; break;
                    case "choose": Choose(1, message.choice); break;
                }
            }
        }
        else
        {
            if (message.kind == "welcome" && message.version == world.Fingerprint)
            { PeerVerified = true; Status = "连接成功。选择角色并准备，等待房主开始。"; }
            if (message.kind == "start" && PeerVerified && State.phase == LanPhase.Lobby)
            { State.phase = LanPhase.Loading; LoadBattle(); }
        }
    }

    static bool ValidInput(LanMessage m) => Finite(m.x) && Finite(m.y) && Finite(m.aimX) && Finite(m.aimY) && m.dash >= 0 && m.fire >= 0;
    static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f) && Mathf.Abs(f) < 1000000f;

    void ReadInput()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (TestInputSource != null) { localInput = TestInputSource(); return; }
#endif
        if (Input.GetKeyDown(KeyCode.Escape)) MenuOpen = !MenuOpen;
        bool accepts = State.phase == LanPhase.Playing && !MenuOpen && Application.isFocused;
        localInput.x = accepts ? Input.GetAxisRaw("Horizontal") : 0;
        localInput.y = accepts ? Input.GetAxisRaw("Vertical") : 0;
        Vector3 aim = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        localInput.aimX = aim.x; localInput.aimY = aim.y;
        if (accepts && Input.GetKeyDown(KeyCode.LeftShift)) localInput.dash++;
        if (accepts && Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) localInput.fire++;
        localInput.revive = accepts && Input.GetKey(KeyCode.E);
    }

    int Slot(PlayerController player) => Players[1] == player ? 1 : 0;
    LanMessage InputFor(int slot) => slot == 0 ? localInput : remoteInput;
    public Vector2 Movement(PlayerController player)
    {
        int slot = Slot(player);
        if (!CanSimulate || State.players[slot].down || (slot == 1 && Time.unscaledTime - lastInput > 0.5f)) return Vector2.zero;
        var input = InputFor(slot); return Vector2.ClampMagnitude(new Vector2(input.x, input.y), 1f);
    }
    public bool DashRequested(PlayerController player)
    {
        if (Slot(player) == 0) return CanSimulate && !MenuOpen && Application.isFocused && Input.GetKeyDown(KeyCode.LeftShift);
        bool request = remoteInput.dash > consumedDash; consumedDash = remoteInput.dash;
        return request && CanSimulate && Time.unscaledTime - lastInput < 0.5f;
    }
    public bool FireRequested(PlayerController player)
    {
        if (Slot(player) == 0) return CanSimulate && !MenuOpen && Application.isFocused && Input.GetMouseButtonDown(0)
            && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        bool request = remoteInput.fire > consumedFire; consumedFire = remoteInput.fire;
        return request && CanSimulate && Time.unscaledTime - lastInput < 0.5f;
    }
    public Vector3 Aim(PlayerController player)
    {
        var input = InputFor(Slot(player));
        Vector2 position = player.transform.position;
        Vector2 offset = new Vector2(input.aimX, input.aimY) - position;
        return position + Vector2.ClampMagnitude(offset, 25f);
    }
    public bool IsDown(PlayerController player) => State.players[Slot(player)].down;

    public Rigidbody2D NearestTarget(Vector2 position)
    {
        PlayerController nearest = Players.Where((p, i) => p != null && !State.players[i].down)
            .OrderBy(p => ((Vector2)p.transform.position - position).sqrMagnitude).FirstOrDefault();
        return nearest != null ? nearest.GetComponent<Rigidbody2D>() : null;
    }
    public Vector3 SpawnPosition()
    {
        var alive = Players.Where((p, i) => p != null && !State.players[i].down).ToArray();
        if (alive.Length == 0) return Vector3.zero;
        for (int i = 0; i < 12; i++)
        {
            Vector3 point = alive[UnityEngine.Random.Range(0, alive.Length)].transform.position
                + (Vector3)(UnityEngine.Random.insideUnitCircle.normalized * 19f);
            if (alive.All(p => Vector3.Distance(point, p.transform.position) > 13f)) return point;
        }
        return alive[0].transform.position + Vector3.up * 24f;
    }
    void RecycleDistantEnemies()
    {
        foreach (var enemy in FindObjectsOfType<EnemyController>())
            if (Players.All(p => Vector3.Distance(p.transform.position, enemy.transform.position) > 35f)) enemy.transform.position = SpawnPosition();
    }

    // 经验由 PlayerStat 独立管理，AddExperience 保留但不再被 Exp_Item 调用
    // （Exp_Item 现在直接 player.Exp +=，触发 OnLevelUp → BeginUpgradeFor）
    public void AddExperience(long amount)
    {
        // 废弃：保留签名兼容，实际加在 PlayerStat.Exp 上
    }

    /// <summary>per-player 升级触发：经验满时由 PlayerStat.OnLevelUp 调用。不暂停游戏，只给该玩家弹升级 UI。</summary>
    public void BeginUpgradeFor(PlayerStat stat)
    {
        if (!IsHost) return;
        int slot = Slot(stat.GetComponent<PlayerController>());
        if (slot < 0) return;
        if (State.phase == LanPhase.Victory || State.phase == LanPhase.Defeat) return;

        offers[slot] = BuildOffers(stat);
        upgradePending[slot] = true;
        upgradeChosen[slot] = false;
        State.players[slot].choices = offers[slot].Select(DescribeOffer).ToArray();
        State.players[slot].chosen = false;

        // 不暂停游戏！游戏继续运行，只是该玩家有个升级浮层
        RefreshState();
    }

    public static List<string> BuildOffers(PlayerStat stat)
    {
        // 基础属性池（和 BuildBoxPool 一致）
        var pool = new List<string> { "MaxHP", "Damage", "Defense", "Cooldown" };
        if (stat.CanUpgradeMoveSpeed()) pool.Add("MoveSpeed");
        if (stat.HasAmount) pool.Add("Amount");
        if (stat._playerStartWeapon == Define.Weapons.Axe)
        {
            foreach (var name in new[] { "AxeRangeUp", "AxeAngleUp", "AxeDamageUp" })
                if (stat.CanUpgradeAxe(name)) pool.Add(name);
        }
        if (stat._playerStartWeapon == Define.Weapons.LaserCannon)
        {
            foreach (var name in new[] { "LaserRangeUp", "LaserDamageUp" })
                if (stat.CanUpgradeLaser(name)) pool.Add(name);
        }
        // 武器池（和 BuildBoxPool 完全一致的过滤逻辑）
        foreach (Define.Weapons weapon in Enum.GetValues(typeof(Define.Weapons)))
        {
            // 角色专属起始武器（>=100）只有对应起始武器的角色才能刷到
            bool isStartWeapon = (int)weapon >= 100;
            if (isStartWeapon && stat._playerStartWeapon != weapon) continue;
            // 通用武器 + 角色自己的专属起始武器：已拥有<5级可升级，未拥有<4把可新增
            bool owned = stat.GetWeaponDict().TryGetValue(weapon, out int current);
            if ((owned && current < 5) || (!owned && stat.GetWeaponDict().Count < 4))
                pool.Add("weapon:" + weapon);
        }
        // 洗牌取 3 个
        for (int i = pool.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); var value = pool[i]; pool[i] = pool[j]; pool[j] = value; }
        return pool.Take(3).ToList();
    }
    public static string DescribeOffer(string value) => value.StartsWith("weapon:")
        ? UILocalization.GetName(value.Substring(7)) + " · 获得 / 升级" : UILocalization.GetDesc(value);
    public void SelectUpgrade(int index)
    {
        if (IsHost) Choose(LocalSlot, index);
        else Send(new LanMessage { kind = "choose", choice = index });
    }
    public void Choose(int slot, int index)
    {
        if (!IsHost || slot < 0 || slot > 1
            || !upgradePending[slot] || upgradeChosen[slot]
            || index < 0 || index >= offers[slot].Count) return;
        ApplyOffer(Players[slot].GetComponent<PlayerStat>(), offers[slot][index]);
        upgradeChosen[slot] = true;
        upgradePending[slot] = false;
        State.players[slot].chosen = true;
        // 不恢复时间流速——升级前就没暂停！
        RefreshState();
    }
    public static bool ApplyOffer(PlayerStat stat, string offer)
    {
        if (offer.StartsWith("weapon:"))
        {
            var wp = (Define.Weapons)Enum.Parse(typeof(Define.Weapons), offer.Substring(7));
            stat.AddOrSetWeaponDict(wp, 1);
            stat.RecordObtain("Weapon", wp.ToString());
            return true;
        }
        // 角色专属道具防护
        switch (offer)
        {
            case "Amount":
                if (!stat.HasAmount) return false;
                stat.Amount++; break;
            case "AxeRangeUp":
            case "AxeAngleUp":
            case "AxeDamageUp":
                if (stat._playerStartWeapon != Define.Weapons.Axe) return false;
                if (!stat.CanUpgradeAxe(offer)) return false;  // 防御层：满 3 级后拒绝
                stat.ApplyAxeUpgrade(offer); break;
            case "LaserRangeUp":
            case "LaserDamageUp":
                if (stat._playerStartWeapon != Define.Weapons.LaserCannon) return false;
                if (!stat.CanUpgradeLaser(offer)) return false;
                stat.ApplyLaserUpgrade(offer); break;
            case "MaxHP": stat.MaxHP += 10; if (stat.HP > 0) stat.HP = Mathf.Min(stat.HP + 15, stat.MaxHP); break;
            case "Damage": stat.Damage += 10; break;
            case "Defense": stat.Defense++; break;
            case "Cooldown": stat.Cooldown += 10; break; // 所有角色都能获得攻速加成！
            case "MoveSpeed": stat.ApplyMoveSpeedUpgrade(); break;
            default: return false;
        }
        stat.RecordObtain("Stat", offer);
        stat.RefreshWeapons();
        return true;
    }
    /// <summary>
    /// 宝箱专用池（单机联机共用）。
    /// 属性：MaxHP/Damage/Defense/Cooldown/MoveSpeed（所有角色）
    ///       + Amount（仅 HasAmount=true = Shotgun）
    ///       + AxeRangeUp/AngleUp/DamageUp（仅起始武器 Axe 且未满级）
    /// 武器：通用 Knife/Fireball/Spin/Poison（所有角色）
    ///       + 角色专属起始武器（Shotgun 给角色1、Lightning 给角色2、Axe 给角色3）
    ///       武器过滤和 BuildOffers 一致：已拥有<5级可升级，未拥有<4把可新增
    /// </summary>
    public static List<string> BuildBoxPool(PlayerStat stat)
    {
        var pool = new List<string> { "MaxHP", "Damage", "Defense", "Cooldown" };
        if (stat.CanUpgradeMoveSpeed()) pool.Add("MoveSpeed");
        if (stat.HasAmount) pool.Add("Amount");
        if (stat._playerStartWeapon == Define.Weapons.Axe)
        {
            foreach (var name in new[] { "AxeRangeUp", "AxeAngleUp", "AxeDamageUp" })
                if (stat.CanUpgradeAxe(name)) pool.Add(name);
        }
        if (stat._playerStartWeapon == Define.Weapons.LaserCannon)
        {
            foreach (var name in new[] { "LaserRangeUp", "LaserDamageUp" })
                if (stat.CanUpgradeLaser(name)) pool.Add(name);
        }
        // 武器：通用 + 角色专属起始武器
        foreach (Define.Weapons weapon in Enum.GetValues(typeof(Define.Weapons)))
        {
            // 角色专属起始武器：只有起始武器等于它的角色才能从宝箱刷到
            bool isStartWeapon = (int)weapon >= 100;
            if (isStartWeapon && stat._playerStartWeapon != weapon) continue;
            // 和 BuildOffers 一致的过滤：已拥有<5级 或 未拥有且<4把武器
            bool owned = stat.GetWeaponDict().TryGetValue(weapon, out int current);
            if ((owned && current < 5) || (!owned && stat.GetWeaponDict().Count < 4))
                pool.Add("weapon:" + weapon);
        }
        return pool;
    }

    public void OpenBox(PlayerStat player)
    {
        if (!CanSimulate) return;
        // 宝箱只给属性，不给武器（武器是升级系统的）
        var pool = BuildBoxPool(player);
        string offer = pool[UnityEngine.Random.Range(0, pool.Count)];
        bool applied = ApplyOffer(player, offer);
        string desc = DescribeOffer(offer);
        Status = "宝箱奖励：" + desc;
        ToastManager.Show(desc); // BuildBoxPool 已过滤，不会出现无效道具
    }

    public void Down(PlayerController player)
    {
        if (!CanSimulate || IsDown(player)) return;
        int slot = Slot(player); State.players[slot].down = true;
        player.GetComponent<PlayerStat>().HP = 0; player.SetCoopDown(true);
        if (State.players.All(p => p.down)) Finish(false);
    }
    void UpdateRescue()
    {
        for (int down = 0; down < 2; down++)
        {
            if (!State.players[down].down) continue;
            int helper = 1 - down;
            bool held = InputFor(helper).revive && (helper == 0 || Time.unscaledTime - lastInput < 0.5f);
            if (!State.players[helper].down && held && Vector3.Distance(Players[down].transform.position, Players[helper].transform.position) <= 2.5f)
                State.players[down].rescue += Time.deltaTime;
            else State.players[down].rescue = 0;
            if (State.players[down].rescue < 3f) continue;
            State.players[down].down = false; State.players[down].rescue = 0;
            var stat = Players[down].GetComponent<PlayerStat>(); stat.HP = Math.Max(1, stat.MaxHP / 2);
            Players[down].SetCoopDown(false);
        }
    }
    public void Finish(bool victory)
    {
        if (!IsHost || State.phase == LanPhase.Victory || State.phase == LanPhase.Defeat) return;
        State.phase = victory ? LanPhase.Victory : LanPhase.Defeat;
        Managers.GamePause(); RefreshState();
    }
    public void PauseForBoss(float seconds) { if (IsHost) StartCoroutine(BossPause(seconds)); }
    System.Collections.IEnumerator BossPause(float seconds)
    {
        Managers.GamePause(); yield return new WaitForSecondsRealtime(seconds);
        if (State.phase == LanPhase.Playing) Managers.GamePlay();
    }

    void RefreshState()
    {
        State.time = Managers.GameTime;
        State.paused = Managers.gameStop;
        var boss = hostLoaded ? FindObjectOfType<BossController>() : null;
        State.bossHp = boss != null ? boss.GetComponent<EnemyStat>().HP : 0;
        State.bossMaxHp = boss != null ? boss.GetComponent<EnemyStat>().MaxHP : 0;
        for (int i = 0; i < 2; i++)
        {
            if (Players[i] == null) continue;
            var stat = Players[i].GetComponent<PlayerStat>(); var p = Players[i].transform.position;
            State.players[i].x = p.x; State.players[i].y = p.y;
            State.players[i].hp = stat.HP; State.players[i].maxHp = stat.MaxHP;
            // 独立经验/等级
            State.players[i].exp = stat.Exp; State.players[i].maxExp = stat.MaxExp;
            State.players[i].level = stat.Level;
            // per-player 升级弹窗状态
            State.players[i].upgradePending = upgradePending[i];
            State.players[i].chosen = upgradeChosen[i];
            State.players[i].weapons = string.Join("  ", stat.GetWeaponDict().Select(w => UILocalization.GetName(w.Key.ToString()) + " " + w.Value));
            // 同步属性加成（和单机右上角 HUD 格式对齐）
            State.players[i].damage = stat.Damage;
            State.players[i].defense = stat.Defense;
            State.players[i].moveSpeed = stat.MoveSpeed;
            State.players[i].cooldown = stat.Cooldown;
            State.players[i].amount = stat.TotalAmount;
            State.players[i].attackPerSecond = stat.AttackPerSecond;
            State.players[i].hasAmount = stat.HasAmount;
            // 起始武器实际伤害（与单机"伤害"槽位一致的重算值）
            var startWeapon = Players[i].GetComponentsInChildren<WeaponController>()
                .FirstOrDefault(w => w._weaponType == (int)stat._playerStartWeapon);
            State.players[i].weaponDamage = startWeapon != null ? startWeapon._damage : 0;
            // 激光炮角色专属数据（与单机 UI_Player "范围" 槽位对齐）
            State.players[i].isLaser = stat._playerStartWeapon == Define.Weapons.LaserCannon;
            if (State.players[i].isLaser)
            {
                var laser = Players[i].GetComponentInChildren<LaserController>();
                if (laser != null) State.players[i].laserWidth = laser.CurrentWidth;
            }
            // Axe 角色专属数据
            State.players[i].isAxe = stat._playerStartWeapon == Define.Weapons.Axe;
            if (State.players[i].isAxe)
            {
                var axe = Players[i].GetComponentInChildren<AxeController>();
                if (axe != null)
                {
                    State.players[i].axeRange = axe.AttackRange;
                    State.players[i].axeAngle = axe.AttackAngle;
                    State.players[i].axeCooldown = axe._cooldown;
                    State.players[i].axeDamage = axe._damage;
                }
            }
        }
    }
    void ReceiveState(LanState state)
    {
        if (state == null || state.players == null || state.players.Length != 2 || !PeerVerified) return;
        // A final lobby packet can still be in flight after the reliable start command.
        if (LocalObject != null && state.phase == LanPhase.Lobby) return;
        State = state;
        if (LocalObject == null) return;
        Managers.GameTime = State.time;
        LocalObject.transform.position = new Vector3(State.players[1].x, State.players[1].y, 0);
        world.Apply(State.visuals);
        // Guest has no simulation; keep time running for terrain/camera presentation.
        Time.timeScale = 1;
    }
    void Disconnect(string reason)
    {
        if (State.phase == LanPhase.Victory || State.phase == LanPhase.Defeat) { transport?.Dispose(); return; }
        State.phase = LanPhase.Disconnected; Status = reason; transport?.Dispose();
        if (hostLoaded) Managers.GamePause();
    }
    public void Exit()
    {
        if (exiting) return; exiting = true;
        transport?.Dispose();
        // 1. 先清 overlay UI（必须在 Destroy 之前，Destroy 后 overlay 引用无效）
        if (overlay != null && overlay.gameObject != null)
        {
            var canvas = overlay.GetComponentInChildren<Canvas>();
            if (canvas != null) Destroy(canvas.gameObject);
        }
        // 2. Destroy 自己 → OnDestroy 自动把 Instance = null → LanSession.Active = false
        //    这样回到主菜单再进单机 GameScene 时，Init() 会走单机分支（playerUI 赋值、Spawner 启用）
        Destroy(gameObject);
        // 3. 切主菜单
        Managers.Scene.LoadScene(Define.SceneType.MainMenuScene);
        Managers.GamePlay();
    }
    void OnDestroy() { transport?.Dispose(); if (Instance == this) Instance = null; }
    void OnApplicationQuit() { transport?.Dispose(); }
}
```

## Assets/Scripts/Multiplayer/LanTransport.cs

```csharp
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Two-peer LAN prototype transport. No Unity API is used on worker threads.
/// Frames are length-prefixed; state has a one-frame mailbox so slow peers cannot
/// accumulate obsolete snapshots. Control messages are bounded and ordered.</summary>
public sealed class LanTransport : IDisposable
{
    public const int Port = 7777;
    public const int MaxFrameBytes = 1024 * 1024;
    readonly ConcurrentQueue<string> received = new ConcurrentQueue<string>();
    readonly ConcurrentQueue<byte[]> outgoing = new ConcurrentQueue<byte[]>();
    readonly CancellationTokenSource stop = new CancellationTokenSource();
    readonly object gate = new object();
    TcpListener listener;
    TcpClient client;
    byte[] latestOutgoing;
    string latestIncoming;
    volatile string error;
    volatile bool connected;
    public bool Connected => connected;
    public string Error => error;
    public long BytesSent;
    public long BytesReceived;

    public void Host()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, Port);
            listener.Start(2);
            _ = AcceptLoop();
        }
        catch (Exception ex) { Fail("无法创建房间（端口可能被占用）：" + ex.Message); }
    }

    async Task AcceptLoop()
    {
        try
        {
            while (!stop.IsCancellationRequested)
            {
                var next = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                lock (gate)
                {
                    if (client != null || stop.IsCancellationRequested) { next.Close(); continue; }
                    client = next;
                }
                StartPeer(next);
            }
        }
        catch (Exception ex) { if (!stop.IsCancellationRequested) Fail(ex.Message); }
    }

    public async void Join(string address)
    {
        try
        {
            var next = new TcpClient(AddressFamily.InterNetwork);
            lock (gate) { if (stop.IsCancellationRequested) { next.Close(); return; } client = next; }
            var connection = next.ConnectAsync(IPAddress.Parse(address), Port);
            if (await Task.WhenAny(connection, Task.Delay(6000, stop.Token)).ConfigureAwait(false) != connection)
            { Fail("连接超时，请检查房主 IP、局域网和 TCP 7777 端口。"); return; }
            await connection.ConfigureAwait(false);
            if (!stop.IsCancellationRequested) StartPeer(next);
        }
        catch (Exception ex) { if (!stop.IsCancellationRequested) Fail("连接失败：" + ex.Message); }
    }

    void StartPeer(TcpClient peer)
    {
        peer.NoDelay = true;
        connected = true;
        _ = Task.Run(() => ReadLoop(peer));
        _ = Task.Run(() => WriteLoop(peer));
    }

    public void Send(string json, bool state = false)
    {
        if (stop.IsCancellationRequested) return;
        byte[] data = Encoding.UTF8.GetBytes(json);
        if (data.Length + 1 > MaxFrameBytes) { Fail("场景状态超过测试版传输上限。"); return; }
        byte[] frame = new byte[data.Length + 5];
        int length = data.Length + 1;
        frame[0] = (byte)(length >> 24); frame[1] = (byte)(length >> 16);
        frame[2] = (byte)(length >> 8); frame[3] = (byte)length;
        frame[4] = state ? (byte)1 : (byte)0;
        Buffer.BlockCopy(data, 0, frame, 5, data.Length);
        if (state) Interlocked.Exchange(ref latestOutgoing, frame);
        else if (outgoing.Count < 32) outgoing.Enqueue(frame);
        else Fail("网络发送队列已满。");
    }

    public bool TryReceive(out string json) => received.TryDequeue(out json);
    public string TakeState() => Interlocked.Exchange(ref latestIncoming, null);

    async Task ReadLoop(TcpClient peer)
    {
        try
        {
            NetworkStream stream = peer.GetStream();
            byte[] header = new byte[4];
            while (!stop.IsCancellationRequested)
            {
                await ReadExactly(stream, header, 4, stop.Token).ConfigureAwait(false);
                int length = DecodeLength(header);
                byte[] body = new byte[length];
                await ReadExactly(stream, body, length, stop.Token).ConfigureAwait(false);
                Interlocked.Add(ref BytesReceived, length + 4);
                string json = Encoding.UTF8.GetString(body, 1, body.Length - 1);
                if (body[0] == 1) Interlocked.Exchange(ref latestIncoming, json);
                else if (body[0] == 0 && received.Count < 32) received.Enqueue(json);
                else throw new InvalidDataException("Invalid frame or message flood.");
            }
        }
        catch (Exception ex) { if (!stop.IsCancellationRequested) Fail("连接已中断：" + ex.Message); }
    }

    public static int DecodeLength(byte[] header)
    {
        int length = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
        if (length < 2 || length > MaxFrameBytes) throw new InvalidDataException("Invalid frame length.");
        return length;
    }

    static async Task ReadExactly(Stream stream, byte[] data, int length, CancellationToken token)
    {
        int offset = 0;
        while (offset < length)
        {
            int count = await stream.ReadAsync(data, offset, length - offset, token).ConfigureAwait(false);
            if (count == 0) throw new EndOfStreamException("对方已退出房间");
            offset += count;
        }
    }

    async Task WriteLoop(TcpClient peer)
    {
        try
        {
            var stream = peer.GetStream();
            while (!stop.IsCancellationRequested)
            {
                if (!outgoing.TryDequeue(out byte[] frame)) frame = Interlocked.Exchange(ref latestOutgoing, null);
                if (frame == null) { await Task.Delay(5, stop.Token).ConfigureAwait(false); continue; }
                await stream.WriteAsync(frame, 0, frame.Length, stop.Token).ConfigureAwait(false);
                Interlocked.Add(ref BytesSent, frame.Length);
            }
        }
        catch (Exception ex) { if (!stop.IsCancellationRequested) Fail("发送失败：" + ex.Message); }
    }

    void Fail(string message)
    {
        // Some Mono/Windows socket messages contain trailing NUL padding.
        string clean = message.Replace("\0", string.Empty).Trim();
        error = clean.Length > 250 ? clean.Substring(0, 250) : clean;
        Dispose();
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (stop.IsCancellationRequested) return;
            stop.Cancel(); connected = false;
            listener?.Stop(); client?.Close();
        }
    }
}
```

## Assets/Scripts/Multiplayer/LanWorldView.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>Client renders passive sprites only: never instantiates combat scripts.
/// Full snapshots also repair missed spawns/despawns without replaying old events.</summary>
public sealed class LanWorldView
{
    readonly Sprite[] sprites;
    readonly Dictionary<Sprite, int> spriteIds = new Dictionary<Sprite, int>();
    readonly Dictionary<int, SpriteRenderer> replicas = new Dictionary<int, SpriteRenderer>();
    readonly Dictionary<int, Vector3> targets = new Dictionary<int, Vector3>();
    readonly Dictionary<SpriteRenderer, int> hostIds = new Dictionary<SpriteRenderer, int>();
    readonly List<SpriteRenderer> expired = new List<SpriteRenderer>();
    int nextId;
    public string Fingerprint { get; }

    public LanWorldView()
    {
        var catalog = Resources.Load<LanAssetCatalog>("LanAssetCatalog");
        if (catalog == null) throw new InvalidOperationException("缺少联机资源目录，请运行 Tools/Multiplayer/Rebuild Asset Catalog");
        sprites = catalog.sprites;
        for (int i = 0; i < sprites.Length; i++)
        {
            spriteIds[sprites[i]] = i;
        }
        string content = "lan-prototype-1|" + catalog.contentHash;
        foreach (var data in Resources.LoadAll<TextAsset>("Data").OrderBy(x => x.name, StringComparer.Ordinal)) content += data.text;
        using (var hash = SHA256.Create()) Fingerprint = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(content)));
    }

    public LanVisualState[] Capture()
    {
        var states = new List<LanVisualState>();
        // Prototype cap is 50 enemies. Profile before replacing this scan with a registry.
        foreach (var renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())
        {
            if (!renderer.enabled || renderer.sprite == null || !spriteIds.TryGetValue(renderer.sprite, out int sprite)) continue;
            if (!hostIds.TryGetValue(renderer, out int id)) hostIds[renderer] = id = ++nextId;
            var t = renderer.transform;
            Vector3 p = t.position, scale = t.lossyScale;
            states.Add(new LanVisualState { id = id, sprite = sprite, x = p.x, y = p.y, z = p.z,
                rotation = t.eulerAngles.z, sx = scale.x, sy = scale.y, color = renderer.color,
                flipX = renderer.flipX, flipY = renderer.flipY,
                layer = renderer.sortingLayerID, order = renderer.sortingOrder });
        }
        expired.Clear();
        foreach (var pair in hostIds) if (pair.Key == null || !pair.Key.gameObject.activeInHierarchy) expired.Add(pair.Key);
        foreach (var key in expired) hostIds.Remove(key);
        return states.ToArray();
    }

    public void Apply(LanVisualState[] states)
    {
        var alive = new HashSet<int>();
        foreach (var state in states)
        {
            if (state.sprite < 0 || state.sprite >= sprites.Length) continue;
            alive.Add(state.id);
            Vector3 position = new Vector3(state.x, state.y, state.z);
            if (!replicas.TryGetValue(state.id, out var renderer))
            {
                renderer = new GameObject("LAN View " + state.id, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                replicas.Add(state.id, renderer);
                renderer.transform.position = position;
            }
            if (Vector3.Distance(renderer.transform.position, position) > 4f) renderer.transform.position = position;
            targets[state.id] = position;
            renderer.sprite = sprites[state.sprite];
            renderer.color = state.color;
            renderer.flipX = state.flipX; renderer.flipY = state.flipY;
            renderer.sortingLayerID = state.layer; renderer.sortingOrder = state.order;
            renderer.transform.rotation = Quaternion.Euler(0, 0, state.rotation);
            renderer.transform.localScale = new Vector3(state.sx, state.sy, 1);
        }
        foreach (int id in replicas.Keys.Where(id => !alive.Contains(id)).ToArray())
        { UnityEngine.Object.Destroy(replicas[id].gameObject); replicas.Remove(id); targets.Remove(id); }
    }

    public void Interpolate(float dt)
    {
        float blend = 1f - Mathf.Exp(-30f * dt);
        foreach (var pair in replicas)
            if (pair.Value != null) pair.Value.transform.position = Vector3.Lerp(pair.Value.transform.position, targets[pair.Key], blend);
    }
}
```

## Assets/Scripts/Scene/BaseScene.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class BaseScene : MonoBehaviour
{
    public abstract Define.SceneType _sceneType { get; }

    void Awake()
    {
        Init();
    }

    protected virtual void Init()
    {
        Object obj = GameObject.FindObjectOfType(typeof(EventSystem));

        if (obj == null)
            Managers.Resource.Instantiate("UI/EventSystem").name = "@EventSystem";

    }

    public abstract void Clear();
}
```

## Assets/Scripts/Scene/GameScene.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class GameScene : BaseScene
{
    private UI_Player playerUI;

    public override Define.SceneType _sceneType { get { return Define.SceneType.GameScene; } }
    protected override void Init()
    {
        base.Init();
        if (LanSession.Active)
        {
            LanSession.Instance.InitializeBattle();
            return;
        }
        playerUI = Managers.UI.ShowSceneUI<UI_Player>("UI_Player");
        if (EndlessMode.Selected) playerUI.gameObject.SetActive(false);
        Managers.ResetGameTime();
        GameObject player = Managers.Game.Spawn(Define.WorldObject.Player, "Player/Player");
        player.GetOrAddComponent<PlayerController>().Init(Managers.Game.StartPlayer);
        if (EndlessMode.Selected) player.transform.position = new Vector3(0f, -3f, 0f);
        Camera.main.gameObject.GetOrAddComponent<CameraController>().SetPlayer(player);
        if (EndlessMode.Selected)
            new GameObject("EndlessDungeon").AddComponent<EndlessDungeon>();
        else
            Managers.Resource.Instantiate("Content/Grid");
        if (EndlessMode.Selected)
        {
            Spawner oldSpawner = player.GetComponentInChildren<Spawner>();
            if (oldSpawner != null) oldSpawner.enabled = false;
            gameObject.AddComponent<EndlessMode>();
        }
    }
    private void Update()
    {
        if (LanSession.Active) return;
        if (playerUI == null) return;  // 防御：Exit 刚 Destroy LanSession 时可能跨帧导致 playerUI 还没初始化
        if (EndlessMode.Active == null) playerUI.SetPlayerUI();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UI_GameMenu.Toggle();
        }

        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    GameObject player = Managers.Game.getPlayer();
        //    PlayerStat playerStat = player.GetOrAddComponent<PlayerStat>();

        //    List<Define.Weapons> weapons = new List<Define.Weapons>(playerStat.GetWeaponDict().Keys);
        //    foreach (Define.Weapons weapon  in weapons)
        //    {
        //        playerStat.AddOrSetWeaponDict(weapon, 5, true);
        //    }
        //    playerStat.Level = 100;
        //    playerStat.MaxHP = 9999;
        //    playerStat.HP = 9999;
        //    playerStat.Damage = 100;
        //    playerStat.MoveSpeed = 10;
        //    playerStat.Defense = 100;
        //    playerStat.MaxExp = 999999;
        //    playerStat.Exp = 99999;
        //    Managers.GameTime = 300f;

        //}
    }

    void SetActiveSkillCursorImg()
    {
        if (!Managers.Game.getPlayer().GetOrAddComponent<PlayerStat>().GetWeaponDict().TryGetValue(Define.Weapons.Lightning, out int weapon))
            return;
        Image cursorCoolTimeImg = playerUI.gameObject.FindChild<Image>("CursorCoolTimeImg");
        if (Managers.UI.GetPopupUICount() == 0)
            cursorCoolTimeImg.gameObject.SetActive(true);
        else
            cursorCoolTimeImg.gameObject.SetActive(false);
    }
    public override void Clear()
    {
        Managers.Clear();
    }
}
```

## Assets/Scripts/Scene/MainMenuScene.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuScene : BaseScene
{
    public override Define.SceneType _sceneType { get { return Define.SceneType.MainMenuScene; } }


    protected override void Init()
    {
        base.Init();
        Managers.UI.ShowSceneUI<UI_MainMenu>();
    }
    public override void Clear()
    {
        Managers.UI.Clear();
    }
}
```

## Assets/Scripts/Scene/SplashScene.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScene : MonoBehaviour
{
    float _time = 0;
    private void FixedUpdate()
    {
        _time += Time.fixedDeltaTime;
        if(_time > 1.5)
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}
```

## Assets/Scripts/Stat/EnemyStat.cs

```csharp
public class EnemyStat : Stat
{
    public Define.MonsterStyle MonsterStyle { get; set; } = Define.MonsterStyle.unknown;
    public Define.MonsterType MonsterType { get; set; } = Define.MonsterType.Enemy;
    private long _expPoint;
    public long ExpPoint
    {
        get { return _expPoint;} set { _expPoint = value; }}
    private int _expMul;
    public int ExpMul { get { return _expMul; } set { _expMul = value; } }

    void Start()
    {
        ExpPoint = 5;
        ExpMul = 1;
    }


}
```

## Assets/Scripts/Stat/PlayerStat.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStat : Stat
{


    //1 : knife, 2 : firebal, 3: spin, 4: posion 5: lightning 6: shotgun
    //WeaponDict - WeaponID : WeaponLevel
    Dictionary<string, int> _playerStat = new Dictionary<string, int>();
    [SerializeField]
    Dictionary<Define.Weapons, int> _weaponDict = new Dictionary<Define.Weapons, int>();
    public Define.Weapons _playerStartWeapon;
    public Dictionary<Define.Weapons, int> GetWeaponDict()
    {
        return _weaponDict;
    }
    public bool AddOrSetWeaponDict(Define.Weapons key, int value, bool addOrSet = false)
    {
        if (_weaponDict.Count == 0)
        {
            _playerStartWeapon = key;
        }
        if (!_weaponDict.ContainsKey(key))
        {
            if (_weaponDict.Count >= 4)
                return false;
            //key spawn
            _weaponDict.Add(key, 0);
        }

        if (!addOrSet)
            _weaponDict[key] += value;
        else
            _weaponDict[key] = value;

        SetWeaponLevel();
        return true;
    }

    public override int Level { get { return base.Level; }
        set
        {
            base.Level = value;
            if (!_playerStat.ContainsKey("Level"))
                _playerStat.Add("Level", _level);
            _playerStat["Level"] = _level;
        }
    }
    public override int MaxHP
    {
        get { return base.MaxHP; }
        set
        {
            base.MaxHP = value;
            if (!_playerStat.ContainsKey("MaxHP"))
                _playerStat.Add("MaxHP", _maxHp);
            _playerStat["MaxHP"] = _maxHp;
        }
    }
    public override float MoveSpeed
    {
        get { return base.MoveSpeed; }
        set
        {
            base.MoveSpeed = value;
            if (!_playerStat.ContainsKey("MoveSpeed"))
                _playerStat.Add("MoveSpeed", (int)_Movespeed);
            _playerStat["MoveSpeed"] = (int)_Movespeed;
        }
    }
    public override int Damage
    {
        get { return base.Damage; }
        set
        {
            base.Damage = value;
            if (!_playerStat.ContainsKey("Damage"))
                _playerStat.Add("Damage", _damage);
            _playerStat["Damage"] = _damage;
        }
    }
    public override int Defense
    {
        get { return base.Defense; }
        set
        {
            base.Defense = value;
            if (!_playerStat.ContainsKey("Defense"))
                _playerStat.Add("Defense", _defense);
            _playerStat["Defense"] = _defense;
        }
    }
    public int Cooldown { get { return _cooldown; }
        set
        {
            _cooldown = value;
            if (!_playerStat.ContainsKey("Cooldown"))
                _playerStat.Add("Cooldown", _cooldown);
            _playerStat["Cooldown"] = _cooldown;
        }
    }
    public int Amount { get { return _amount; }
        set
        {
            _amount = value;
            if (!_playerStat.ContainsKey("Amount"))
                _playerStat.Add("Amount", _amount);
            _playerStat["Amount"] = _amount;
        }
    }

    /// <summary>
    /// 当前攻速（每秒攻击次数），与 WeaponController 的冷却计算逻辑保持一致：
    /// 实际冷却 = 基础冷却 × 100/(100 + 攻速加成)，攻速 = 1 / 实际冷却。
    /// 基础冷却取角色初始武器当前等级的基础数值。
    /// </summary>
    public float AttackPerSecond
    {
        get
        {
            int weaponID = (int)_playerStartWeapon;
            if (weaponID == 0 || !Managers.Data.WeaponData.ContainsKey(weaponID))
                return 0f;
            Data.WeaponData weapon = Managers.Data.WeaponData[weaponID];
            int level = _weaponDict.ContainsKey(_playerStartWeapon) ? Math.Max(1, _weaponDict[_playerStartWeapon]) : 1;
            float baseCooldown = 1f;
            foreach (Data.WeaponLevelData lv in weapon.weaponLevelData)
            {
                if (lv.level == level)
                {
                    baseCooldown = lv.cooldown;
                    break;
                }
            }
            if (baseCooldown <= 0f)
                return 0f;
            float actualCooldown = baseCooldown * (100f / (100f + Cooldown));
            return 1f / actualCooldown;
        }
    }

    /// <summary>
    /// 当前总弹量（每次攻击实际发射数）= 武器基础弹数(countPerCreate) + 玩家弹量(Amount)。
    /// 基础弹数取角色初始武器当前等级的基础数值。
    /// </summary>
    public int TotalAmount
    {
        get
        {
            int weaponID = (int)_playerStartWeapon;
            if (weaponID == 0 || !Managers.Data.WeaponData.ContainsKey(weaponID))
                return Amount;
            Data.WeaponData weapon = Managers.Data.WeaponData[weaponID];
            int level = _weaponDict.ContainsKey(_playerStartWeapon) ? Math.Max(1, _weaponDict[_playerStartWeapon]) : 1;
            int baseCount = 0;
            foreach (Data.WeaponLevelData lv in weapon.weaponLevelData)
            {
                if (lv.level == level)
                {
                    baseCount = lv.countPerCreate;
                    break;
                }
            }
            return baseCount + Amount;
        }
    }

    private long _exp;
    public long Exp
    {
        get { return _exp;}
        set
        {
            _exp = value;
            while (_exp >= MaxExp)
            {
               OnLevelUp();
            }
        }
    }
    private long _maxExp = 1;

    public long MaxExp
    {
        get => _maxExp;
        set => _maxExp = value;
    }

    private int _cooldown;
    private int _amount;

    // 角色特性标记：是否拥有弹量/冷却属性（由初始角色数据决定）
    public bool HasAmount { get; set; } = false;
    public bool HasCooldown { get; set; } = false;

    public int AxeRangeLevel { get; private set; }
    public int AxeAngleLevel { get; private set; }
    public int AxeDamageLevel { get; private set; }

    // ===== 激光炮（Aemeath）专属升级上限 =====
    public const int LaserRangeMaxLevel = 3;
    public const int LaserDamageMaxLevel = 3;
    public int LaserRangeLevel { get; private set; }
    public int LaserDamageLevel { get; private set; }

    public bool CanUpgradeLaser(string upgrade)
    {
        if (_playerStartWeapon != Define.Weapons.LaserCannon) return false;
        switch (upgrade)
        {
            case "LaserRangeUp":   return LaserRangeLevel   < LaserRangeMaxLevel;
            case "LaserDamageUp":  return LaserDamageLevel  < LaserDamageMaxLevel;
            default: return false;
        }
    }
    public bool ApplyLaserUpgrade(string upgrade)
    {
        if (!CanUpgradeLaser(upgrade)) return false;
        switch (upgrade)
        {
            case "LaserRangeUp":   LaserRangeLevel++;  break;
            case "LaserDamageUp":  LaserDamageLevel++; break;
        }
        RefreshWeapons();
        return true;
    }

    // ===== 移动速度升级上限（所有角色通用，每个模式都生效） =====
    public const int MoveSpeedMaxLevel = 3;
    public const float MoveSpeedPerLevel = 1f;
    public int MoveSpeedLevel { get; private set; }

    public bool CanUpgradeMoveSpeed() => MoveSpeedLevel < MoveSpeedMaxLevel;
    public bool ApplyMoveSpeedUpgrade()
    {
        if (!CanUpgradeMoveSpeed()) return false;
        MoveSpeedLevel++;
        MoveSpeed += MoveSpeedPerLevel;
        return true;
    }

    public bool CanUpgradeAxe(string upgrade)
    {
        if (_playerStartWeapon != Define.Weapons.Axe) return false;
        switch (upgrade)
        {
            case "AxeRangeUp": return AxeRangeLevel < 3;
            case "AxeAngleUp": return AxeAngleLevel < 3;
            case "AxeDamageUp": return AxeDamageLevel < 3;
            default: return false;
        }
    }

    public bool ApplyAxeUpgrade(string upgrade)
    {
        if (!CanUpgradeAxe(upgrade)) return false;
        switch (upgrade)
        {
            case "AxeRangeUp": AxeRangeLevel++; break;
            case "AxeAngleUp": AxeAngleLevel++; break;
            case "AxeDamageUp": AxeDamageLevel++; break;
        }
        RefreshWeapons();
        return true;
    }




    void Awake()
    {
        Init();
    }

    void Init()
    {
        Level = 1;
        HP = 50;
        MaxHP = 50;
        MoveSpeed = 5.0f;
        Damage = 1;
        Defense = 0;
        Cooldown = 0;
        Amount = 0;
        Exp = 0;
        MaxExp = 10;
        SetWeaponLevel();
    }


    void OnLevelUp()
    {
        if (LanSession.Active)
            LanSession.Instance.BeginUpgradeFor(this);
        else
            Managers.Event.LevelUpEvent();

        Level += 1;
        Exp = 0;
        MaxExp += Math.Max(100, (long)(_maxExp*1.1));
    }

    void SetWeaponLevel()
    {
        //Set CommonWeaponSpawingPool
        foreach (KeyValuePair<Define.Weapons, int> weapon in GetWeaponDict())
        {
            string weaponName = weapon.Key.ToString();
            string weaponSpawningPool = weapon.Key + "SpawningPool";

            GameObject weaponPool = Util.FindChild(gameObject, weaponSpawningPool, true);
            if (weaponPool == null)
            {
                weaponPool = Managers.Resource.Instantiate($"Weapon/SpawningPool/{weaponSpawningPool}", transform);
                if (weaponPool == null)
                    continue;
            }
            if (weapon.Value == 0)
            {
                GetWeaponDict().Remove(weapon.Key);
                Managers.Resource.Destroy(weaponPool);
            }

            weaponPool.GetComponentInChildren<WeaponController>().Level = weapon.Value;
            if (!LanSession.Active && Managers.UI.getSceneUI() is UI_Player ui)
                ui.SetWeaponImage(this);
        }
    }

    public void RefreshWeapons() { SetWeaponLevel(); }


    public Dictionary<string, int> getPlayerStatData()
    {
        return _playerStat;
    }

    // ===== 背包系统 =====
    // 记录所有获得过的武器和属性（独立字典，与 _weaponDict / _playerStat 分开）
    readonly Dictionary<string, int> _inventory = new Dictionary<string, int>();
    public Dictionary<string, int> GetInventory() => _inventory;
    public void RecordObtain(string category, string itemName)
    {
        string key = $"{category}:{itemName}";
        if (!_inventory.ContainsKey(key)) _inventory[key] = 0;
        _inventory[key]++;
    }
}
```

## Assets/Scripts/Stat/Stat.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Stat : MonoBehaviour
{
    [SerializeField]
    protected int _level;
    [SerializeField]
    protected int _hp;
    [SerializeField]
    protected int _maxHp;
    [SerializeField]
    protected float _Movespeed;
    [FormerlySerializedAs("_attack")] [SerializeField]
    protected int _damage;
    [SerializeField]
    protected int _defense;


    public virtual int Level { get { return _level; } set { _level = value; } }
    public virtual int HP { get {  return _hp; } set { _hp = value; if (_hp > _maxHp) _hp = _maxHp; } }
    public virtual int MaxHP { get { return _maxHp; } set { _maxHp = value; } }
    public virtual float MoveSpeed { get { return _Movespeed; } set { _Movespeed = value; } }
    public virtual int Damage { get { return _damage; } set { _damage = value; } }
    public virtual int Defense { get { return _defense; } set { _defense = value; } }


}
```

## Assets/Scripts/UI/FixedMenuViewport.cs

```csharp
using UnityEngine;
using UnityEngine.UI;

// Menu layers share a 1920x1080 design space, regardless of window aspect ratio.
[RequireComponent(typeof(Canvas))]
public sealed class FixedMenuViewport : MonoBehaviour
{
    private RectTransform content;
    private RectTransform canvasRect;

    public void Configure(bool drawBlackBackground)
    {
        if (content != null)
            return;

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
            scaler.enabled = false;
        canvas.scaleFactor = 1f;
        canvasRect = (RectTransform)transform;

        // Snapshot the original children before adding the wrapper. Keeping their
        // local layout preserves prefab anchors and animations below each child.
        Transform[] children = new Transform[transform.childCount];
        for (int i = 0; i < children.Length; i++)
            children[i] = transform.GetChild(i);

        if (drawBlackBackground)
        {
            GameObject backdrop = new GameObject("Letterbox", typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)backdrop.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = backdrop.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
        }

        GameObject root = new GameObject("MenuRoot_1920x1080", typeof(RectTransform), typeof(RectMask2D));
        content = (RectTransform)root.transform;
        content.SetParent(transform, false);
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(1920f, 1080f);
        content.anchoredPosition = Vector2.zero;
        foreach (Transform child in children)
            child.SetParent(content, false);

        Canvas.ForceUpdateCanvases();
        Fit();
    }

    private void LateUpdate()
    {
        Fit();
    }

    private void Fit()
    {
        if (content == null)
            return;
        Vector2 available = canvasRect.rect.size;
        float scale = Mathf.Min(available.x / 1920f, available.y / 1080f);
        content.localScale = Vector3.one * Mathf.Max(scale, 0.0001f);
    }
}
```

## Assets/Scripts/UI/Popup/GameScene/UI_GameMenu.cs

```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_GameMenu : UI_Popup
{
    public override Define.PopupUIGroup PopupID => Define.PopupUIGroup.UI_GameMenu;

    public override void Init()
    {
        base.Init();
        var font = GetComponentInChildren<TMP_Text>(true).font;
        foreach (Transform child in transform) child.gameObject.SetActive(false);
        transform.localScale = Vector3.one;
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        SettingsControls.Build(transform, font, () => OnBackToGame(null), () => OnBackToMain(null), false);
    }

    public static void Toggle()
    {
        if (Managers.UI.IsPopupOpen(Define.PopupUIGroup.UI_GameMenu))
            Managers.UI.CloseAllGroupPopupUI(Define.PopupUIGroup.UI_GameMenu);
        else if (!Managers.gameStop)
        {
            Managers.UI.CloseAllGroupPopupUI(Define.PopupUIGroup.UI_Inventory);
            Managers.UI.ShowPopupUI<UI_GameMenu>();
            Managers.GamePause();
        }
    }

    public void OnBackToGame(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Managers.UI.CloseAllGroupPopupUI(PopupID);
    }

    public void OnBackToMain(PointerEventData data)
    {
        Managers.Scene.LoadScene(Define.SceneType.MainMenuScene);
        Managers.GamePlay();
    }

    // Legacy events belong to inactive prefab controls.
    public void OnVolumnChanged() { }
    public void OnValueChanged() { }
}

// Both game modes use the same controls while retaining their own pause/exit flow.
public static class SettingsControls
{
    static readonly Color Panel = new Color(.055f, .075f, .115f, .98f);
    static readonly Color Accent = new Color(.24f, .74f, .62f);

    static Image Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    static TMP_Text Label(Transform parent, TMP_FontAsset font, string text, Vector2 position, Vector2 size, int fontSize)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        return label;
    }

    static void ActionButton(Transform parent, TMP_FontAsset font, string text, float y, Action action)
    {
        var image = Box(parent, text, new Vector2(0, y), new Vector2(520, 64), new Color(.12f, .25f, .29f));
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => action());
        Label(image.transform, font, text, Vector2.zero, new Vector2(500, 60), 27).alignment = TextAlignmentOptions.Center;
    }

    static void Range(Transform parent, TMP_FontAsset font, string title, float y, float min, float max, float value, Action<float> changed, Func<float, string> format, bool wholeNumbers = false)
    {
        Label(parent, font, title, new Vector2(-130, y), new Vector2(260, 42), 27);
        var number = Label(parent, font, format(value), new Vector2(185, y), new Vector2(150, 42), 25);
        number.alignment = TextAlignmentOptions.MidlineRight;
        var track = Box(parent, title + " Slider", new Vector2(0, y - 46), new Vector2(520, 32), Color.clear);
        Box(track.transform, "Track", Vector2.zero, new Vector2(520, 8), new Color(.22f, .27f, .34f)).raycastTarget = false;
        var fillArea = Box(track.transform, "Fill Area", Vector2.zero, new Vector2(520, 8), Color.clear);
        fillArea.raycastTarget = false;
        var fill = Box(fillArea.transform, "Fill", Vector2.zero, Vector2.zero, Accent);
        fill.raycastTarget = false;
        var handleArea = Box(track.transform, "Handle Area", Vector2.zero, new Vector2(496, 32), Color.clear);
        handleArea.raycastTarget = false;
        var handle = Box(handleArea.transform, "Handle", Vector2.zero, new Vector2(24, 0), Color.white);
        var slider = track.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(v => { changed(v); number.text = format(v); });
        // LAN creates its overlay in the lobby. Refresh when the battle menu opens.
        track.gameObject.AddComponent<SettingsRangeSync>().Configure(slider, number, format,
            title == "主音量" ? (Func<float>)(() => Managers.Sound._soundVolumn) : () => CameraController.ViewLevel);
    }

    public static void Build(Transform parent, TMP_FontAsset font, Action resume, Action leave, bool multiplayer)
    {
        var shade = Box(parent, "Settings", Vector2.zero, Vector2.zero, new Color(0, 0, 0, .55f));
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = Vector2.one;
        shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
        var dismiss = shade.gameObject.AddComponent<Button>();
        dismiss.transition = Selectable.Transition.None;
        dismiss.onClick.AddListener(() => resume());
        var panel = Box(shade.transform, "Settings Panel", Vector2.zero, new Vector2(640, 590), Panel);
        panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
        Label(panel.transform, font, "设置", new Vector2(0, 230), new Vector2(520, 55), 40).alignment = TextAlignmentOptions.Center;
        Range(panel.transform, font, "主音量", 146, 0, 1, Managers.Sound._soundVolumn,
            Managers.Sound.SetMasterVolume, v => Mathf.RoundToInt(v * 100) + "%");
        Range(panel.transform, font, "视角高度", 32, CameraController.MinViewLevel, CameraController.MaxViewLevel,
            CameraController.ViewLevel, CameraController.SetViewLevel, v => CameraController.HeightForLevel(v).ToString("F1"), true);
        ActionButton(panel.transform, font, "返回主菜单", -100, leave);
        ActionButton(panel.transform, font, "继续游戏", -178, resume);
        if (multiplayer)
            Label(panel.transform, font, "设置期间战斗继续", new Vector2(0, -248), new Vector2(520, 35), 21).alignment = TextAlignmentOptions.Center;
    }

    public static void Gear(Transform parent, Action open)
    {
        var image = Box(parent, "Settings Button", new Vector2(58, -58), new Vector2(56, 56), Color.clear);
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0, 1);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => open());
        // Draw the gear with UI geometry; no font glyph or bitmap dependency.
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI / 8;
            var segment = Box(image.transform, "Gear", new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 13,
                new Vector2(i % 2 == 0 ? 15 : 7, 7), new Color(.65f, .65f, .65f, 1f));
            segment.rectTransform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            segment.raycastTarget = false;
        }
    }
}

public sealed class SettingsRangeSync : MonoBehaviour
{
    Slider slider;
    TMP_Text label;
    Func<float> read;
    Func<float, string> format;
    public void Configure(Slider control, TMP_Text text, Func<float, string> display, Func<float> current)
    {
        slider = control; label = text; format = display; read = current;
        OnEnable();
    }
    void OnEnable()
    {
        if (read == null) return;
        float value = read();
        slider.SetValueWithoutNotify(value);
        label.text = format(value);
    }
}
```

## Assets/Scripts/UI/Popup/GameScene/UI_GameOver.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UI_GameOver : UI_Popup
{
    enum Images
    {
        PretectImg,
        GameOverImg,
        DeadImg
    }

    enum Buttons
    {
        BackToMainButton
    }


    public override Define.PopupUIGroup PopupID { get { return Define.PopupUIGroup.UI_GameOver; } }
    public RuntimeAnimatorController[] _animeCon;
    private Animator _anime;


    public override void Init()
    {
        base.Init();
        Managers.Sound.Play("Dead");
        _anime = gameObject.FindChild<Animator>("DeadImg");
        // 防御：动画控制器数组可能为空或长度不足（角色有4个但动画只有2个），越界时回退到第一个
        if (_animeCon != null && _animeCon.Length > 0)
        {
            int idx = Mathf.Clamp(Managers.Game.StartPlayer.id - 1, 0, _animeCon.Length - 1);
            _anime.runtimeAnimatorController = _animeCon[idx];
        }
        Bind<Image>(typeof(Images));
        Bind<Button>(typeof(Buttons));

        GetImage((int)Images.PretectImg).gameObject.AddUIEvent(OnClickFinishAnime);
        GetButton((int)Buttons.BackToMainButton).gameObject.AddUIEvent(OnClickBackToMain);

    }

    void OnClickFinishAnime(PointerEventData data)
    {
        // 防御：当前 Animator 控制器（DeadAnime）可能没有 GameOverAnime 状态，避免警告
        if (_anime.HasState(0, Animator.StringToHash("GameOverAnime")))
            _anime.Play("GameOverAnime", 0, 1f);
        GetButton((int)Buttons.BackToMainButton).gameObject.SetActive(true);
    }

    void OnClickBackToMain(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Managers.Scene.LoadScene(Define.SceneType.MainMenuScene);
        Managers.GamePlay();
    }
}
```

## Assets/Scripts/UI/Popup/GameScene/UI_GameVictory.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_GameVictory : UI_Popup
{
    enum Images
    {
        VictoryLogo
    }

    enum Buttons
    {
        BackToMainButton
    }

    public override Define.PopupUIGroup PopupID { get { return Define.PopupUIGroup.UI_GameVictory; } }

    public override void Init()
    {
        base.Init();
        Managers.Sound.Play("Win");
        Bind<Image>(typeof(Images));
        Bind<Button>(typeof(Buttons));


        GetButton((int)Buttons.BackToMainButton).gameObject.AddUIEvent(OnClickBackToMain);
    }


    void OnClickBackToMain(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Managers.Scene.LoadScene(Define.SceneType.MainMenuScene);
        Managers.GamePlay();
    }
}
```

## Assets/Scripts/UI/Popup/GameScene/UI_Inventory.cs

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包面板。预制体结构：Root(全屏透明) → Panel(居中360x180带背景) → GridPanel → 格子
/// 不调 Managers.GamePause()，战斗继续进行。
/// </summary>
public class UI_Inventory : UI_Popup
{
    public override Define.PopupUIGroup PopupID => Define.PopupUIGroup.UI_Inventory;

    public override void Init()
    {
        base.Init();
        Managers.Sound.SetAudioVolumn(Define.Sound.Bgm, Managers.Sound._soundVolumn);
        Refresh();
    }

    public void Refresh()
    {
        Transform gridPanel = transform.Find("Panel/GridPanel");
        if (gridPanel == null) { Debug.LogWarning("[UI_Inventory] Panel/GridPanel not found!"); return; }

        for (int i = gridPanel.childCount - 1; i >= 0; i--)
            Object.Destroy(gridPanel.GetChild(i).gameObject);

        PlayerStat stat;
        if (LanSession.Active)
        {
            // 联机：拿本地玩家对应的 PlayerStat
            var localPlayer = LanSession.Instance.Players[LanSession.Instance.LocalSlot];
            if (localPlayer == null) return;
            stat = localPlayer.GetComponent<PlayerStat>();
        }
        else
        {
            // 单机：拿唯一玩家
            GameObject playerGo = Managers.Game.getPlayer();
            if (playerGo == null) return;
            stat = playerGo.GetComponent<PlayerStat>();
        }
        if (stat == null) return;

        Dictionary<string, int> inv = stat.GetInventory();
        if (inv == null || inv.Count == 0) return;

        foreach (var kv in inv)
        {
            var item = Managers.UI.MakeSubItem<InventoryItem>(gridPanel);
            item.SetData(kv.Key, kv.Value);
        }
    }
}
```

## Assets/Scripts/UI/Popup/GameScene/UI_ItemBoxOpen.cs

```csharp
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UI_ItemBoxOpen : UI_Popup
{
    enum Buttons
    {
        ItemBoxButton
    }
    enum Texts
    {
        ItemBoxText
    }
    enum Images
    {
        ItemBoxPanel,
        ItemBoxImage,
        BackgroundImg
    }

    [SerializeField]
    private List<Transform> _weaponUILocation;

    private List<Define.Weapons> _weaponList;
    private PlayerStat _player;

    public override Define.PopupUIGroup PopupID { get { return Define.PopupUIGroup.UI_ItemBoxOpen; } }



    public override void Init()
    {
        base.Init();
        _player = Managers.Game.getPlayer().GetOrAddComponent<PlayerStat>();
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<Button>(typeof(Buttons));

        // 序列化槽位为空时，自动按名称收集武器显示槽位（weaponLocation / weaponLocation (N)）
        if (_weaponUILocation == null || _weaponUILocation.Count == 0)
        {
            _weaponUILocation = new List<Transform>();
            int idx = 0;
            Transform slot;
            while ((slot = Util.FindChild<Transform>(gameObject, idx == 0 ? "weaponLocation" : $"weaponLocation ({idx})", true)) != null)
            {
                _weaponUILocation.Add(slot);
                idx++;
            }
        }

        GetImage((int)Images.BackgroundImg).gameObject.AddUIEvent(OnOpenChest);
    }

    void OnOpenChest(PointerEventData data)
    {
        Managers.Sound.Play("BoxOpen", Define.Sound.Effect);
        _weaponList = Managers.Event.SetRandomWeaponfromItemBox(_player);
        if (_weaponList == null)
        {
            if (_weaponUILocation.Count > 0)
            {
                GameObject go = Managers.Resource.Instantiate("UI/SubItem/WeaponInven", parent: _weaponUILocation[0].transform);
                Util.FindChild<Image>(go, "WeaponImg", true).sprite = Managers.Resource.LoadSprite("Health");
            }
        }
        else
            for (int i = 0; i < _weaponList.Count && i < _weaponUILocation.Count; i++)
            {
                GameObject go = Managers.Resource.Instantiate("UI/SubItem/WeaponInven", parent: _weaponUILocation[i].transform);
                Util.FindChild<Image>(go, "WeaponImg", true).sprite = Managers.Resource.LoadSprite(_weaponList[i].ToString());
            }
        Get<Image>((int)Images.ItemBoxImage).GetComponent<Animator>().Play("Open");

        Button btn = GetButton((int)Buttons.ItemBoxButton);
        btn.gameObject.SetActive(true);
        btn.gameObject.AddUIEvent(Close);
    }

    void Close(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Managers.Event.SetLevelUpWeaponfromItemBox(_weaponList, _player);
        Managers.UI.CloseAllGroupPopupUI(PopupID);
    }
}
```

## Assets/Scripts/UI/Popup/GameScene/UI_LevelUp.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_LevelUp : UI_Popup
{
    public enum Panels
    {
        GridPanel,
        BackgroundImage,
    }

    private int _maxUpgradeNum = 3;

    public override Define.PopupUIGroup PopupID
    {
        get { return Define.PopupUIGroup.UI_LevelUp; }
    }

    public override void Init()
    {
        base.Init();
        Managers.Sound.Play("LevelUp", Define.Sound.Effect);
        Bind<GameObject>(typeof(Panels));

        // ====== 运行时代码兜底布局 ======
        // 根节点（Canvas）铺满屏幕
        RectTransform rootRT = GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.anchoredPosition = Vector2.zero;
        rootRT.sizeDelta = Vector2.zero;

        // BackgroundImage 铺满根节点做遮罩
        GameObject bgObj = Get<GameObject>((int)Panels.BackgroundImage);
        if (bgObj != null)
        {
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.pivot = new Vector2(0.5f, 0.5f);
            bgRT.anchoredPosition = Vector2.zero;
            bgRT.sizeDelta = Vector2.zero;
        }

        // GridPanel：单点锚在屏幕中心，固定设计尺寸 600×550
        GameObject gridPanel = Get<GameObject>((int)Panels.GridPanel);
        RectTransform gridRT = gridPanel.GetComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0.5f, 0.5f);
        gridRT.anchorMax = new Vector2(0.5f, 0.5f);
        gridRT.pivot = new Vector2(0.5f, 0.5f);
        gridRT.anchoredPosition = Vector2.zero;
        gridRT.sizeDelta = new Vector2(600f, 550f);

        // GridLayoutGroup：三卡片整体居中、垂直排列
        GridLayoutGroup gridLayout = gridPanel.GetComponent<GridLayoutGroup>();
        if (gridLayout == null) gridLayout = gridPanel.AddComponent<GridLayoutGroup>();
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
        gridLayout.cellSize = new Vector2(560f, 160f);
        gridLayout.spacing = new Vector2(0f, 20f);
        gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
        gridLayout.padding = new RectOffset(0, 0, 0, 0);

        foreach(Transform child in gridPanel.transform)
        {
            Managers.Resource.Destroy(child.gameObject);
        }
        PlayerStat player = Managers.Game.getPlayer().GetOrAddComponent<PlayerStat>();
        // 单机升级3选1与联机对齐：共用 BuildOffers 的等概率池 + 各角色满级过滤
        List<string> offers = LanSession.BuildOffers(player);
        List<string[]> itemList = new List<string[]>();
        foreach (string offer in offers)
            itemList.Add(offer.StartsWith("weapon:")
                ? new string[] { "2", offer.Substring(7) }
                : new string[] { "1", offer });
        for(int i = 0; i<itemList.Count; i++)
        {
            Debug.Log($"Item{i + 1}  : {itemList[i][1]}");
        }

        for(int i = 0; i< _maxUpgradeNum; i++)
        {
            GameObject upgradePanel = Managers.UI.MakeSubItem<UpgdPanel>(parent:gridPanel.transform).gameObject;
            UpgdPanel upgradeDesc = upgradePanel.GetOrAddComponent<UpgdPanel>();
            upgradeDesc.SetData(itemList[i]);
            upgradeDesc.SetInfo(itemList[i][1]);
        }
    }
}
```

## Assets/Scripts/UI/Popup/GameScene/UI_PlayerStat.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UI_PlayerStat : UI_Popup
{
    enum Objects
    {

        StatPanel,
        WeaponPanel
    }
    enum Images
    {
        BackgroundImg,
    }

    private PlayerStat _player;

    public override Define.PopupUIGroup PopupID { get { return Define.PopupUIGroup.UI_GameMenu; } }


    public override void Init()
    {
        base.Init();
        _player = Managers.Game.getPlayer().GetOrAddComponent<PlayerStat>();
        Bind<GameObject>(typeof(Objects));
        Bind<Image>(typeof(Images));

        GameObject statPanel = GetObject((int)Objects.StatPanel);
        foreach(Transform child in statPanel.transform)
        {
            Managers.Resource.Destroy(child.gameObject);
        }

        foreach(KeyValuePair<string, int> stat in _player.getPlayerStatData())
        {

            StatInven statInven = Managers.UI.MakeSubItem<StatInven>(statPanel.transform, "StatData");
            statInven.SetInfo(stat.Key, stat.Value);
        }

        GameObject weaponPanel = GetObject((int)Objects.WeaponPanel);
        foreach (Transform child in weaponPanel.transform)
        {
            Managers.Resource.Destroy(child.gameObject);
        }
        foreach (KeyValuePair<Define.Weapons, int> weapon in _player.GetWeaponDict())
        {
            WeaponInven weaponInven = Managers.UI.MakeSubItem<WeaponInven>(weaponPanel.transform, "WeaponData");
            weaponInven.SetInfo(weapon.Key.ToString(), weapon.Value);
        }

        GetImage((int)Images.BackgroundImg).gameObject.AddUIEvent(Close);

    }

    void Close(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Managers.UI.ClosePopupUI(PopupID);
    }
}
```

## Assets/Scripts/UI/Popup/GameScene/UI_TimeStop.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UI_TimeStop : UI_Popup
{
    enum Images
    {
        Image
    }

    public override Define.PopupUIGroup PopupID { get { return Define.PopupUIGroup.UI_TimeStop; } }

    public override void Init()
    {
        base.Init();
        Bind<Image>(typeof(Images));
        Image image = GetImage((int)Images.Image);

        StartCoroutine(ChangeImageColor(image, new Color(0.19811f, 0.1188f, 0.082983f, 0.3f)));
    }


    IEnumerator ChangeImageColor(Image image, Color color)
    {
        yield return new WaitForSecondsRealtime(0.1f);
        image.color = color;
    }
}
```

## Assets/Scripts/UI/Popup/MainMenuScene/UI_CharacterSelect.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;

public class UI_CharacterSelect : UI_Popup
{
    enum Images
    {
        BackgroundImg,
        ProtectImage,
        PlayerImage

    }
    enum Panels
    {
        SelectPanel,
        GridPanel,
        InfoPanel
    }


    enum Texts
    {
        NameText,
        DescText
    }

    enum Buttons
    {
        StartButton
    }

    public override Define.PopupUIGroup PopupID { get { return Define.PopupUIGroup.UI_CharacterSelect; } }

    private Dictionary<int, Data.Player> playerData;
    private List<GameObject> playerGOList = new List<GameObject>();
    private Data.Player currentPlayer = null;


    public override void Init()
    {
        base.Init();
        playerData = Managers.Data.PlayerData;

        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(Panels));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Button>(typeof(Buttons));

        Button startButton = GetButton((int)Buttons.StartButton);
        RectTransform startRect = (RectTransform)startButton.transform;
        startRect.anchorMin = startRect.anchorMax = new Vector2(1f, 0f);
        startRect.pivot = new Vector2(1f, 0f);
        startRect.sizeDelta = new Vector2(340f, 84f);
        startRect.anchoredPosition = new Vector2(-48f, 48f);
        startRect.localScale = Vector3.one;

        TextMeshProUGUI startLabel = startButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (startLabel != null)
        {
            startLabel.enableAutoSizing = false;
            startLabel.fontSize = 40f;
            startLabel.alignment = TextAlignmentOptions.Center;
            startLabel.rectTransform.anchorMin = Vector2.zero;
            startLabel.rectTransform.anchorMax = Vector2.one;
            startLabel.rectTransform.offsetMin = new Vector2(16f, 8f);
            startLabel.rectTransform.offsetMax = new Vector2(-16f, -8f);
        }

        GetImage((int)Images.ProtectImage).gameObject.AddUIEvent(OnClickDelete);


        GameObject gridPanel = GetObject((int)Panels.GridPanel).gameObject;
        foreach(Transform child in gridPanel.transform)
        {
            Managers.Resource.Destroy(child.gameObject);
        }

        //PlayerInven Create
        foreach (KeyValuePair<int, Data.Player> kv in playerData.OrderBy(p => p.Key))
        {
            if (!kv.Value.selectable)
                continue;
            GameObject go = Managers.Resource.Instantiate("UI/SubItem/PlayerInven", parent: gridPanel.transform);
            // 与角色1-3同管线：头像统一取自各自 Farmer 表的 "Stand 0" 子帧（player4.asset）
            string iconKey = kv.Value.name == "Aemeath" ? "player4" : kv.Value.name;
            go.FindChild<Image>("PlayerImg", true).sprite = Managers.Resource.LoadSprite(iconKey);
            go.GetOrAddComponent<PlayerInven>().Player = kv.Value;
            Data.Player playerStat = go.GetOrAddComponent<PlayerInven>().Player;
            BindUIEvent(go, (PointerEventData data) => { OnClickUpdate(go, playerStat); });
            playerGOList.Add(go);
        }
        OnClickUpdate(playerGOList[0], playerGOList[0].GetComponent<PlayerInven>().Player);

        GetButton((int)Buttons.StartButton).gameObject.AddUIEvent(OnClickStartGame);

    }

    void OnClickUpdate(GameObject go, Data.Player playerStat)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Debug.Log($"gameobject : {go.name}, player = {playerStat.id}");
        currentPlayer = playerStat;
        foreach(GameObject goes in playerGOList)
        {
            goes.GetComponent<Image>().color = Color.white;
        }
        go.GetComponent<Image>().color = Color.yellow;
        string name = playerStat.name;
        int wid = playerStat.weaponID;
        // 三个角色各自专属描述：没有的属性不显示
        string desc;
        if (wid == (int)Define.Weapons.Shotgun)
        {
            // 角色1：霰弹枪 · 每次多发散射、无限弹道、无攻速加成
            var w = Managers.Data.WeaponData[wid].weaponLevelData.First(v => v.level == 1);
            desc = $"生命：{playerStat.maxHp}  移速：{playerStat.moveSpeed}\n" +
                "霰弹枪 · 多发散射 · 弹道无限\n鼠标瞄准，自动射击\n" +
                $"伤害 {w.damage} · 间隔 {w.cooldown} 秒 · 每次 {w.countPerCreate} 发\n" +
                "专属强化：扩大弹丸 / 射速强化 / 增加弹数";
        }
        else if (wid == (int)Define.Weapons.Lightning)
        {
            // 角色2：闪电 · 有攻速加成(coolDown=20)、无弹道消耗
            var w = Managers.Data.WeaponData[wid].weaponLevelData.First(v => v.level == 1);
            desc = $"生命：{playerStat.maxHp}  移速：{playerStat.moveSpeed}\n" +
                "闪电 · 远程范围\n鼠标瞄准，鼠标左键释放落雷\n" +
                $"伤害 {w.damage} · 间隔 {w.cooldown} 秒 · 判定半径 {w.size}\n" +
                $"攻速加成 {playerStat.coolDown}%\n" +
                "专属强化：延长判定 / 冷却缩减 / 伤害提升";
        }
        else if (wid == (int)Define.Weapons.Axe)
        {
            // 角色3：重斧 · 已有专属描述
            var w = Managers.Data.WeaponData[wid].weaponLevelData.First(v => v.level == 1);
            desc = $"生命：{playerStat.maxHp}  移速：{playerStat.moveSpeed}\n" +
                "大斧头 · 慢速重击\n鼠标控制方向，自动挥砍指示扇形\n" +
                $"伤害 {w.damage} · 间隔 {w.cooldown:0.##} 秒\n距离 {w.size:0.#} · 角度 {w.angle:0}°\n" +
                "专属强化：加长柄 / 宽斧刃 / 磨刀石";
        }
        else if (wid == (int)Define.Weapons.LaserCannon)
        {
            // 角色4：Aemeath · 激光炮
            var w = Managers.Data.WeaponData[wid].weaponLevelData.First(v => v.level == 1);
            float laserAtk = w.cooldown > 0f ? 1f / (w.cooldown * (100f / (100f + playerStat.coolDown))) : 0f;
            desc = $"生命：{playerStat.maxHp}  移速：{playerStat.moveSpeed}  攻速：{laserAtk:F1}次/秒\n" +
                "光束炮 · 精准贯穿\n鼠标瞄准，自动发射贯穿激光\n" +
                $"伤害 {w.damage} · 间隔 {w.cooldown:0.##} 秒 · 射程约 10 单位\n" +
                "专属强化：聚焦镜片（射程）/ 能量核心（伤害）";
        }
        else
        {
            // 兜底：其他武器（如 Knife/Flame 等未实现角色），只显示有值的属性
            desc = $"生命：{playerStat.maxHp}";
            if (!Mathf.Approximately(playerStat.moveSpeed, 5f)) desc += $"  移速：{playerStat.moveSpeed}";
            if (playerStat.damage != 0) desc += $"  攻击加成：{playerStat.damage}";
            if (playerStat.defense != 0) desc += $"  防御：{playerStat.defense}";
            if (playerStat.coolDown != 0) desc += $"  攻速加成：{playerStat.coolDown}%";
            if (playerStat.amount != 0) desc += $"  弹量：{playerStat.amount}";
        }

        string previewIcon = name == "Aemeath" ? "player4" : name;
        GetImage((int)Images.PlayerImage).sprite = Managers.Resource.LoadSprite(previewIcon);
        GetText((int)Texts.NameText).text = UILocalization.GetName(name);
        GetText((int)Texts.DescText).text = desc;
    }
    void OnClickDelete(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Managers.UI.CloseAllGroupPopupUI(PopupID);
    }


    void OnClickStartGame(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        //Todo
        //Setting PlayerStat
        Managers.Game.StartPlayer = currentPlayer;
        //Move to GameScene
        Debug.Log($"StartPlayer : {Managers.Game.StartPlayer.id}");
        Managers.Scene.LoadScene(Define.SceneType.GameScene);
    }
}
```

## Assets/Scripts/UI/Popup/MainMenuScene/UI_Encyclopedia.cs

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 道具图鉴弹窗：以网格形式展示全部道具的中文名称与描述。
/// </summary>
public class UI_Encyclopedia : UI_Popup
{
    /// <summary>仅在预制体上存在并用于绑定点击关闭的图片</summary>
    enum Images
    {
        ProtectImage
    }

    enum Buttons
    {
        CloseButton
    }

    enum Panels
    {
        GridPanel
    }

    /// <summary>图鉴展示的道具：属性 + 武器</summary>
    private static readonly string[] _encyclopediaItems =
    {
        "MaxHP", "MoveSpeed", "Damage", "Defense", "Cooldown", "Amount",
        "Knife", "Fireball", "Spin", "Poison", "Lightning", "Shotgun",
        "Axe", "AxeRangeUp", "AxeAngleUp", "AxeDamageUp"
    };

    public override Define.PopupUIGroup PopupID { get { return Define.PopupUIGroup.UI_Encyclopedia; } }

    public override void Init()
    {
        base.Init();
        Bind<Image>(typeof(Images));
        Bind<Button>(typeof(Buttons));
        Bind<GameObject>(typeof(Panels));

        GetImage((int)Images.ProtectImage).gameObject.AddUIEvent(Close);
        GetButton((int)Buttons.CloseButton).gameObject.AddUIEvent(Close);

        BuildGrid();
    }

    void BuildGrid()
    {
        GameObject gridPanel = GetObject((int)Panels.GridPanel);
        if (gridPanel == null)
            return;

        // 清空旧条目，避免重复创建
        foreach (Transform child in gridPanel.transform)
            Managers.Resource.Destroy(child.gameObject);

        foreach (string itemKey in _encyclopediaItems)
        {
            GameObject go = Managers.Resource.Instantiate("UI/SubItem/EncyclopediaItem", gridPanel.transform);
            if (go == null)
                continue;
            go.GetOrAddComponent<EncyclopediaItem>().SetInfo(itemKey);
        }
    }

    void Close(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        ClosePopupUI();
    }
}
```

## Assets/Scripts/UI/Popup/MainMenuScene/UI_Tutorial.cs

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 游戏教程弹窗：分三页讲解游戏背景/胜利条件、玩法介绍、键位操作。
/// 需配合 Prec UI_Tutorial.prefab：TitleText、ContentText、PageText，
/// PrevButton、NextButton、CloseButton、ProtectImage。
/// </summary>
public class UI_Tutorial : UI_Popup
{
    enum Images
    {
        ProtectImage
    }

    enum Buttons
    {
        CloseButton,
        PrevButton,
        NextButton
    }

    enum Texts
    {
        TitleText,
        ContentText,
        PageText
    }

    /// <summary>三页教程内容：背景/胜利条件、玩法、键位操作</summary>
    private static readonly string[] _pageContents =
    {
        // 第 1 页：游戏背景与胜利条件
        "<b>游戏背景</b>\n\n" +
        "这里是幽灵生存者世界，僵尸大军正不断涌向这里。\n" +
        "你是一名幸存者，必须在这片被怪物包围的战场上存活下去！\n\n" +
        "<b>胜利条件</b>\n" +
        "· 存活更长时间，逐步击退敌人\n" +
        "· 击败每隔一段时间出现的强大 Boss\n" +
        "· 每次击败 Boss 即可获胜，挑战更高难度",

        // 第 2 页：玩法介绍
        "<b>玩法介绍</b>\n\n" +
        "· 霰弹枪随鼠标自动射击，魔法阵用左键释放\n" +
        "· 重斧农夫跟随鼠标瞄准，亮起的扇形为挥砍范围\n" +
        "· 击杀怪物获得经验，升级后解锁属性或武器\n" +
        "· 消灭敌人会掉落经验、道具和宝箱\n" +
        "· 拾取道具可提升攻击力、攻速、移速等属性\n" +
        "· 合理搭配武器与属性加成，才能在后期存活\n" +
        "· 收集经验升级强化，越战越强！",

        // 第 3 页：键位操作
        "<b>键位操作</b>\n\n" +
        "· WASD / 方向键：移动角色\n" +
        "· 鼠标移动：瞄准方向（远程武器）\n" +
        "· 左 Shift：向移动方向冲刺，冲刺期间短暂无敌\n\n" +
        "操作简单，走位与冲刺是生存的关键！"
    };

    public override Define.PopupUIGroup PopupID { get { return Define.PopupUIGroup.UI_Tutorial; } }

    int _page = 0;

    public override void Init()
    {
        base.Init();
        Bind<Image>(typeof(Images));
        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));

        GetImage((int)Images.ProtectImage).gameObject.AddUIEvent(Close);
        GetButton((int)Buttons.CloseButton).gameObject.AddUIEvent(Close);

        GetButton((int)Buttons.PrevButton).gameObject.AddUIEvent(OnPrevPage);
        GetButton((int)Buttons.NextButton).gameObject.AddUIEvent(OnNextPage);

        ShowPage(0);
    }

    void ShowPage(int index)
    {
        _page = Mathf.Clamp(index, 0, _pageContents.Length - 1);

        GetText((int)Texts.TitleText).text = "游戏教程（" + (_page + 1) + "/" + _pageContents.Length + "）";
        GetText((int)Texts.ContentText).text = _pageContents[_page];
        GetText((int)Texts.PageText).text = (_page + 1) + " / " + _pageContents.Length;

        GetButton((int)Buttons.PrevButton).gameObject.SetActive(_page > 0);
        GetButton((int)Buttons.NextButton).gameObject.SetActive(_page < _pageContents.Length - 1);
    }

    void OnPrevPage(PointerEventData data)
    {
        ShowPage(_page - 1);
    }

    void OnNextPage(PointerEventData data)
    {
        ShowPage(_page + 1);
    }

    void Close(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        ClosePopupUI();
    }
}
```

## Assets/Scripts/UI/Popup/UI_Popup.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UI_Popup : UI_Base
{
    public abstract Define.PopupUIGroup PopupID { get; }
    public override void Init()
    {
        Managers.UI.SetCanvas(gameObject, true);
    }

    public virtual void ClosePopupUI()
    {
        Managers.UI.ClosePopupUI(this);
    }
}
```

## Assets/Scripts/UI/Scene/GameScene/UI_Player.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Player : UI_Scene
{
    enum Texts
    {
        GameTime,
        LevelText,
        HPText,
        AtkText,
        SpdText,
        DefText,
        CoolText,
        AmmoText
    }

    enum Images
    {
        WeaponImgList,
        CursorCoolTimeImg
    }
    enum Sliders
    {
        ExpSlider,
        HPSlider
    }

    enum Buttons
    {
        InventoryBtn
    }

    public override void Init()
    {
        base.Init();
        SettingsControls.Gear(transform, UI_GameMenu.Toggle);
        // 先完成 Bind 绑定，再播放 BGM，避免音效异常影响 UI 初始化
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<Slider>(typeof(Sliders));
        Bind<Button>(typeof(Buttons));

        // 绑定背包按钮：优先使用 prefab 里的 InventoryBtn；不存在则动态创建兜底
        Button invBtn = GetButton((int)Buttons.InventoryBtn);
        if (invBtn != null)
        {
            invBtn.onClick.AddListener(ToggleInventory);
        }
        else
        {
            invBtn = CreateInventoryButton();
            if (invBtn != null) invBtn.onClick.AddListener(ToggleInventory);
        }

        // 删除左上角 WeaponImgList 武器图标区域（用户要求）
        Image weaponList = GetImage((int)Images.WeaponImgList);
        if (weaponList != null)
            weaponList.gameObject.SetActive(false);

        Managers.Sound.Play(Define.BGMs.BGM_01.ToString(), Define.Sound.Bgm);
    }

    /// <summary>动态创建背包按钮（当 prefab 里没有 InventoryBtn 节点时兜底）。
    /// 位置：StatusPanel（右上角 anchor 1,1 中心 -30,-30 size 360×90）正上方，与属性栏同区域形成一个模块。</summary>
    Button CreateInventoryButton()
    {
        var parent = GetComponent<RectTransform>();
        var btnGo = new GameObject("InventoryBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        var btnRect = (RectTransform)btnGo.transform;
        btnRect.anchorMin = btnRect.anchorMax = new Vector2(1, 1);
        btnRect.pivot = new Vector2(0.5f, 1);        // 上边缘中心对齐
        btnRect.anchoredPosition = new Vector2(-30, -130); // StatusPanel(-30,-30) 中心上方 10px
        btnRect.sizeDelta = new Vector2(48, 48);

        var img = btnGo.GetComponent<Image>();
        var sprite = Managers.Resource.Load<Sprite>("Prefabs/SpriteIcon/InventoryBtn");
        if (sprite != null) img.sprite = sprite;
        img.preserveAspect = true;
        img.color = Color.white;

        return btnGo.GetComponent<Button>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            ToggleInventory();
    }

    public void ToggleInventory()
    {
        if (Managers.UI.IsPopupOpen(Define.PopupUIGroup.UI_GameMenu)) return;
        Define.PopupUIGroup group = Define.PopupUIGroup.UI_Inventory;
        if (Managers.UI.IsPopupOpen(group))
            Managers.UI.ClosePopupUI(group);
        else
            Managers.UI.ShowPopupUI<UI_Inventory>();
    }

    public void SetPlayerUI()
    {
        SetGameTime();
        SetExpAndLevel();
        SetPlayerStatus();
    }

    void SetPlayerStatus()
    {
        GameObject playerGo = Managers.Game.getPlayer();
        if (playerGo == null)
            return;
        PlayerStat player = playerGo.GetOrAddComponent<PlayerStat>();
        AxeController axe = player._playerStartWeapon == Define.Weapons.Axe ? playerGo.GetComponentInChildren<AxeController>() : null;
        LaserController laser = player._playerStartWeapon == Define.Weapons.LaserCannon ? playerGo.GetComponentInChildren<LaserController>() : null;

        // 红色血量条
        Slider hpSlider = Get<Slider>((int)Sliders.HPSlider);
        if (hpSlider != null)
        {
            float ratio = player.MaxHP > 0 ? (float)player.HP / player.MaxHP : 0f;
            if (ratio < 0) ratio = 0;
            else if (ratio > 1) ratio = 1;
            hpSlider.value = ratio;
        }

        // 生命
        TextMeshProUGUI hpTxt = GetText((int)Texts.HPText);
        if (hpTxt != null)
            hpTxt.text = $"生命: {player.HP}/{player.MaxHP}";
        // 伤害（所有角色显示起始武器的实际伤害）
        TextMeshProUGUI atkTxt = GetText((int)Texts.AtkText);
        if (atkTxt != null)
        {
            atkTxt.text = axe != null ? $"斧头伤害: {axe._damage}" : $"伤害: {GetStartWeaponDamage(playerGo, player)}";
        }
        // 移速
        TextMeshProUGUI spdTxt = GetText((int)Texts.SpdText);
        if (spdTxt != null)
            spdTxt.text = $"移速: {player.MoveSpeed:F1}";
        // 防御
        TextMeshProUGUI defTxt = GetText((int)Texts.DefText);
        if (defTxt != null)
            defTxt.text = $"防御: {player.Defense}";
        // 攻速（每秒攻击次数，两个角色都显示）
        TextMeshProUGUI coolTxt = GetText((int)Texts.CoolText);
        if (coolTxt != null)
        {
            coolTxt.gameObject.SetActive(true);
            coolTxt.text = axe != null ? $"间隔: {axe._cooldown:F2}秒" : $"攻速: {player.AttackPerSecond:F1}次/秒";
        }
        // 弹量 / 斧头范围 / 激光范围（Aemeath 光束炮：显示激光宽度）
        TextMeshProUGUI ammoTxt = GetText((int)Texts.AmmoText);
        if (ammoTxt != null)
        {
            ammoTxt.gameObject.SetActive(player.HasAmount || axe != null || laser != null);
            if (laser != null)
                ammoTxt.text = $"范围: {laser.CurrentWidth:F1}";
            else if (axe != null)
                ammoTxt.text = $"范围: {axe.AttackRange:F1}/{axe.AttackAngle:0}°";
            else if (player.HasAmount)
                ammoTxt.text = $"弹量: {player.TotalAmount}";
        }
    }
    /// <summary>起始武器的实际伤害（含武器等级与攻击力加成的重算值），用于属性面板"伤害"槽位。</summary>
    int GetStartWeaponDamage(GameObject playerGo, PlayerStat player)
    {
        foreach (var wc in playerGo.GetComponentsInChildren<WeaponController>())
            if (wc._weaponType == (int)player._playerStartWeapon) return wc._damage;
        return 0;
    }

    void SetGameTime()
    {
        TextMeshProUGUI txt = Get<TextMeshProUGUI>((int)Texts.GameTime);
        if (txt == null)
            return;
        txt.text = string.Format("{0:D2}:{1:D2}", (int)Mathf.Floor(Managers.GameTime / 60),
            (int)Mathf.Floor(Managers.GameTime % 60));
    }

    void SetExpAndLevel()
    {
        GameObject playerGo = Managers.Game.getPlayer();
        if (playerGo == null)
            return;
        PlayerStat player = playerGo.GetOrAddComponent<PlayerStat>();
        double ratio = player.Exp / (double)player.MaxExp;
        if (ratio < 0)
            ratio = 0;
        else if (ratio > 1)
            ratio = 1;

        Slider expSlider = Get<Slider>((int)Sliders.ExpSlider);
        if (expSlider != null)
            expSlider.value = (float)ratio;

        TextMeshProUGUI levelTxt = GetText((int)Texts.LevelText);
        if (levelTxt != null)
            levelTxt.text = player.Level.ToString();
    }

    public void SetWeaponImage(PlayerStat player)
    {
        Image weaponImgListImg = GetImage((int)Images.WeaponImgList);
        if (weaponImgListImg == null)
            return;
        // 左上角 WeaponImgList 已隐藏，跳过武器图标刷新
        if (!weaponImgListImg.gameObject.activeSelf)
            return;
        GameObject weaponImgList = weaponImgListImg.gameObject;

        foreach(Transform child in weaponImgList.transform)
        {
            Managers.Resource.Destroy(child.gameObject);
        }

        foreach(KeyValuePair<Define.Weapons, int> weapon in player.GetWeaponDict())
        {
            Image weaponImg =  Managers.Resource.Instantiate("UI/SubItem/WeaponInven", weaponImgList.transform).GetOrAddComponent<Image>();
            weaponImg.gameObject.GetOrAddComponent<WeaponListImage>().SetInfo(weapon.Key.ToString());

        }

    }
    public void ActiveCheckCursorImage()
    {
        Image cursorCoolTimeImg = Get<Image>((int)Images.CursorCoolTimeImg);
        if (cursorCoolTimeImg == null)
            return;

        cursorCoolTimeImg.gameObject.SetActive(true);
    }
}
```

## Assets/Scripts/UI/Scene/MainMenuScene/AnimeOver.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimeOver : MonoBehaviour
{
    public bool _animeOver = false;

    public void SetAnimeOver()
    {
        _animeOver = true;
        Managers.Sound.Play("A_Bit_Of_Hope", Define.Sound.Bgm);
    }
}
```

## Assets/Scripts/UI/Scene/MainMenuScene/UI_MainMenu.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UI_MainMenu : UI_Scene
{
    enum Images
    {
        BackgroundImg,
        FrontImage,
        Logo,
    }

    enum ImageObjects
    {
        Farmers,
        Monsters
    }

    enum Texts
    {
        PresstoStartText
    }

    enum Buttons
    {
        GamePlayButton,
        GameExitButton
    }

    private Animator _anime;
    private Image backgroundImg;
    private bool _animeOver;
    private GameObject _encyclopediaBtn;
    private GameObject _tutorialBtn;
    private GameObject _multiplayerBtn;
    private GameObject _endlessBtn;

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(ImageObjects));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Button>(typeof(Buttons));

        ApplyFullHdTypography();

        backgroundImg = GetImage((int)Images.BackgroundImg);
        _anime = backgroundImg.transform.GetComponent<Animator>();

        GetImage((int)Images.FrontImage).gameObject.AddUIEvent(SetAnimationOver);

        GetButton((int)Buttons.GamePlayButton).gameObject.AddUIEvent(ShowCharacterSelectUI);
        GetButton((int)Buttons.GameExitButton).gameObject.AddUIEvent(ExitGame);
        CreateFeatureButtons();
    }

    /// <summary>运行时复制退出按钮生成"道具图鉴"与"游戏教程"两个按钮，避免手工编辑预制体布局</summary>
    void CreateFeatureButtons()
    {
        Button exitBtn = GetButton((int)Buttons.GameExitButton);

        // 道具图鉴按钮
        GameObject encyclopedia = CreateCopyButton(exitBtn, "EncyclopediaButton", "道具图鉴", -280f, ShowEncyclopediaUI);
        _encyclopediaBtn = encyclopedia;

        // 游戏教程按钮
        GameObject tutorial = CreateCopyButton(exitBtn, "TutorialButton", "游戏教程", -380f, ShowTutorialUI);
        _tutorialBtn = tutorial;

        _multiplayerBtn = CreateCopyButton(exitBtn, "MultiplayerButton", "多人游戏 · 测试版", 0f, ShowMultiplayer);
        _endlessBtn = CreateCopyButton(exitBtn, "EndlessButton", "无尽模式 · 守护核心", -180f, ShowEndless);
        _endlessBtn.GetComponent<UI_EventHandler>().OnClickHandler = ShowEndless;
        _endlessBtn.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
        var multiplayerRect = (RectTransform)_multiplayerBtn.transform;
        multiplayerRect.anchorMin = multiplayerRect.anchorMax = multiplayerRect.pivot = new Vector2(1f, 0f);
        multiplayerRect.sizeDelta = new Vector2(380f, 76f);
        multiplayerRect.anchoredPosition = new Vector2(-44f, 42f);
        _multiplayerBtn.GetComponentInChildren<TextMeshProUGUI>(true).fontSize = 30f;
        // Copied UI_EventHandler delegates must not retain the exit action.
        _multiplayerBtn.GetComponent<UI_EventHandler>().OnClickHandler = ShowMultiplayer;
        _multiplayerBtn.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();

        // Keep the existing menu actions available beside the new mode.
        RectTransform exitRect = (RectTransform)exitBtn.transform;
        exitRect.anchoredPosition = new Vector2(0f, -480f);
    }

    /// <summary>复制一个按钮并设置标签、位置与点击事件</summary>
    GameObject CreateCopyButton(Button src, string name, string label, float yPos, Action<PointerEventData> action)
    {
        GameObject go = UnityEngine.Object.Instantiate(src.gameObject, src.transform.parent);
        go.name = name;

        TextMeshProUGUI labelTxt = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelTxt != null)
        {
            labelTxt.name = name + "Text";
            labelTxt.text = label;
        }

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(340f, 84f);
        rect.anchoredPosition = new Vector2(0f, yPos);

        // 初始不激活：与"开始/退出"按钮及"按任意键开始"提示一样，待开场动画结束、
        // "按任意键开始"提示出现时，由 SetAnimationOver 统一激活。
        go.SetActive(false);
        go.AddUIEvent(action);
        return go;
    }

    void ApplyFullHdTypography()
    {
        // The title is sprite artwork, not text. Never stretch its glyphs.
        Image logo = GetImage((int)Images.Logo);
        logo.preserveAspect = true;
        RectTransform titleRect = logo.rectTransform;
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -70f);
        titleRect.sizeDelta = new Vector2(1200f, 340f);

        TextMeshProUGUI prompt = GetText((int)Texts.PresstoStartText);
        prompt.enableAutoSizing = false;
        prompt.fontSize = 44f;
        prompt.alignment = TextAlignmentOptions.Center;
        RectTransform promptRect = prompt.rectTransform;
        promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);
        promptRect.anchoredPosition = new Vector2(0f, -260f);
        promptRect.sizeDelta = new Vector2(640f, 90f);

        foreach (Buttons buttonId in System.Enum.GetValues(typeof(Buttons)))
        {
            Button button = GetButton((int)buttonId);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(340f, 84f);
            rect.anchoredPosition = new Vector2(0f,
                buttonId == Buttons.GamePlayButton ? -80f : -480f);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
                continue;
            label.enableAutoSizing = false;
            label.fontSize = 40f;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(16f, 8f);
            label.rectTransform.offsetMax = new Vector2(-16f, -8f);
        }
    }

    void SetAnimationOver(PointerEventData data)
    {
        _animeOver = backgroundImg.transform.GetComponent<AnimeOver>()._animeOver;
        if (!_animeOver)
        {
            _anime.Play("MainGameStartAnime", -1, 1.0f);
        }
        else
        {
            Managers.Resource.Destroy(GetText((int)Texts.PresstoStartText).gameObject);
            foreach(Buttons button in System.Enum.GetValues(typeof(Buttons)))
            {
                GetButton((int)button).gameObject.SetActive(true);
            }
            if (_encyclopediaBtn != null)
                _encyclopediaBtn.SetActive(true);
            if (_tutorialBtn != null)
                _tutorialBtn.SetActive(true);
            if (_multiplayerBtn != null)
                _multiplayerBtn.SetActive(true);
            if (_endlessBtn != null)
                _endlessBtn.SetActive(true);
            Managers.Resource.Destroy(GetImage((int)Images.FrontImage).gameObject);
        }
    }
    void ShowCharacterSelectUI(PointerEventData data)
    {
        EndlessMode.Selected = false;
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Debug.Log("Show!");
        Managers.UI.ShowPopupUI<UI_CharacterSelect>();
    }

    void ShowEndless(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        EndlessMode.Selected = true;
        Managers.UI.ShowPopupUI<UI_CharacterSelect>();
    }

    void ShowMultiplayer(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        LanSession.Open(_multiplayerBtn.GetComponentInChildren<TextMeshProUGUI>().font);
    }

    void ShowEncyclopediaUI(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Managers.UI.ShowPopupUI<UI_Encyclopedia>();
    }

    void ShowTutorialUI(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Managers.UI.ShowPopupUI<UI_Tutorial>();
    }

    void ExitGame(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
```

## Assets/Scripts/UI/Scene/UI_Scene.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class UI_Scene : UI_Base
{
    public override void Init()
    {
        Managers.UI.SetCanvas(gameObject, false);
    }
}
```

## Assets/Scripts/UI/SubItem/EncyclopediaItem.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 道具图鉴条目：展示单个道具的图标、中文名称与中文描述。
/// </summary>
public class EncyclopediaItem : UI_Base
{
    enum Images
    {
        ItemImg
    }

    enum Texts
    {
        NameText,
        DescText
    }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
    }

    /// <param name="itemKey">道具的英文枚举名（如 Knife / Cooldown），用于加载图标与本地化文本</param>
    public void SetInfo(string itemKey)
    {
        GetImage((int)Images.ItemImg).sprite = Managers.Resource.LoadSprite(itemKey);
        GetImage((int)Images.ItemImg).preserveAspect = true;
        GetImage((int)Images.ItemImg).type = Image.Type.Simple;
        GetText((int)Texts.NameText).text = UILocalization.GetName(itemKey);
        GetText((int)Texts.DescText).text = UILocalization.GetDesc(itemKey);
    }
}
```

## Assets/Scripts/UI/SubItem/InventoryItem.cs

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包物品图标：icon + 右下角次数角标(count>1才显示) + 点击切换 tooltip
/// UI_Base.Bind 按 enum 名字查找子对象，预制体节点名必须与 enum 值一致。
/// </summary>
public class InventoryItem : UI_Base, IPointerClickHandler
{
    enum Texts
    {
        CountText,      // 右下角次数
        TooltipName,    // 简介面板-名称
        TooltipDesc     // 简介面板-描述
    }

    enum Images
    {
        IconImage       // 主图标
    }

    enum Panels
    {
        TooltipPanel    // 简介面板（预制体里默认 SetActive(false)）
    }

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(Panels));
    }

    public void SetData(string key, int count)
    {
        string[] parts = key.Split(':');
        string category = parts.Length > 0 ? parts[0] : "";
        string rawName  = parts.Length > 1 ? parts[1] : key;

        // 图标
        Image icon = GetImage((int)Images.IconImage);
        if (icon != null)
        {
            string spriteName = MapSpriteName(rawName);
            var sprite = Managers.Resource.Load<Sprite>($"Prefabs/SpriteIcon/{spriteName}");
            if (sprite != null) icon.sprite = sprite;
        }

        // 次数角标：count > 1 才显示
        TextMeshProUGUI countTxt = GetText((int)Texts.CountText);
        if (countTxt != null)
        {
            countTxt.gameObject.SetActive(count > 1);
            countTxt.text = count.ToString();
        }

        // 填 tooltip
        GameObject tp = Get<GameObject>((int)Panels.TooltipPanel);
        if (tp != null)
        {
            GetText((int)Texts.TooltipName).text = MapDisplayName(rawName);
            GetText((int)Texts.TooltipDesc).text = MapDesc(rawName, category);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameObject tp = Get<GameObject>((int)Panels.TooltipPanel);
        if (tp != null) tp.SetActive(!tp.activeSelf);
    }

    // ======================================================
    static string MapSpriteName(string raw)
    {
        return raw == "MaxHP" ? "MaxHp" : raw;
    }

    static string MapDisplayName(string raw)
    {
        switch (raw)
        {
            case "MaxHP":     return "最大生命";
            case "MoveSpeed": return "移动速度";
            case "Damage":    return "攻击力";
            case "Defense":   return "防御力";
            case "Cooldown":  return "冷却缩减";
            case "Amount":    return "弹量";
            case "Fireball":  return "火球术";
            case "Knife":     return "飞刀";
            case "Lightning": return "闪电链";
            case "Shotgun":   return "霰弹";
            case "Spin":      return "旋风斩";
            case "Poison":    return "毒雾";
            default:          return raw;
        }
    }

    static string MapDesc(string raw, string category)
    {
        if (category == "Weapon")
        {
            switch (raw)
            {
                case "Fireball":  return "发射火球对敌人造成范围伤害";
                case "Knife":     return "射出锋利飞刀，快速攻击前方敌人";
                case "Lightning": return "释放闪电在敌人间跳跃";
                case "Shotgun":   return "近距离发射多枚弹丸";
                case "Spin":      return "武器围绕角色旋转攻击";
                case "Poison":    return "释放毒雾持续伤害敌人";
                default:          return "获得新武器";
            }
        }
        else
        {
            switch (raw)
            {
                case "MaxHP":     return "增加 10 点最大生命值";
                case "MoveSpeed": return "增加 1 点移动速度";
                case "Damage":    return "增加 10 点攻击力";
                case "Defense":   return "增加 1 点防御力";
                case "Cooldown":  return "增加 10% 冷却缩减";
                case "Amount":    return "增加 1 发弹量";
                default:          return "获得属性提升";
            }
        }
    }
}
```

## Assets/Scripts/UI/SubItem/PlayerInven.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInven : MonoBehaviour
{
    public Data.Player Player { get; set; } = new Data.Player();
}
```

## Assets/Scripts/UI/SubItem/StatInven.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class StatInven : UI_Base
{
    enum Images
    {
        StatInven,
        StatImg
    }

    enum Texts
    {
        TitleText,
        LevelText
    }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
    }

    public void SetInfo(string statName, int statNum)
    {
        GetImage((int)Images.StatImg).sprite = Managers.Resource.LoadSprite(statName);
        GetText((int)Texts.TitleText).text = UILocalization.GetName(statName);
        GetText((int)Texts.LevelText).text = statNum.ToString();
    }

}
```

## Assets/Scripts/UI/SubItem/UpgdPanel.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class UpgdPanel : UI_Base
{
    enum Texts
    {
        UpgdTitleText,
        UpgdDescText
    }

    enum Images
    {
        UpgdInven,
        UpgdImg,
        UpgdPanel,
        UpgdDescPanel,
    }

    private int _itemType;
    private string _itemName;

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));

        GetImage((int)Images.UpgdPanel).gameObject.AddUIEvent(OnStatOrWeaponUp);
    }

    void OnStatOrWeaponUp(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        string title =  Get<TextMeshProUGUI>((int)Texts.UpgdTitleText).text;
        Debug.Log($"{title} select!");
        Managers.Event.LevelUpOverEvent(_itemType, _itemName);
    }

    public void SetInfo(string title)
    {
        GetImage((int)Images.UpgdImg).sprite = Managers.Resource.Load<Sprite>($"Prefabs/SpriteIcon/{title}");
        GetImage((int)Images.UpgdImg).preserveAspect = true;
        GetImage((int)Images.UpgdImg).type = Image.Type.Simple;
        Get<TextMeshProUGUI>((int)Texts.UpgdTitleText).text = UILocalization.GetName(title);
        Get<TextMeshProUGUI>((int)Texts.UpgdDescText).text = UILocalization.GetDesc(title);
    }

    internal void SetData(string[] data)
    {
        _itemType = Int32.Parse(data[0]);
        _itemName = data[1];
    }
}
```

## Assets/Scripts/UI/SubItem/WeaponInven.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class WeaponInven : UI_Base
{
    enum Images
    {
        WeaponInven,
        WeaponImg
    }

    enum Texts
    {
        WeaponLevelText,
        LvText
    }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
    }

    public void SetInfo(string weaponName, int weaponLevel)
    {
        GetImage((int)Images.WeaponImg).sprite = Managers.Resource.LoadSprite(weaponName);
        GetImage((int)Images.WeaponImg).preserveAspect = true;
        GetImage((int)Images.WeaponImg).type = Image.Type.Simple;
        GetText((int)Texts.WeaponLevelText).text = weaponLevel.ToString();
    }
}
```

## Assets/Scripts/UI/SubItem/WeaponListImage.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponListImage : UI_Base
{
    enum Images
    {
        WeaponInven,
        WeaponImg,
    }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
    }


    public void SetInfo(string spriteName)
    {
        Debug.Log($"sprite name : {spriteName}");
        Debug.Log($"{Managers.Resource.Load<Sprite>($"Prefabs/SpriteIcon/{spriteName}")}");
        GetImage((int)Images.WeaponImg).sprite = Managers.Resource.Load<Sprite>($"Prefabs/SpriteIcon/{spriteName}");
        GetImage((int)Images.WeaponImg).preserveAspect = true;
        GetImage((int)Images.WeaponImg).type = Image.Type.Simple;
    }
}
```

## Assets/Scripts/UI/UI_Base.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public abstract class UI_Base : MonoBehaviour
{
    public abstract void Init();

    private void Awake()
    {
        Init();
    }

    Dictionary<Type, UnityEngine.Object[]> _objects = new Dictionary<Type, UnityEngine.Object[]>();
    protected void Bind<T>(Type type) where T : UnityEngine.Object
    {
        string[] name = Enum.GetNames(type);

        if (_objects.ContainsKey(typeof(T)))
            return;

        UnityEngine.Object[] objects = new UnityEngine.Object[name.Length];

        _objects.Add(typeof(T), objects);


        for(int i = 0; i<name.Length; i++)
        {
            if (typeof(T) == typeof(GameObject))
                objects[i] = Util.FindChild(gameObject, name[i], true);
            else
                objects[i] = Util.FindChild<T>(gameObject, name[i], true);

            if (objects[i] == null)
                Debug.Log($"Fail to Bind {name[i]}!");
        }
    }

    protected T Get<T>(int idx)where T : UnityEngine.Object
    {
        UnityEngine.Object[] objects = null;
        _objects.TryGetValue(typeof(T), out objects);
        return objects[idx] as T;
    }

    protected GameObject GetObject(int idx) { return Get<GameObject>(idx); }
    protected TextMeshProUGUI GetText(int idx) { return Get<TextMeshProUGUI>(idx); }

    protected Button GetButton(int idx) { return Get<Button>(idx); }

    protected Image GetImage(int idx) { return Get<Image>(idx); }

    public static void BindUIEvent(GameObject go, Action<PointerEventData> action, Define.UIEvent type = Define.UIEvent.Click)
    {
        UI_EventHandler evt = Util.GetOrAddComponent<UI_EventHandler>(go);

        switch (type)
        {
            case Define.UIEvent.Click:
                evt.OnClickHandler -= action;
                evt.OnClickHandler += action;
                break;
            case Define.UIEvent.Drag:
                evt.OnDragHandler -= action;
                evt.OnDragHandler += action;
                break;
        }
    }

}
```

## Assets/Scripts/UI/UI_ButtonHandler.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_EventHandler : MonoBehaviour, IPointerClickHandler, IDragHandler
{
    public Action<PointerEventData> OnDragHandler = null;
    public Action<PointerEventData> OnClickHandler = null;

    public void OnDrag(PointerEventData eventData)
    {
        if (OnDragHandler != null)
            OnDragHandler.Invoke(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (OnClickHandler != null)
            OnClickHandler.Invoke(eventData);
    }
}
```

## Assets/Scripts/UI/WorldSpace/UI_DamageText.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_DamageText : MonoBehaviour
{

    private TextMeshPro _text;
    private Color _alpha;

    public int _damage;

    private float _moveSpeed;
    private float _alphaSpeed;
    private float _destroyTime;


    // Start is called before the first frame update
    void Start()
        {
            _moveSpeed = 2.0f;
            _alphaSpeed = 2.0f;
            _destroyTime = 2.0f;

            _text = GetComponent<TextMeshPro>();
            _alpha = _text.color;
            _text.text = _damage.ToString();
            Invoke("DestroyObject", _destroyTime);
        }

        // Update is called once per frame
        void Update()
        {
            transform.Translate(new Vector3(0, _moveSpeed * Time.deltaTime, 0)); // ?띿뒪???꾩튂

            _alpha.a = Mathf.Lerp(_alpha.a, 0, Time.deltaTime * _alphaSpeed); // ?띿뒪???뚰뙆媛?
            _text.color = _alpha;
        }

        private void DestroyObject()
        {
            Destroy(gameObject);
        }
}
```

## Assets/Scripts/UI/WorldSpace/UI_HPBar.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UI_HPBar : UI_Base
{
    enum GameObjects
    {
        HPBar,
    }

    private Stat _stat;

    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));
    }

    private void Start()
    {
        _stat = transform.parent.GetComponent<Stat>();
    }

    private void Update()
    {
        Transform parent = transform.parent;
        if (parent == null)
            return;
        transform.position = parent.position;

        if (_stat == null)
            _stat = parent.GetComponent<Stat>();
        if (_stat == null)
            return;

        float ratio = _stat.HP / (float)_stat.MaxHP;
        setHpRatio(ratio);
    }

    public void setHpRatio(float ratio)
    {
        if (ratio < 0)
            ratio = 0;
        if (ratio > 1)
            ratio = 1;
        GameObject hpBar = GetObject((int)GameObjects.HPBar);
        if (hpBar == null)
            return;
        hpBar.GetComponent<Slider>().value = ratio;
    }
}
```

## Assets/Scripts/Utils/Define.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    public enum WorldObject
    {
        Unknown,
        Player,
        Enemy,
        Weapon
    }

    public enum Weapons
    {
        Knife = 1,
        Fireball = 2,
        Spin = 3,
        Poison = 4,
        Lightning = 101,
        Shotgun = 102,
        Axe = 103,
        LaserCannon = 104
    }
    public enum MonsterStyle
    {
        unknown = 0,
        zombie = 1,
        zombieElite = 2,
        skeleton = 3,
        skeletonElite = 4,
        tombStone = 5
    }
    public enum MonsterType
    {
        unknown,
        Enemy,
        middleBoss,
        Boss
    }
    public enum PlayerStartWeapon
    {
        Lightning = 1,
        Shotgun = 2,
        Axe = 3,
        LaserCannon = 4
    }

    public enum PopupUIGroup
    {
        Unknown,
        UI_GameMenu,
        UI_ItemBoxOpen,
        UI_LevelUp,
        UI_GameOver,
        UI_GameVictory,
        UI_TimeStop,
        UI_CharacterSelect,
        UI_Encyclopedia,
        UI_Tutorial,
        UI_Inventory
    }

    public enum SceneUI
    {
        Unknown,
        UI_Player,
        UI_MainMenu,
    }

    public enum Sound
    {
        Bgm,
        Effect,
        MaxCount,
    }
    public enum BGMs
    {
        BGM_01,
        BGM_02,
        BGM_03,
        BGM_04,
        A_Bit_Of_Hope
    }
    public enum UIEvent
    {
        Click,
        Drag,

    }
    public enum SceneType
    {
        Unknown,
        GameScene,
        MainMenuScene
    }

}
```

## Assets/Scripts/Utils/Extension.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class Extension
{
    public static void AddUIEvent(this GameObject go, Action<PointerEventData> action, Define.UIEvent type = Define.UIEvent.Click)
    {
        UI_Base.BindUIEvent(go, action, type);
    }
    public static T GetOrAddComponent<T>(this GameObject go) where T : UnityEngine.Component
    {
        return Util.GetOrAddComponent<T>(go);
    }

    public static T FindChild<T>(this GameObject go, string name = null, bool recursive = false) where T : UnityEngine.Object
    {
        return Util.FindChild<T>(go, name, recursive);
    }
    public static GameObject FindChild(this GameObject go, string name = null, bool recursive = false)
    {
        return Util.FindChild(go, name, recursive);
    }
}
```

## Assets/Scripts/Utils/UILocalization.cs

```csharp
using System.Collections.Generic;

/// <summary>
/// 界面中文化工具：将游戏内逻辑使用的英文枚举名（属性/武器）
/// 映射为中文显示文本。仅用于显示层，逻辑层仍使用英文枚举名
/// （精灵加载、升级判定等依赖英文名，不可改动）。
/// </summary>
public static class UILocalization
{
    /// <summary>显示名称映射：英文枚举名 → 中文名</summary>
    private static readonly Dictionary<string, string> _names = new Dictionary<string, string>
    {
        // 属性
        { "Level", "等级" },
        { "MaxHP", "最大生命值" },
        { "MoveSpeed", "移动速度" },
        { "Damage", "攻击力" },
        { "Defense", "防御力" },
        { "Cooldown", "攻速加成" },
        { "Amount", "弹道数量" },
        // 武器
        { "Knife", "小刀" },
        { "Fireball", "火球" },
        { "Spin", "回旋刃" },
        { "Poison", "毒雾" },
        { "Lightning", "闪电" },
        { "Shotgun", "霰弹枪" },
        { "Axe", "大斧头" },
        { "AxeRangeUp", "加长斧柄" },
        { "AxeAngleUp", "宽斧刃" },
        { "AxeDamageUp", "磨刀石" },
        // 角色（角色选择界面）
        { "player0", "玩家 1" },
        { "player1", "玩家 2" },
        { "player2", "重斧农夫" },
        { "player3", "玩家 4" },
        { "Aemeath", "艾米丝" },
        // 激光炮专属武器/强化
        { "LaserCannon", "光束炮" },
        { "LaserRangeUp", "聚焦镜片" },
        { "LaserDamageUp", "能量核心" },
    };

    /// <summary>效果说明映射：英文枚举名 → 中文说明（升级面板描述用）</summary>
    private static readonly Dictionary<string, string> _descs = new Dictionary<string, string>
    {
        { "MaxHP", "生命上限 +10" },
        { "MoveSpeed", "移动速度 +1" },
        { "Damage", "攻击力 +10" },
        { "Defense", "防御力 +1" },
        { "Cooldown", "攻击速度加成" },
        { "Amount", "弹道数量 +1" },
        { "Knife", "向前方连续投掷小刀" },
        { "Fireball", "投掷会爆炸的火球" },
        { "Spin", "环绕自身旋转的刀刃" },
        { "Poison", "留下持续伤害的毒雾" },
        { "Lightning", "可以增加落雷的伤害和攻速" },
        { "Shotgun", "扇形散射多枚弹丸" },
        { "Axe", "强化重斧：提高伤害和攻击距离" },
        { "AxeRangeUp", "斧头攻击距离 +0.4，武器和斩击同步延长（最多 3 级）" },
        { "AxeAngleUp", "斧头扇形角度 +15°（最多 3 级）" },
        { "AxeDamageUp", "斧头基础伤害 +10（最多 3 级）" },
        { "LaserCannon", "提升伤害和射程并增加攻速" },
        { "LaserRangeUp", "激光射程 +1，宽度 +0.3（最多 3 级）" },
        { "LaserDamageUp", "激光伤害 +30%（最多 3 级）" },
    };

    /// <summary>获取中文显示名，未匹配时原样返回</summary>
    public static string GetName(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        return _names.TryGetValue(key, out string cn) ? cn : key;
    }

    /// <summary>获取中文效果说明，未匹配时回退为中文名</summary>
    public static string GetDesc(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        return _descs.TryGetValue(key, out string cn) ? cn : GetName(key);
    }
}
```

## Assets/Scripts/Utils/Util.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util
{
    public static T GetOrAddComponent<T>(GameObject go) where T: UnityEngine.Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
            component = go.AddComponent<T>();
        return component;
    }

    public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
    {
        Transform transform =  FindChild<Transform>(go, name, recursive);
        if (transform == null)
            return null;

        return transform.gameObject;
    }
    public static T FindChild<T> (GameObject go, string name = null, bool recursive = false) where T: UnityEngine.Object
    {
        if (go == null)
            return null;

        if (!recursive)
        {
            for(int i=0; i<go.transform.childCount; i++)
            {
                Transform transform = go.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || transform.name == name)
                {
                    T component = transform.GetComponent<T>();
                    if (component != null)
                        return component;
                }
            }
        }
        else
        {
            foreach(T component in go.GetComponentsInChildren<T>(true))
            {
                if (string.IsNullOrEmpty(name) ||  component.name == name)
                    return component;
            }
        }

        return null;
    }

    //public static bool CheckContainsKey(Dictionary<object, object> dict, object key, object value)
    //{
    //    if (dict.Keys.GetType() != key.GetType())
    //    {
    //        Debug.Log("unVaild type of key");
    //        return false;
    //    }
    //    else if(dict.Values.GetType() != value.GetType())
    //    {
    //        Debug.Log("unVaild type of value");
    //        return false;
    //    }
    //    else if (!dict.ContainsKey(key))
    //    {
    //        Debug.Log($"There's no key in weaponDict");
    //        return false;
    //    }
    //    else
    //    {
    //        Debug.Log("Fine");
    //        return true;
    //    }
    //}
}
```

## Assets/Scripts/Weapons/AxeController.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>A rigid axe rotates around its grip; damage is a swept sector, not a sprite collider.</summary>
public sealed class AxeController : WeaponController
{
    const float WindupTime = 0.16f;
    const float SwingTime = 0.20f;
    const float RecoveryTime = 0.18f;
    const float AxeDetailScale = 0.55f;
    const float SlashRadius = 2f;

    [SerializeField] SpriteRenderer axeRenderer;
    [SerializeField] SpriteRenderer axeHeadRenderer;
    [SerializeField] SpriteRenderer handRenderer;
    [SerializeField] SpriteRenderer[] slashRenderers;
    [SerializeField] Sprite[] slashFrames;
    [SerializeField] SpriteRenderer rangeRenderer;
    [SerializeField] Sprite[] rangeFrames;

    readonly HashSet<BaseController> hitThisSwing = new HashSet<BaseController>();
    PlayerStat stats;
    SpriteRenderer bodyRenderer;
    float nextAttack, elapsed, aimAngle, swingRange, swingAngle, swingForce;
    int swingDamage;
    bool swinging, playedSwingSound;

    public override int _weaponType => (int)Define.Weapons.Axe;
    public float AttackRange => _size;
    public float AttackAngle { get; private set; } = 80f;
    public bool IsSwinging => swinging;

    protected override void SetWeaponStat()
    {
        base.SetWeaponStat();
        stats = GetComponentInParent<PlayerStat>();
        bodyRenderer = stats.GetComponent<SpriteRenderer>();
        var data = Managers.Data.WeaponData[_weaponType].weaponLevelData.Find(v => v.level == Level);
        _size = data.size + stats.AxeRangeLevel * 0.4f;
        AttackAngle = data.angle + stats.AxeAngleLevel * 15f;
        _damage = Mathf.RoundToInt((data.damage + stats.AxeDamageLevel * 10) * (100f + stats.Damage) / 100f);
        // The windup, active swing and recovery must still fit within the attack interval.
        _cooldown = Mathf.Max(WindupTime + SwingTime + RecoveryTime + 0.06f, _cooldown);
        if (!swinging) SetVisualScale(_size);
    }

    void OnEnable()
    {
        nextAttack = Time.time + 0.25f;
        swinging = false;
        HideSlash();
    }

    void OnDisable()
    {
        swinging = false;
        hitThisSwing.Clear();
        HideSlash();
        if (rangeRenderer != null) rangeRenderer.enabled = false;
        if (axeHeadRenderer != null) axeHeadRenderer.enabled = false;
        if (handRenderer != null) handRenderer.enabled = false;
    }

    void Update()
    {
        if (stats == null || axeRenderer == null) return;
        if (stats.HP <= 0 || (LanSession.Active && LanSession.Instance.IsDown(Owner)))
        {
            swinging = false;
            axeRenderer.enabled = false;
            if (handRenderer != null) handRenderer.enabled = false;
            if (axeHeadRenderer != null) axeHeadRenderer.enabled = false;
            if (rangeRenderer != null) rangeRenderer.enabled = false;
            HideSlash();
            return;
        }
        if (!CanOperate || Managers.gameStop || Time.deltaTime <= 0) return;
        axeRenderer.enabled = true;
        if (handRenderer != null) handRenderer.enabled = true;
        // Farmer sprites are centered on the torso; the grip belongs at the cuff.
        transform.localPosition = new Vector3((bodyRenderer.flipX ? -4f : 4f) / 18f, -5f / 18f, 0);
        if (axeHeadRenderer != null) axeHeadRenderer.enabled = true;
        Vector2 direction = (Vector2)AimPosition - (Vector2)transform.position;
        // Keep the last direction when the cursor is exactly on the grip.
        if (direction.sqrMagnitude > 0.0001f)
            aimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        ShowRange();

        if (!swinging)
        {
            axeRenderer.transform.localRotation = Quaternion.Euler(0, 0, aimAngle + AttackAngle * 0.5f + 15f);
            if (Time.time < nextAttack) return;

            swinging = true;
            playedSwingSound = false;
            elapsed = 0;
            swingRange = AttackRange;
            swingAngle = AttackAngle;
            swingDamage = _damage;
            swingForce = _force;
            nextAttack = Time.time + _cooldown;
            hitThisSwing.Clear();
            SetVisualScale(swingRange);
        }

        float previousElapsed = elapsed;
        elapsed += Time.deltaTime;
        float half = swingAngle * 0.5f;
        if (elapsed < WindupTime)
        {
            float angle = aimAngle + half + Mathf.Lerp(15f, 0, elapsed / WindupTime);
            axeRenderer.transform.localRotation = Quaternion.Euler(0, 0, angle);
            return;
        }

        if (!playedSwingSound)
        {
            playedSwingSound = true;
            Managers.Sound.Play("Melee0", Define.Sound.Effect, 0.8f);
        }
        float progress = Mathf.Clamp01((elapsed - WindupTime) / SwingTime);
        // Also process the final slice if a low-frame-rate update crosses the entire active phase.
        if (previousElapsed < WindupTime + SwingTime)
            HitSector(half - swingAngle * progress, half);

        if (elapsed < WindupTime + SwingTime)
        {
            axeRenderer.transform.localRotation = Quaternion.Euler(0, 0, aimAngle + half - swingAngle * progress);
            ShowSlash(progress, false);
        }
        else
        {
            float recovery = (elapsed - WindupTime - SwingTime) / RecoveryTime;
            axeRenderer.transform.localRotation = Quaternion.Euler(0, 0,
                aimAngle + Mathf.Lerp(-half, half + 15f, Mathf.SmoothStep(0, 1, recovery)));
            ShowSlash(1, true);
            if (recovery >= 1)
            {
                swinging = false;
                HideSlash();
                SetVisualScale(AttackRange);
            }
        }
    }

    void ShowRange()
    {
        if (rangeRenderer == null || rangeFrames == null || rangeFrames.Length == 0) return;
        float angle = swinging ? swingAngle : AttackAngle;
        float range = swinging ? swingRange : AttackRange;
        rangeRenderer.enabled = true;
        rangeRenderer.sprite = rangeFrames[Mathf.Clamp(Mathf.RoundToInt((angle - 80f) / 15f), 0, rangeFrames.Length - 1)];
        rangeRenderer.transform.localRotation = Quaternion.Euler(0, 0, aimAngle);
        rangeRenderer.transform.localScale = Vector3.one * range;
        bool active = swinging && elapsed >= WindupTime && elapsed < WindupTime + SwingTime;
        rangeRenderer.color = active ? new Color(1f, 0.77f, 0.3f, 1f) : new Color(1f, 0.88f, 0.55f, 0.75f);
    }

    void HitSector(float minimumAngle, float maximumAngle)
    {
        Vector2 origin = transform.position;
        foreach (var collider in Physics2D.OverlapCircleAll(origin, swingRange, LayerMask.GetMask("Enemy")))
        {
            var enemy = collider.GetComponentInParent<BaseController>();
            var enemyStats = collider.GetComponentInParent<EnemyStat>();
            if (enemy == null || enemy._type != Define.WorldObject.Enemy || enemyStats == null || enemyStats.HP <= 0 || hitThisSwing.Contains(enemy)) continue;
            Vector2 point = collider.ClosestPoint(origin);
            Vector2 offset = point - origin;
            if (offset.sqrMagnitude < 0.0001f) offset = (Vector2)collider.bounds.center - origin;
            if (!ContainsPoint(offset, aimAngle, swingRange, minimumAngle, maximumAngle)) continue;
            // Several colliders on one monster must still result in exactly one hit per swing.
            hitThisSwing.Add(enemy);
            float resistance = enemyStats.MonsterType == Define.MonsterType.Boss ? 0.1f
                : enemyStats.MonsterType == Define.MonsterType.middleBoss ? 0.25f : 1f;
            enemy.OnDamaged(swingDamage, swingForce * resistance);
        }
    }

    public static bool ContainsPoint(Vector2 offset, float directionAngle, float range, float minimumAngle, float maximumAngle)
    {
        if (offset.sqrMagnitude > range * range) return false;
        float relative = Mathf.DeltaAngle(directionAngle, Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
        return relative >= minimumAngle - 0.001f && relative <= maximumAngle + 0.001f;
    }

    void SetVisualScale(float range)
    {
        if (axeRenderer != null && axeRenderer.sprite != null)
        {
            float length = axeRenderer.sprite.bounds.size.x;
            float stretch = range / length;
            axeRenderer.transform.localScale = new Vector3(stretch, AxeDetailScale, 1f);
            if (axeHeadRenderer != null)
            {
                // The handle reaches the sector radius; cancel its stretch on the blade.
                axeHeadRenderer.transform.localPosition = new Vector3(length, 0, 0);
                axeHeadRenderer.transform.localScale = new Vector3(AxeDetailScale / stretch, 1f, 1f);
            }
        }
        if (slashRenderers == null) return;
        foreach (var slash in slashRenderers)
            if (slash != null) slash.transform.localScale = Vector3.one * (range / SlashRadius);
    }

    void ShowSlash(float progress, bool fading)
    {
        if (slashFrames == null || slashFrames.Length < 4 || slashRenderers == null) return;
        int count = swingAngle > 80.01f ? 2 : 1;
        for (int i = 0; i < slashRenderers.Length; i++)
        {
            var slash = slashRenderers[i];
            if (slash == null) continue;
            float offset = count == 1 ? 0 : (i == 0 ? 1 : -1) * (swingAngle - 80f) * 0.5f;
            float localProgress = (40f - (swingAngle * 0.5f - swingAngle * progress - offset)) / 80f;
            slash.enabled = i < count && (fading || localProgress >= 0);
            slash.sprite = slashFrames[fading ? 3 : Mathf.Clamp(Mathf.FloorToInt(localProgress * 3), 0, 2)];
            slash.transform.localRotation = Quaternion.Euler(0, 0, aimAngle + offset);
            slash.color = new Color(1, 1, 1, fading ? Mathf.Clamp01(1 - (elapsed - WindupTime - SwingTime) / RecoveryTime) : 1);
        }
    }

    void HideSlash()
    {
        if (slashRenderers == null) return;
        foreach (var slash in slashRenderers) if (slash != null) slash.enabled = false;
    }
}
```

## Assets/Scripts/Weapons/FireballController.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireballController : WeaponController
{

    public override int _weaponType { get { return (int)Define.Weapons.Fireball; } }

    private bool _isCool = false;

    void Update()
    {
        if (!CanOperate) return;
        if (!_isCool)
        {
            StartCoroutine(SpawnWeapon());
        }
    }

    IEnumerator SpawnWeapon()
    {
        _isCool = true;
        float angle = SetTarget();
        Managers.Sound.Play("Shoot_03");
        for (int i = 0; i < _countPerCreate; i++)
        {
            GameObject go = Managers.Game.Spawn(Define.WorldObject.Weapon, "Weapon/Fireball", transform.position);
            SetWeapon(go, angle);
            if (i == _countPerCreate - 1)
                break;
        }
        yield return new WaitForSeconds(_cooldown);
        _isCool = false;
    }

    float SetTarget()
    {
        List<GameObject> FoundEnemys = new List<GameObject>(GameObject.FindGameObjectsWithTag("Enemy"));
        float shortestDist = float.MaxValue;
        GameObject shortestDistEnemy = gameObject;
        foreach (GameObject enemy in FoundEnemys)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < shortestDist)
            {
                shortestDist = dist;
                shortestDistEnemy = enemy;
            }
        }
        Vector3 dirVec = (shortestDistEnemy.transform.position - transform.position).normalized;
        return Mathf.Atan2(dirVec.y, dirVec.x) * Mathf.Rad2Deg;
    }

    protected void SetWeapon(GameObject weapon, float angle)
    {
        Fireball fireball = weapon.GetOrAddComponent<Fireball>();
        angle += Random.Range(-5f, 5f);
        fireball._dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
        fireball._damage = _damage;
        fireball._speed = _movSpeed;
        fireball._force = _force;
        fireball._size = _size;
        fireball._panatrate = _penetrate;
    }
}
```

## Assets/Scripts/Weapons/KnifeController.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using Data;
using UnityEngine;

public class KnifeController : WeaponController
{
    public override int _weaponType { get { return (int)Define.Weapons.Knife; } }

    private float _termKnifeThrow = 0.1f;
    private bool _isCool = false;


    void Update()
    {
        if (!CanOperate) return;
        if (!_isCool)
        {
            StartCoroutine(SpawnWeapon());
        }
    }


    IEnumerator StartKnifeCoolTime()
    {

        yield return new WaitForSeconds(_cooldown);
        _isCool = false;
    }


    IEnumerator SpawnWeapon()
    {
        _isCool = true;
        for (int i = 0; i < _countPerCreate; i++)
        {
            if (!CanOperate) break;
            Managers.Sound.Play("Shoot_01");
            GameObject go = Managers.Game.Spawn(Define.WorldObject.Weapon, "Weapon/Knife");
            go.transform.position = transform.position;
            SetWeapon(go);
            if (i == _countPerCreate-1)
                break;
            yield return new WaitForSeconds(_termKnifeThrow);
        }

        StartCoroutine(StartKnifeCoolTime());
    }

    protected void SetWeapon(GameObject weapon)
    {
        Knife knife = weapon.GetOrAddComponent<Knife>();
        //Create Knife to ranmdom range position
        knife.transform.position += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);

        Vector2 dirOfPlayer = _player.GetComponent<PlayerController>()._lastDirVec;

        knife._dir = new Vector3(dirOfPlayer.x, dirOfPlayer.y, 0);

        knife._damage = _damage;
        knife._speed = _movSpeed;
        knife._force = _force;
    }
}
```

## Assets/Scripts/Weapons/LaserController.cs

```csharp
using System.Collections;
using UnityEngine;

/// <summary>
/// LaserCannon（Aemeath）— 瞬发直线贯穿激光
/// 参照 LightningController（角色1）的瞬间伤害 + 纯视觉效果模式
///
/// 伤害：OverlapCircleAll(origin, range) + 点到直线垂直距离筛选 → 无物理碰撞，不推角色
/// 视觉：Laser.prefab（无 Collider/Rigidbody），靠 scale 延伸
///       中心放在 origin + dir * range/2 → 从角色到射程末端刚好铺满
///
/// 射程公式：range = BaseRange × _size + LaserRangeLevel × 3f
/// 宽度公式：visualWidth = BaseVisualWidth × _size + LaserRangeLevel × 0.1f
/// 伤害公式：_damage × (1 + LaserDamageLevel × 0.3f)
/// </summary>
public class LaserController : WeaponController
{
    public override int _weaponType { get { return (int)Define.Weapons.LaserCannon; } }

    // 手持激光炮的发射口（GunHole）。参照 ShotgunController：手里握着武器跟随鼠标旋转，
    // 激光从 GunHole 位置、沿武器朝向发射，而不是从角色脚底。
    [SerializeField]
    private GameObject gunhole;

    // 基础射程（单位）— 初始较短，WeaponData.size 继续乘上去
    private const float BaseRange = 10f;
    // 基础视觉宽度（单位）— 加宽让光束更明显
    private const float BaseVisualWidth = 2.2f;
    // 判定半宽（单位）— 敌人到激光线段的垂直距离 ≤ 此值才算命中
    private const float HitHalfWidth = 0.5f;
    // 激光柱显示时长（秒）— 足够看清起点/整条激光
    private const float LaserLifetime = 0.40f;
    // 每次 LaserRangeLevel 增加的射程（单位）— 射程加成减少
    private const float RangePerLevel = 1f;
    // 每次 LaserRangeLevel 增加的视觉宽度（单位）— 宽度加成增加
    private const float WidthPerLevel = 0.3f;

    private bool _isCool;
    private PlayerStat _stat;

    /// <summary>当前射程（单位），供 UI 右上角属性面板显示。范围 = 基础 × 武器尺寸 + 专属升级加成。</summary>
    public float CurrentRange
    {
        get
        {
            float r = BaseRange * _size;
            if (_stat != null) r += _stat.LaserRangeLevel * RangePerLevel;
            return r;
        }
    }

    /// <summary>当前激光视觉宽度（单位），供 UI 右上角属性面板显示。</summary>
    public float CurrentWidth
    {
        get
        {
            float w = BaseVisualWidth * _size;
            if (_stat != null) w += _stat.LaserRangeLevel * WidthPerLevel;
            return w;
        }
    }

    void Start()
    {
        _stat = GetComponentInParent<PlayerStat>();
    }

    protected override void SetWeaponStat()
    {
        base.SetWeaponStat();
        // 专属伤害加成：每级 LaserDamageLevel +30%
        if (_stat != null)
        {
            float mult = 1f + _stat.LaserDamageLevel * 0.3f;
            _damage = Mathf.RoundToInt(_damage * mult);
        }
    }

    void Update()
    {
        if (!CanOperate) return;
        // 手持激光炮跟随鼠标旋转（参照 ShotgunController 手--鼠标瞄准）
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.AngleAxis(SetAngleFromHandToCursor(), Vector3.forward), 5f * Time.deltaTime);
        if (!_isCool)
        {
            StartCoroutine(FireCycle());
        }
    }

    float SetAngleFromHandToCursor()
    {
        Vector3 dirVec = (AimPosition - transform.position).normalized;
        return Mathf.Atan2(dirVec.y, dirVec.x) * Mathf.Rad2Deg;
    }

    IEnumerator FireCycle()
    {
        _isCool = true;

        // 发射起点优先用 GunHole（手持武器炮口）——参照 ShotgunController
        Vector2 origin = GunOrigin();
        // 方向 = 鼠标方向（与手持武器旋转一致，保证激光从炮口射出）
        Vector3 aim = AimPosition;
        Vector2 dir = (Vector2)aim - origin;
        if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
        dir.Normalize();

        // 射程 / 视觉宽度（含专属升级加成）
        float range = CurrentRange;
        float visualWidth = CurrentWidth;

        // 1. 瞬间伤害判定（无物理碰撞）
        PerformDamage(origin, dir, range);

        // 2. 视觉激光柱
        StartCoroutine(LaserBeamVFX(origin, dir, range, visualWidth));

        yield return new WaitForSeconds(_cooldown);
        _isCool = false;
    }

    // 从 Player 的 SpriteRenderer bounds 算出脚底 Y — 射线从此出发
    Vector2 GetPlayerFootPosition()
    {
        // transform.position 是 SpawningPool 的根位置（= Player transform.position）
        // Player 的 SpriteRenderer 挂在 PlayerController 的 GameObject 上
        var owner = Owner;
        if (owner != null)
        {
            var sr = owner.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var b = sr.bounds;
                return new Vector2(owner.transform.position.x, b.min.y);
            }
        }
        // fallback: 用 transform.position，往下偏移 0.8f（角色半高约 0.8）
        Vector3 p = transform.position;
        return new Vector2(p.x, p.y - 0.8f);
    }

    // 发射起点 = 手持武器炮口（GunHole 世界坐标）。若未配置 gunhole 则退回角色脚底
    Vector2 GunOrigin()
    {
        if (gunhole != null)
            return gunhole.transform.position;
        return GetPlayerFootPosition();
    }

    void PerformDamage(Vector2 origin, Vector2 dir, float range)
    {
        var hits = Physics2D.OverlapCircleAll(origin, range, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            var bc = hit.GetComponentInParent<BaseController>();
            if (bc == null || bc._type != Define.WorldObject.Enemy) continue;

            Vector2 toTarget = (Vector2)hit.transform.position - origin;
            float distSq = toTarget.sqrMagnitude;
            if (distSq > range * range) continue;

            // Dot > 0 敌人在 origin 前方
            float dot = Vector2.Dot(toTarget, dir);
            if (dot < 0) continue;

            // 点到直线的垂直距离 ≤ HitHalfWidth
            Vector2 proj = dot * dir;
            Vector2 perp = toTarget - proj;
            if (perp.sqrMagnitude > HitHalfWidth * HitHalfWidth) continue;

            bc.OnDamaged(_damage, _force);
        }
    }

    IEnumerator LaserBeamVFX(Vector2 origin, Vector2 dir, float range, float visualWidth)
    {
        // 参照 LightningController：Spawn(Unknown) — 不带物理碰撞
        GameObject go = Managers.Game.Spawn(Define.WorldObject.Unknown, "Weapon/Laser");
        if (go == null) yield break;

        // 激光柱中心 = origin + dir × range/2 → 从角色到射程末端铺满
        go.transform.position = origin + dir * (range * 0.5f);

        // 旋转对齐（横长条 sprite，rotation 0° 朝右）
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0, 0, angle);

        // 缩放计算
        // Laser_beam.png: 2240×1680, PPU=2240 → 世界尺寸 w=1.0, h=0.75
        var sr = go.GetComponent<SpriteRenderer>();
        Sprite sprite = sr != null ? sr.sprite : null;
        float sw = sprite != null ? sprite.rect.width / sprite.pixelsPerUnit : 1.0f;
        float sh = sprite != null ? sprite.rect.height / sprite.pixelsPerUnit : 0.75f;

        float sx = range / Mathf.Max(sw, 0.01f);
        float sy = visualWidth / Mathf.Max(sh, 0.01f);
        go.transform.localScale = new Vector3(sx, sy, 1f);

        // 淡出销毁
        float t = 0f;
        Color startColor = sr != null ? sr.color : Color.white;
        while (t < LaserLifetime)
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                float alpha = Mathf.Clamp01(1f - t / LaserLifetime);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }
        Managers.Resource.Destroy(go);
    }
}
```

## Assets/Scripts/Weapons/LightningController.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LightningController : WeaponController
{
    public override int _weaponType { get { return (int)Define.Weapons.Lightning; } }


    private GameObject _playerUI = null;
    private Image _image_skill;

    private bool _isCool = false;
    protected override void SetWeaponStat()
    {
        base.SetWeaponStat();
    }

    private void OnEnable()
    {
        if (LanSession.Active) return;
        _playerUI = GameObject.Find("UI_Player");
        if (object.ReferenceEquals(_playerUI, null))
        {
            Managers.UI.ShowSceneUI<UI_Player>();
            return;
        }

        _image_skill = _playerUI.FindChild<Image>("CursorCoolTimeImg");
        _image_skill.gameObject.SetActive(true);
    }

    void Update()
    {
        if (!CanOperate) return;
        UpdateSkillCoolTimeImage();
        if (LanSession.Active ? LanSession.Instance.FireRequested(Owner) : Input.GetMouseButtonDown(0))
        {
            if (!_isCool)
            {
                StartCoroutine(DamageCoolTime());
                StartCoroutine(LightnigEffect());
                Vector3 worldMousePos = AimPosition;
                Collider2D[] collider2Ds = Physics2D.OverlapCircleAll((Vector2)worldMousePos, _size, LayerMask.GetMask("Enemy"));
                foreach (Collider2D coll in collider2Ds)
                {
                    GameObject go = coll.gameObject;
                    go.GetComponent<BaseController>().OnDamaged(_damage, _force);
                }
            }
        }
    }

    void UpdateSkillCoolTimeImage()
    {
        if (_image_skill == null) return;
        _image_skill.transform.position = Managers.Game.MousePos;
    }


    IEnumerator DamageCoolTime()
    {
        _isCool = true;
        float currentCooltime = _cooldown;
        while (currentCooltime > 0f)
        {
            currentCooltime -= Time.deltaTime;
            if (_image_skill != null) _image_skill.fillAmount = ((_cooldown - currentCooltime) / _cooldown);
            yield return new WaitForFixedUpdate();

        }
        _isCool = false;
    }
    IEnumerator LightnigEffect()
    {
        GameObject lightnigEffect = Managers.Game.Spawn(Define.WorldObject.Unknown, "Weapon/Lightning");
        lightnigEffect.transform.position = AimPosition;
        yield return new WaitForSeconds(0.5f);
        Managers.Resource.Destroy(lightnigEffect);
    }
}
```

## Assets/Scripts/Weapons/PoisonController.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonController : WeaponController
{
    public override int _weaponType { get { return (int)Define.Weapons.Poison; } }

    private bool _isCool = false;


    private void Update()
    {
        if (!CanOperate) return;
        if (!_isCool)
        {
            StartCoroutine(DamageCoolTime());
            Collider2D[] collider2Ds = Physics2D.OverlapCircleAll((Vector2)transform.position, _size, LayerMask.GetMask("Enemy"));
            foreach(Collider2D coll in collider2Ds)
            {
                GameObject go = coll.gameObject;
                go.GetComponent<BaseController>().OnDamaged(_damage, _force);
            }
        }

    }

    protected override void SetWeaponStat()
    {
        base.SetWeaponStat();
        Transform DamageZonEffect = transform.GetChild(0);
        DamageZonEffect.localScale = Vector3.one * _size * 2;
    }

    IEnumerator DamageCoolTime()
    {
        _isCool = true;
        yield return new WaitForSeconds(_cooldown);
        _isCool = false;
    }
}
```

## Assets/Scripts/Weapons/Projectile/Bullet.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector3 _dir = new Vector3(1, 0, 0);

    public Vector3 BulletDir { set { _dir = value; } }

    public int _damage;
    public float _movSpeed;
    public int _penetrate;
    public float _force;
    private int piercing = 0;
    private float _lifeTime = 0.3f;
    private float _createTime = 0f;
    private void OnEnable()
    {
        piercing = 0;
        _createTime = Managers.GameTime;
    }

    void FixedUpdate()
    {
        if (Managers.GameTime - _createTime > _lifeTime)
        {
            Managers.Resource.Destroy(gameObject);
        }
        OnMove();
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        GameObject go = col.gameObject;
        if (go.CompareTag("Enemy"))
        {
            go.GetComponent<BaseController>().OnDamaged(_damage, _force);
            piercing += 1;
            if(piercing >= _penetrate)
                Managers.Resource.Destroy(gameObject);
        }
    }

    void OnMove()
    {
        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0,0,angle-90);
        transform.position += _dir * (_movSpeed * Time.fixedDeltaTime);
    }
}
```

## Assets/Scripts/Weapons/Projectile/EnemyBullet.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public Vector3 _dir = new Vector3(1, 0, 0);

    [SerializeField]
    private float lifeTime = 2f;
    [SerializeField]
    private float createTime = 0f;

    public int _damage = 3;
    public float _speed = 6f;

    private void OnEnable()
    {
        createTime = Managers.GameTime;
    }

    void FixedUpdate()
    {
        if (Managers.GameTime - createTime > lifeTime)
        {
            Managers.Resource.Destroy(gameObject);
        }
        OnMove();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject go = other.gameObject;
        if (go.CompareTag("Player"))
        {
            go.GetComponent<BaseController>().OnDamaged(_damage);
            Managers.Resource.Destroy(gameObject);
        }
    }


    void OnMove()
    {
        transform.position += _dir * (_speed * Time.fixedDeltaTime);
    }
}
```

## Assets/Scripts/Weapons/Projectile/Explosion.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public int _damage = 10;
    public float _force = 0f;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Enemy")
        {
            collision.GetComponent<BaseController>().OnDamaged(_damage, _force);
        }
    }

    public void DoDestroy()
    {
        Managers.Resource.Destroy(transform.parent.gameObject);
    }
}
```

## Assets/Scripts/Weapons/Projectile/Fireball.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : MonoBehaviour
{

    public Vector3 _dir = new Vector3(1, 0, 0);


    [SerializeField]
    private float _lifeTime = 2f;
    [SerializeField]
    private float _createTime = 0f;

    public int _damage = 10;
    public float _speed = 3f;
    public float _force = 0f;
    public float _size = 1f;
    public int _panatrate = 1;
    private int _piercing = 0;
    private void OnEnable()
    {
        _piercing = 0;
        _createTime = Managers.GameTime;
    }


    void FixedUpdate()
    {
        if (Managers.GameTime - _createTime > _lifeTime)
        {
            DoExplosion();
        }
        OnMove();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject go = other.gameObject;
        if (go.CompareTag("Enemy"))
        {
            _piercing++;
            if(_piercing >= _panatrate)
            {
                DoExplosion();
            }
            else
                go.GetComponent<BaseController>().OnDamaged(_damage, _force/2);
        }
    }
    void DoExplosion()
    {
        Managers.Sound.Play("Explosion_02");
        GameObject explosion = Managers.Game.Spawn(Define.WorldObject.Weapon,"Weapon/Explosion", transform.position);
        explosion.transform.localScale = Vector3.one * _size;
        Explosion explosionStat = explosion.FindChild<Explosion>();
        explosionStat._damage = (int)(_damage * 1.1);
        explosionStat._force = _force;
        Managers.Resource.Destroy(gameObject);
    }

    void OnMove()
    {
        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        transform.position += _dir * (_speed * Time.fixedDeltaTime);
    }
}
```

## Assets/Scripts/Weapons/Projectile/Knife.cs

```csharp
using UnityEngine;

public class Knife : MonoBehaviour
{
    public Vector3 _dir = new Vector3(1, 0, 0);

    [SerializeField]
    private float _lifeTime = 3f;
    [SerializeField]
    private float _createTime = 0f;

    public int _damage = 10;
    public float _speed = 10f;
    public float _force = 0f;
    public int _panatrate = 1;
    private int _piercing = 0;


    private void OnEnable()
    {
        _piercing = 0;
        _createTime = Managers.GameTime;
    }

    void FixedUpdate()
    {
        if (Managers.GameTime - _createTime > _lifeTime)
        {
            Managers.Resource.Destroy(gameObject);
        }
        OnMove();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject go = other.gameObject;
        if (go.CompareTag("Enemy"))
        {

            go.GetComponent<BaseController>().OnDamaged(_damage, _force);
            _piercing++;
            if (_piercing >= _panatrate)
                Managers.Resource.Destroy(gameObject);
        }
    }


    void OnMove()
    {
        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0,0,angle);
        transform.position += _dir * (_speed * Time.fixedDeltaTime);
    }
}
```

## Assets/Scripts/Weapons/ShotgunController.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR;
using Random = UnityEngine.Random;

public class ShotgunController : WeaponController
{
    public override int _weaponType { get { return (int)Define.Weapons.Shotgun; } }

    [SerializeField]
    private GameObject gunhole;

    private bool _isCool = false;
    private float _bulletTargetRange = 60f;


    void Update()
    {
        if (!CanOperate) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.AngleAxis(SetAngleFromHandToCursor(), Vector3.forward), 5f * Time.deltaTime);
        if (!_isCool)
        {
            StartCoroutine(ShotCoolTime());
        }
    }

    void SetBulletAngle()
    {
        float bulletAngle = _bulletTargetRange / (_countPerCreate+1);
        float angle = SetAngleFromHandToCursor();
        float startBulletAngle = angle - (_bulletTargetRange/2);
        for (int i = 1; i <= _countPerCreate; i++)
        {
            CreateBullet(i, bulletAngle, startBulletAngle);
        }

    }

    void CreateBullet(int num, float bulletAngle, float startBulletAngle)
    {
        GameObject bullet = Managers.Game.Spawn(Define.WorldObject.Unknown, "Weapon/Bullet", gunhole.transform.position);
        //set damage, dir
        Bullet bulletStat = bullet.GetOrAddComponent<Bullet>();
        float _ang = startBulletAngle + bulletAngle * num + Random.Range(-5f, 5f);
        Vector3 bulletDir = new Vector3(Mathf.Cos(_ang * Mathf.Deg2Rad), Mathf.Sin(_ang * Mathf.Deg2Rad), 0);
        bulletStat.BulletDir = bulletDir;
        bulletStat._damage = _damage;
        bulletStat._movSpeed = _movSpeed;
        bulletStat._penetrate = _penetrate;
        bulletStat._force = _force;
    }

    float SetAngleFromHandToCursor()
    {
        Vector3 dirVec = (AimPosition - transform.position).normalized;
        return Mathf.Atan2(dirVec.y, dirVec.x) * Mathf.Rad2Deg;

    }


    IEnumerator ShotCoolTime()
    {
        _isCool = true;
        Managers.Sound.Play("Shoot_02");
        SetBulletAngle();
        yield return new WaitForSeconds(_cooldown);

        _isCool = false;
    }
}
```

## Assets/Scripts/Weapons/SpinController.cs

```csharp
using System.Collections.Generic;
using Data;
using UnityEngine;

public class SpinController : WeaponController
{
    public override int _weaponType { get { return (int)Define.Weapons.Spin; } }

    private List<GameObject> _spinWeapons = new List<GameObject>();

    private float _circleR = 4;
    private float _deg = 0;

    void FixedUpdate()
    {
        if (!CanOperate) return;
        _deg += Time.deltaTime * _movSpeed;
        if (_deg < 360)
        {
            for (int i = 0; i < _countPerCreate; i++)
            {
                var rad = Mathf.Deg2Rad * (_deg + (i * (360 / _countPerCreate)));
                var x = _circleR * Mathf.Sin(rad);
                var y = _circleR * Mathf.Cos(rad);
                _spinWeapons[i].transform.position = transform.position + new Vector3(x, y);
                _spinWeapons[i].transform.rotation = Quaternion.Euler(0, 0, _deg * Mathf.Max(5f, _movSpeed/25));
            }
        }
        else
        {
            _deg = 0;
        }
    }

    //Todo
    protected void SetWeapon(GameObject weapon)
    {
        SpinWeapon spin = weapon.GetOrAddComponent<SpinWeapon>();
        spin.Owner = Owner;
        spin._damage = _damage;
        spin._force = _force;
    }

    protected override void SetWeaponStat()
    {
        base.SetWeaponStat();
        while(_spinWeapons.Count > _countPerCreate)
        {
            GameObject spin = _spinWeapons[_spinWeapons.Count-1];
            Managers.Resource.Destroy(spin);
            _spinWeapons.RemoveAt(_spinWeapons.Count - 1);
        }

        while(_spinWeapons.Count < _countPerCreate)
        {
            GameObject spinWeapon = Managers.Game.Spawn(Define.WorldObject.Weapon, "Weapon/Spin");
            SetWeapon(spinWeapon);
            _spinWeapons.Add(spinWeapon);
        }
        foreach (var spin in _spinWeapons) SetWeapon(spin);
    }
}
```

## Assets/Scripts/Weapons/SpinWeapon.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

public class SpinWeapon : MonoBehaviour
{
    public PlayerController Owner;

    public int _damage = 10;
    public float _force = 0f;

    public float _createTime = 0f;


    private void OnEnable()
    {
        _createTime = Managers.GameTime;
    }



    private void OnTriggerEnter2D(Collider2D other)
    {
        if (LanSession.Active && (Owner == null || LanSession.Instance.IsDown(Owner) || !LanSession.Instance.CanSimulate)) return;
        GameObject go = other.gameObject;
        if (go.CompareTag("Enemy"))
        {
            go.GetComponent<BaseController>().OnDamaged(_damage, _force);
        }
    }
}
```

## Assets/Scripts/Weapons/WeaponController.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponController : MonoBehaviour
{
    protected GameObject _player = null;
    private PlayerStat _playerStat;
    private Dictionary<int, Data.WeaponData> _weaponData;
    private Dictionary<int, Data.WeaponLevelData> _weaponStat;
    public Define.WorldObject _type = Define.WorldObject.Weapon;
    private Animator _anime;
    protected PlayerController Owner => _player != null ? _player.GetComponent<PlayerController>() : null;
    protected bool CanOperate => !LanSession.Active || (Owner != null && LanSession.Instance.CanSimulate && !LanSession.Instance.IsDown(Owner));
    protected Vector3 AimPosition => LanSession.Active ? LanSession.Instance.Aim(Owner) : Managers.Game.WorldMousePos;


    public abstract int _weaponType { get; }
    private int _level = 1;
    public int Level
    {
        get
        {
            Debug.Log($"Get level : {_level}");
            return _level;
        }
        set
        {
            Debug.Log($"Set level : {_level}");
            _level = value;
            SetWeaponStat();
        }
    }

    public int _damage=1;
    public float _movSpeed=1;
    public float _force=1;
    public float _cooldown=1;
    public float _size=1;
    public int _penetrate=1;
    public int _countPerCreate=1;



    void Awake()
    {
        var ownerStat = GetComponentInParent<PlayerStat>();
        _player = ownerStat != null ? ownerStat.gameObject : Managers.Game.getPlayer();
        _playerStat = _player.GetComponent<PlayerStat>();
        _weaponData = Managers.Data.WeaponData;
        _anime = transform.GetComponent<Animator>();
        _weaponStat = MakeLevelDataDict(_weaponType);

    }

    protected virtual void SetWeaponStat()
    {
        if (_level > 5)
            _level = 5;

        _damage = (int)(_weaponStat[_level].damage * ((float)(100+ _playerStat.Damage)/100f));
        _movSpeed = _weaponStat[_level].movSpeed;
        _force = _weaponStat[_level].force;
        _cooldown = _weaponStat[_level].cooldown * (100f/(100f +_playerStat.Cooldown));
        _size = _weaponStat[_level].size;
        _penetrate = _weaponStat[_level].penetrate;
        _countPerCreate = _weaponStat[_level].countPerCreate + _playerStat.Amount;
    }

    protected Dictionary<int, Data.WeaponLevelData> MakeLevelDataDict(int weaponID)
    {
        Dictionary<int, Data.WeaponLevelData> _weaponLevelData = new Dictionary<int, Data.WeaponLevelData>();
        foreach (Data.WeaponLevelData weaponLevelData in _weaponData[weaponID].weaponLevelData)
            _weaponLevelData.Add(weaponLevelData.level, weaponLevelData);
        return _weaponLevelData;

    }



}
```
