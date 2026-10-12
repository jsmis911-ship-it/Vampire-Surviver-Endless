using System.Collections;
using UnityEngine;

public class EnemyController : BaseController
{
    public GameObject hudDamageText;
    
    protected EnemyStat _stat;
    public RuntimeAnimatorController[] _animeCon;
    public Rigidbody2D _target;
    bool _isLive = true;
    bool _isRange = false;
    bool _isAttack = false;
    Vector2 _pathWaypoint;
    bool _hasPathWaypoint;
    int _pathVersion;
    Vector2 _lastProgressPosition;
    float _lastProgressTime;
    public bool TargetsCrystal { get; private set; }
    public void SetCrystalTarget(Rigidbody2D crystal)
    {
        _target = crystal;
        TargetsCrystal = crystal != null;
        _hasPathWaypoint = false;
        _lastProgressPosition = transform.position;
        _lastProgressTime = Time.time;
    }


    protected override void Init()
    {
        _stat = GetComponent<EnemyStat>();
        _rigid = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
        _anime = GetComponent<Animator>();
        _type = Define.WorldObject.Enemy;
        _target = Managers.Game.getPlayer().GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (LanSession.Active)
        {
            if (!LanSession.Instance.CanSimulate) return;
            _target = LanSession.Instance.NearestTarget(_rigid.position);
        }
        if (_target == null) return;
        if (!_isLive)
            return;

        OnMove();

        if (_isRange && !_isAttack)
        {
            StartCoroutine(RangeAttack());
        }
    }

    void OnMove()
    {
        Vector2 destination = _target.position;
        if (TargetsCrystal && EndlessDungeon.Active != null)
        {
            if (Vector2.Distance(_rigid.position, _lastProgressPosition) > .5f)
            {
                _lastProgressPosition = _rigid.position;
                _lastProgressTime = Time.time;
            }
            else if (Time.time - _lastProgressTime > 4f)
            {
                // Physics can pin a pooled enemy against a wall at a dogleg.
                // Advance exactly one reachable flow cell, never through a wall.
                Vector2 recovery = EndlessDungeon.Active.NextWaypoint(_rigid.position);
                if (Vector2.Distance(recovery, _rigid.position) > .3f)
                {
                    Debug.LogWarning($"Endless enemy unstuck from {_rigid.position} to {recovery}");
                    _rigid.position = recovery;
                }
                _hasPathWaypoint = false;
                _lastProgressPosition = _rigid.position;
                _lastProgressTime = Time.time;
            }
            if (!_hasPathWaypoint || _pathVersion != EndlessDungeon.Active.FlowVersion
                || Vector2.Distance(_rigid.position, _pathWaypoint) < .12f)
            {
                Vector2 from = _hasPathWaypoint && _pathVersion == EndlessDungeon.Active.FlowVersion
                    ? _pathWaypoint : EndlessDungeon.Active.Snap(_rigid.position);
                _pathWaypoint = EndlessDungeon.Active.NextWaypoint(from);
                _hasPathWaypoint = true;
                _pathVersion = EndlessDungeon.Active.FlowVersion;
            }
            destination = _pathWaypoint;
        }
        Vector2 dirVec = destination - _rigid.position;
        Vector2 nextVec = Vector2.ClampMagnitude(dirVec, _stat.MoveSpeed * Time.fixedDeltaTime);

        _rigid.MovePosition(_rigid.position + nextVec);
        _rigid.velocity = Vector2.zero;
    }

    IEnumerator RangeAttack()
    {
        _isAttack = true;
        SpawnBullet();
        yield return new WaitForSeconds(2f);
        _isAttack = false;
    }

    void SpawnBullet()
    {
        EnemyBullet bullet = Managers.Resource.Instantiate("Weapon/EnemyBullet",_rigid.position).GetOrAddComponent<EnemyBullet>();
        bullet._damage = _stat.Damage;
        bullet._speed = 5f;
        bullet._dir = (_target.position - _rigid.position).normalized;
    }
    private void LateUpdate()
    {
        // 防御：死亡后或目标/自身刚体已被销毁（通关切场景）时跳过，避免 MissingReferenceException
        if (!_isLive || _target == null || _rigid == null)
            return;
        _sprite.flipX = (_target.position.x - _rigid.position.x < 0) ? true : false;
    }

