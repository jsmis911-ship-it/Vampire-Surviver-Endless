#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 联机调试工具菜单（仅 Unity Editor 下可用，打包自动屏蔽）。
/// 使用方法：Unity Play 模式下 → 顶部菜单栏 Tools → LAN Debug → 一键进入联机战斗
/// 打包时 #if UNITY_EDITOR 宏会把整文件从编译中排除，对 Release 产物零影响。
/// </summary>
public static class LanDebugMenu
{
    [MenuItem("Tools/LAN Debug/一键进入联机战斗 %#L")]
    public static void QuickStart()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[LAN Debug] 请先按 Play 进入播放模式，再点此菜单。");
            EditorApplication.isPlaying = true;
            // 场景加载需要时间，等几帧后再触发
            EditorApplication.delayCall += DoQuickStart;
            return;
        }
        DoQuickStart();
    }

    static void DoQuickStart()
    {
        Debug.Log("[LAN Debug] 一键进入联机战斗…");
        LanSession.Editor_QuickStartTest();
    }

    [MenuItem("Tools/LAN Debug/手动打开 LAN Lobby")]
    public static void OpenLobbyManually()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[LAN Debug] 请先按 Play 进入播放模式。");
            return;
        }
        // 走正常流程：从主菜单按钮点击触发（自动找字体）
        var menuBtn = UnityEngine.GameObject.Find("MultiplayerButton");
        if (menuBtn != null)
        {
            var btn = menuBtn.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.onClick.Invoke();
            return;
        }
        // 兜底：手动调用 Open（传默认字体 null）
        LanSession.Open(null);
    }
}
#endif
