using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包物品图标：icon + 右下角次数角标(count>1才显示) + 点击切换 tooltip
/// UI_Base.Bind 按 enum 名字查找子对象，预制体节点名必须与 enum 值一致。
/// </summary>
public class InventoryItem : UI_Base, IPointerClickHandler
{
    enum Texts
    {
        CountText,      // 右下角次数
        TooltipName,    // 简介面板-名称
        TooltipDesc     // 简介面板-描述
    }

    enum Images
    {
        IconImage       // 主图标
    }

    enum Panels
    {
        TooltipPanel    // 简介面板（预制体里默认 SetActive(false)）
    }

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(Panels));
    }

    public void SetData(string key, int count)
    {
        string[] parts = key.Split(':');
        string category = parts.Length > 0 ? parts[0] : "";
        string rawName  = parts.Length > 1 ? parts[1] : key;

        // 图标
        Image icon = GetImage((int)Images.IconImage);
        if (icon != null)
        {
            string spriteName = MapSpriteName(rawName);
            var sprite = Managers.Resource.Load<Sprite>($"Prefabs/SpriteIcon/{spriteName}");
            if (sprite != null) icon.sprite = sprite;
        }

        // 次数角标：count > 1 才显示
        TextMeshProUGUI countTxt = GetText((int)Texts.CountText);
        if (countTxt != null)
        {
            countTxt.gameObject.SetActive(count > 1);
            countTxt.text = count.ToString();
        }

        // 填 tooltip
        GameObject tp = Get<GameObject>((int)Panels.TooltipPanel);
        if (tp != null)
        {
            GetText((int)Texts.TooltipName).text = MapDisplayName(rawName);
            GetText((int)Texts.TooltipDesc).text = MapDesc(rawName, category);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameObject tp = Get<GameObject>((int)Panels.TooltipPanel);
        if (tp != null) tp.SetActive(!tp.activeSelf);
    }

    // ======================================================
    static string MapSpriteName(string raw)
    {
        return raw == "MaxHP" ? "MaxHp" : raw;
    }

    static string MapDisplayName(string raw)
    {
        switch (raw)
        {
            case "MaxHP":     return "最大生命";
            case "MoveSpeed": return "移动速度";
            case "Damage":    return "攻击力";
            case "Defense":   return "防御力";
            case "Cooldown":  return "冷却缩减";
            case "Amount":    return "弹量";
            case "Fireball":  return "火球术";
            case "Knife":     return "飞刀";
            case "Lightning": return "闪电链";
            case "Shotgun":   return "霰弹";
            case "Spin":      return "旋风斩";
            case "Poison":    return "毒雾";
            default:          return raw;
        }
    }

    static string MapDesc(string raw, string category)
    {
        if (category == "Weapon")
        {
            switch (raw)
            {
                case "Fireball":  return "发射火球对敌人造成范围伤害";
                case "Knife":     return "射出锋利飞刀，快速攻击前方敌人";
                case "Lightning": return "释放闪电在敌人间跳跃";
                case "Shotgun":   return "近距离发射多枚弹丸";
                case "Spin":      return "武器围绕角色旋转攻击";
                case "Poison":    return "释放毒雾持续伤害敌人";
                default:          return "获得新武器";
            }
        }
        else
        {
            switch (raw)
            {
                case "MaxHP":     return "增加 10 点最大生命值";
                case "MoveSpeed": return "增加 1 点移动速度";
                case "Damage":    return "增加 10 点攻击力";
                case "Defense":   return "增加 1 点防御力";
                case "Cooldown":  return "增加 10% 冷却缩减";
                case "Amount":    return "增加 1 发弹量";
                default:          return "获得属性提升";
            }
        }
    }
}
