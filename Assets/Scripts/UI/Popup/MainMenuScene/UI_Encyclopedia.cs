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
