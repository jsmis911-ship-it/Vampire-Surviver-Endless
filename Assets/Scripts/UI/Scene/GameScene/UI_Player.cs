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
