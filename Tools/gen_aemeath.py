# -*- coding: utf-8 -*-
"""
角色4 Aemeath 外观改造（套用角色1模板后，向参考图"黑+青+白"配色对齐）：
  - 保留粉发 / 皮肤 / 动作骨架（用户认可移动帧，不改逻辑）
  - 裙子/衣服 粉红色系 -> 黑 + 青 + 白（参考图黑青白制服）
  - 金色光环 -> 青色光环
  - 红色发饰  -> 青色发饰
  - 肩部两侧添加 静态小翅膀（黑骨架 + 白翼面 + 青翼尖，参考图样式）
输入：当前 Farmer 4.png（基于角色1模板生成）
输出：重写像素内容，不改切片/meta，动画与两模式同步生效
"""
from PIL import Image

SRC = r"d:\Vampire-Surviver\Assets\Resources\Sprites\Farmer 4.png"

# ---- 配色（参考图）----
BLACK   = (36, 36, 44, 255)      # 黑色衣服/翼骨
BLACK_D = (20, 20, 26, 255)      # 衣服阴影
CYAN    = (0, 210, 228, 255)     # 青色主色
CYAN_D  = (0, 150, 175, 255)     # 青色深
CYAN_HI = (160, 245, 255, 255)   # 青色高光/白
WHITE   = (242, 242, 240, 255)   # 白

# ---- 颜色替换规则：衣服粉/紫粉 -> 黑青白；金色光环/红饰 -> 青 ----
# 裙子(粉)与头发(粉)RGB 不同值，据此仅改裙子，头发保留
CLOTH_MAP = {
    (229, 85, 122):  BLACK,      # 裙主粉
    (244, 109, 153): CYAN,       # 裙亮粉 -> 青
    (197, 88, 194):  CYAN_D,     # 深紫粉 -> 青深
    (255, 150, 246): CYAN_HI,    # 粉紫亮 -> 青高光
    (239, 175, 141): BLACK_D,    # 浅肤色阴影 -> 黑（衣物阴影）
}
NON_COLORED = [(255,150,185),(240,118,160,(255))]

# 抗干扰：精确判定
def key(rgb): return (rgb[0], rgb[1], rgb[2])

im = Image.open(SRC).convert("RGBA")
px = im.load()
W, H = im.size

# 1) 衣服/裙子 粉 -> 黑青白
for y in range(H):
    for x in range(W):
        c = px[x, y]
        if c[3] == 0:
            continue
        k = key(c)
        # 保护：头发粉/发影粉 不碰（发色保留）
        if k in ((255,150,185),(240,118,160)):
            continue
        if k in CLOTH_MAP:
            px[x, y] = CLOTH_MAP[k]

# 2) 金色光环 -> 青色光环
HALO_MAP = {
    (255, 238, 150): CYAN,      # 光环底
    (255, 250, 210): CYAN_HI,   # 光环高光
}
for y in range(H):
    for x in range(W):
        c = px[x, y]
        if c[3] == 0: continue
        k = key(c)
        if k in HALO_MAP:
            px[x, y] = HALO_MAP[k]

# 3) 红色发饰 -> 青色发饰
for y in range(H):
    for x in range(W):
        c = px[x, y]
        if c[3] == 0: continue
        if key(c) == (232, 70, 110):
            px[x, y] = CYAN

# 4) 肩部两侧加 静态小翅膀（黑边框 + 白翼面 + 青翼尖，参考图样式）
#    12帧坐标（与 Farmer meta 一致）
FRAMES = [
    (0, 63), (19, 63), (38, 63),
    (0, 42), (19, 42), (38, 42),
    (0, 21), (19, 21), (38, 21),
    (0, 0), (19, 0), (38, 0),
]
# 每片翼用 左右向羽毛形，从肩部(外侧x)向下外展；黑边框/白翼面/青翼尖。
W = im.size[0]
# (lx, ly, color) 相对帧原点（帧宽18）
def FLIP_LR(wing):
    """把左翼(x0-2向下垂)水平镜像为右翼(x15-17)"""
    return [(17 - lx, ly, col) for (lx, ly, col) in wing]
L_FEATHER = [
    (0, 9,  BLACK), (1, 9,  WHITE), (2, 9,  BLACK),
    (0, 10, WHITE), (1, 10, WHITE), (2, 10, BLACK),
    (0, 11, CYAN),  (1, 11, WHITE), (2, 11, BLACK),
    (0, 12, CYAN),  (1, 12, BLACK),
    (0, 13, CYAN),
]
def draw_wing(cx, cy, wing):
    for (lx, ly, col) in wing:
        sx = cx + lx
        sy = cy + ly
        if 0 <= sx < W and 0 <= sy < H:
            px[sx, sy] = col

L_WING = L_FEATHER
R_WING = FLIP_LR(L_FEATHER)
for (cx, cy) in FRAMES:
    draw_wing(cx, cy, L_WING)
    draw_wing(cx, cy, R_WING)

im.save(SRC)
print("written", SRC, im.size)