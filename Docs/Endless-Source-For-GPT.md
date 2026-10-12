# 无尽模式核心与集成代码全文

包含三个无尽模式核心类，以及场景、角色、怪物、相机、管理器和入口集成代码。其余依赖与资源位于完整项目中。

来源版本：`18bd017afd4d75853a39f1eb7a3e5485b1b495a4`。原始文件路径保留在每段标题。

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
