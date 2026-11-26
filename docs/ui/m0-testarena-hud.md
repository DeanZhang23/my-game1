# M0 TestArena HUD component kit

**Project:** Echoes of Fate / 命运残响  
**Scope:** Visual lock for the *existing* TestArena graybox rail and unit plates. This is not a new interface, not a full official HUD, and not a restyle of 墓灯村, the shop, the world map, the 语言 button, the 种子 field, the left roster, the 3×3 board chrome, or the combat log.

**Style:** Q 版暗黑, flat graphic UI. See `docs/art/STYLE_GUIDE.md`.  
**Previews:** `docs/ui/previews/`  
**Slices:** `Assets/_Project/Art/UI/Hud/`  
**Manifest (sizes, 9-slice, pivots, content rects):** `Assets/_Project/Art/UI/Hud/manifest.json`

Player-visible copy is always a localization key, never baked into a 9-slice or bar PNG. Button labels in the previews are runtime-text stand-ins of the locked strings 出征 / 掷骰 / 确认.

M0 has **no** lock, **no** reroll, and **no** crack / corrosion in this flow. Reuse existing `ui_dice_*_normal.png` faces; do not redraw them.

---

## 1. Right-rail layout (keep the current order)

Reference canvas: the current TestArena graybox at **1616×909** (1080p-ish). The right rail stays right-aligned. Do not invent a second column or a new top bar.

```
种子 field          (unchanged)
命运能量  {cur}/{max}   [optional 可掷骰]
[=======|==== fate bar 240×16, tick at 50 ========]
[slot] [slot] [slot]   [combo badge]
[  掷骰 100×32  ] [  确认 100×32  ]
[         出征 208×36              ]
combat log              (unchanged chrome)
```

Measured anchors on the 1616×909 shots (right edge of the current button cluster is x=1605):

| Element | x | y | Notes |
|---------|---|---|-------|
| Energy label + value | 1365 | 142 | Label 12px left, value 11px right, width 240 |
| Fate track | 1365 | 160 | 240×16 |
| Dice slots (3× 48, 6px gap) | 1365 | 184 | Badge to the right, 8px gap, vertically centered on the slots |
| 掷骰 / 确认 | 1381 / 1489 | 244 | 8px gap; the 208-wide stack is centered under the 240 bar |
| 出征 | 1381 | 284 | Ends at y=320, still above the log (~y=333) |

The current graybox buttons are larger (151×40 secondaries, 182×43 出征) and 掷骰/确认 span x=1283–1605. This kit follows the locked 100×32 / 208×36 sizes, so the cluster is tighter and sits under the 240px energy bar. The 语言 button and 种子 field are not restyled.

During AutoCombat and Resolve the current graybox *hides* 出征. This kit does the same: the 出征 slot is not shown once the battle has started, so the log stays in its current place. 出征 only appears in Setup (enabled when the formation is valid, disabled when the board is empty).

---

## 2. Typography (px, 1080p-ish graybox)

Previews use Noto Sans CJK SC. Engine text should match these sizes; the typeface can follow the project default as long as CJK + Latin both read at 11px.

| Role | Size | Color | Notes |
|------|------|-------|-------|
| Button label | 14 | Primary: `#0B1220`. Secondary: `#E8DCC4`. Disabled (both): `#6B6560` | Bold. Locked strings only. |
| Energy label | 12 | `#E8DCC4` | Runtime, loc `ui.testarena.fate_energy` |
| Energy value | 11 | `#E8DCC4` | Runtime, `{current}/{max}` |
| Ready tag 「可掷骰」 | 11 | `#E8C547` | Optional runtime, see loc keys |
| Nameplate name | 11 | `#E8DCC4` (dead: `#6B6560`) | One line, ellipsis if needed |
| Nameplate numbers | 9 | `#E8DCC4` (dead: `#9A938A`) | In the reserved column, not inside the bar |
| Combo badge | 11 | `#E8DCC4` | Runtime `combo.twin` / `combo.attunement` |

No wrapping anywhere on this kit. Names truncate with `…`.

---

## 2.1 Chrome weight (skulls, bones, blood, wear)

