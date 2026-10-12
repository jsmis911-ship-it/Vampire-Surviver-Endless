using System.Collections;
using UnityEngine;

/// <summary>
/// LaserCannon（Aemeath）— 瞬发直线贯穿激光
/// 参照 LightningController（角色1）的瞬间伤害 + 纯视觉效果模式
///
/// 伤害：OverlapCircleAll(origin, range) + 点到直线垂直距离筛选 → 无物理碰撞，不推角色
/// 视觉：Laser.prefab（无 Collider/Rigidbody），靠 scale 延伸
///       中心放在 origin + dir * range/2 → 从角色到射程末端刚好铺满
///
/// 射程公式：range = BaseRange × _size + LaserRangeLevel × 3f
/// 宽度公式：visualWidth = BaseVisualWidth × _size + LaserRangeLevel × 0.1f
/// 伤害公式：_damage × (1 + LaserDamageLevel × 0.3f)
/// </summary>
public class LaserController : WeaponController
{
    public override int _weaponType { get { return (int)Define.Weapons.LaserCannon; } }

    // 手持激光炮的发射口（GunHole）。参照 ShotgunController：手里握着武器跟随鼠标旋转，
    // 激光从 GunHole 位置、沿武器朝向发射，而不是从角色脚底。
    [SerializeField]
    private GameObject gunhole;

    // 基础射程（单位）— 初始较短，WeaponData.size 继续乘上去
    private const float BaseRange = 10f;
    // 基础视觉宽度（单位）— 加宽让光束更明显
    private const float BaseVisualWidth = 2.2f;
    // 判定半宽（单位）— 敌人到激光线段的垂直距离 ≤ 此值才算命中
    private const float HitHalfWidth = 0.5f;
    // 激光柱显示时长（秒）— 足够看清起点/整条激光
    private const float LaserLifetime = 0.40f;
    // 每次 LaserRangeLevel 增加的射程（单位）— 射程加成减少
    private const float RangePerLevel = 1f;
    // 每次 LaserRangeLevel 增加的视觉宽度（单位）— 宽度加成增加
    private const float WidthPerLevel = 0.3f;

    private bool _isCool;
    private PlayerStat _stat;

    /// <summary>当前射程（单位），供 UI 右上角属性面板显示。范围 = 基础 × 武器尺寸 + 专属升级加成。</summary>
    public float CurrentRange
    {
        get
        {
            float r = BaseRange * _size;
            if (_stat != null) r += _stat.LaserRangeLevel * RangePerLevel;
            return r;
        }
    }

    /// <summary>当前激光视觉宽度（单位），供 UI 右上角属性面板显示。</summary>
    public float CurrentWidth
    {
        get
        {
            float w = BaseVisualWidth * _size;
            if (_stat != null) w += _stat.LaserRangeLevel * WidthPerLevel;
            return w;
        }
    }

    void Start()
    {
        _stat = GetComponentInParent<PlayerStat>();
    }

    protected override void SetWeaponStat()
    {
        base.SetWeaponStat();
        // 专属伤害加成：每级 LaserDamageLevel +30%
        if (_stat != null)
        {
            float mult = 1f + _stat.LaserDamageLevel * 0.3f;
            _damage = Mathf.RoundToInt(_damage * mult);
        }
    }

