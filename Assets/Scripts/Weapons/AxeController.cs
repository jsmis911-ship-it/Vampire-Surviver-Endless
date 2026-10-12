using System.Collections.Generic;
using UnityEngine;

/// <summary>A rigid axe rotates around its grip; damage is a swept sector, not a sprite collider.</summary>
public sealed class AxeController : WeaponController
{
    const float WindupTime = 0.16f;
    const float SwingTime = 0.20f;
    const float RecoveryTime = 0.18f;
    const float AxeDetailScale = 0.55f;
    const float SlashRadius = 2f;

    [SerializeField] SpriteRenderer axeRenderer;
    [SerializeField] SpriteRenderer axeHeadRenderer;
    [SerializeField] SpriteRenderer handRenderer;
    [SerializeField] SpriteRenderer[] slashRenderers;
    [SerializeField] Sprite[] slashFrames;
    [SerializeField] SpriteRenderer rangeRenderer;
    [SerializeField] Sprite[] rangeFrames;

    readonly HashSet<BaseController> hitThisSwing = new HashSet<BaseController>();
    PlayerStat stats;
    SpriteRenderer bodyRenderer;
    float nextAttack, elapsed, aimAngle, swingRange, swingAngle, swingForce;
    int swingDamage;
    bool swinging, playedSwingSound;

    public override int _weaponType => (int)Define.Weapons.Axe;
    public float AttackRange => _size;
    public float AttackAngle { get; private set; } = 80f;
    public bool IsSwinging => swinging;

    protected override void SetWeaponStat()
    {
        base.SetWeaponStat();
        stats = GetComponentInParent<PlayerStat>();
        bodyRenderer = stats.GetComponent<SpriteRenderer>();
        var data = Managers.Data.WeaponData[_weaponType].weaponLevelData.Find(v => v.level == Level);
        _size = data.size + stats.AxeRangeLevel * 0.4f;
        AttackAngle = data.angle + stats.AxeAngleLevel * 15f;
        _damage = Mathf.RoundToInt((data.damage + stats.AxeDamageLevel * 10) * (100f + stats.Damage) / 100f);
        // The windup, active swing and recovery must still fit within the attack interval.
        _cooldown = Mathf.Max(WindupTime + SwingTime + RecoveryTime + 0.06f, _cooldown);
        if (!swinging) SetVisualScale(_size);
    }

    void OnEnable()
    {
        nextAttack = Time.time + 0.25f;
        swinging = false;
        HideSlash();
    }

    void OnDisable()
    {
        swinging = false;
        hitThisSwing.Clear();
        HideSlash();
        if (rangeRenderer != null) rangeRenderer.enabled = false;
        if (axeHeadRenderer != null) axeHeadRenderer.enabled = false;
        if (handRenderer != null) handRenderer.enabled = false;
    }

    void Update()
    {
        if (stats == null || axeRenderer == null) return;
        if (stats.HP <= 0 || (LanSession.Active && LanSession.Instance.IsDown(Owner)))
        {
            swinging = false;
            axeRenderer.enabled = false;
            if (handRenderer != null) handRenderer.enabled = false;
            if (axeHeadRenderer != null) axeHeadRenderer.enabled = false;
            if (rangeRenderer != null) rangeRenderer.enabled = false;
            HideSlash();
            return;
        }
        if (!CanOperate || Managers.gameStop || Time.deltaTime <= 0) return;
        axeRenderer.enabled = true;
        if (handRenderer != null) handRenderer.enabled = true;
        // Farmer sprites are centered on the torso; the grip belongs at the cuff.
        transform.localPosition = new Vector3((bodyRenderer.flipX ? -4f : 4f) / 18f, -5f / 18f, 0);
        if (axeHeadRenderer != null) axeHeadRenderer.enabled = true;
        Vector2 direction = (Vector2)AimPosition - (Vector2)transform.position;
        // Keep the last direction when the cursor is exactly on the grip.
        if (direction.sqrMagnitude > 0.0001f)
            aimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        ShowRange();

        if (!swinging)
        {
            axeRenderer.transform.localRotation = Quaternion.Euler(0, 0, aimAngle + AttackAngle * 0.5f + 15f);
            if (Time.time < nextAttack) return;

            swinging = true;
            playedSwingSound = false;
            elapsed = 0;
            swingRange = AttackRange;
            swingAngle = AttackAngle;
            swingDamage = _damage;
            swingForce = _force;
            nextAttack = Time.time + _cooldown;
            hitThisSwing.Clear();
            SetVisualScale(swingRange);
        }

        float previousElapsed = elapsed;
        elapsed += Time.deltaTime;
        float half = swingAngle * 0.5f;
        if (elapsed < WindupTime)
        {
            float angle = aimAngle + half + Mathf.Lerp(15f, 0, elapsed / WindupTime);
            axeRenderer.transform.localRotation = Quaternion.Euler(0, 0, angle);
            return;
        }

        if (!playedSwingSound)
        {
            playedSwingSound = true;
            Managers.Sound.Play("Melee0", Define.Sound.Effect, 0.8f);
        }
        float progress = Mathf.Clamp01((elapsed - WindupTime) / SwingTime);
        // Also process the final slice if a low-frame-rate update crosses the entire active phase.
        if (previousElapsed < WindupTime + SwingTime)
            HitSector(half - swingAngle * progress, half);

        if (elapsed < WindupTime + SwingTime)
        {
            axeRenderer.transform.localRotation = Quaternion.Euler(0, 0, aimAngle + half - swingAngle * progress);
            ShowSlash(progress, false);
        }
        else
        {
            float recovery = (elapsed - WindupTime - SwingTime) / RecoveryTime;
            axeRenderer.transform.localRotation = Quaternion.Euler(0, 0,
                aimAngle + Mathf.Lerp(-half, half + 15f, Mathf.SmoothStep(0, 1, recovery)));
            ShowSlash(1, true);
            if (recovery >= 1)
            {
                swinging = false;
                HideSlash();
                SetVisualScale(AttackRange);
            }
        }
    }

