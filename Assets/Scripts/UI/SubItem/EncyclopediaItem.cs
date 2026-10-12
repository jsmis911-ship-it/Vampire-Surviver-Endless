using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 道具图鉴条目：展示单个道具的图标、中文名称与中文描述。
/// </summary>
public class EncyclopediaItem : UI_Base
{
    enum Images
    {
        ItemImg
    }

    enum Texts
    {
        NameText,
        DescText
    }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
    }

    /// <param name="itemKey">道具的英文枚举名（如 Knife / Cooldown），用于加载图标与本地化文本</param>
    public void SetInfo(string itemKey)
    {
        GetImage((int)Images.ItemImg).sprite = Managers.Resource.LoadSprite(itemKey);
        GetImage((int)Images.ItemImg).preserveAspect = true;
        GetImage((int)Images.ItemImg).type = Image.Type.Simple;
        GetText((int)Texts.NameText).text = UILocalization.GetName(itemKey);
        GetText((int)Texts.DescText).text = UILocalization.GetDesc(itemKey);
    }
}
