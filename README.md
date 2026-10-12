# Vampire Surviver · 无尽地牢模式公开项目

完整 Unity 工程，包含当前的单人无尽地牢守护核心模式及原有游戏功能。

- Unity：**2022.3.62f3c1**
- [项目快照与打开方法](Docs/Public-Snapshot.md)
- [无尽模式说明](ENDLESS_MODE.md)
- [无尽模式代码全文，供网页版 GPT 查阅](Docs/Endless-Source-For-GPT.md)
- [全部游戏脚本代码全文](Docs/Project-Scripts-For-GPT.md)

---

以下保留原项目说明与来源链接。

# 亡灵幸存者（UNDEAD SURVIVOR）

![image-20230119153212524](https://user-images.githubusercontent.com/118050445/213386697-4c664a95-2179-4d55-bc08-21a887409a50.png)



- 为了提高对游戏开发的理解与熟练度，制作了一款休闲风格、以最少功能保证最大乐趣的类吸血鬼幸存者（Vampire Survivor-like）游戏。

- 游戏试玩 - https://drive.google.com/file/d/1DCUpQkDisK1UOMmwQSigFkcPXnLAeTBA/view?usp=sharing
- 项目说明文档 - https://github.com/newtron-vania/Undead_Survivor-Vampire_Survivor-copy-practice/blob/main/%EC%96%B8%EB%8D%B0%EB%93%9C%20%EC%84%9C%EB%B0%94%EC%9D%B4%EB%B2%84%20%EC%84%A4%EB%AA%85%EC%84%9C.pdf
- 游玩视频 - https://youtu.be/Xxo9mynXYRA

- 已实现功能
  - 角色选择
  - UI
  - 生成池（SpawningPool）
  - 对象池（Pooling Object）
  - 道具
  - 武器
  - 中 Boss
  - Boss
  - 等等...
- 待补充功能
  - 角色升级
  - 被动道具
  - 更多武器
  - 怪物生成模式


## 已实现的系统

- 动画

  为每个对象添加了额外的动画，UI 上也附加了动画。

<img src= "https://user-images.githubusercontent.com/118050445/213386727-87c1615a-3b8d-4b3f-b300-4d5d6ca11fb8.gif" width = "60px" height = "80px"> <img src= "https://user-images.githubusercontent.com/118050445/213386797-26b81c54-db72-4dd4-97a6-1f9cdf6f68ed.gif" width = "60px" height = "80px">
<img src= "https://user-images.githubusercontent.com/118050445/213386810-0ec5b0ec-2cec-4773-a522-86eb0365c52b.gif" width = "60px" height = "80px">
<img src= "https://user-images.githubusercontent.com/118050445/213386826-73aafc42-61e4-4ff8-90e4-93199913b55b.gif" width = "60px" height = "80px">
<img src= "https://user-images.githubusercontent.com/118050445/213386839-a33f57e8-d17c-479f-8506-4436a0f4c745.gif" width = "60px" height = "80px">
<img src= "https://user-images.githubusercontent.com/118050445/213386847-8e814c36-0649-4c89-902c-10df8f23a706.gif" width = "60px" height = "80px">
<img src= "https://user-images.githubusercontent.com/118050445/213386869-6df641d2-d16e-4d71-82af-f587b333cdaa.gif" width = "60px" height = "80px">

- UI 动画
<p align="center">
<img src="https://user-images.githubusercontent.com/118050445/213387108-4b8f7ed4-e847-4b8a-9e3e-4cd73ac40304.gif" width = "50% height = "50%">
</p>

## 武器（Weapon）

每个角色出生时携带专属武器，随游戏进程可获取额外武器并提升武器属性。
- 武器的属性由武器等级和玩家属性决定，通过 DataManager 读取各武器各等级对应的属性数据，并根据武器 ID 与当前武器等级将对应数据保存到武器属性中。

- 当前已实现武器 - 小刀（Knife）、火球（Fireball）、旋转武器（SpinWeapon）、毒（Poison）、霰弹枪（Shotgun）、闪电（Lightning）

<img src= "https://user-images.githubusercontent.com/118050445/213397871-6552ff87-e7dc-404d-912f-6846f2228c72.gif" alt="text" width="30%" height="30%"> <img src= "https://user-images.githubusercontent.com/118050445/213397892-9a159b47-d3f4-470e-b141-aa4b3b1fb5cc.gif" alt="text" width="30%" height="30%"> <img src= "https://user-images.githubusercontent.com/118050445/213397899-14df5e24-3a2a-406d-893d-1ab6c7b0fa4e.gif" alt="text" width="30%" height="30%">


## 玩家属性（PlayerStat）
 角色的主要属性由最大生命值（MaxHp）、移动速度（MoveSpeed）、攻击力（Damage）、防御力（Defense）、冷却时间（Cooldown）、数量（Amount）组成。
 - MaxHp：最大生命值。每 1 点增加 1。
 - MoveSpeed：移动速度。每 1 点增加 1。
 - Damage：攻击力。每 10 点攻击力增加 10%（加法计算）。
 - Defense：防御力。每 1 点增加 1。
 - Cooldown：武器冷却时间。每 10 点冷却时间减少 10%（乘法计算）。
 - Amount：武器生成数量。一次生成的武器数量增加，每 1 点增加 1。

<p align="center">
<img src= "https://user-images.githubusercontent.com/118050445/213406700-4d3f1217-0df9-479e-adb2-b1157b4b60af.PNG" alt="text" width="50%" height="50%">
</p>

## 道具（Item）
 怪物死亡时生成经验值（Exp），并有一定概率生成道具。角色可通过 ItemGetterObject 检测一定距离内的道具，检测到道具时道具会向角色移动，与角色碰撞时执行对应道具的 OnItemEvent。

<p align="center">
<img src= "https://user-images.githubusercontent.com/118050445/213404095-ff6fb57f-997d-464e-a811-c83c217ba9f8.gif" alt="text" width="50%" height="50%">
</p>

- Exp：按特定数值增加角色经验值。

<p align="center">
<img src= "https://user-images.githubusercontent.com/118050445/213404010-26593362-7aad-4fff-81df-99e42e3331c4.gif" alt="text" width="50%" height="50%">
</p>

- Health：按特定数值恢复角色生命值。

<p align="center">
<img src= "https://user-images.githubusercontent.com/118050445/213404146-cb846101-234b-4776-8985-123da72e05be.gif" alt="text" width="50%" height="50%">
</p>

- Magnet：将地图上存在的所有道具吸引向角色移动。

<p align="center">
<img src= "https://user-images.githubusercontent.com/118050445/213404176-cec7ab90-7e15-4b41-ae4d-256efc8a07c3.gif" alt="text" width="50%" height="50%">
</p>

- ItemBox：不会向角色移动的道具。拾取时生成 ItemBoxOpenUI，可随机获取或强化武器。如果武器已无法强化或获取，则获得 Health 道具。

<p align="center">
<img src= "https://user-images.githubusercontent.com/118050445/213404163-b701615f-9192-474a-af70-add9af5ed383.gif" alt="text" width="50%" height="50%">
</p>


## 生成池（SpawningPool）
- 角色周围一定范围内存在生成怪物的 SpawningPool，随机指定位置生成怪物。
- 每经过 1 分钟游戏时间生成一个中 Boss，中 Boss 比普通怪物拥有更强的属性。
- 5 分钟后生成 Boss，Boss 死亡时游戏胜利。

### 怪物种类
- 近战：怪物默认在接触玩家时造成伤害。
- 远程：特定怪物会向玩家位置发射子弹。
- 中 Boss：体型和重量比普通怪物更大，属性在普通怪物基础上按中 Boss 修正有所提升。
- Boss：与其他怪物独立存在，拥有 1 个以上技能，随机选择 1 个技能使用。技能有冷却时间。

### Boss 技能示例

  - 闪现（Blink）— 时间停止后移动到玩家周围
<p align="center">
<img src= "https://user-images.githubusercontent.com/118050445/213416839-579a61dd-ebb0-445f-ac83-7ffdad75fa83.gif" width ="50%" height="50%">



  - 冲刺（Rush）— 向玩家位置直线冲锋，重复 3 次
<p align="center">
<img src= "https://user-images.githubusercontent.com/118050445/213416857-4163c548-b036-489b-9c92-dd354db46dfc.gif" width ="50%" height="50%">
</p>


# 开发成果
<p align="center">
<img src="https://user-images.githubusercontent.com/118050445/213421054-cb5d8223-3279-4d0d-9623-08725e602b18.gif" width="70%" height="70%">
</p>

##
- 制作周期：20 天
- 参考资源
  - 亡灵幸存者资源包（Undead Survivor Assets Pack）：https://assetstore.unity.com/packages/2d/undead-survivor-assets-pack-238068
  - 2D Flat Explosion：https://assetstore.unity.com/packages/2d/textures-materials/2d-flat-explosion-66932
  - 8-Bit Sfx：https://assetstore.unity.com/packages/audio/sound-fx/8-bit-sfx-32831
  - Casual Game BGM #5：https://assetstore.unity.com/packages/audio/music/casual-game-bgm-5-135943
  - 2D Potions Pixel Art：https://assetstore.unity.com/packages/2d/gui/icons/2d-potions-pixel-art-196023
  - Buttons Set：https://assetstore.unity.com/packages/2d/gui/buttons-set-211824
  - Monsters_Creatures_Fantasy：https://assetstore.unity.com/packages/2d/characters/monsters-creatures-fantasy-167949