    void ShowRange()
    {
        if (rangeRenderer == null || rangeFrames == null || rangeFrames.Length == 0) return;
        float angle = swinging ? swingAngle : AttackAngle;
        float range = swinging ? swingRange : AttackRange;
        rangeRenderer.enabled = true;
        rangeRenderer.sprite = rangeFrames[Mathf.Clamp(Mathf.RoundToInt((angle - 80f) / 15f), 0, rangeFrames.Length - 1)];
        rangeRenderer.transform.localRotation = Quaternion.Euler(0, 0, aimAngle);
        rangeRenderer.transform.localScale = Vector3.one * range;
        bool active = swinging && elapsed >= WindupTime && elapsed < WindupTime + SwingTime;
        rangeRenderer.color = active ? new Color(1f, 0.77f, 0.3f, 1f) : new Color(1f, 0.88f, 0.55f, 0.75f);
    }

    void HitSector(float minimumAngle, float maximumAngle)
    {
        Vector2 origin = transform.position;
        foreach (var collider in Physics2D.OverlapCircleAll(origin, swingRange, LayerMask.GetMask("Enemy")))
        {
            var enemy = collider.GetComponentInParent<BaseController>();
            var enemyStats = collider.GetComponentInParent<EnemyStat>();
            if (enemy == null || enemy._type != Define.WorldObject.Enemy || enemyStats == null || enemyStats.HP <= 0 || hitThisSwing.Contains(enemy)) continue;
            Vector2 point = collider.ClosestPoint(origin);
            Vector2 offset = point - origin;
            if (offset.sqrMagnitude < 0.0001f) offset = (Vector2)collider.bounds.center - origin;
            if (!ContainsPoint(offset, aimAngle, swingRange, minimumAngle, maximumAngle)) continue;
            // Several colliders on one monster must still result in exactly one hit per swing.
            hitThisSwing.Add(enemy);
            float resistance = enemyStats.MonsterType == Define.MonsterType.Boss ? 0.1f
                : enemyStats.MonsterType == Define.MonsterType.middleBoss ? 0.25f : 1f;
            enemy.OnDamaged(swingDamage, swingForce * resistance);
        }
    }

    public static bool ContainsPoint(Vector2 offset, float directionAngle, float range, float minimumAngle, float maximumAngle)
    {
        if (offset.sqrMagnitude > range * range) return false;
        float relative = Mathf.DeltaAngle(directionAngle, Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
        return relative >= minimumAngle - 0.001f && relative <= maximumAngle + 0.001f;
    }

    void SetVisualScale(float range)
    {
        if (axeRenderer != null && axeRenderer.sprite != null)
        {
            float length = axeRenderer.sprite.bounds.size.x;
            float stretch = range / length;
            axeRenderer.transform.localScale = new Vector3(stretch, AxeDetailScale, 1f);
            if (axeHeadRenderer != null)
            {
                // The handle reaches the sector radius; cancel its stretch on the blade.
                axeHeadRenderer.transform.localPosition = new Vector3(length, 0, 0);
                axeHeadRenderer.transform.localScale = new Vector3(AxeDetailScale / stretch, 1f, 1f);
            }
        }
        if (slashRenderers == null) return;
        foreach (var slash in slashRenderers)
            if (slash != null) slash.transform.localScale = Vector3.one * (range / SlashRadius);
    }

    void ShowSlash(float progress, bool fading)
    {
        if (slashFrames == null || slashFrames.Length < 4 || slashRenderers == null) return;
        int count = swingAngle > 80.01f ? 2 : 1;
        for (int i = 0; i < slashRenderers.Length; i++)
        {
            var slash = slashRenderers[i];
            if (slash == null) continue;
            float offset = count == 1 ? 0 : (i == 0 ? 1 : -1) * (swingAngle - 80f) * 0.5f;
            float localProgress = (40f - (swingAngle * 0.5f - swingAngle * progress - offset)) / 80f;
            slash.enabled = i < count && (fading || localProgress >= 0);
            slash.sprite = slashFrames[fading ? 3 : Mathf.Clamp(Mathf.FloorToInt(localProgress * 3), 0, 2)];
            slash.transform.localRotation = Quaternion.Euler(0, 0, aimAngle + offset);
            slash.color = new Color(1, 1, 1, fading ? Mathf.Clamp01(1 - (elapsed - WindupTime - SwingTime) / RecoveryTime) : 1);
        }
    }

    void HideSlash()
    {
        if (slashRenderers == null) return;
        foreach (var slash in slashRenderers) if (slash != null) slash.enabled = false;
    }
}
