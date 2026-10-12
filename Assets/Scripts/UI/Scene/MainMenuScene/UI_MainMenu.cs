using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UI_MainMenu : UI_Scene
{
    enum Images
    {
        BackgroundImg,
        FrontImage,
        Logo,
    }

    enum ImageObjects
    {
        Farmers,
        Monsters
    }

    enum Texts
    {
        PresstoStartText
    }

    enum Buttons
    {
        GamePlayButton,
        GameExitButton
    }

    private Animator _anime;
    private Image backgroundImg;
    private bool _animeOver;
    private GameObject _encyclopediaBtn;
    private GameObject _tutorialBtn;
    private GameObject _multiplayerBtn;
    private GameObject _endlessBtn;

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(ImageObjects));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Button>(typeof(Buttons));

        ApplyFullHdTypography();

        backgroundImg = GetImage((int)Images.BackgroundImg);
        _anime = backgroundImg.transform.GetComponent<Animator>();

        GetImage((int)Images.FrontImage).gameObject.AddUIEvent(SetAnimationOver);

        GetButton((int)Buttons.GamePlayButton).gameObject.AddUIEvent(ShowCharacterSelectUI);
        GetButton((int)Buttons.GameExitButton).gameObject.AddUIEvent(ExitGame);
        CreateFeatureButtons();
    }

    /// <summary>运行时复制退出按钮生成"道具图鉴"与"游戏教程"两个按钮，避免手工编辑预制体布局</summary>
    void CreateFeatureButtons()
    {
        Button exitBtn = GetButton((int)Buttons.GameExitButton);

        // 道具图鉴按钮
        GameObject encyclopedia = CreateCopyButton(exitBtn, "EncyclopediaButton", "道具图鉴", -280f, ShowEncyclopediaUI);
        _encyclopediaBtn = encyclopedia;

        // 游戏教程按钮
        GameObject tutorial = CreateCopyButton(exitBtn, "TutorialButton", "游戏教程", -380f, ShowTutorialUI);
        _tutorialBtn = tutorial;

        _multiplayerBtn = CreateCopyButton(exitBtn, "MultiplayerButton", "多人游戏 · 测试版", 0f, ShowMultiplayer);
        _endlessBtn = CreateCopyButton(exitBtn, "EndlessButton", "无尽模式 · 守护核心", -180f, ShowEndless);
        _endlessBtn.GetComponent<UI_EventHandler>().OnClickHandler = ShowEndless;
        _endlessBtn.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
        var multiplayerRect = (RectTransform)_multiplayerBtn.transform;
        multiplayerRect.anchorMin = multiplayerRect.anchorMax = multiplayerRect.pivot = new Vector2(1f, 0f);
        multiplayerRect.sizeDelta = new Vector2(380f, 76f);
        multiplayerRect.anchoredPosition = new Vector2(-44f, 42f);
        _multiplayerBtn.GetComponentInChildren<TextMeshProUGUI>(true).fontSize = 30f;
        // Copied UI_EventHandler delegates must not retain the exit action.
        _multiplayerBtn.GetComponent<UI_EventHandler>().OnClickHandler = ShowMultiplayer;
        _multiplayerBtn.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();

        // Keep the existing menu actions available beside the new mode.
        RectTransform exitRect = (RectTransform)exitBtn.transform;
        exitRect.anchoredPosition = new Vector2(0f, -480f);
    }

    /// <summary>复制一个按钮并设置标签、位置与点击事件</summary>
    GameObject CreateCopyButton(Button src, string name, string label, float yPos, Action<PointerEventData> action)
    {
        GameObject go = UnityEngine.Object.Instantiate(src.gameObject, src.transform.parent);
        go.name = name;

        TextMeshProUGUI labelTxt = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelTxt != null)
        {
            labelTxt.name = name + "Text";
            labelTxt.text = label;
        }

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(340f, 84f);
        rect.anchoredPosition = new Vector2(0f, yPos);

        // 初始不激活：与"开始/退出"按钮及"按任意键开始"提示一样，待开场动画结束、
        // "按任意键开始"提示出现时，由 SetAnimationOver 统一激活。
        go.SetActive(false);
        go.AddUIEvent(action);
        return go;
    }

    void ApplyFullHdTypography()
    {
        // The title is sprite artwork, not text. Never stretch its glyphs.
        Image logo = GetImage((int)Images.Logo);
        logo.preserveAspect = true;
        RectTransform titleRect = logo.rectTransform;
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -70f);
        titleRect.sizeDelta = new Vector2(1200f, 340f);

        TextMeshProUGUI prompt = GetText((int)Texts.PresstoStartText);
        prompt.enableAutoSizing = false;
        prompt.fontSize = 44f;
        prompt.alignment = TextAlignmentOptions.Center;
        RectTransform promptRect = prompt.rectTransform;
        promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);
        promptRect.anchoredPosition = new Vector2(0f, -260f);
        promptRect.sizeDelta = new Vector2(640f, 90f);

        foreach (Buttons buttonId in System.Enum.GetValues(typeof(Buttons)))
        {
            Button button = GetButton((int)buttonId);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(340f, 84f);
            rect.anchoredPosition = new Vector2(0f,
                buttonId == Buttons.GamePlayButton ? -80f : -480f);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
                continue;
            label.enableAutoSizing = false;
            label.fontSize = 40f;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(16f, 8f);
            label.rectTransform.offsetMax = new Vector2(-16f, -8f);
        }
    }

    void SetAnimationOver(PointerEventData data)
    {
        _animeOver = backgroundImg.transform.GetComponent<AnimeOver>()._animeOver;
        if (!_animeOver)
        {
            _anime.Play("MainGameStartAnime", -1, 1.0f);
        }
        else
        {
            Managers.Resource.Destroy(GetText((int)Texts.PresstoStartText).gameObject);
            foreach(Buttons button in System.Enum.GetValues(typeof(Buttons)))
            {
                GetButton((int)button).gameObject.SetActive(true);
            }
            if (_encyclopediaBtn != null)
                _encyclopediaBtn.SetActive(true);
            if (_tutorialBtn != null)
                _tutorialBtn.SetActive(true);
            if (_multiplayerBtn != null)
                _multiplayerBtn.SetActive(true);
            if (_endlessBtn != null)
                _endlessBtn.SetActive(true);
            Managers.Resource.Destroy(GetImage((int)Images.FrontImage).gameObject);
        }
    }
    void ShowCharacterSelectUI(PointerEventData data)
    {
        EndlessMode.Selected = false;
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Debug.Log("Show!");
        Managers.UI.ShowPopupUI<UI_CharacterSelect>();
    }

    void ShowEndless(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        EndlessMode.Selected = true;
        Managers.UI.ShowPopupUI<UI_CharacterSelect>();
    }

    void ShowMultiplayer(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        LanSession.Open(_multiplayerBtn.GetComponentInChildren<TextMeshProUGUI>().font);
    }

    void ShowEncyclopediaUI(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Managers.UI.ShowPopupUI<UI_Encyclopedia>();
    }

    void ShowTutorialUI(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Managers.UI.ShowPopupUI<UI_Tutorial>();
    }

    void ExitGame(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