Dean asked for a heavier, carved feel (骷髅头, 骨头, 血迹, 破损) inspired by a pixel dark-fantasy HUD — **mood only**. The TestArena rail/board layout and the locked palette are unchanged. No pastoral green, no warm purple-brown, no full-window frame, no 墓灯村 / shop / map / official HUD.

How the weight is built:

- **Carved frames:** a dark shell (`#1E2C4A` or rust/ash mix) + a 2px gold (or rust/ash) band + a deep inset well, instead of a 1px hairline rim. Corner chips and hairline cracks are drawn only in the 9-slice *corners*, so edges still stretch.
- **Skulls and bones** are either overlay slices (`ui_ornament_*`) or baked only into unique 9-slice endcaps / corners — never into a stretchable center.
- **Blood** is rust `#8B2E32` spatters (`ui_ornament_blood.png`). Used on enemy and dead nameplates, and as a small stain on a spent fate track. HP fill stays rust; shield fill stays gold.
- Wear is intentional damage (chipped gold, irregular inner corners, cracks), not dirt noise over every surface.
- Ornaments are flat graphic, pixel-clean shapes. They must stay recognizable at UI scale (a 16px skull still reads as a skull; a 12px bone corner still reads as a bone joint).

Overlay stamp positions are in `manifest.json`. Display sizes are unchanged: buttons 100×32 / 208×36, fate track 240×16, dice slots 48×48, nameplates 88×38.

---

## 3. Fate energy bar

Shared pool, 0–100. Label uses loc `ui.testarena.fate_energy` — **do not** hardcode 中/英文 into the bar art. Value is runtime `{current}/{max}`.

| Item | Spec |
|------|------|
| Track display | **240×16** (authored 32×16, 9-slice border 6; display at 240). Carved dark shell + rim, chipped corners. |
| Track fill | `#152038` |
| Track rim, default | Carved `#3A4A66` (`ui_bar_track.png`) |
| Track rim, ready | Carved `#C9A227` (`ui_bar_track_ready.png`) |
| Endcaps | Overlay `ui_bar_endcap_left.png` (16×16 skull cap) and `ui_bar_endcap_right.png` (14×16 bone cap) on the ends. Use `*_ready` variants when can-roll. They overlay the 240 width; the 50 tick stays at x=120. |
| Fill | `#C9A227` with a 1px `#E8C547` top plane (`ui_bar_fill_fate.png`), Image Type **Filled**, horizontal, origin left, pivot (0.0, 0.5). Inset 3px for the thicker frame. |
| 50 tick | `ui_bar_tick.png` (3×16 carved sliver) at center x = track_x + 120. Swap to `ui_bar_tick_ready.png` (`#E8C547`) in the ready state. |
| Spent wear | A small `ui_ornament_blood.png` stain near the left cap, spent state only. |

**Gameplay states (do not invent others):**

| State | When | Look |
|-------|------|------|
| Empty | 0, including Setup | Empty track, cold rim, tick visible, no fill |
| Filling | 1–49, not yet rolled | Gold fill, cold rim, no glow |
| Ready / 可掷骰 | ≥ 50 **and** this battle has not rolled yet | Gold rim, emphasized 50 tick, 2–3px `#E8C547` leading edge on the fill. Optional runtime tag 「可掷骰」 on the label row. **Not** “full 100”. |
| Full ready | 100 and not yet rolled | Same as Ready |
| Spent | Already rolled (or never going to roll again) | Bar still shows remaining energy (roll costs 50, confirm costs 0), cold rim, no ready glow, default tick |

Can-roll is `energy >= 50 AND this battle has not rolled yet`. One roll per battle. After commit (or if the pool never reaches 50), 掷骰 stays disabled.

---

## 4. Dice result area

Three **48×48** slots in a row (6px gap). Source dice are 128×128 authored, 64×64 design size; scale `ui_dice_{sword|shield|star|feather|coin|skull}_normal.png` into the slot (42px face in a 48px frame works). **M0 does not use lock or cracked faces in this flow.**

