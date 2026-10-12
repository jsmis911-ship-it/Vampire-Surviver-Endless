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
