# GDD Slice 1: Login → Intro → First Catch

**Project:** Nia Island / Abyss Hook  
**Engine:** Unity 2D  
**Repo:** https://github.com/DeanZhang23/my-game1  
**Status:** Approved (MVP Slice 1)

## 1. One-line fantasy

Black cat **Nia** (golden pupils), lighthouse guardian, casts into a Lovecraftian abyss and pulls up fish that are also secrets.

## 2. MVP scope

**In:**

1. Login screen
2. First story intro
3. Core fishing: move cat on pier, tap Cast, on bite tap Hook, resolve catch via fish stats vs rod stats

**Out:** Island exploration, puzzles, deep lore systems, inventory UI beyond catch result, multi-rod, shop, save cloud sync beyond local stub.

## 3. Core loop (one session after intro)

1. Player is on the pier with Nia and starter rod equipped.
2. Optional: move left/right on pier (clamped).
3. Tap **Cast** → line goes out; enter **Waiting** state.
4. After a short random delay, show **Bite** cue (visual + brief timing window).
5. Tap **Hook** inside the window → **Resolve**.
6. Compare rod vs fish → Success (show fish card) or Fail (line snap / fish escapes).
7. Return to Ready; player can cast again.

No stamina drain in MVP. Loop must be playable with keyboard/mouse or touch equivalent (single primary button = Cast/Hook).

## 4. One-catch win / fail

**States:** Ready → Casting → Waiting → BiteWindow → Resolving → Result → Ready

| Outcome | Condition |
|---|---|
| **Miss** | No Hook tap (or tap outside BiteWindow) → fish escapes, no stat check |
| **Fail (snap)** | Hook in window BUT `rod.power < fish.resistance` |
| **Success** | Hook in window AND `rod.power >= fish.resistance` |

MVP formula (deterministic, no RNG on resolve once hooked):

```
success = rod.power >= fish.resistance
```

Bite timing only gates whether resolve happens. Keep BiteWindow ~0.8–1.2s so it is fair to learn.

## 5. Minimal stats

### Starter rod — Harbor Line

| Field | Value |
|---|---|
| id | `rod_harbor` |
| power | 3 |
| note | Default equipped; only rod in MVP |

### Fish (2–3)

| id | name | resistance | rarity | catch note |
|---|---|---|---|---|
| `fish_gutter` | Gutter Minnow | 1 | Common | Always catchable with Harbor Line |
| `fish_lantern` | Lantern Smelt | 3 | Uncommon | Equal to rod → borderline success |
| `fish_whisper` | Whisper Eel | 5 | Rare | Always snaps Harbor Line (teaches upgrade need later; still counts as a resolved Fail) |

**Bite spawn weights (MVP):** Gutter 60% / Lantern 30% / Whisper 10%. Weight only picks which fish bites; resolve still uses the table above.

## 6. Flow: Login → Intro → Fishing

1. **Boot** → Login screen (guest / local account stub OK: username field + Start; no real auth required for playability).
2. On Start → **Intro scene** (2–4 short beats, skippable after first line):
   - Lighthouse at night; abyss water below.
   - Nia on pier; golden pupils catch light.
   - One line of copy: she fishes not for food, but for what the deep remembers.
   - CTA: **Cast the first line** → loads Fishing pier.
3. **Fishing pier** → core loop above. First successful catch triggers a 1-line toast ("A secret surfaces.") then back to Ready. No branching story after that in Slice 1.

## 7. UX / feel constraints for Art & Eng

- Primary action: one big **Cast / Hook** button (label swaps by state).
- Bite cue must be unmistakable (pulled line + UI flash); do not rely on audio alone.
- Result panel: fish name + resistance vs rod power (show the numbers so the rule is learnable).
- Fail on Whisper should feel "too strong," not random unfairness.

## 8. Cut aggressively (do not build yet)

Multi-location map, dialogue trees, inventory bag, rod upgrades UI, lore codex, day/night cycle, combat, crafting.

## 9. Engineer tickets (first 1–2, independently testable)

### Ticket E1 — Pier Ready + Cast / Bite / Hook state machine (no fish data yet)

**Goal:** On a blank pier scene, Nia can move L/R in bounds; button drives Ready→Cast→Waiting→BiteWindow→Ready (Miss if no tap). BiteWindow duration tunable in Inspector.

**Done when:** Play Mode: cast, see bite cue, miss by not tapping, return to Ready; tap in window goes to a stub "Resolved" log then Ready. No login/intro required to test.

**Test:** Manual Play Mode checklist; screenshot of each state optional for Verifier.

### Ticket E2 — Catch resolve with Harbor Line + 3 fish table

**Depends on:** E1.

**Goal:** On Hook-in-window, pick fish by weights, compare `rod.power` vs `fish.resistance`, show Success/Fail result UI with both numbers; Gutter always wins, Whisper always fails, Lantern succeeds.

**Done when:** 10 casts produce mix of species; every Gutter = Success, every Whisper = Fail, Lantern = Success; Miss still skips resolve.

**Test:** Play Mode; Verifier confirms three outcomes + Miss path.

*(Login + Intro can be E3 after E1/E2 prove the loop.)*
