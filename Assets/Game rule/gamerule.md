---
name: cucumber-clicker-unity
description: Build "Cucumber Clicker", an idle/clicker mobile game in Unity C# where players tap a cucumber, sell cucumbers at the market, pickle them in real-time jars for much higher value, and buy upgrades, new recipes and (very expensive) auto-producers. Use when creating, extending, balancing or debugging this game.
---

# Cucumber Clicker — Unity C# Mobile Game

You are building a portrait-mode mobile idle game in Unity (C#). The core idea is Cookie Clicker, but with a second layer: **pickling**. Raw cucumbers are cheap; pickled cucumbers are worth far more but take real-world time.

Read this whole file before writing code. Follow the build phases in order and check off the acceptance list at the end of each phase.

---

## 1. Core loop

```
Tap cucumber ──► Cucumbers ──┬──► Sell raw at Market ──────────────► Coins
                             └──► Put in Jar (recipe, real time) ──► Pickles ──► Sell ──► Coins (×10 … ×1500)
Coins ──► Upgrades (tap power, jars, recipes, producers) ──► more cucumbers / better pickles
```

- **Tap**: tapping the big cucumber in the center of the Farm screen gives `+tapPower` cucumbers (starts at 1).
- **Sell raw**: 1 cucumber = 1 coin (base).
- **Pickle**: choose a recipe, load cucumbers into an empty jar, wait real time, collect pickles, sell them.
  - Reference example from the designer: 5 cucumbers raw = 5 coins; 5 cucumbers in Quick Brine for 3 minutes = 50 coins.
- **Upgrade**: spend coins on tap power, more/bigger jars, faster/better recipes, new recipes, and auto-producers.
- **Unlock**: new recipes, upgrades and producers appear as the player progresses (based on lifetime stats).

Design intent: short recipes give the best coins-per-minute for active players; long recipes are for players who close the game and come back. Auto-producers are a **late-game reward** and must feel expensive.

---

## 2. Technical requirements

- **Engine**: Unity 6 LTS (or the version already in the project). C#, uGUI + TextMeshPro.
- **Target**: Android and iOS, portrait only. Canvas Scaler: Scale With Screen Size, reference 1080×1920, match 0.5. Respect the device safe area (notches) with a `SafeArea` component on the root panel.
- **Language**: all in-game text is **English**. Keep every player-facing string in one place (`Strings.cs` or a localization table) so it can be translated later.
- **Numbers**: store currency and counts as `double`. Display with a shared formatter: `999`, `1.23K`, `45.6M`, `7.89B`, `1.00T`, then `aa`, `ab`, ….
- **Time**: all timed logic uses `DateTime.UtcNow` stored as `long` ticks. Never use `Time.time` or frame counts for pickling, because pickling continues while the game is closed.
- **Data-driven**: recipes, upgrades and producers are `ScriptableObject` assets. No balance numbers hard-coded in gameplay scripts.
- **No external servers.** Everything is local.

### Project structure

```
Assets/
  _Game/
    Scripts/
      Core/        GameManager.cs, GameState.cs, TimeService.cs, NumberFormatter.cs
      Economy/     Wallet.cs, Market.cs, CostFormula.cs
      Pickling/    PicklingManager.cs, JarState.cs, RecipeData.cs
      Upgrades/    UpgradeManager.cs, UpgradeData.cs, UnlockCondition.cs
      Producers/   ProducerManager.cs, ProducerData.cs
      Save/        SaveSystem.cs, SaveData.cs
      UI/          FarmScreen.cs, PickleScreen.cs, MarketScreen.cs, UpgradeScreen.cs,
                   TabBar.cs, FloatingText.cs, JarView.cs, ShopItemView.cs, SafeArea.cs
      Debug/       DebugPanel.cs
    Data/          Recipes/*.asset, Upgrades/*.asset, Producers/*.asset
    Prefabs/  Scenes/Main.unity  Art/  Audio/
  Tests/EditMode/  FormulaTests.cs, PicklingTests.cs, SaveTests.cs
```

Keep game logic in plain C# classes that do not depend on `MonoBehaviour` where possible, so it can be unit-tested. UI scripts only read state and call methods; they never change numbers directly.

---

## 3. Game systems

### 3.1 Tapping
- Big cucumber button centered on the Farm screen.
- Each tap: `cucumbers += tapPower`, spawn floating `+N` text at the touch point, squash-and-stretch the cucumber (scale 1 → 0.9 → 1.05 → 1 over ~0.15 s), small particle burst, light haptic on supported devices.
- Support multi-touch (each finger counts) but cap at 20 taps/second to stop auto-clicker abuse.

### 3.2 Market
- Sell raw cucumbers: buttons **Sell 1 / Sell 10 / Sell All**.
- Sell pickles: one row per pickle type the player owns, showing count and value each, with **Sell All**.
- Show the coins earned as a floating number and update the wallet instantly.

### 3.3 Pickling (jars)
- The player owns **jars** (start with 1, capacity 5 cucumbers).
- To start a batch: tap an empty jar → choose a recipe (only unlocked ones) → choose amount (slider, 1 … min(capacity, cucumbers owned)) → **Start**. Cucumbers are removed immediately.
- A running jar shows recipe name, amount, progress bar and remaining time (`2m 13s`, `3h 05m`).
- When done, the jar shows **Ready!**. Tapping it collects the pickles into the pickle inventory.
- **Snapshot rule**: when a batch starts, store `startUtcTicks`, `durationSeconds` and `valuePerPickle`. Upgrades bought later do not change a batch that is already running. This keeps the math simple and predictable.
- **Real-time rule**: a jar is finished when `TimeService.UtcNow >= start + duration`, whether or not the game was open.
- Pickle inventory stores, per recipe, `count` and `totalValue`; selling pays `totalValue` and resets both. (This handles batches made at different quality levels.)

### 3.4 Recipes (pickling methods)

Value = coins per cucumber put in. Unlock = lifetime coins earned that makes the recipe visible and buyable; Cost = coins to unlock it.

| id | Name | Time | Value / cucumber | Visible at lifetime coins | Unlock cost |
|---|---|---|---|---|---|
| quick_brine | Quick Brine | 3 min | 10 | start (owned) | 0 |
| garlic_dill | Garlic Dill | 10 min | 25 | 1,000 | 500 |
| bread_butter | Bread & Butter | 30 min | 60 | 10,000 | 5,000 |
| spicy_chili | Spicy Chili | 1 h | 100 | 50,000 | 25,000 |
| kosher_sour | Kosher Sour | 4 h | 300 | 500,000 | 200,000 |
| cucumber_kimchi | Cucumber Kimchi | 8 h | 550 | 2,500,000 | 1,000,000 |
| golden_royal | Golden Royal Pickle | 24 h | 1,500 | 25,000,000 | 10,000,000 |

`RecipeData` fields: `id, displayName, description, icon, baseDurationSeconds, baseValuePerCucumber, unlockCondition, unlockCost, jarTint (Color)`.

### 3.5 Upgrades

All repeatable upgrades use `cost(level) = baseCost × growth^level` (level = times already bought).

**Tap upgrades**

| id | Name | Effect per level | Base cost | Growth | Max |
|---|---|---|---|---|---|
| tap_power | Stronger Hands | +1 cucumber per tap | 15 | 1.5 | 100 |
| tap_multiplier | Golden Gloves | ×2 tap power (multiplies) | 5,000 | 10 | 10 — visible at 2,500 lifetime coins |

`tapPower = (1 + tap_power level) × 2^(tap_multiplier level)`

**Jar upgrades**

| id | Name | Effect | Base cost | Growth | Max |
|---|---|---|---|---|---|
| jar_count | Extra Jar | +1 jar | 100 | 4 | 7 (8 jars total) |
| jar_capacity | Bigger Jars | +5 capacity for every jar | 50 | 1.8 | 40 |

**Per-recipe upgrades** (one set for each unlocked recipe; base cost = `max(50, recipe.unlockCost × 0.5)`):

| Name | Effect per level | Growth | Max |
|---|---|---|---|
| Better Ingredients | +10% value | 1.6 | 25 |
| Faster Fermentation | −5% duration | 2.0 | 10 (−50% cap) |

`duration = baseDuration × (1 − 0.05 × speedLevel)`
`valuePerCucumber = baseValue × (1 + 0.10 × qualityLevel)`

**Market upgrade**

| id | Name | Effect | Base cost | Growth | Max |
|---|---|---|---|---|---|
| market_haggle | Haggling | +5% sell price on everything | 2,000 | 2.2 | 20 — visible at 1,000 lifetime coins |

### 3.6 Auto-producers (late game, deliberately expensive)

Producers generate cucumbers per second. They must feel like a long-term goal: the first one should take a dedicated player several hours of play to afford. Hidden entirely until the visibility threshold; then shown as a locked card with a progress bar toward its cost.

| id | Name | Cucumbers / sec each | Base cost | Visible at lifetime coins |
|---|---|---|---|---|
| farmhand | Farmhand | 0.5 | 50,000 | 100,000 |
| garden_plot | Garden Plot | 3 | 400,000 | 750,000 |
| greenhouse | Greenhouse | 20 | 3,000,000 | 5,000,000 |
| hydro_farm | Hydroponic Farm | 120 | 25,000,000 | 40,000,000 |
| cucumber_factory | Cucumber Factory | 800 | 250,000,000 | 400,000,000 |

- Cost growth: `baseCost × 1.18^owned`.
- Production is added every frame as a fraction (`cucumbers += rate × deltaTime`); display rounds down.
- **Offline production**: when the game reopens, grant `rate × elapsedSeconds × offlineEfficiency`, capped at `offlineCapHours`. Defaults: `offlineEfficiency = 0.5`, `offlineCapHours = 8`. Put these in a `GameConfig` ScriptableObject. Show a "Welcome back" popup with the amount.

### 3.7 Unlock system
- `UnlockCondition` supports: `LifetimeCoinsAtLeast`, `TotalTapsAtLeast`, `RecipeUnlocked(id)`, `PicklesSoldAtLeast`.
- Visibility states for every shop item: **Hidden** → **Teaser** (shown as "???" with silhouette when the player reaches 50% of the condition) → **Available** → **Owned / Maxed**.
- When something becomes Available, show a toast ("New recipe: Garlic Dill!") and a red dot on its tab.

### 3.8 Lifetime stats (needed for unlocks and a Stats panel)
`lifetimeCoins, lifetimeCucumbers, totalTaps, picklesMade, picklesSold, firstPlayUtc, totalPlayTimeSeconds`.

---

## 4. Time and offline handling

- `TimeService.UtcNow` wraps `DateTime.UtcNow` so tests and the debug panel can fake or skip time.
- Save `lastSeenUtcTicks` on every save.
- On load and on `OnApplicationPause(false)` (returning to the app): `elapsed = now − lastSeen`.
  - If `elapsed < 0` (device clock moved backwards): treat elapsed as 0, do not advance anything, and keep the stored timestamps. Log a warning.
  - Jars need no special handling — they compare against their own start time. Clamp displayed remaining time at 0.
  - Apply offline producer income (section 3.6).
- **Notifications (optional, Phase 5)**: use `com.unity.mobile.notifications` to schedule a local notification "Your Garlic Dill pickles are ready!" when a batch starts; cancel it if collected early or the jar is cleared. Ask for permission the first time the player starts a batch of 30 minutes or longer, not at launch.

---

## 5. Save system

- JSON file at `Application.persistentDataPath/save.json`. Use Newtonsoft JSON (`com.unity.nuget.newtonsoft-json`) because `JsonUtility` cannot serialize dictionaries; or use serializable lists if you prefer `JsonUtility`.
- `SaveData` contains `saveVersion`, wallet, inventory, jars, upgrade levels by id, unlocked recipe ids, producer counts, lifetime stats, `lastSeenUtcTicks`, settings (sound, music, haptics).
- Save on: every 30 s, `OnApplicationPause(true)`, `OnApplicationQuit`, and right after any purchase. On mobile, `OnApplicationPause(true)` is the reliable one — never rely on quit alone.
- Write to `save.tmp`, then replace `save.json` (avoid corruption if the app is killed mid-write). Keep `save.bak` of the previous good save and fall back to it if parsing fails.
- Migrations: when `saveVersion` is older, run upgrade steps; unknown ids in a save are ignored, missing ids default to 0.
- Settings screen has **Reset Progress** with a two-step confirmation.

---

## 6. UI and screens

Portrait, bottom tab bar with 4 tabs (5 once producers are visible):

1. **Farm** — top bar (coins, cucumbers, cucumbers/sec if any producers), big tappable cucumber in the center, tap power shown under it.
2. **Pickle** — grid of jars (2 columns). Empty / Running (progress + timer + recipe tint) / Ready (bouncing, glowing). Locked jar slots show the Extra Jar price.
3. **Market** — sell raw and sell pickles (section 3.2).
4. **Upgrades** — sections: Tapping, Jars, Recipes (unlock new + per-recipe upgrades), Market.
5. **Workers** — producers (appears only when the first one becomes Teaser).

Rules:
- Coin and cucumber counters are always visible at the top on every tab.
- Buy buttons are green when affordable, grey when not; show cost with the number formatter. Long-press a buy button to buy repeatedly.
- Every tap target is at least 120 px tall at the 1080×1920 reference.
- Visual style: bright, friendly, cartoon; greens for cucumbers, warm yellows for coins, each recipe its own jar liquid color.
- Use placeholder art (simple shapes / free assets) if no art is supplied, but keep sprites swappable through prefabs.

---

## 7. Balancing and validation

- Put a `BalanceSimulator` editor window or EditMode test that simulates an "average active player" (5 taps/sec, buys the cheapest affordable upgrade, always keeps jars full with the best recipe by coins-per-jar-minute) and prints the minutes to reach each unlock.
- Targets (tune numbers in the ScriptableObjects, not in code, until these hold):
  - First upgrade within 30 s.
  - Garlic Dill unlocked within 10–15 min.
  - First Farmhand affordable after roughly 2–4 hours of active play.
  - Golden Royal Pickle after several days of mixed active/idle play.
- Unit tests (EditMode) must cover: cost formula, tapPower formula, recipe duration/value with upgrades, jar completion across a fake time jump, clock-rollback guard, offline income cap, save → load round-trip, loading an old-version save.

---

## 8. Debug panel (development builds only)

Hidden behind a 5-tap on the version label, compiled out with `#if DEVELOPMENT_BUILD || UNITY_EDITOR`:
add coins/cucumbers, skip time (+1 min, +1 h, +1 day via `TimeService` offset), unlock everything, reset save, show raw save JSON.

---

## 9. Build phases

Work in this order. Finish and test each phase before the next.

1. **Core tap + market**: tapping, wallet, sell raw, number formatter, top bar, Farm and Market screens.
2. **Pickling**: jars, Quick Brine, start/progress/collect, pickle inventory, selling pickles, real-time completion.
3. **Save + offline**: save system, `TimeService`, pause/resume handling, clock-rollback guard, debug time skip.
4. **Upgrades + unlocks**: all upgrades, all recipes, unlock states, toasts, red dots, Upgrades screen.
5. **Producers + polish**: producers, offline income popup, notifications, juice (particles, haptics, sounds), settings, balance simulator.
6. **Mobile build**: safe area, Android/iOS player settings, test on a real device or device simulator, frame rate set to 60 (`Application.targetFrameRate = 60`).

## 10. Acceptance checklist

- [ ] Tapping the cucumber adds exactly `tapPower` cucumbers and shows feedback.
- [ ] 5 cucumbers sold raw = 5 coins; 5 cucumbers in Quick Brine for 3 min then sold = 50 coins (no upgrades).
- [ ] A jar started, app closed for longer than the recipe time, and reopened shows Ready.
- [ ] Moving the device clock backwards does not finish jars early or grant income.
- [ ] Upgrades bought mid-batch do not change that batch.
- [ ] Every recipe, upgrade and producer goes Hidden → Teaser → Available correctly.
- [ ] First Farmhand is not affordable early (simulator confirms the 2–4 h target).
- [ ] Killing the app at any moment never loses more than 30 s of progress and never corrupts the save.
- [ ] All text is English and comes from the central strings file.
- [ ] Layout works on a tall phone with a notch and on a tablet.
- [ ] All EditMode tests pass.
