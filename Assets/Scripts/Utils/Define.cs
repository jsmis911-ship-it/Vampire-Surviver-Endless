using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    public enum WorldObject
    {
        Unknown,
        Player,
        Enemy,
        Weapon
    }

    public enum Weapons
    {
        Knife = 1,
        Fireball = 2,
        Spin = 3,
        Poison = 4,
        Lightning = 101,
        Shotgun = 102,
        Axe = 103,
        LaserCannon = 104
    }
    public enum MonsterStyle
    {
        unknown = 0,
        zombie = 1,
        zombieElite = 2,
        skeleton = 3,
        skeletonElite = 4,
        tombStone = 5
    }
    public enum MonsterType
    {
        unknown,
        Enemy,
        middleBoss,
        Boss
    }
    public enum PlayerStartWeapon
    {
        Lightning = 1,
        Shotgun = 2,
        Axe = 3,
        LaserCannon = 4
    }

    public enum PopupUIGroup
    {
        Unknown,
        UI_GameMenu,
        UI_ItemBoxOpen,
        UI_LevelUp,
        UI_GameOver,
        UI_GameVictory,
        UI_TimeStop,
        UI_CharacterSelect,
        UI_Encyclopedia,
        UI_Tutorial,
        UI_Inventory
    }

    public enum SceneUI
    {
        Unknown,
        UI_Player,
        UI_MainMenu,
    }

    public enum Sound
    {
        Bgm,
        Effect,
        MaxCount,
    }
    public enum BGMs
    {
        BGM_01,
        BGM_02,
        BGM_03,
        BGM_04,
        A_Bit_Of_Hope
    }
    public enum UIEvent
    {
        Click,
        Drag,

    }
    public enum SceneType
    {
        Unknown,
        GameScene,
        MainMenuScene
    }

}
