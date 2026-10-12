using UnityEngine;

public class Box_Item : Base_Item
{
    public override void OnItemEvent(PlayerStat player)
    {
        if (LanSession.Active) { LanSession.Instance.OpenBox(player); return; }
        // 单机：直接随机给道具 + Toast，不再弹选择 UI
        Managers.Event.GiveRandomItemFromBox(player);
        Managers.Resource.Destroy(gameObject);
    }
}
