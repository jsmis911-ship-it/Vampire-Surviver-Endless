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
