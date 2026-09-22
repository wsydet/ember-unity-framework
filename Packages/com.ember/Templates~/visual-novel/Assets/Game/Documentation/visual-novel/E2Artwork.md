# E2 正式素材变体记录

2026-09-21，使用内置 `image_gen`，未使用 CLI/API fallback。两张都从现有正式原图编辑，保留原文件；输出已经人工查看，构图与风格一致、表情和天色变化清楚。后续用户已确认本轮测试全部通过、无报错，见 [收束记录](Implementation.md#e2-测试通过与文档收束)。合成边缘与双图中间帧的逐项人工观感验收未单独确认，仍保留为美术复核项。

| 资源键 | 项目内文件（相对于 Assets/GameResource/Resources） | 原图 | 尺寸 |
|---|---|---|---|
| `lastlight_wan_smile` | `UI/Module/Narrative/Atlas/LastLight/Portraits/lin_smile.png` | 同目录 `lin_evening.png` | 1024×1536，RGBA |
| `lastlight_rooftop_night` | `UI/Module/Narrative/Atlas/LastLight/Backgrounds/rooftop_night.png` | 同目录 `rooftop_dusk.png` | 1672×941，RGB |

女主保持同一人物、姿态、服饰和暖色轮廓光，只柔化眼神并微笑。屋顶保持建筑、栏杆、云层和镜头位置，改为蓝色入夜，保留暖色门灯。旧的 `alice_smile` 兼容键不改为新图，避免改变旧剧情/测试的资源含义。表格源与正文已引用新键，正式 bytes 由配表中心 BakeCurrent 导出。

最终提示词（编辑目标均为表中原图）：

```text
Edit target: supplied visual novel female portrait. Create precisely aligned expression variant for a sprite crossfade. Preserve canvas aspect 2:3, exact person identity, hairstyle, pose, framing, clothing, coat silhouette and warm rim light, all pixels outside the facial expression as unchanged as possible. Change only her face: a small gentle closed-mouth relieved smile, slightly softened eyes looking in exactly same direction. No movement, new props, text or border. Preserve existing background treatment and transparency if present. Production illustration matching source quality. One full portrait only.
```

```text
Edit target: supplied 16:9 rooftop visual novel background. Make an exactly composition-aligned later-at-night lighting variant for slow crossfade. Preserve geometry and positions of door, pipes, railing, distant buildings, mountains, clouds, wet roof and framing. Only advance twilight lighting into cool blue early night, reduce orange horizon glow almost completely; existing warm lit door and windows stay warmly luminous, wet roof reflects them. Same detailed cinematic illustrated style. No people, no new objects, no text, no logo, no camera movement. Deliver one landscape 16:9 image.
```
