using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    static readonly float[] ViewHeights = { 7.5f, 10f, 15f };
    public const int MinViewLevel = 0;
    public const int MaxViewLevel = 2;
    public static int ViewLevel { get; private set; } = 1;
    public static float ViewHeight => ViewHeights[ViewLevel];
    Camera _camera;
    void Awake()
    {
        _camera = GetComponent<Camera>();
        ApplyViewHeight(_camera);
    }
    public static float HeightForLevel(float level)
    {
        return ViewHeights[Mathf.Clamp(Mathf.RoundToInt(level), MinViewLevel, MaxViewLevel)];
    }
    public static void SetViewLevel(float level)
    {
        ViewLevel = Mathf.Clamp(Mathf.RoundToInt(level), MinViewLevel, MaxViewLevel);
        Camera camera = Camera.main;
        if (camera != null) ApplyViewHeight(camera);
    }
    static void ApplyViewHeight(Camera camera)
    {
        PixelPerfectCamera pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
        camera.orthographicSize = pixelPerfect == null
            ? ViewHeight
            : pixelPerfect.CorrectCinemachineOrthoSize(ViewHeight);
    }

    GameObject _player = null;

    Vector3 _delta = new Vector3(0, 0, -10f);

    public void SetPlayer(GameObject player)
    {
        _player = player;
        bool dungeon = EndlessMode.Selected && !LanSession.Active;
        transform.rotation = dungeon ? Quaternion.Euler(24f, 0, 0) : Quaternion.identity;
        _delta = dungeon ? new Vector3(0, 4.45f, -10f) : new Vector3(0, 0, -10f);
    }
    void Update()
    {
        if (object.ReferenceEquals(_player, null))
            return;

        transform.position = Vector3.Slerp(transform.position, _player.transform.position + _delta, 0.1f);
    }
}
