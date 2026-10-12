using System.Collections.Generic;

/// <summary>
/// 界面中文化工具：将游戏内逻辑使用的英文枚举名（属性/武器）
/// 映射为中文显示文本。仅用于显示层，逻辑层仍使用英文枚举名
/// （精灵加载、升级判定等依赖英文名，不可改动）。
/// </summary>
public static class UILocalization
{
    /// <summary>显示名称映射：英文枚举名 → 中文名</summary>
    private static readonly Dictionary<string, string> _names = new Dictionary<string, string>
    {
        // 属性
        { "Level", "等级" },
        { "MaxHP", "最大生命值" },
        { "MoveSpeed", "移动速度" },
        { "Damage", "攻击力" },
        { "Defense", "防御力" },
        { "Cooldown", "攻速加成" },
        { "Amount", "弹道数量" },
        // 武器
        { "Knife", "小刀" },
        { "Fireball", "火球" },
        { "Spin", "回旋刃" },
        { "Poison", "毒雾" },
        { "Lightning", "闪电" },
        { "Shotgun", "霰弹枪" },
        { "Axe", "大斧头" },
        { "AxeRangeUp", "加长斧柄" },
        { "AxeAngleUp", "宽斧刃" },
        { "AxeDamageUp", "磨刀石" },
        // 角色（角色选择界面）
        { "player0", "玩家 1" },
        { "player1", "玩家 2" },
        { "player2", "重斧农夫" },
        { "player3", "玩家 4" },
        { "Aemeath", "艾米丝" },
        // 激光炮专属武器/强化
        { "LaserCannon", "光束炮" },
        { "LaserRangeUp", "聚焦镜片" },
        { "LaserDamageUp", "能量核心" },
    };

    /// <summary>效果说明映射：英文枚举名 → 中文说明（升级面板描述用）</summary>
    private static readonly Dictionary<string, string> _descs = new Dictionary<string, string>
    {
        { "MaxHP", "生命上限 +10" },
        { "MoveSpeed", "移动速度 +1" },
        { "Damage", "攻击力 +10" },
        { "Defense", "防御力 +1" },
        { "Cooldown", "攻击速度加成" },
        { "Amount", "弹道数量 +1" },
        { "Knife", "向前方连续投掷小刀" },
        { "Fireball", "投掷会爆炸的火球" },
        { "Spin", "环绕自身旋转的刀刃" },
        { "Poison", "留下持续伤害的毒雾" },
        { "Lightning", "可以增加落雷的伤害和攻速" },
        { "Shotgun", "扇形散射多枚弹丸" },
        { "Axe", "强化重斧：提高伤害和攻击距离" },
        { "AxeRangeUp", "斧头攻击距离 +0.4，武器和斩击同步延长（最多 3 级）" },
        { "AxeAngleUp", "斧头扇形角度 +15°（最多 3 级）" },
        { "AxeDamageUp", "斧头基础伤害 +10（最多 3 级）" },
        { "LaserCannon", "提升伤害和射程并增加攻速" },
        { "LaserRangeUp", "激光射程 +1，宽度 +0.3（最多 3 级）" },
        { "LaserDamageUp", "激光伤害 +30%（最多 3 级）" },
    };

    /// <summary>获取中文显示名，未匹配时原样返回</summary>
    public static string GetName(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        return _names.TryGetValue(key, out string cn) ? cn : key;
    }

    /// <summary>获取中文效果说明，未匹配时回退为中文名</summary>
    public static string GetDesc(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        return _descs.TryGetValue(key, out string cn) ? cn : GetName(key);
    }
}