| Element | File | Spec |
|---------|------|------|
| Empty slot | `ui_dice_slot_empty.png` | 48×48 carved well, 2px gold rim, bone-corner brackets baked in the four 12px corners (9-slice border 12). Void well, no face. Pivot (0.5, 0.5). |
| Combo badge | `ui_badge_combo.png` | 52×22 carved chrome, 12px skull baked in the left 16px endcap (9-slice L16 T6 R8 B6). Runtime 11px text in the field to the right, via `combo.twin` (双生) or `combo.attunement` (同调). **Hide the badge when there is no combo.** Place to the right of the trio. |
| Pending-confirm glow | `ui_dice_pending_glow.png` | 180×68 soft gold aura, pivot (0.5, 0.5), sits *behind* the trio during the 6-second pending-confirm window (and until auto-confirm on timeout or battle end). Not neon. `#7EB3C9` is not used here. |

**States:**

| Moment | Slots | 掷骰 | 确认 |
|--------|-------|------|------|
| Setup | Empty | Disabled | Disabled |
| AutoCombat, energy &lt; 50, not yet rolled | Empty | Disabled | Disabled |
| Can-roll (energy ≥ 50, not yet rolled) | Empty | **Enabled** | Disabled |
| After roll, pending confirm (6s) | Three normal faces + combo if any, gold glow | Disabled | **Enabled** |
| After commit, or never reached 50 | Faces may clear; 掷骰 stays disabled | Disabled | Disabled |

Logic for the 6-second timer is not part of this kit; this is only the pending-confirm visual.

---

## 5. Unit nameplates

Replaces the current designed **60×26** plates, which wrap English names and the words 「生命」「护盾」 and overflow the box (see `docs/ui/previews/mock_nameplates.png`).

**Size: 88×38.** The brief’s 88×36 is 2px short once 9px numbers sit *beside* a full-width-looking HP bar and a thinner shield bar. The extra 2px stay inside the existing 91×79 cell / aisle and still hang under a setup token (~44) or a combat portrait (~32) without eating the board.

| Item | Spec |
|------|------|
| Ally chrome | `ui_nameplate_ally.png` — carved gold frame, chipped corners. Stamp `ui_ornament_bone_corner.png` at (0,0) only (a bottom-right bone would cover shield numbers). |
| Enemy chrome | `ui_nameplate_enemy.png` — carved rust frame. Stamp `ui_ornament_skull_rust.png` (12px) at (1,1) and `ui_ornament_blood.png` (12px) at (74,1), in the frame, not over the number column. |
| Dead chrome | `ui_nameplate_dead.png` — carved ash frame, cracks. Stamp `ui_ornament_skull_ash.png` (12px) at (1,1) and blood at (74,1). Runtime: name `#6B6560`, CanvasGroup **50%** fade, empty HP fill. |
| 9-slice | border 10, pivot **(0.5, 0.0)** (top-center, hangs under the token). Do not bake unique skulls into the 10px corners — they are 12px overlays. |
| Name | One line, 11px `#E8DCC4`, rect `(4, 3)–(84, 16)`. Ellipsis if the string exceeds 80px. `Bloodacolyte` (68px at 11pt) and `Lampsister` (57px) and 锁链判官 all fit without clipping. |
| HP bar | Rust `#8B2E32` on track `#2A2420`. Content rect `(4, 18)–(58, 26)`. **Default Unity green is forbidden.** |
| Shield bar | Thinner gold `#C9A227` on `#2A2420`. Content rect `(4, 28)–(58, 35)`. |
| Numbers | 9px, reserved column `(60, 18)–(84, 26)` for `{hp}/{hpMax}` and `(60, 28)–(84, 35)` for shield current. Do not paint 「生命」 / 「护盾」 on the plate. If a screen reader / tooltip still needs words, use `ui.unit.hp` / `ui.unit.shield` — prefer bars + numbers. |

Empty vital tracks are baked into the nameplate chrome so the layout is pixel-exact. Overlay `ui_bar_fill_hp.png` / `ui_bar_fill_shield.png` with Image Type Filled, 1px inset inside the baked track. `ui_bar_track_vital.png` is an optional standalone underlay if a plate is ever composed without the baked tracks.

