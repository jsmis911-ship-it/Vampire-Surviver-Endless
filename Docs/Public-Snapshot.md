# 公开项目快照

本仓库包含 2026-10-12 导出的完整 Unity 项目及当前无尽模式。

- Unity 编辑器：2022.3.62f3c1。
- 来源代码版本：`18bd017afd4d75853a39f1eb7a3e5485b1b495a4`。
- 导出当前工作区的所有 Git 管理文件；包含当前的 simhei SDF 字体资源及 ShaderGraphSettings 设置修改。
- 包含 Assets、Packages、ProjectSettings、项目文档和工具。Unity 导入时重建 Library、Temp、Logs 等本机缓存。
- 以独立初始提交发布当前项目快照，原有 README 来源说明和第三方许可文件均保留。

## 打开项目

将仓库克隆到本地，Unity Hub 添加仓库目录，使用上述 Unity 编辑器打开，等待包和资源导入。打开 `Assets/Scenes/MainMenuScene.unity`，进入 Play 模式，选择「无尽模式 · 守护核心」。

## 代码阅读

- [无尽模式说明](../ENDLESS_MODE.md)
- [无尽模式核心及集成代码全文](Endless-Source-For-GPT.md)
- [全部 Assets/Scripts 游戏脚本全文](Project-Scripts-For-GPT.md)
- [完整项目源码](../Assets)

无尽模式当前仍使用程序生成的像素风几何与纹理；验证记录和当前实现范围见 ENDLESS_MODE.md。
