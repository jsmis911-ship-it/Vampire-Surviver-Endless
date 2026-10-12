using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magnet_Item : Base_Item
{
    public override void OnItemEvent(PlayerStat player)
    {
        if (LanSession.Active)
        {
            foreach (var exp in FindObjectsOfType<Exp_Item>()) exp.target = player.transform;
            return;
        }
        StartCoroutine(MagnetExp(player));
    }

    IEnumerator MagnetExp(PlayerStat player)
    {
        ItemGetter itemGetter = player.transform.Find("GetItemRange").GetComponent<ItemGetter>();
        float size = itemGetter._size;
        itemGetter.transform.GetComponent<CircleCollider2D>().radius = size * 100;
        yield return new WaitForSeconds(0.1f);
        itemGetter.transform.GetComponent<CircleCollider2D>().radius = size;

    }

    protected override void OnTriggerEnter2D(Collider2D col)
    {
        if (col.transform.tag == "Player")
        {
            if (LanSession.Active && (claimed || !LanSession.Instance.CanSimulate || LanSession.Instance.IsDown(col.GetComponent<PlayerController>()))) return;
            claimed = true;
            PlayerStat playerStat = col.GetComponent<PlayerStat>();
            OnItemEvent(playerStat);
            target = null;
            Managers.Resource.Destroy(gameObject, 0.2f);
        }
    }
}
