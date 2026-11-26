# M0 TestArena SFX Kit — 测试场必要音效包

**项目：** 命运残响 / Echoes of Fate  
**切片：** M0 TestArena  
**引擎：** Unity 6 LTS（可直接导入的短 WAV）  
**文档路径：** `docs/audio/m0-testarena-sfx.md`  
**资源路径：** `Assets/_Project/Audio/SFX/`

本包只提供测试场战斗反馈与界面操作所需的五条短音效。不做完整配乐，不做墓灯村氛围乐，不做语音，不做商店 / 大地图音效，也不在本包中把声音接进 TestArena。

This pack is the M0 TestArena *necessary* SFX kit only. No BGM, no Grave-Lamp Village ambience, no voice, no shop or world-map cues, and no engine wiring in this change.

---

## 1. 范围 / Scope

### 1.1 做（本包）

- 五条原创短音效，放入 `Assets/_Project/Audio/SFX/`。
- 本说明：文件名、用途、触发时机、时长、建议播放音量 / 混音备注、格式。

### 1.2 不做（明确砍掉）

| 不做 | 说明 |
|---|---|
| BGM / 循环配乐 | 包括战斗床、菜单乐、胜利 / 失败sting |
| 墓灯村氛围 | 环境床、风、灯火、空间混响设计 |
| 语音 | 单位呼喊、倒下惨叫、旁白 |
| 商店 / 大地图 | 任何非测试场界面音 |
| 工程接线 | 不改场景、预制体、脚本、Audio Mixer 资产，不把 AudioSource 挂进 TestArena |
| 战斗数值 | 不改伤害、骰面、命运值或其他模拟数据 |

本包只提交音频资源与本文档。接线交给工程在后续任务完成。

This change does **not** modify combat numbers and does **not** hook the clips into TestArena scenes, prefabs, or scripts.

---

## 2. 风格 / Style

已锁定视觉风格是 **Q 版暗黑**（chibi dark-fantasy）：克制、墓灯金、冷蓝黑、锈红、湿石、羊皮纸、灰烬。音频与之对齐：

- 克制、短促、略带玩具尺度（toy-like in scale），但音色偏暗，不当成搞笑音效。
- 不是写实电影预告片，不是现代电音 / EDM，不是 8-bit / chiptune，也不是日常番式闪亮 UI。
- 受击与倒下要有分量，但不要枪响、碎玻璃、好莱坞爆音或漫画拳。
- 骰子是石 / 骨骰落在石或木面上，不是塑料赌场骰、老虎机或卡通摇盒。
- 确认音是短促的封印 / 戳印，带一点暗金、立刻衰减的低频分音，不是升级叮、合唱或撒金粉。
- 出征是沉而决断的按键提示：闷住的低号角气息，叠一次皮具 / 金属或低鼓轻击。不是进军号、合唱或战争片冲锋。

全部五条均为本项目原创的程序合成音频，不使用第三方采样、未授权素材或商业音效库。

All five clips are original procedural audio created for this project. No third-party samples.

---

## 3. 技术规格 / Format

| 项目 | 规格 |
|---|---|
| 容器 | 标准 RIFF/WAVE，仅 `fmt ` + `data` 块 |
| 编码 | PCM，16-bit little-endian |
| 采样率 | 44100 Hz |
| 声道 | 单声道 mono |
| 峰值 | 约 -6 dBFS，有余量，无削波 |
| 起收 | 2–5 ms fade-in；干净 fade-out，无起音 / 尾音咔哒 |
| 导入 | Unity 6 可直接导入。建议保持单声道、不要在 Importer 里再做 Normalize（资源已按峰值对齐） |

---

## 4. 文件与触发 / Files and triggers

建议播放音量是给后续接线的混音备注，不是本包已经写入工程的数值。重叠受击可能叠音，所以受击音量偏保守。

Suggested playback volumes below are notes for engineering. They are not wired in this pack.

| 文件名 | 用途 | 触发时机（TestArena） | 时长（约） | 建议播放音量 / 混音备注 | 格式 |
|---|---|---|---|---|---|
| `sfx_combat_hit.wav` | 受击。任意单位共用的短促躯干撞击：低频闷击 + 极短的钝质甲胄 / 布料刻点。 | 单位受到伤害时播放一次，与 0.4 s 受击闪白（hit-flash）VFX 同步。不要循环。重叠受击可以叠播，因此音量需保守。 | 321 ms | ~0.70。多段受击叠在一起时不应爆音；可挂在 SFX / Combat 总线并考虑短时压缩或每源衰减。 | PCM 16-bit, 44.1 kHz, mono |
| `sfx_combat_down.wav` | 倒下。比受击更低、更长的塌倒：甲胄 / 布料落定、轻软灰烬扑散、碎石摩擦。不是惨叫或滑稽倒地。 | 单位死亡时播放一次，与脚边死亡烟散（death-puff）VFX 同步。不要循环。 | 522 ms | ~0.85。通常同时只会出现一声，可比受击略响。 | PCM 16-bit, 44.1 kHz, mono |
| `sfx_ui_dice_roll.wav` | 战斗中掷骰。三枚石 / 骨骰落在石或木面上：窗口内 3–5 次不规则撞击，带一点刮擦。 | 自动战斗（AutoCombat）中，玩家在命运值 fate ≥ 50 时点击「掷骰」。每场战斗只掷一次。 | 458 ms | ~0.80。一次性 UI / 战斗反馈，不要循环。 | PCM 16-bit, 44.1 kHz, mono |
| `sfx_ui_dice_confirm.wav` | 确认骰面。柔和的木 / 石封印感，带一声很快熄灭的暗金分音（约 277 Hz）。干、偏羊皮纸与石质。 | 玩家点击「确认」时播放；或确认在 6 秒时自动触发时播放。 | 320 ms | ~0.75。避免与掷骰尾音叠成两次“成功音”；若两声间隔很近，可略压此条。 | PCM 16-bit, 44.1 kHz, mono |
| `sfx_ui_march.wav` | 出征按钮。沉、决断的短提示：闷住的低号角气息（一个音，略带紧密音程）+ 一次皮具 / 低鼓轻击。像踏入裂隙或点亮墓灯。仍是按钮 one-shot，不是床垫式 sting。 | 玩家在有效阵型上成功点击「出征 / March」时播放。阵型为空或按钮禁用时不要播放。 | 691 ms | ~0.90。作为进入战斗的界面句点，可以是五条里最靠前的；仍需为后续 BGM（若有）留出空间。 | PCM 16-bit, 44.1 kHz, mono |

时长以本包实际导出为准，四舍五入到毫秒。若重新跑内部合成脚本，只要风格与峰值规格不变，毫秒级差异可以接受。

---

## 5. 许可 / License

这些音效是为《命运残响 / Echoes of Fate》项目原创的程序合成音频。未使用第三方采样库、Freesound 包或其他未授权版权材料。权利归本项目所有，仅用于本游戏。

Original procedural audio created for this project. No third-party samples.