    void Update()
    {
        if (!CanOperate) return;
        // 手持激光炮跟随鼠标旋转（参照 ShotgunController 手--鼠标瞄准）
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.AngleAxis(SetAngleFromHandToCursor(), Vector3.forward), 5f * Time.deltaTime);
        if (!_isCool)
        {
            StartCoroutine(FireCycle());
        }
    }

    float SetAngleFromHandToCursor()
    {
        Vector3 dirVec = (AimPosition - transform.position).normalized;
        return Mathf.Atan2(dirVec.y, dirVec.x) * Mathf.Rad2Deg;
    }

    IEnumerator FireCycle()
    {
        _isCool = true;

        // 发射起点优先用 GunHole（手持武器炮口）——参照 ShotgunController
        Vector2 origin = GunOrigin();
        // 方向 = 鼠标方向（与手持武器旋转一致，保证激光从炮口射出）
        Vector3 aim = AimPosition;
        Vector2 dir = (Vector2)aim - origin;
        if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
        dir.Normalize();

        // 射程 / 视觉宽度（含专属升级加成）
        float range = CurrentRange;
        float visualWidth = CurrentWidth;

        // 1. 瞬间伤害判定（无物理碰撞）
        PerformDamage(origin, dir, range);

        // 2. 视觉激光柱
        StartCoroutine(LaserBeamVFX(origin, dir, range, visualWidth));

        yield return new WaitForSeconds(_cooldown);
        _isCool = false;
    }

    // 从 Player 的 SpriteRenderer bounds 算出脚底 Y — 射线从此出发
    Vector2 GetPlayerFootPosition()
    {
        // transform.position 是 SpawningPool 的根位置（= Player transform.position）
        // Player 的 SpriteRenderer 挂在 PlayerController 的 GameObject 上
        var owner = Owner;
        if (owner != null)
        {
            var sr = owner.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var b = sr.bounds;
                return new Vector2(owner.transform.position.x, b.min.y);
            }
        }
        // fallback: 用 transform.position，往下偏移 0.8f（角色半高约 0.8）
        Vector3 p = transform.position;
        return new Vector2(p.x, p.y - 0.8f);
    }

    // 发射起点 = 手持武器炮口（GunHole 世界坐标）。若未配置 gunhole 则退回角色脚底
    Vector2 GunOrigin()
    {
        if (gunhole != null)
            return gunhole.transform.position;
        return GetPlayerFootPosition();
    }

    void PerformDamage(Vector2 origin, Vector2 dir, float range)
    {
        var hits = Physics2D.OverlapCircleAll(origin, range, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            var bc = hit.GetComponentInParent<BaseController>();
            if (bc == null || bc._type != Define.WorldObject.Enemy) continue;

            Vector2 toTarget = (Vector2)hit.transform.position - origin;
            float distSq = toTarget.sqrMagnitude;
            if (distSq > range * range) continue;

            // Dot > 0 敌人在 origin 前方
            float dot = Vector2.Dot(toTarget, dir);
            if (dot < 0) continue;

            // 点到直线的垂直距离 ≤ HitHalfWidth
            Vector2 proj = dot * dir;
            Vector2 perp = toTarget - proj;
            if (perp.sqrMagnitude > HitHalfWidth * HitHalfWidth) continue;

            bc.OnDamaged(_damage, _force);
        }
    }

    IEnumerator LaserBeamVFX(Vector2 origin, Vector2 dir, float range, float visualWidth)
    {
        // 参照 LightningController：Spawn(Unknown) — 不带物理碰撞
        GameObject go = Managers.Game.Spawn(Define.WorldObject.Unknown, "Weapon/Laser");
        if (go == null) yield break;

        // 激光柱中心 = origin + dir × range/2 → 从角色到射程末端铺满
        go.transform.position = origin + dir * (range * 0.5f);

        // 旋转对齐（横长条 sprite，rotation 0° 朝右）
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0, 0, angle);

        // 缩放计算
        // Laser_beam.png: 2240×1680, PPU=2240 → 世界尺寸 w=1.0, h=0.75
        var sr = go.GetComponent<SpriteRenderer>();
        Sprite sprite = sr != null ? sr.sprite : null;
        float sw = sprite != null ? sprite.rect.width / sprite.pixelsPerUnit : 1.0f;
        float sh = sprite != null ? sprite.rect.height / sprite.pixelsPerUnit : 0.75f;

        float sx = range / Mathf.Max(sw, 0.01f);
        float sy = visualWidth / Mathf.Max(sh, 0.01f);
        go.transform.localScale = new Vector3(sx, sy, 1f);

        // 淡出销毁
        float t = 0f;
        Color startColor = sr != null ? sr.color : Color.white;
        while (t < LaserLifetime)
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                float alpha = Mathf.Clamp01(1f - t / LaserLifetime);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }
        Managers.Resource.Destroy(go);
    }
}
