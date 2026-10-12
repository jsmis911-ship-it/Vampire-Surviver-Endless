using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class EventManager
{
    public enum PlayerStats
    {
        MaxHP,
        MoveSpeed,
        Damage,
        Defense,
        Cooldown,
        Amount,
        AxeRangeUp,
        AxeAngleUp,
        AxeDamageUp,
        LaserRangeUp,
        LaserDamageUp
    }


    public List<string[]> SetRandomItem(PlayerStat player, int Maxcount)
    {
        int i = 0;
        List<string[]> PoolList = new List<string[]>();
        var axeUpgrades = new List<string>();
        foreach (var name in new[] { "AxeRangeUp", "AxeAngleUp", "AxeDamageUp" })
            if (player.CanUpgradeAxe(name)) axeUpgrades.Add(name);
        var laserUpgrades = new List<string>();
        foreach (var name in new[] { "LaserRangeUp", "LaserDamageUp" })
            if (player.CanUpgradeLaser(name)) laserUpgrades.Add(name);
        while(i < Maxcount)
        {
            string[] selected = new string[2];
            float random = Random.Range(0, 100);
            int rd = 0;

            Debug.Log($"Random number for levelUp : {random}");
            if (random < 30)
                rd = 0;
            else if (random < 50)
                rd = 1;
            else
                rd = 2;


            if (i == 0 && axeUpgrades.Count > 0)
            {
                selected[0] = "1";
                selected[1] = axeUpgrades[Random.Range(0, axeUpgrades.Count)];
            }
            else if (i == 0 && laserUpgrades.Count > 0)
            {
                selected[0] = "1";
                selected[1] = laserUpgrades[Random.Range(0, laserUpgrades.Count)];
            }
            else if (rd== 0)
            {
                if (player.GetWeaponDict()[player._playerStartWeapon] >= 5)
                    continue;
                selected[0] = "0";
                selected[1] = player._playerStartWeapon.ToString();
            }
            else if (rd == 1)
            {
                selected[0] = "1";
                PlayerStats stat = SetRandomStat(player);
                selected[1] = stat.ToString();
            }
            else
            {
                selected[0] = "2";
                Define.Weapons weapon = SetRandomWeapon(player);
                if (player.GetWeaponDict().GetValueOrDefault<Define.Weapons, int>(weapon) >= 5)
                    continue;
                if (player.GetWeaponDict().Count >= 4 && !player.GetWeaponDict().ContainsKey(weapon))
                    continue;
                selected[1] = weapon.ToString();
            }

            bool isContains = false;
            foreach (string[] type in PoolList)
            {
                if(selected[1] == type[1])
                {
                    Debug.Log($"{type[1]} is already contained");
                    isContains = true;
                    break;
                }
            }
            if (isContains)
                continue;
            PoolList.Add(selected);
            i++;
        }
        return PoolList;
    }

    public PlayerStats SetRandomStat(PlayerStat player)
    {
        // Lightning/Axe/LaserCannon 升级随机池排除弹道数量
        bool excludeAmount = player != null && (player._playerStartWeapon == Define.Weapons.Lightning || player._playerStartWeapon == Define.Weapons.Axe || player._playerStartWeapon == Define.Weapons.LaserCannon);
        List<PlayerStats> stats = new List<PlayerStats>();
        foreach (PlayerStats ps in System.Enum.GetValues(typeof(PlayerStats)))
        {
            if (excludeAmount && ps == PlayerStats.Amount)
                continue;
            if ((ps == PlayerStats.AxeRangeUp || ps == PlayerStats.AxeAngleUp || ps == PlayerStats.AxeDamageUp)
                && (player == null || !player.CanUpgradeAxe(ps.ToString())))
                continue;
            // 激光炮专属增幅：只有 Aemeath 能刷到，范围/伤害均有 3 级上限
            if (ps == PlayerStats.LaserRangeUp || ps == PlayerStats.LaserDamageUp)
            {
                if (player == null || !player.CanUpgradeLaser(ps.ToString()))
                    continue;
            }
            if (ps == PlayerStats.MoveSpeed
                && (player == null || !player.CanUpgradeMoveSpeed()))
                continue;
            stats.Add(ps);
        }
        return stats[Random.Range(0, stats.Count)];
    }

    public Define.Weapons SetRandomWeapon(PlayerStat player = null)
    {
        // 和联机 BuildOffers/BuildBoxPool 对齐：
        // 通用武器所有角色都能刷，角色专属起始武器（>=100）只有对应起始武器的角色能刷
        List<Define.Weapons> weapons = new List<Define.Weapons>();
        foreach (Define.Weapons w in System.Enum.GetValues(typeof(Define.Weapons)))
        {
            int val = (int)w;
            bool isStartWeapon = val >= 100;
            if (isStartWeapon && (player == null || player._playerStartWeapon != w))
                continue;
            weapons.Add(w);
        }
        return weapons[Random.Range(0, weapons.Count)];
    }
    public void DropItem(EnemyStat stat, Transform transform)
    {
        GameObject item = null;
        float rand = Random.Range(0, 100);
        // Endless chests are awarded on wave completion, never by random kills.
        if (EndlessMode.Active != null)
            rand = rand < 5 ? 5 : 100;
        Debug.Log($"rand dropItem : {rand}");
        if ((rand < 1 || stat.MonsterType == Define.MonsterType.middleBoss) && EndlessMode.Active == null)
        {
            item = Managers.Resource.Instantiate("Content/Box");
        }
        else if (rand < 8)
        {
            int rd = Random.Range(1, 11);
            if (rd < 7)
            {
                item = Managers.Resource.Instantiate("Content/Health");
            }
            else
            {
                item = Managers.Resource.Instantiate("Content/Magnet");
            }
        }
        if (item == null)
            return;
        item.transform.position = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
    }

    public Define.Weapons SetRandomWeaponInItem()
    {
        return SetRandomWeapon();
    }

    public void LevelUpEvent()
    {
        Managers.UI.ShowPopupUI<UI_LevelUp>();
        // 单机升级不再暂停游戏（战斗继续进行，玩家可以边打怪边选升级）
    }

    public void LevelUpOverEvent(int itemType, string itemName)
    {
        //PlayerStatorWeaponUp
        PlayerStat player = Managers.Game.getPlayer().GetComponent<PlayerStat>();
        if (itemType == 1)
        {
            switch (itemName)
            {
                case "MaxHP":
                    player.MaxHP += 10;
                    player.HP = Mathf.Min(player.HP + 15, player.MaxHP);
                    break;
                case "MoveSpeed":
                    player.ApplyMoveSpeedUpgrade();
                    break;
                case "Damage":
                    player.Damage += 10;
                    break;
                case "Defense":
                    player.Defense += 1;
                    break;
                case "Cooldown":
                    player.Cooldown += 10;
                    break;
                case "Amount":
                    player.Amount += 1;
                    break;
                case "AxeRangeUp":
                case "AxeAngleUp":
                case "AxeDamageUp":
                    player.ApplyAxeUpgrade(itemName);
                    break;
                case "LaserRangeUp":
                case "LaserDamageUp":
                    player.ApplyLaserUpgrade(itemName);
                    break;
            }
            player.AddOrSetWeaponDict(player._playerStartWeapon, 0);
            player.RecordObtain("Stat", itemName);
        }
            
        else
        {
            Define.Weapons weaponType = (Define.Weapons)System.Enum.Parse(typeof(Define.Weapons), itemName);
            player.AddOrSetWeaponDict(weaponType, 1);
            player.RecordObtain("Weapon", weaponType.ToString());
        }
            

        Managers.UI.ClosePopupUI(Define.PopupUIGroup.UI_LevelUp);
    }

    public void ShowItemBoxUI()
    {
        Managers.UI.ShowPopupUI<UI_ItemBoxOpen>();
        // 单机宝箱也不暂停游戏
    }

    /// <summary>
    /// 单机宝箱：随机抽 1 个属性，直接生效 + Toast 提示。
    /// 用 LanSession.BuildBoxPool 确保和联机宝箱完全一致的角色过滤逻辑。
    /// </summary>
    public void GiveRandomItemFromBox(PlayerStat player)
    {
        if (EndlessMode.Active != null)
        {
            EndlessMode.Active.OpenReward();
            return;
        }
        var pool = LanSession.BuildBoxPool(player);
        int idx = Random.Range(0, pool.Count);
        string offer = pool[idx];

        LanSession.ApplyOffer(player, offer);

        string desc = LanSession.DescribeOffer(offer);
        ToastManager.Show(desc);
    }
    public List<Define.Weapons> SetRandomWeaponfromItemBox(PlayerStat player)
    {
        bool weaponFull = true;
        if (player.GetWeaponDict().Count >= 4)
        {
            foreach(KeyValuePair<Define.Weapons, int> weapon in player.GetWeaponDict())
            {
                if (weapon.Key == player._playerStartWeapon)
                    continue;
                if(weapon.Value < 5)
                {
                    weaponFull = false;
                    break;
                }
            }
            if (weaponFull)
            {
                return null;
            }
        }
        int maxCount = 3;
        int rd = Random.Range(1, maxCount+1);
        int i = 0;
        List<Define.Weapons> weaponList = new List<Define.Weapons>();
        while(i < rd)
        {
            Define.Weapons wp = SetRandomWeaponInItem();
            int weaponlevel = player.GetWeaponDict().GetValueOrDefault<Define.Weapons, int>(wp);
            if (weaponlevel >= 5 || (player.GetWeaponDict().Count == 4 && weaponlevel == 0))
                continue;
            weaponList.Add(wp);
            i++;
        }

        return weaponList;
    }

    public void SetLevelUpWeaponfromItemBox(List<Define.Weapons> weaponList, PlayerStat player)
    {
        if(weaponList == null)
        {
            player.HP = Mathf.Min(player.HP + 15, player.MaxHP);
            return;
        }
        foreach(Define.Weapons weaponType in weaponList)
        {
            player.AddOrSetWeaponDict(weaponType, 1);
            player.RecordObtain("Weapon", weaponType.ToString());
        }
        
    }

    public void PlayHitEnemyEffectSound()
    {
        int rd = Random.Range(1, 3);
        switch (rd)
        {
            case 1:
                Managers.Sound.Play("Hit0");
                break;
            case 2:
                Managers.Sound.Play("Hit1");
                break;
        }
    }

    public void PlayHitPlayerEffectSound()
    {
        Managers.Sound.Play("Hit_01");
    }

}
