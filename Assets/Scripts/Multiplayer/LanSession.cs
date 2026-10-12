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
