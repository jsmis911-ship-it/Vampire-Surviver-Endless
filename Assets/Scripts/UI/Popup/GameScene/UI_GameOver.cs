using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UI_GameOver : UI_Popup
{
    enum Images
    {
        PretectImg,
        GameOverImg,
        DeadImg
    }

    enum Buttons
    {
        BackToMainButton
    }


    public override Define.PopupUIGroup PopupID { get { return Define.PopupUIGroup.UI_GameOver; } }
    public RuntimeAnimatorController[] _animeCon;
    private Animator _anime;


    public override void Init()
    {
        base.Init();
        Managers.Sound.Play("Dead");
        _anime = gameObject.FindChild<Animator>("DeadImg");
        // 防御：动画控制器数组可能为空或长度不足（角色有4个但动画只有2个），越界时回退到第一个
        if (_animeCon != null && _animeCon.Length > 0)
        {
            int idx = Mathf.Clamp(Managers.Game.StartPlayer.id - 1, 0, _animeCon.Length - 1);
            _anime.runtimeAnimatorController = _animeCon[idx];
        }
        Bind<Image>(typeof(Images));
        Bind<Button>(typeof(Buttons));

        GetImage((int)Images.PretectImg).gameObject.AddUIEvent(OnClickFinishAnime);
        GetButton((int)Buttons.BackToMainButton).gameObject.AddUIEvent(OnClickBackToMain);

    }

    void OnClickFinishAnime(PointerEventData data)
    {
        // 防御：当前 Animator 控制器（DeadAnime）可能没有 GameOverAnime 状态，避免警告
        if (_anime.HasState(0, Animator.StringToHash("GameOverAnime")))
            _anime.Play("GameOverAnime", 0, 1f);
        GetButton((int)Buttons.BackToMainButton).gameObject.SetActive(true);
    }

    void OnClickBackToMain(PointerEventData data)
    {
        Managers.Sound.Play("Select", Define.Sound.Effect);
        Managers.Scene.LoadScene(Define.SceneType.MainMenuScene);
        Managers.GamePlay();
    }
}
