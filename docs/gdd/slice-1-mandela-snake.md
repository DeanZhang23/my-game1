# GDD Addendum: Mandela Snake (曼德拉蛇)

**Project:** Nia Island / Abyss Hook  
**Parent:** [Slice 1 MVP](./slice-1-mvp.md)  
**Status:** Design addendum — one fish row only  
**Repo:** https://github.com/DeanZhang23/my-game1

## Scope

Add **one** catchable fish to the Slice 1 table. No new systems, no rod upgrades, no new UI flows. Art produces sprites in parallel; Engineer wires the row when ready (same resolve rules as Slice 1).

## Fish row

| Field | Value |
|---|---|
| id | `fish_mandela` |
| display name (EN) | Mandela Snake |
| display name (ZH) | 曼德拉蛇 |
| resistance | **2** |
| rarity | Uncommon |
| bite weight (suggestion) | **20%** |
| catch note | A long body that remembers a pier that never stood here — Nia’s golden eyes catch a name she never learned. |

### Resolve vs Harbor Line (`rod_harbor`, power 3)

`success = rod.power >= fish.resistance` → **always Success** when Hook lands in BiteWindow (2 ≤ 3). Miss still skips resolve. Fail (snap) does not apply for this fish with the starter rod.

## Updated bite spawn weights (suggestion)

Rebalance Slice 1 weights so the pool still sums to 100%:

| id | name | resistance | previous weight | suggested weight |
|---|---|---|---|---|
| `fish_gutter` | Gutter Minnow | 1 | 60% | **45%** |
| `fish_mandela` | Mandela Snake | 2 | — | **20%** |
| `fish_lantern` | Lantern Smelt | 3 | 30% | **25%** |
| `fish_whisper` | Whisper Eel | 5 | 10% | **10%** |

Weights only pick which fish bites; resolve still uses resistance vs rod power.

## Out of scope

Rod upgrades, Mandela-specific mechanics, lore codex entries, new catch animations beyond existing Success/Fail result panel.