**Placement:** hang the plate just under the token feet (2px gap).

- **Setup:** keep the existing in-cell names. An 88×38 plate inside a 91×79 cell would cover the slot-id pill and crowd the ~44px token. HP/shield bars are not meaningful before combat.
- **Combat / Resolve:** use the new 88×38 plates on living and dead units.
- **Aisle spacing:** when two units stand ~90px apart, two 88px plates sit about 2px apart. Accept the tight gap; do not overlap, and do not invent a vertical offset in M0.

---

## 6. Buttons

Chrome 9-slices only. **Do not bake 出征 / 掷骰 / 确认 (or any glyph) into the PNGs.** Skull and bone ornaments live in the 9-slice *endcaps*; stretch the mid-band only. Borders are in the table below and in `manifest.json`.

| Kind | Size | Fill | Rim | Label |
|------|------|------|-----|-------|
| Secondary (掷骰, 确认) | **100×32** | `#152038` | Carved dark shell + 2px `#C9A227`, chipped corners. 12px skull in the left endcap, bone knuckle on the right. 9-slice L14 T10 R12 B10. | 14px `#E8DCC4` |
| Primary (出征) | **208×36** | `#C9B89A` | Carved dark shell + 2px `#C9A227`, chipped corners. 16px skull in the left endcap, 16×8 bone on the right. 9-slice L18 T10 R14 B10. | 14px `#0B1220` |

The 8px gap between the two secondaries makes the pair exactly 208, matching 出征.

**States (both kinds):**

| State | File suffix | Look |
|-------|-------------|------|
| Normal | `_normal` | As the table above |
| Hover | `_hover` | Rim `#E8C547` |
| Pressed | `_pressed` | Slightly darker fill (primary parchment `#9A8A68`, secondary `#10182A`), gold rim |
| Disabled | `_disabled` | Fill `#1E2C4A`, rim and label `#6B6560`, **no gold** |

**Enabled / disabled by game state (visual contract only; do not change button logic in this kit):**

| Phase | 出征 | 掷骰 | 确认 |
|-------|------|------|------|
| Setup, formation valid | Enabled | Disabled | Disabled |
| Setup, formation invalid | Disabled | Disabled | Disabled |
| AutoCombat, cannot roll | **Hidden** | Disabled | Disabled |
| AutoCombat, can-roll | **Hidden** | Enabled | Disabled |
| AutoCombat, pending confirm | **Hidden** | Disabled | Enabled |
| After commit / Resolve | **Hidden** | Disabled | Disabled |

---

## 7. Localization keys

| Key | Locked / suggested string | Where |
|-----|---------------------------|-------|
| `ui.testarena.fate_energy` | 命运能量 | Energy label |
| `ui.testarena.btn_march` | 出征 | Primary button |
| `ui.testarena.btn_roll` | 掷骰 | Secondary |
| `ui.testarena.btn_confirm` | 确认 | Secondary |
| `combo.twin` | 双生 | Badge, hide if no combo |
| `combo.attunement` | 同调 | Badge, hide if no combo |
| `ui.testarena.can_roll` | 可掷骰 | **Suggested** (not in the original key list). Optional gold tag on the energy label row in the ready state. The ready *look* (gold rim, leading edge, tick) does not depend on this string. |
| `ui.unit.hp` / `ui.unit.shield` | 生命 / 护盾 | Only if a tooltip still needs words. Prefer bars + numbers; do not bake these onto the plate. |

---

## 8. Locked layout calls

These are locked with this kit (not left for the engineer to invent):

1. **Nameplates are 88×38**, not 88×36 — 9px numbers need a reserved column beside the bars.
2. **Button cluster stays 100×32 / 208×36**, right-aligned under the 240px energy bar. Tighter than the current 322px graybox pair, but aligned to the bar.
3. **出征 is hidden during AutoCombat and Resolve**, matching the current graybox. It is not shown in a disabled state in those phases.
4. **Setup keeps the existing in-cell names.** The new 88×38 plates are for combat / resolve only.
5. **Aisle plates may sit ~2px apart.** Accept the tight gap; no overlap, no extra offset in M0.
6. **`ui.testarena.can_roll` is in the key list** for the optional 「可掷骰」 tag. The ready *look* (gold rim, leading edge, tick) does not depend on the word.

