using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : BaseController
{
    protected PlayerStat _stat;
    public RuntimeAnimatorController[] animeCon;

    [SerializeField] 
    Vector2 _inputVec;
    [SerializeField] 
    public Vector2 _lastDirVec = new Vector2(1, 0);

    bool _isDamaged = false;
    float _invincibility_time = 0.2f;

    // ===== 冲刺（Dash）模块 =====
    // 左Shift触发，沿最后移动方向突进一小段距离；突进期间无敌；
    // 冷却固定为常量2秒，不随攻速/冷却等参数变化。
    // dashSpeed 随 MoveSpeed 线性增长（倍率 2.4 = 12/5，保持初始手感），
    // dashDuration 自适应保证突进距离恒定（2.16 单位），无敌时间随 duration 走。
    const float DashTargetDistance = 2.16f;          // 固定突进距离（单位）
    const float DashSpeedMultiplier = 2.4f;           // dashSpeed = MoveSpeed × 2.4
    const float DASH_COOLDOWN = 2f;                   // 冲刺冷却常量（秒）
    bool _isDashing = false;                           // 是否正在突进
    bool _isInvincible = false;                       // 突进期间无敌标记
    float _nextDashTime = 0f;                         // 下一次可冲刺的时间点

    protected override void Init()
    {
        _stat = GetComponent<PlayerStat>();
        _rigid = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
        _anime = GetComponent<Animator>();
        _type = Define.WorldObject.Player;
    }
    

    void Update()
    {
        if (LanSession.Active)
            _inputVec = LanSession.Instance.Movement(this);
        else
        {
            _inputVec.x = Input.GetAxisRaw("Horizontal");
            _inputVec.y = Input.GetAxisRaw("Vertical");
        }

        // 左Shift触发冲刺：冷却固定2秒，不受攻速/冷却影响
        bool wantsDash = LanSession.Active ? LanSession.Instance.DashRequested(this) : Input.GetKeyDown(KeyCode.LeftShift);
        if (wantsDash && Time.time >= _nextDashTime && (!LanSession.Active || !LanSession.Instance.IsDown(this)))
        {
            _nextDashTime = Time.time + DASH_COOLDOWN;
            StartCoroutine(Dash());
        }
    }

    // 冲刺：沿最后移动方向高速突进，期间无敌
    // dashSpeed 跟随 MoveSpeed 线性增长，dashDuration 自适应保证突进距离恒定
    IEnumerator Dash()
    {
        float dashSpeed = _stat.MoveSpeed * DashSpeedMultiplier;
        float dashDuration = DashTargetDistance / dashSpeed;

        _isDashing = true;
        _isInvincible = true;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            Vector2 dir = (_lastDirVec.magnitude > 0.01f) ? _lastDirVec.normalized : Vector2.right;
            _rigid.MovePosition(_rigid.position + dir * (dashSpeed * Time.fixedDeltaTime));
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        _isDashing = false;
        _isInvincible = false;
    }

    private void FixedUpdate()
    {
        if (LanSession.Active && (!LanSession.Instance.CanSimulate || LanSession.Instance.IsDown(this))) return;
        // 冲刺期间由 Dash() 控制移动，不再叠加普通移动
        if (_isDashing)
            return;

        Vector2 nextVec = _inputVec.normalized * (_stat.MoveSpeed * Time.fixedDeltaTime);
        //Position change the player to last direction;
        _rigid.MovePosition(_rigid.position + nextVec);

        if (_inputVec.normalized.magnitude != 0)
        {
            _lastDirVec = _inputVec.normalized;
        }
    }

    private void LateUpdate()
    {
        _anime.SetFloat("speed", _inputVec.magnitude);
        if (_inputVec.x != 0)
        {
            _sprite.flipX = (_inputVec.x < 0) ? true : false;
        }
    }
    public void Init(Data.Player playerData)
    {
        int animIdx = Mathf.Clamp(playerData.id - 1, 0, animeCon.Length - 1);
        _anime.runtimeAnimatorController = animeCon[animIdx];
        _stat.MaxHP = playerData.maxHp;
        _stat.HP = playerData.maxHp;
        _stat.Damage = playerData.damage;
        _stat.Defense = playerData.defense;
        _stat.MoveSpeed = playerData.moveSpeed;
        _stat.Cooldown = playerData.coolDown;
        _stat.Amount = playerData.amount;
        // 根据角色初始数据决定是否显示弹道/冷却属性
        // Axe/LaserCannon 不显示弹道数量（光束/扇形重砍，不是多发射击）
        int wid = playerData.weaponID;
        _stat.HasAmount = wid != (int)Define.Weapons.Axe
                       && wid != (int)Define.Weapons.LaserCannon
                       && ((playerData.coolDown == 0) || (playerData.amount > 0));
        _stat.HasCooldown = playerData.coolDown != 0;
        var weaponID = (Define.Weapons)playerData.weaponID;
        if (!Enum.IsDefined(typeof(Define.Weapons), weaponID))
            weaponID = Define.Weapons.Knife;
        _stat.AddOrSetWeaponDict(weaponID, 1);
    }
    public void OnDamaged(Collision2D collision)
    {
        if (LanSession.Active && (!LanSession.Instance.CanSimulate || LanSession.Instance.IsDown(this))) return;
        _isDamaged = true;
        Stat EnemyStat = collision.transform.GetComponent<EnemyStat>();


        _stat.HP -= Mathf.Max(EnemyStat.Damage - _stat.Defense, 1);
        
        if (_stat.HP <= 0)
            OnDead();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.transform.tag == "Enemy")
        {
            // 冲刺无敌期间不受伤
            if (_isInvincible)
                return;

            float currentTime = Managers.GameTime;
            if (!_isDamaged)
            {
                OnDamaged(collision);
                if (LanSession.Active && LanSession.Instance.IsDown(this)) return;
                StartCoroutine(OnDamagedColor());
            }
        }
    }

    public override void OnDamaged(int damage, float force = 0)
    {
        if (LanSession.Active && (!LanSession.Instance.CanSimulate || LanSession.Instance.IsDown(this))) return;
        // 冲刺无敌期间不受伤
        if (_isInvincible)
            return;

        Managers.Event.PlayHitPlayerEffectSound();
        _stat.HP -= Mathf.Max(damage - _stat.Defense, 1);
        OnDead();
    }

    IEnumerator OnDamagedColor()
    {
        _sprite.color = Color.red;

        yield return new WaitForSeconds(_invincibility_time);

        _isDamaged = false;
        _sprite.color = Color.white;
    }


    public override void OnDead()
    {
        if (LanSession.Active)
        {
            if (_stat.HP <= 0) LanSession.Instance.Down(this);
            return;
        }
        if(_stat.HP <= 0)
        {
            _anime.SetTrigger("dead");
            _stat.HP = 0;
            if (EndlessMode.Active != null)
            {
                EndlessMode.Active.PlayerDefeated();
                return;
            }
            Managers.UI.ShowPopupUI<UI_GameOver>();
            Managers.GamePause();
        }
            
    }

    public void SetCoopDown(bool down)
    {
        StopAllCoroutines();
        _isDashing = false; _isInvincible = false; _isDamaged = false;
        _inputVec = Vector2.zero; _rigid.velocity = Vector2.zero;
        _sprite.color = down ? new Color(0.4f, 0.4f, 0.5f, 0.65f) : Color.white;
        _anime.SetFloat("speed", 0);
        foreach (var collider in GetComponents<Collider2D>()) collider.enabled = !down;
        if (!down) StartCoroutine(ReviveProtection());
    }

    IEnumerator ReviveProtection()
    {
        _isInvincible = true;
        yield return new WaitForSeconds(1f);
        _isInvincible = false;
    }
    

}
