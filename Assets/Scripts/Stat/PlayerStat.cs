using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStat : Stat
{


    //1 : knife, 2 : firebal, 3: spin, 4: posion 5: lightning 6: shotgun
    //WeaponDict - WeaponID : WeaponLevel
    Dictionary<string, int> _playerStat = new Dictionary<string, int>();
    [SerializeField]
    Dictionary<Define.Weapons, int> _weaponDict = new Dictionary<Define.Weapons, int>();
    public Define.Weapons _playerStartWeapon;
    public Dictionary<Define.Weapons, int> GetWeaponDict()
    {
        return _weaponDict;
    }
    public bool AddOrSetWeaponDict(Define.Weapons key, int value, bool addOrSet = false)
    {
        if (_weaponDict.Count == 0)
        {
            _playerStartWeapon = key;
        }
        if (!_weaponDict.ContainsKey(key))
        {
            if (_weaponDict.Count >= 4)
                return false;
            //key spawn
            _weaponDict.Add(key, 0);
        }

        if (!addOrSet)
            _weaponDict[key] += value;
        else
            _weaponDict[key] = value;

        SetWeaponLevel();
        return true;
    }

    public override int Level { get { return base.Level; } 
        set 
        { 
            base.Level = value;
            if (!_playerStat.ContainsKey("Level"))
                _playerStat.Add("Level", _level);
            _playerStat["Level"] = _level;
        } 
    }
    public override int MaxHP
    {
        get { return base.MaxHP; }
        set
        {
            base.MaxHP = value;
            if (!_playerStat.ContainsKey("MaxHP"))
                _playerStat.Add("MaxHP", _maxHp);
            _playerStat["MaxHP"] = _maxHp;
        }
    }
    public override float MoveSpeed
    {
        get { return base.MoveSpeed; }
        set
        {
            base.MoveSpeed = value;
            if (!_playerStat.ContainsKey("MoveSpeed"))
                _playerStat.Add("MoveSpeed", (int)_Movespeed);
            _playerStat["MoveSpeed"] = (int)_Movespeed;
        }
    }
    public override int Damage 
    {
        get { return base.Damage; }
        set
        {
            base.Damage = value;
            if (!_playerStat.ContainsKey("Damage"))
                _playerStat.Add("Damage", _damage);
            _playerStat["Damage"] = _damage;
        }
    }
    public override int Defense
    {
        get { return base.Defense; }
        set
        {
            base.Defense = value;
            if (!_playerStat.ContainsKey("Defense"))
                _playerStat.Add("Defense", _defense);
            _playerStat["Defense"] = _defense;
        }
    }
    public int Cooldown { get { return _cooldown; } 
        set 
        { 
            _cooldown = value;
            if (!_playerStat.ContainsKey("Cooldown"))
                _playerStat.Add("Cooldown", _cooldown);
            _playerStat["Cooldown"] = _cooldown;
        } 
    }
    public int Amount { get { return _amount; }
        set 
        { 
            _amount = value;
            if (!_playerStat.ContainsKey("Amount"))
                _playerStat.Add("Amount", _amount);
            _playerStat["Amount"] = _amount;
        } 
    }

    /// <summary>
    /// 当前攻速（每秒攻击次数），与 WeaponController 的冷却计算逻辑保持一致：
    /// 实际冷却 = 基础冷却 × 100/(100 + 攻速加成)，攻速 = 1 / 实际冷却。
    /// 基础冷却取角色初始武器当前等级的基础数值。
    /// </summary>
    public float AttackPerSecond
    {
        get
        {
            int weaponID = (int)_playerStartWeapon;
            if (weaponID == 0 || !Managers.Data.WeaponData.ContainsKey(weaponID))
                return 0f;
            Data.WeaponData weapon = Managers.Data.WeaponData[weaponID];
            int level = _weaponDict.ContainsKey(_playerStartWeapon) ? Math.Max(1, _weaponDict[_playerStartWeapon]) : 1;
            float baseCooldown = 1f;
            foreach (Data.WeaponLevelData lv in weapon.weaponLevelData)
            {
                if (lv.level == level)
                {
                    baseCooldown = lv.cooldown;
                    break;
                }
            }
            if (baseCooldown <= 0f)
                return 0f;
            float actualCooldown = baseCooldown * (100f / (100f + Cooldown));
            return 1f / actualCooldown;
        }
    }

    /// <summary>
    /// 当前总弹量（每次攻击实际发射数）= 武器基础弹数(countPerCreate) + 玩家弹量(Amount)。
    /// 基础弹数取角色初始武器当前等级的基础数值。
    /// </summary>
    public int TotalAmount
    {
        get
        {
            int weaponID = (int)_playerStartWeapon;
            if (weaponID == 0 || !Managers.Data.WeaponData.ContainsKey(weaponID))
                return Amount;
            Data.WeaponData weapon = Managers.Data.WeaponData[weaponID];
            int level = _weaponDict.ContainsKey(_playerStartWeapon) ? Math.Max(1, _weaponDict[_playerStartWeapon]) : 1;
            int baseCount = 0;
            foreach (Data.WeaponLevelData lv in weapon.weaponLevelData)
            {
                if (lv.level == level)
                {
                    baseCount = lv.countPerCreate;
                    break;
                }
            }
            return baseCount + Amount;
        }
    }

    private long _exp;
    public long Exp
    {
        get { return _exp;}
        set
        {
            _exp = value;
            while (_exp >= MaxExp)
            {
               OnLevelUp();
            }
        }
    }
    private long _maxExp = 1;

    public long MaxExp
    {
        get => _maxExp;
        set => _maxExp = value;
    }

    private int _cooldown;
    private int _amount;

    // 角色特性标记：是否拥有弹量/冷却属性（由初始角色数据决定）
    public bool HasAmount { get; set; } = false;
    public bool HasCooldown { get; set; } = false;

    public int AxeRangeLevel { get; private set; }
    public int AxeAngleLevel { get; private set; }
    public int AxeDamageLevel { get; private set; }

    // ===== 激光炮（Aemeath）专属升级上限 =====
    public const int LaserRangeMaxLevel = 3;
    public const int LaserDamageMaxLevel = 3;
    public int LaserRangeLevel { get; private set; }
    public int LaserDamageLevel { get; private set; }

    public bool CanUpgradeLaser(string upgrade)
    {
        if (_playerStartWeapon != Define.Weapons.LaserCannon) return false;
        switch (upgrade)
        {
            case "LaserRangeUp":   return LaserRangeLevel   < LaserRangeMaxLevel;
            case "LaserDamageUp":  return LaserDamageLevel  < LaserDamageMaxLevel;
            default: return false;
        }
    }
    public bool ApplyLaserUpgrade(string upgrade)
    {
        if (!CanUpgradeLaser(upgrade)) return false;
        switch (upgrade)
        {
            case "LaserRangeUp":   LaserRangeLevel++;  break;
            case "LaserDamageUp":  LaserDamageLevel++; break;
        }
        RefreshWeapons();
        return true;
    }

    // ===== 移动速度升级上限（所有角色通用，每个模式都生效） =====
    public const int MoveSpeedMaxLevel = 3;
    public const float MoveSpeedPerLevel = 1f;
    public int MoveSpeedLevel { get; private set; }

    public bool CanUpgradeMoveSpeed() => MoveSpeedLevel < MoveSpeedMaxLevel;
    public bool ApplyMoveSpeedUpgrade()
    {
        if (!CanUpgradeMoveSpeed()) return false;
        MoveSpeedLevel++;
        MoveSpeed += MoveSpeedPerLevel;
        return true;
    }

    public bool CanUpgradeAxe(string upgrade)
    {
        if (_playerStartWeapon != Define.Weapons.Axe) return false;
        switch (upgrade)
        {
            case "AxeRangeUp": return AxeRangeLevel < 3;
            case "AxeAngleUp": return AxeAngleLevel < 3;
            case "AxeDamageUp": return AxeDamageLevel < 3;
            default: return false;
        }
    }

    public bool ApplyAxeUpgrade(string upgrade)
    {
        if (!CanUpgradeAxe(upgrade)) return false;
        switch (upgrade)
        {
            case "AxeRangeUp": AxeRangeLevel++; break;
            case "AxeAngleUp": AxeAngleLevel++; break;
            case "AxeDamageUp": AxeDamageLevel++; break;
        }
        RefreshWeapons();
        return true;
    }




    void Awake()
    {
        Init();
    }

    void Init()
    {
        Level = 1;
        HP = 50;
        MaxHP = 50;
        MoveSpeed = 5.0f;
        Damage = 1;
        Defense = 0;
        Cooldown = 0;
        Amount = 0;
        Exp = 0;
        MaxExp = 10;
        SetWeaponLevel();
    }


    void OnLevelUp()
    {
        if (LanSession.Active)
            LanSession.Instance.BeginUpgradeFor(this);
        else
            Managers.Event.LevelUpEvent();

        Level += 1;
        Exp = 0;
        MaxExp += Math.Max(100, (long)(_maxExp*1.1));
    }

    void SetWeaponLevel()
    {
        //Set CommonWeaponSpawingPool 
        foreach (KeyValuePair<Define.Weapons, int> weapon in GetWeaponDict())
        {
            string weaponName = weapon.Key.ToString();
            string weaponSpawningPool = weapon.Key + "SpawningPool";

            GameObject weaponPool = Util.FindChild(gameObject, weaponSpawningPool, true);
            if (weaponPool == null)
            {
                weaponPool = Managers.Resource.Instantiate($"Weapon/SpawningPool/{weaponSpawningPool}", transform);
                if (weaponPool == null)
                    continue;
            }
            if (weapon.Value == 0)
            {
                GetWeaponDict().Remove(weapon.Key);
                Managers.Resource.Destroy(weaponPool);
            }

            weaponPool.GetComponentInChildren<WeaponController>().Level = weapon.Value;
            if (!LanSession.Active && Managers.UI.getSceneUI() is UI_Player ui)
                ui.SetWeaponImage(this);
        }
    }

    public void RefreshWeapons() { SetWeaponLevel(); }


    public Dictionary<string, int> getPlayerStatData()
    {
        return _playerStat;
    }

    // ===== 背包系统 =====
    // 记录所有获得过的武器和属性（独立字典，与 _weaponDict / _playerStat 分开）
    readonly Dictionary<string, int> _inventory = new Dictionary<string, int>();
    public Dictionary<string, int> GetInventory() => _inventory;
    public void RecordObtain(string category, string itemName)
    {
        string key = $"{category}:{itemName}";
        if (!_inventory.ContainsKey(key)) _inventory[key] = 0;
        _inventory[key]++;
    }
}
