using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exp_Item : Base_Item
{
    public Sprite[] _sprite;
    public long _exp;
    public int _expMul= 1;

    public override void OnItemEvent(PlayerStat player)
    {
        // 联机/单机统一：每个玩家独立经验池，谁拾取加给谁
        long percentExp = (long)Math.Truncate((double)player.MaxExp / 100);
        if (percentExp > _exp)
            _exp = percentExp;
        player.Exp += _exp * _expMul;
    }

    public void SetExp(long exp, int mul)
    {
        _exp = exp;
        _expMul = mul;
    }

}
