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
