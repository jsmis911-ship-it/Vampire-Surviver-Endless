using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 游戏教程弹窗：分三页讲解游戏背景/胜利条件、玩法介绍、键位操作。
/// 需配合 Prec UI_Tutorial.prefab：TitleText、ContentText、PageText，
/// PrevButton、NextButton、CloseButton、ProtectImage。
/// </summary>
public class UI_Tutorial : UI_Popup
{
    enum Images
    {
        ProtectImage
    }

    enum Buttons
    {
        CloseButton,
        PrevButton,
        NextButton
    }

    enum Texts
    {
        TitleText,
        ContentText,
        PageText
    }

    /// <summary>三页教程内容：背景/胜利条件、玩法、键位操作</summary>
    private static readonly string[] _pageContents =
    {
        // 第 1 页：游戏背景与胜利条件
        "<b>游戏背景</b>\n\n" +
        "这里是幽灵生存者世界，僵尸大军正不断涌向这里。\n" +
        "你是一名幸存者，必须在这片被怪物包围的战场上存活下去！\n\n" +
        "<b>胜利条件</b>\n" +
        "· 存活更长时间，逐步击退敌人\n" +
        "· 击败每隔一段时间出现的强大 Boss\n" +
        "· 每次击败 Boss 即可获胜，挑战更高难度",

        // 第 2 页：玩法介绍
        "<b>玩法介绍</b>\n\n" +
        "· 霰弹枪随鼠标自动射击，魔法阵用左键释放\n" +
        "· 重斧农夫跟随鼠标瞄准，亮起的扇形为挥砍范围\n" +
        "· 击杀怪物获得经验，升级后解锁属性或武器\n" +
        "· 消灭敌人会掉落经验、道具和宝箱\n" +
        "· 拾取道具可提升攻击力、攻速、移速等属性\n" +
        "· 合理搭配武器与属性加成，才能在后期存活\n" +
        "· 收集经验升级强化，越战越强！",

        // 第 3 页：键位操作
        "<b>键位操作</b>\n\n" +
        "· WASD / 方向键：移动角色\n" +
        "· 鼠标移动：瞄准方向（远程武器）\n" +
        "· 左 Shift：向移动方向冲刺，冲刺期间短暂无敌\n\n" +
        "操作简单，走位与冲刺是生存的关键！"
    };

    public override Define.PopupUIGroup PopupID { get { return Define.PopupUIGroup.UI_Tutorial; } }

    int _page = 0;

    public override void Init()
    {
        base.Init();
        Bind<Image>(typeof(Images));
        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));

        GetImage((int)Images.ProtectImage).gameObject.AddUIEvent(Close);
        GetButton((int)Buttons.CloseButton).gameObject.AddUIEvent(Close);

        GetButton((int)Buttons.PrevButton).gameObject.AddUIEvent(OnPrevPage);
        GetButton((int)Buttons.NextButton).gameObject.AddUIEvent(OnNextPage);

        ShowPage(0);
    }

    void ShowPage(int index)
    {
        _page = Mathf.Clamp(index, 0, _pageContents.Length - 1);

        GetText((int)Texts.TitleText).text = "游戏教程（" + (_page + 1) + "/" + _pageContents.Length + "）";
        GetText((int)Texts.ContentText).text = _pageContents[_page];
        GetText((int)Texts.PageText).text = (_page + 1) + " / " + _pageContents.Length;

        GetButton((int)Buttons.PrevButton).gameObject.SetActive(_page > 0);
        GetButton((int)Buttons.NextButton).gameObject.SetActive(_page < _pageContents.Length - 1);
    }

    void OnPrevPage(PointerEventData data)
    {
        ShowPage(_page - 1);
    }

    void OnNextPage(PointerEventData data)
    {
        ShowPage(_page + 1);
    }

    void Close(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        ClosePopupUI();
    }
}
