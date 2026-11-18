# 短定向性能记录

2026-11-17，Unity6000.5.4f1/URP17.5，Windows Editor/D3D12/Linear/HDR，RTX5060Ti、Core Ultra7 265KF，1920×1080。质量为半径16DU、1/4降采样、2次横竖9采样、非缩放30Hz。每组预热30帧，测量180帧；无长时、全量、构建或真机测试。原始逐帧值与有效GPU样本数见 performance.json。

| 组 | CPU主线程p50 ms | GPU p50 ms | 活动边界 | 180帧捕获次数 | 输出RT数 / 内存 |
|---|---:|---:|---:|---:|---:|
| A Solid，禁用全部局部玻璃采样，保留UI布局 | 2.4240 | 0.757504 | 0 | 0 | 0 / 0 |
| B 1 Popup，下层20表面共享组，收起GM | 3.5004 | 0.759808 | 3 | 132 | 3 / 1,558,710 B |
| C 2 Popup，下层共享组，展开GM | 4.1436 | 0.872960 | 4 | 184 | 4 / 2,078,280 B |
| D 关闭全部SoftGlass消费者，返回既有页面 | 1.8858 | 0.652544 | 0 | 0 | 0 / 0 |

每个输出480×270，B10G11R11_UFloatPack32，MSAA1，Profiler.GetRuntimeMemorySizeLong为519,570B。只把该列称为输出RT内存：RenderGraph另外有可复用临时ScratchA/B，每张观测约519,572/519,576B；不包含URP完整相机颜色/深度目标、驱动开销或总显存。不能把输出内存列当作全部GPU显存。

B的3个边界分别是Showcase、Popup与GM入口；C在此基础上多一个Popup。每个Popup的遮罩和卡片共享输出；不是一个Graphic就一次捕获。D末尾记录0相机段、0模糊材质、0共享表面材质；180帧捕获计数不变。GM仅收起时入口仍然是合法玻璃消费者，因此仍有一个边界。

测量使用FrameTimingManager；CPU/GPU为整个GameView帧，受Editor、窗口焦点及采样时序影响，不能解释为单个blur pass成本。只纳入非零GPU帧时间，并保留每帧原值；首轮RT名称过滤写成精确相等，漏掉RTHandle自动尺寸后缀，已修正StartsWith后定向重测，以performance.json为准，首轮另存且不采用其RT结论。CPU主线程p50相对A约+1.08/+1.72ms，但未进行统计显著性、热稳态、Player或设备功耗测量。

设计预算仍是建议，没有用户批准的目标机型或硬阈值；本记录不是移动性能达标声明。深/浅/彩色动态背景、单/双Popup、横竖屏真实截图另见previews/r1；S1另有同帧FrameDebugger和4×MSAA实证。
