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
