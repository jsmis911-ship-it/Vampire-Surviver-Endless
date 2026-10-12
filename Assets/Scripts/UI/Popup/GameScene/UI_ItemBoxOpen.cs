using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UI_ItemBoxOpen : UI_Popup
{
    enum Buttons
    {
        ItemBoxButton
    }
    enum Texts
    {
        ItemBoxText
    }
    enum Images
    {
        ItemBoxPanel,
        ItemBoxImage,
        BackgroundImg
    }

    [SerializeField]
    private List<Transform> _weaponUILocation;

    private List<Define.Weapons> _weaponList;
    private PlayerStat _player;

    public override Define.PopupUIGroup PopupID { get { return Define.PopupUIGroup.UI_ItemBoxOpen; } }



    public override void Init()
    {
        base.Init();
        _player = Managers.Game.getPlayer().GetOrAddComponent<PlayerStat>();
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<Button>(typeof(Buttons));

        // 序列化槽位为空时，自动按名称收集武器显示槽位（weaponLocation / weaponLocation (N)）
        if (_weaponUILocation == null || _weaponUILocation.Count == 0)
        {
            _weaponUILocation = new List<Transform>();
            int idx = 0;
            Transform slot;
            while ((slot = Util.FindChild<Transform>(gameObject, idx == 0 ? "weaponLocation" : $"weaponLocation ({idx})", true)) != null)
            {
                _weaponUILocation.Add(slot);
                idx++;
            }
        }

        GetImage((int)Images.BackgroundImg).gameObject.AddUIEvent(OnOpenChest);
    }

    void OnOpenChest(PointerEventData data)
    {
        Managers.Sound.Play("BoxOpen", Define.Sound.Effect);
        _weaponList = Managers.Event.SetRandomWeaponfromItemBox(_player);
        if (_weaponList == null)
        {
            if (_weaponUILocation.Count > 0)
            {
                GameObject go = Managers.Resource.Instantiate("UI/SubItem/WeaponInven", parent: _weaponUILocation[0].transform);
                Util.FindChild<Image>(go, "WeaponImg", true).sprite = Managers.Resource.LoadSprite("Health");
            }
        }
        else
            for (int i = 0; i < _weaponList.Count && i < _weaponUILocation.Count; i++)
            {
                GameObject go = Managers.Resource.Instantiate("UI/SubItem/WeaponInven", parent: _weaponUILocation[i].transform);
                Util.FindChild<Image>(go, "WeaponImg", true).sprite = Managers.Resource.LoadSprite(_weaponList[i].ToString());
            }
        Get<Image>((int)Images.ItemBoxImage).GetComponent<Animator>().Play("Open");

        Button btn = GetButton((int)Buttons.ItemBoxButton);
        btn.gameObject.SetActive(true);
        btn.gameObject.AddUIEvent(Close);
    }

    void Close(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Managers.Event.SetLevelUpWeaponfromItemBox(_weaponList, _player);
        Managers.UI.CloseAllGroupPopupUI(PopupID);
    }
}
