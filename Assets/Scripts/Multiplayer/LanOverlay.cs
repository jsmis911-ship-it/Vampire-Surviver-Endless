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
