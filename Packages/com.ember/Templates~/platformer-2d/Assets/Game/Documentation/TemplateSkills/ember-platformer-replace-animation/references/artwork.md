# 素材与导入

## 先确定本次范围

读取目标方案 Graph.Nodes 和 Animation.Slots：节点 ID、父节点、动画 State、Key、Clip、Speed、BlendSeconds及启用机制。典型基线有 Idle/Accelerate/Move/Decelerate、Jump/Fall/AirJump、WallSlide/WallJump、GroundDash/AirDash、DropThrough、LedgeHang/LedgeClimb、Land/HardLand/Rolling、HeadJump；父节点也有后备槽。实际图变化时按实际内容生成，禁用动作的动画槽仍必须完整。

把每组素材映射到稳定节点，不通过文件名自动认定动作。用户只要求替换 Idle/Move 时保留其余原片段；用户要求全套但仅提供 Idle 时列出缺失并停止依赖这些素材的接线。允许先导入已明确的素材或展示映射草稿。扩展动作缺图可由用户明确选择占位或复用已有片段；复用 Clip 不等于可以让多个槽共享 Key。

## 输入信息

| 项目 | 需确定的内容 |
|---|---|
| 来源 | 单帧PNG序列或spritesheet；原文件列表，不覆盖用户原图 |
| 顺序 | 连续编号、显式列表，或sheet行列方向与Unity底部原点转换；不能字典序把10排在2前 |
| 切片 | 帧尺寸、行列、边距/间隔、空格处理；检查非整除和越界 |
| 节奏 | 帧率或各帧时长；Loop/非Loop；最后一帧需要保持多久 |
| 对齐 | 默认朝向、统一脚底基准、每帧轴心；尺寸不同是否已裁剪并有偏移数据 |
| 比例 | PPU与期望世界显示尺寸；区分角色实际像素轮廓与透明画布 |

建议透明PNG、统一画布/脚底、`idle_0001.png`式连续编号。像素画通常Point、关闭mipmap和不必要压缩；其他画风依实际素材选择，不强制所有图片同分辨率。相同PPU只保证像素比例，不能自动保证不同裁剪框的脚底稳定。Sprite Pivot是纹理/切片归一化坐标；脚底基准与物理根之间的偏移仍须在实际Prefab上检查。

## Unity 导入

资源归项目自有的Player美术目录，先核对现有目录规范。使用AssetDatabase/TextureImporter；Single Sprite设置spritePixelsPerUnit，轴心对齐经ReadTextureSettings取得TextureImporterSettings后设置spriteAlignment/spritePivot，再SetTextureSettings。先查看框架`Packages/com.ember/Basic/Editor/SpriteImportUtility.cs`已有导入能力，按实际API复用。多切片先检查已安装2D Sprite包的Sprite Data Provider接口，使用实际可用的API维护SpriteRect及名称/ID映射；不要猜新版API或手写meta/GUID。已有sheet重切片会影响子资产身份，先找引用并保留映射；无法保证时新建导入副本。

每次SaveAndReimport后重新读取Sprite子资产的实际名称、rect、pivot、PPU和稳定身份。现有Prefab若根与Sprite均居中，直接换成底部Pivot会让角色悬空；统一脚底可以保留居中Pivot并统一画布，也可显式调整Sprite局部视觉偏移，必须与保留动作对齐，不能移动物理根来补偿。缺帧/重复名/未知动作/范围越界报明原因，不静默忽略。动画必须引用导入后的Sprite资产，不能引用临时Texture生成的未保存Sprite。

等间隔序列每帧time=i/fps，并设置clip.frameRate。当前Unity对象引用曲线会计入末键之后的一帧：4fps的两个键0/0.25秒实际clip.length为0.5秒；再加0.5秒重复末键会变成0.75秒，意外延长末帧。不要无条件追加重复末键；导入后读取实际clip.length并播放检查最后一帧和循环接缝。非等间隔用累计时长并核对尾段，与混合Transform曲线的最长时间一并验证。Loop由AnimationClipSettings控制；非Loop播放到末帧不会决定FSM退出。

创建片段后检查 ObjectReferenceCurve：路径相对Animator，一般为`Sprite`，类型SpriteRenderer，属性`m_Sprite`。仅此曲线即可完成普通序列帧替换；需要姿态曲线时仅限Sprite子树局部Transform，明确缩放和脚底影响。空片段、Legacy、AnimationEvent、颜色/alpha曲线或根运动在接线前拒绝。
