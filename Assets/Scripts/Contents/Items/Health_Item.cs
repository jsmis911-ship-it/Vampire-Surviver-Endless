using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health_Item : Base_Item
{
    int _healthHp = 15;
    public override void OnItemEvent(PlayerStat player)
    {
        Managers.Sound.Play("GetHealth");
        player.HP = Mathf.Min(player.HP + _healthHp, player.MaxHP);
    }

}
