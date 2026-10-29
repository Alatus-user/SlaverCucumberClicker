# Cucumber Clicker — Main Screen UI Layout

This file defines the main game screen for the AI agent building **Cucumber Clicker** (Unity C#, portrait mobile). It replaces the bottom tab bar described in `SKILL.md` section 6. Everything else in `SKILL.md` still applies.

The game has **one main screen**. There are no separate scenes for the shop or pickling: they live inside a **drawer tray** that the player pulls up from the bottom.

---

## 1. Screen zones

Reference resolution 1080 × 1920, portrait. Heights below are at the reference size; the safe area is added on top of them.

```
┌──────────────────────────────────┐
│  [safe area inset]               │
│ ┌──────────────────────────────┐ │
│ │ 🪙 12.5K        🥒 340    ⚙ │ │  ZONE A — Top Bar (fixed, 160 px)
│ │                  +3.5/s      │ │
│ └──────────────────────────────┘ │
│                                  │
│                                  │
│            ╭────────╮            │
│           ╱          ╲           │
│          │   BIG      │          │  ZONE B — Cucumber Area
│          │ CUCUMBER   │          │  (fills everything between
│          │  (tap me)  │          │   top bar and drawer)
│           ╲          ╱           │
│            ╰────────╯            │
│           +2 per tap             │
│                                  │
│ ┌──────────────────────────────┐ │
│ │            ━━━━              │ │  ZONE C — Drawer Tray
│ │ [Pickle•] [Market] [Upgrades]│ │  (collapsed = 220 px peek)
│ └──────────────────────────────┘ │
│  [safe area inset]               │
└──────────────────────────────────┘
```

Draw order (back to front): Background → Cucumber Area → Drawer Tray → Top Bar → popups/toasts.
The Top Bar is always on top and always visible, even when the drawer is fully open.

---

## 2. Zone A — Top Bar

Fixed at the top, below the safe area inset, 160 px tall, full width with 40 px side padding.

| Element | Position | Content |
|---|---|---|
| Coins | left | coin icon + coins (formatted with `NumberFormatter`, e.g. `12.5K`) |
| Cucumbers | center | cucumber icon + cucumber count |
| Production rate | under cucumbers, small text | `+3.5/s` — **hidden until the player owns at least 1 producer** |
| Settings | right | gear icon button (opens settings popup) |

Behavior:
- Values update every frame from game state, but the displayed number **counts up smoothly** toward the real value over ~0.25 s instead of jumping.
- When a value increases, the icon does a quick pulse (scale 1 → 1.2 → 1, 0.15 s).
- When the player tries to buy something they can't afford, the coin counter shakes horizontally and flashes red for 0.3 s.
- Use a semi-transparent background panel so the counters stay readable over any background.
- Numbers use a font with tabular (fixed-width) digits so the layout doesn't jitter while counting.

---

## 3. Zone B — Cucumber Area

Everything between the bottom of the Top Bar and the top of the Drawer.

- One **big cucumber** centered in this area. Its size is about **65% of the area's width**, but it must also fit the area's height (use the smaller of the two).
- Under the cucumber: small label `+N per tap` (current tap power).
- The whole cucumber sprite is the tap target (use a `Button`/`IPointerDownHandler` with the sprite's shape; `Image.alphaHitTestMinimumThreshold = 0.1` so transparent corners don't count).
- Tap feedback is the one in `SKILL.md` 3.1: squash-and-stretch, floating `+N` text at the finger position, particle burst, light haptic.
- Gentle idle animation when not tapped for 3 s: slow breathing scale (1.0 ↔ 1.03, 2 s loop).

**Reacting to the drawer:**
- When the drawer moves up, the Cucumber Area shrinks. The cucumber **scales down and stays centered** in the remaining space (recompute every frame while dragging so it moves smoothly with the finger).
- In the **Half** state the cucumber is smaller but **still tappable** — the player can keep tapping while browsing the shop.
- In the **Expanded** state the cucumber is covered; disable its input.

---

## 4. Zone C — Drawer Tray

A panel anchored to the bottom of the screen that slides up and down. It holds all menus.

### 4.1 Snap states

| State | Visible height | What the player sees | Cucumber |
|---|---|---|---|
| **Collapsed** (default) | 220 px + bottom safe area | grab handle + tab row only | full size |
| **Half** | 50% of screen height | tabs + top part of the selected menu | smaller, tappable |
| **Expanded** | screen height − Top Bar − safe areas − 20 px gap | full menu | covered, not tappable |

The game starts in **Collapsed**.

### 4.2 Drawer header (always visible)

```
┌────────────────────────────────────────┐
│                 ━━━━━                  │  grab handle: 120×12 px pill, 30 px from top
│  [🫙 Pickle•2] [🏪 Market] [⬆ Upgrades] [👨‍🌾 Workers] │  tab row, 140 px tall
└────────────────────────────────────────┘
```

- Tabs, in order: **Pickle**, **Market**, **Upgrades**, **Workers**. The Workers tab is **not shown at all** until the first producer reaches the Teaser state (see `SKILL.md` 3.7). When it appears, play a slide-in animation and show a toast.
- Each tab: icon + short label. Selected tab is highlighted (filled background, bold label).
- **Badges** on tabs:
  - Pickle: green number badge = how many jars are **Ready** to collect.
  - Upgrades / Workers / Pickle: red dot when something new became Available and the player hasn't opened that tab since.
  - Market: number badge with how many **pickle types** are waiting to be sold (only when > 0).
- **Tapping a tab:**
  - If the drawer is Collapsed → select that tab and open to **Half**.
  - If the drawer is open and the tab is already selected → collapse the drawer.
  - If the drawer is open and a different tab is tapped → switch tabs, keep the current height.

### 4.3 Drawer content (by tab)

Each tab's content is a vertical `ScrollRect` below the header. Content details come from `SKILL.md`:

- **Pickle** — jar grid (2 columns) as in `SKILL.md` 3.3. Tapping an empty jar opens the recipe picker as a popup above the drawer (not a new screen). Put a **Collect All** button at the top when 2+ jars are Ready.
- **Market** — Sell 1 / Sell 10 / Sell All raw cucumbers, then one row per pickle type with **Sell All**. Put a big **Sell Everything** button at the top.
- **Upgrades** — sections: Tapping, Jars, Recipes, Market (`SKILL.md` 3.5).
- **Workers** — producer cards (`SKILL.md` 3.6).

Remember the scroll position of each tab while the session is running.

### 4.4 Drag behavior

Implement in a `DrawerController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler`.

- **Drag area**: the drawer header (handle + tab row). Dragging inside the scrolling content scrolls the content, not the drawer — **except**: if the content is already scrolled to the very top and the player drags **down**, hand the drag to the drawer so it closes (nested-scroll handoff).
- While dragging, the drawer follows the finger 1:1, clamped between Collapsed and Expanded. Dragging past the limits gives a little rubber-band resistance (move 30% of the extra distance).
- On release:
  - If finger speed > **1500 px/s** (at reference resolution): go to the next state in the fling direction (up: Collapsed → Half → Expanded; down: the reverse). A very fast fling (> 4000 px/s) can skip Half.
  - Otherwise: snap to the **nearest** state.
- Snap animation: ease-out (e.g. `Mathf.SmoothDamp` or a cubic ease-out), ~0.25 s.
- A **dim overlay** covers the Cucumber Area, fading from 0% (Collapsed) to 40% black (Expanded), proportional to drawer height. Tapping the overlay when Expanded collapses the drawer to Half. In Half the overlay stays at 0% so the cucumber remains clear and tappable.
- **Android back button**: Expanded → Half → Collapsed; when already Collapsed, show the normal "quit?" confirmation.
- Respect `Screen.safeArea`: the drawer's background extends under the bottom inset (home indicator), but its buttons never sit inside it.

### 4.5 Drawer visuals
- Rounded top corners (radius 48 px), soft shadow above it, solid background (not transparent) so text is readable.
- Haptic tick when the drawer snaps to a state.
- When a jar becomes Ready while the drawer is Collapsed, the Pickle tab badge pops in with a bounce, and the handle briefly glows — this is the main way players notice finished pickles without opening the menu.

---

## 5. Unity hierarchy

```
Canvas (Screen Space - Overlay, CanvasScaler 1080x1920, match 0.5)
└── SafeAreaRoot                     (SafeArea.cs)
    ├── Background
    ├── CucumberArea                 (CucumberAreaLayout.cs — resizes to space above drawer)
    │   ├── BigCucumber              (CucumberButton.cs)
    │   ├── TapPowerLabel
    │   └── FloatingTextLayer
    ├── DimOverlay                   (Image, raycast only when Expanded)
    ├── Drawer                       (DrawerController.cs, anchored bottom-stretch)
    │   ├── Header
    │   │   ├── GrabHandle
    │   │   └── TabRow               (DrawerTabs.cs)
    │   │       ├── Tab_Pickle  (+ Badge)
    │   │       ├── Tab_Market  (+ Badge)
    │   │       ├── Tab_Upgrades (+ RedDot)
    │   │       └── Tab_Workers (+ RedDot, inactive until unlocked)
    │   └── Content
    │       ├── PicklePanel     (ScrollRect)
    │       ├── MarketPanel     (ScrollRect)
    │       ├── UpgradesPanel   (ScrollRect)
    │       └── WorkersPanel    (ScrollRect)
    ├── TopBar                       (TopBar.cs — last child so it renders on top)
    └── PopupLayer                   (recipe picker, welcome back, settings, toasts)
```

- `DrawerController` exposes `State` (Collapsed/Half/Expanded), `NormalizedOpen` (0–1) and an event `OnHeightChanged(float visibleHeight)`. `CucumberAreaLayout` and `DimOverlay` listen to that event — they never read the drawer's RectTransform directly.
- Only the active tab's panel is enabled; others are `SetActive(false)` to save draw calls.
- Keep UI scripts free of game math: they read from game state and call manager methods (same rule as `SKILL.md`).

---

## 6. Acceptance checklist

- [ ] Coins and cucumbers are visible at the top at all times, in every drawer state, on notched phones.
- [ ] The big cucumber is centered and tappable when the drawer is Collapsed and Half; not tappable when Expanded.
- [ ] The cucumber shrinks and recenters smoothly while the drawer is being dragged.
- [ ] Drawer can be opened by dragging the handle, dragging the tab row, flinging, or tapping a tab.
- [ ] Release snaps to the nearest state; fast fling goes to the next state.
- [ ] Dragging down at the top of a scrolled list closes the drawer; dragging inside a list otherwise scrolls the list.
- [ ] Tapping the selected tab again collapses the drawer.
- [ ] Workers tab stays hidden until producers are unlocked.
- [ ] Ready-jar badge on the Pickle tab is visible while Collapsed.
- [ ] Android back button closes the drawer step by step.
- [ ] Layout works at 9:16, 9:19.5 (tall phones) and 3:4 (tablets).