Combat mock notes (preview honesty, not open questions): mid-fight HP max of 80 is illustrative where the screenshot labeled only the current value (裂隙术士 80 looked full; 灰烬卫士 60 and 锈蚀步卒 68 use the same max). The overflowing graybox cards were replaced with official idle sprites so the new plates can be read; one 锈蚀步卒 is shown in the aisle (the original shot stacked two overlapping cards). Shield examples live on `mock_nameplates.png` / the kit sheet — the captured mid-fight units all had 护盾 0.

---

## 9. File list

```
docs/ui/m0-testarena-hud.md
docs/ui/previews/kit_sheet.png
docs/ui/previews/mock_setup.png
docs/ui/previews/mock_combat_ready.png
docs/ui/previews/mock_combat_pending.png
docs/ui/previews/mock_nameplates.png
docs/art/STYLE_GUIDE_UI_PATCH.md
docs/art/USAGE_UI_PATCH.md
Assets/_Project/Art/UI/Hud/manifest.json
Assets/_Project/Art/UI/Hud/ui_btn_primary_{normal,hover,pressed,disabled}.png
Assets/_Project/Art/UI/Hud/ui_btn_secondary_{normal,hover,pressed,disabled}.png
Assets/_Project/Art/UI/Hud/ui_bar_track.png
Assets/_Project/Art/UI/Hud/ui_bar_track_ready.png
Assets/_Project/Art/UI/Hud/ui_bar_track_vital.png
Assets/_Project/Art/UI/Hud/ui_bar_tick.png
Assets/_Project/Art/UI/Hud/ui_bar_tick_ready.png
Assets/_Project/Art/UI/Hud/ui_bar_fill_fate.png
Assets/_Project/Art/UI/Hud/ui_bar_fill_hp.png
Assets/_Project/Art/UI/Hud/ui_bar_fill_shield.png
Assets/_Project/Art/UI/Hud/ui_nameplate_ally.png
Assets/_Project/Art/UI/Hud/ui_nameplate_enemy.png
Assets/_Project/Art/UI/Hud/ui_nameplate_dead.png
Assets/_Project/Art/UI/Hud/ui_dice_slot_empty.png
Assets/_Project/Art/UI/Hud/ui_badge_combo.png
Assets/_Project/Art/UI/Hud/ui_dice_pending_glow.png
Assets/_Project/Art/UI/Hud/ui_ornament_skull.png
Assets/_Project/Art/UI/Hud/ui_ornament_skull_rust.png
Assets/_Project/Art/UI/Hud/ui_ornament_skull_ash.png
Assets/_Project/Art/UI/Hud/ui_ornament_bone.png
Assets/_Project/Art/UI/Hud/ui_ornament_bone_corner.png
Assets/_Project/Art/UI/Hud/ui_ornament_blood.png
Assets/_Project/Art/UI/Hud/ui_bar_endcap_left.png
Assets/_Project/Art/UI/Hud/ui_bar_endcap_right.png
Assets/_Project/Art/UI/Hud/ui_bar_endcap_left_ready.png
Assets/_Project/Art/UI/Hud/ui_bar_endcap_right_ready.png
```

Import: Sprite (2D and UI), Mesh Type Full Rect, Filter Bilinear, compression None or High Quality, mipmaps off, PPU 64, PNG RGBA with real transparency (no baked `#0B1220` void on the slices). Buttons and tracks are Image Type Sliced. Bar fills are Image Type Filled (horizontal, origin left) or Sliced + width. See `manifest.json` for per-file 9-slice borders and pivots.

TestArena uses these slices for the right-rail fate bar, dice slots, combo badge, pending glow, 出征 / 掷骰 / 确认 buttons, and the combat / Resolve nameplates. Setup keeps the in-cell names. Combat numbers and the existing enable / disable / hide rules for those buttons are unchanged. Decorative slices do not take clicks.
