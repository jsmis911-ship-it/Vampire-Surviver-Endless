using System;
using UnityEngine;

[Serializable]
public class LanMessage
{
    public string kind;
    public string version;
    public int character;
    public bool ready;
    public int sequence;
    public float x, y, aimX, aimY;
    public int dash, fire;
    public bool revive;
    public int choice, round;
}

public enum LanPhase { Lobby, Loading, Playing, Upgrade, Victory, Defeat, Disconnected }

[Serializable]
public class LanState
{
    public LanPhase phase;
    public float time;
    public int level = 1;
    public long exp, maxExp = 10;
    public bool paused;
    public int round;
    public int bossHp, bossMaxHp;
    public LanPlayerState[] players = { new LanPlayerState(), new LanPlayerState() };
    public LanVisualState[] visuals = new LanVisualState[0];
}

[Serializable]
public class LanPlayerState
{
    public int character = 1;
    public bool ready, down, chosen;
    // 每玩家独立升级弹窗待打开（不暂停游戏，per-player）
    public bool upgradePending;
    public int hp, maxHp;
    // 独立经验/等级（不再队伍共享）
    public long exp, maxExp;
    public int level;
    // 属性加成（和单机右上角格式对齐）
    public int damage, defense, cooldown, amount;
    public float moveSpeed, attackPerSecond;
    public bool hasAmount;
    // 起始武器实际伤害（属性面板"伤害"槽位显示，含攻击力加成重算值）
    public int weaponDamage;
    // 激光炮（character == 4）角色专属
    public bool isLaser;
    public float laserWidth;
    // Axe 角色专属（character == 3）
    public bool isAxe;
    public float axeRange, axeAngle, axeCooldown;
    public int axeDamage;
    public float x, y, rescue;
    public string[] choices = new string[0];
    public string weapons;
}

[Serializable]
public struct LanVisualState
{
    public int id, sprite, layer, order;
    public float x, y, z, rotation, sx, sy;
    public bool flipX, flipY;
    public Color color;
}
