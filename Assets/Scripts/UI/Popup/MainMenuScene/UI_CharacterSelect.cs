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
