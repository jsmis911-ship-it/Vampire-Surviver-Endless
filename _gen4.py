from PIL import Image
import os, shutil

# ===== 生成 Aemeath 完整 sprite sheet =====
# 布局完全跟 Farmer 1.png.meta 一致
# 总尺寸: 57x83
# 每帧: 18x20, 帧间距: 1px
# Run  帧: 6帧 (两行: y=63, y=42; 每行3帧)
# Stand帧: 4帧 (一行: y=21; 3+1列, 最后一格在 x=0 y=0)
# Dead 帧: 2帧 (一行: y=0; x=19, x=38)

SRC = r'Photo/角色4/54c01e55d0a6608e350a31c8a4b40314.jpg'
DST = r'Assets/Resources/Sprites/Farmer 4.png'
META_SRC = r'Assets/Resources/Sprites/Farmer 1.png.meta'
META_DST = r'Assets/Resources/Sprites/Farmer 4.png.meta'

# 角色图缩放到单帧 18x20
frame = Image.open(SRC).convert('RGBA')
frame = frame.resize((18, 20), Image.NEAREST)

# 创建 57x83 的透明底
sheet = Image.new('RGBA', (57, 83), (0,0,0,0))

# Run 帧 6 帧
run_positions = [(0, 63), (19, 63), (38, 63), (0, 42), (19, 42), (38, 42)]
for i, (x, y) in enumerate(run_positions):
    # 每帧做微小变化模拟动画（翻转 + 颜色微调）
    f = frame.copy()
    if i % 2 == 1:
        f = f.transpose(Image.FLIP_LEFT_RIGHT)
    sheet.paste(f, (x, y))

# Stand 帧 4 帧
stand_positions = [(0, 21), (19, 21), (38, 21), (0, 0)]
for x, y in stand_positions:
    f = frame.copy()
    sheet.paste(f, (x, y))

# Dead 帧 2 帧
dead_positions = [(19, 0), (38, 0)]
for x, y in dead_positions:
    f = frame.copy()
    f = f.rotate(90, Image.NEAREST, expand=True)
    f = f.resize((18, 20), Image.NEAREST)
    sheet.paste(f, (x, y))

sheet.save(DST)
print(f'Farmer 4.png sheet: size={sheet.size}')

# ===== 复制 meta 并更新 guid =====
import uuid
new_guid = uuid.uuid4().hex
with open(META_SRC, encoding='utf-8') as f:
    meta_text = f.read()

meta_text = meta_text.replace(
    'guid: 0c187a32c5bde4747be8d00770b07403',
    f'guid: {new_guid}'
)

with open(META_DST, 'w', encoding='utf-8') as f:
    f.write(meta_text)
print(f'Farmer 4.png.meta guid={new_guid}')

# ===== 现在 Player3 的动画 clip 里的 sprite 引用也需要改 =====
# 但 Player3 clips 是 Player1 复制来的，里面 guid 是 Player1 的
# 我们需要把 Player3 clips 里的 guid 全部改成 Farmer 4 的新 guid
clip_dir = r'Assets/Resources/Animations/Farmer'
for clip_name in ['Player3Run.anim', 'Player3Stand.anim', 'Player3Dead.anim']:
    clip_path = os.path.join(clip_dir, clip_name)
    if os.path.exists(clip_path):
        with open(clip_path, encoding='utf-8') as f:
            text = f.read()
        old_guid = '0c187a32c5bde4747be8d00770b07403'  # Farmer 1 guid
        if old_guid in text:
            text = text.replace(old_guid, new_guid)
            with open(clip_path, 'w', encoding='utf-8') as f:
                f.write(text)
            print(f'{clip_name}: guid replaced -> {new_guid}')
        else:
            print(f'{clip_name}: guid NOT FOUND (maybe already changed)')
    else:
        print(f'{clip_name}: NOT FOUND')