    private void OnEnable()
    {
        _isLive = true;
    }

    
    public void Init(Data.Monster monsterStat, int level, Define.MonsterType type)
    {
        _isRange = false;
        _isAttack = false;
        TargetsCrystal = false;
        _hasPathWaypoint = false;
        transform.localScale = Vector3.one;
        int mul = 1;
        switch (type)
        {
            case Define.MonsterType.Enemy:
                mul = 1;
                break;
            case Define.MonsterType.middleBoss:
                mul = 50;
                break;
        }
        _anime.runtimeAnimatorController = _animeCon[monsterStat.id-1];

        if (monsterStat.id == 5)
            _isRange = true;

        if (type == Define.MonsterType.middleBoss)
        {
            transform.localScale = Vector3.one * 2;
            // 参照角色上方小血条逻辑给中Boss加头顶血条
            if (gameObject.GetComponentInChildren<UI_HPBar>() == null)
                Managers.UI.MakeWorldSpaceUI<UI_HPBar>(transform, "UI_HPBar");
        }
        _stat.MonsterStyle = (Define.MonsterStyle)System.Enum.Parse(typeof(Define.MonsterStyle), monsterStat.name);
        _stat.MonsterType = type;
        _stat.MoveSpeed = monsterStat.moveSpeed *((float)(100f+ level)/100f);
        _stat.MaxHP = SetRandomStat((int)(monsterStat.maxHp * ((100f + 10f*level)/ 100f))) * mul;
        _stat.HP = _stat.MaxHP;
        _stat.Damage = SetRandomStat((int)(monsterStat.damage * ((100f + level) / 100f)));
        _stat.Defense = SetRandomStat((int)(monsterStat.defense * ((100f + level) / 100f)));
        _rigid.mass = 3;
        _stat.ExpPoint = 10*level;
        _stat.ExpMul = monsterStat.expMul;
        if(type == Define.MonsterType.middleBoss)
        {
            Debug.Log("Boss Spawn! ");
            Debug.Log($"MaxHp : {_stat.MaxHP}");
            Debug.Log($"Hp : {_stat.HP}");
            Debug.Log($"Damage : {_stat.Damage}");
            Debug.Log($"Defense : {_stat.Defense}");
            Debug.Log($"Weight : {_rigid.mass}");
        }
    }
    
    int SetRandomStat(int value)
    {
        value = (int)(value * Random.Range(0.9f, 1.1f));
        return value;
    }

    
    
    public override void OnDamaged(int damage, float force = 0)
    {
        if (!_isLive || (LanSession.Active && !LanSession.Instance.CanSimulate)) return;
        Managers.Event.PlayHitEnemyEffectSound();
        _anime.SetTrigger("Hit");
        int calculateDamage = Mathf.Max(damage - _stat.Defense, 1);
        _stat.HP -= calculateDamage;
        _rigid.AddForce((_rigid.position - _target.position).normalized * (force * 200f));
        FloatDamageText(calculateDamage);

        
        OnDead();
    }


    void FloatDamageText(int damage)
    {
        GameObject hudText = Instantiate(hudDamageText); // ?�성???�스???�브?�트
        hudText.transform.position = transform.position + Vector3.up*1.5f; // ?�시???�치
        hudText.GetComponent<UI_DamageText>()._damage = damage; // ?��?지 ?�달
    }

    public override void OnDead()
    {
        if(_isLive && _stat.HP <= 0)
        {
            _isLive = false;
            _stat.HP = 0;

            SpawnExp();
            Managers.Event.DropItem(_stat,transform);
            if (EndlessMode.Active != null) EndlessMode.Active.EnemyKilled(this);
            transform.localScale = Vector3.one;
            Managers.Game.Despawn(gameObject);
        }
    }

    void SpawnExp()
    {
        GameObject expGo = Managers.Game.Spawn(Define.WorldObject.Unknown, "Content/Exp");
        expGo.transform.position = transform.position;
        Exp_Item expPoint = expGo.GetComponent<Exp_Item>();
        expPoint.SetExp(_stat.ExpPoint, _stat.ExpMul);

        if (expPoint._expMul == 1)
            expGo.GetComponent<SpriteRenderer>().sprite = expPoint._sprite[0];
        else if(expPoint._expMul == 2)
            expGo.GetComponent<SpriteRenderer>().sprite = expPoint._sprite[1];
        else
            expGo.GetComponent<SpriteRenderer>().sprite = expPoint._sprite[2];

    }


}
