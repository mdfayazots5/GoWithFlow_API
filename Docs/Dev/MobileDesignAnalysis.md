# GoWithFlow — Mobile Design Analysis & Standards
**Author:** Principal Mobile UX Designer / Mobile Architect / Senior Full-Stack Developer  
**Date:** 2026-06-04  
**Status:** DRAFT — Pending Approval Before Implementation  
**Target Platform:** Android (Capacitor 8.4.0 wrapping Angular 19, min SDK 24, target SDK 36)

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current Architecture Overview](#current-architecture-overview)
3. [Screen-by-Screen Audit](#screen-by-screen-audit)
4. [Application-Wide Issues](#application-wide-issues)
5. [Mobile Design Standards](#mobile-design-standards)
   - Typography Scale
   - Spacing System
   - Touch Target Standards
   - Breakpoints & Safe Areas
   - Color Token System
6. [Component Standards](#component-standards)
   - Forms & Inputs
   - Cards
   - Navigation
   - Tables → Card Lists
   - Dialogs & Modals
   - Buttons
   - Badges & Labels
7. [Screen-Specific Standards](#screen-specific-standards)
8. [Hardcoded Values to Replace](#hardcoded-values-to-replace)
9. [Implementation Priority](#implementation-priority)
10. [Reusable Design System Tokens](#reusable-design-system-tokens)

---

## Executive Summary

GoWithFlow is a mobile-first Android application. The codebase has a solid design foundation — CSS custom properties, SCSS variables, responsive mixins, and a Tailwind utility layer — but inconsistent application of these tools has created a fragmented experience. Key problems are:

- **Microscopic text**: Labels at `7px–10px` are unreadable on standard Android DPI at 360px width.
- **Sub-standard touch targets**: Dozens of interactive elements under the 44px minimum.
- **Missing safe-area awareness** in user-facing pages (bottom nav overlap on notched/punched screens).
- **Hardcoded inline styles** (hex colors, pixel sizes) scattered throughout 40 of 51 components, bypassing the token system.
- **No mobile-specific card-list alternative** to HTML tables in admin screens.
- **Overflow risk** in lobby hero card (`text-3xl` with `tracking-[0.3em]` on 320–360px screens).

The design system itself (_variables.scss, _mixins.scss, styles.scss) is well-structured. The fix strategy is to enforce it consistently, not rebuild it.

---

## Current Architecture Overview

### Styling Stack
| Layer | File(s) | Usage |
|---|---|---|
| CSS Custom Properties | `styles.scss :root` | Color tokens (--gwf-*) |
| SCSS Variables | `_variables.scss` | Breakpoints, shadows, radii, spacing |
| SCSS Mixins | `_mixins.scss` | respond-*, gwf-card, gwf-button-primary |
| Global utility classes | `styles.scss` | .gwf-card, .stats-grid, .badge-*, .gwf-page-* |
| Tailwind CSS | All `.component.ts` templates | Layout, spacing, color via gw-* aliases |
| Component SCSS | 13 of 51 components | Shell layout, complex animations, SCSS-only patterns |
| Inline `style=` | ~30 components | **PROBLEM**: hardcoded colors, sizes, overrides |

### Component Inventory
- **51 total components** across user, session, live-session, admin, repractice, auth, shared, voice modules.
- **13 have dedicated `.component.scss`** — the rest rely entirely on Tailwind classes or inline styles.
- **40 components with no SCSS file** means mobile overrides require Tailwind responsive prefixes exclusively.

### Breakpoints (existing)
```scss
$breakpoint-mobile:   480px  // max-width — primary mobile target
$breakpoint-phablet:  640px  // max-width — larger phones / small tablets
$breakpoint-tablet:   768px  // max-width
$breakpoint-tabletL:  1024px // max-width
$breakpoint-desktop:  1025px // min-width
$breakpoint-wide:     1440px // min-width
```
**Gap identified:** No breakpoint for `360px` (smallest common Android screen). Current mobile breakpoint at `480px` is correct but the 360px ultra-small case is unhandled.

---

## Screen-by-Screen Audit

### 1. Login Screen (`login.component.scss`)
**Status: GOOD — mostly correct**
- Uses `clamp()` throughout for brand, card padding, inputs.
- `min(100%, 400px)` wrapper — correct mobile-first constraint.
- Landscape handled with `@media (max-height: 760px)`.
- Input height `50px` — above 44px minimum. ✓
- Login button `52px` height. ✓

**Issues:**
- `field-label` font-size is hardcoded `11px` — should be `clamp(10px, 2.5vw, 11px)`.
- `field-error` hardcoded at `12px` — fine but should use a token.
- Color values (#3D5A99, #2D4580, #D32F2F) are hardcoded — should use `var(--gwf-*)`.

---

### 2. User Dashboard (`user-dashboard.component.ts`)
**Status: GOOD STRUCTURE — text size issues**
- Mobile-first `max-w-lg mx-auto px-4 pt-2 pb-28` wrapper — correct approach.
- `space-y-4` card rhythm — good.
- Quick actions grid `grid-cols-3 gap-2.5` — reasonable on 360px+.
- Recent sessions card — clean row layout, `truncate` on text — good.

**Issues:**
- `text-[8px]` used for weekly report stat labels ("Sessions", "Practice", "Errors", "Resolved") — **8px is unreadable** on standard Android DPI. These labels need to be at least `11px`.
- `text-[9px]` used in 12+ places — borderline unreadable. Minimum for secondary labels should be `11px`.
- `text-[7px]` for "Ready" / "Waiting" labels (from lobby) — **7px is invisible**. Minimum 11px.
- `pb-28` (112px) is hardcoded — does NOT account for `env(safe-area-inset-bottom)`. On Android phones with gesture nav bar, the actual safe area adds to this. Admin layout handles this correctly with `calc(84px + env(safe-area-inset-bottom, 0px))`. User screens need the same treatment.
- Invitation banner "Tap to view" text: `text-[10px]` italic — borderline, should be 11px.
- Mistake cards: `w-7 h-7` (28px) "Practice" button on mobile — **below 44px touch target**. Should be minimum 44px tap target.

---

### 3. Create Session (`create-session.component.ts`)
**Status: GOOD — minor issues**
- Mobile-first `max-w-lg mx-auto px-4` wrapper — correct.
- Input height `h-12` (48px) — above minimum. ✓
- Submit button `h-14` (56px) — good. ✓
- Script search dropdown `max-h-56 overflow-y-auto` — correct pattern.

**Issues:**
- Select elements use `h-10` (40px) — **below 44px minimum touch target**. Should be `h-12`.
- `grid-cols-2 gap-3` for Duration + Expiry selects — at 360px width this gives ~165px per column after gap. Select text "Expires in 1 hr" at `text-[13px]` may be truncated in native select element.
- "Remove" button is inline text with no tap target area — too small.
- Script search: clicking outside to close dropdown via `document.addEventListener` may conflict with Android back gesture.

---

### 4. Session Lobby (`lobby.component.html`)
**Status: MODERATE — overflow and size issues**
- `max-w-lg mx-auto` wrapper — correct.
- Member slot cards with `rounded-[24px]` — good visual design.
- Action buttons `h-14` — good. ✓

**Issues (CRITICAL):**
- Session hero card uses `text-3xl font-black` for session name — at `3xl = 30px` with `font-black`, a long session name like "Weekend Interview Preparation Session" will overflow on a 360px screen even with the `max-w-lg` constraint. Should use `clamp(20px, 5vw, 30px)` or `text-2xl` max.
- Join code uses `text-3xl tracking-[0.3em] font-mono` — 6-character code with 0.3em spacing = roughly 240px wide. This may overflow on 320px screens. Should be `clamp(22px, 6vw, 30px)`.
- `p-7` (28px) hero card padding — leaves only 284px content width at 360px screen (360 - 8px scrollbar - 28 - 28 = 296px minus border). Combined with `text-3xl` name, this is a critical overflow risk.
- Member name: `text-[15px] font-black` with `truncate` — fine but the slot row flex layout at 360px has: avatar(~48px) + gap(16px) + name + gap(16px) + ready-icon(~48px) = only ~232px for name. Truncation works but test with long names.
- `text-[7px]` for ready/waiting sub-labels — **7px is invisible on any screen**. Must be minimum 11px.
- `text-[8px]` for "Members" label — unreadable. Minimum 11px.
- `text-[9px]` "Room Code" label — unreadable. Minimum 11px.
- Leave button `h-14 px-5` — good size but `hidden sm:inline` hides "Leave" text on mobile, leaving only icon. Icon-only is acceptable but must have `aria-label`.
- `pb-10` at bottom — no safe area accounting.

---

### 5. Live Session Room (`session-room.component.ts`)
**Status: EXCELLENT — well-designed for mobile focus mode**
- `h-screen flex flex-col` — correct full-screen approach.
- Fixed topbar at `52px` — good.
- `max-w-[480px] mx-auto` content constraint — correct.
- `padding-bottom: max(24px, env(safe-area-inset-bottom, 24px))` — correct safe area handling. ✓
- `style="max-width: 260px; width: calc(100vw - 2rem)"` for presence toasts — responsive. ✓
- Settings panel capped at `min(40vh, 260px)` — correct overflow prevention. ✓

**Issues (MINOR):**
- Session name/context label `hidden sm:block` — session info is hidden on phones < 640px. On mobile, users can't see which session they're in from the topbar. Should show at least the context tag.
- Leave button `w-9 h-9` (36px) — below 44px. Critical path action. Should be `w-11 h-11`.
- Settings button `w-9 h-9` (36px) — same issue.
- `text-[10px]` and `text-[11px]` settings labels — borderline, should be 12px minimum in settings panel.

---

### 6. Speaker Screen (`speaker-screen.component.html`)
**Status: GOOD — dynamic font sizing is correct**
- `questionFontSize` computed property uses character-length-based `clamp()` — excellent approach. ✓
- Turn progress bar with `flex gap-1 h-1` — good visual indicator.
- Action buttons `h-14` (56px) — good. ✓
- Skip button `h-12` (48px) — good. ✓

**Issues:**
- Turn label `text-[10px]` — borderline. Should be 11px minimum.
- Turn counter `text-[10px]` — same.
- `hint text: text-base` (16px) — good for hint display.
- Auto-submit indicator text `text-[10px]` — important message, should be 12px.
- "TRY AGAIN" / "SKIP" secondary buttons: `h-12 flex-1` — 48px height, good. But with both present, each column is ~160px wide at 360px screen — acceptable.

---

### 7. Listener Screen
*(Not read in detail — assumed similar structure to speaker screen)*  
- Expected issues: same microscopic text sizes, touch target sizes.

---

### 8. Repractice Speaker (`repractice-speaker.component.ts`)
**Status: GOOD — mirrors speaker screen correctly**
- Same dark focus-mode layout pattern.
- `textFontSize` uses same clamp() approach. ✓

**Issues:**
- `text-[9px]` section labels ("What went wrong", "Correction", "Now say this correctly") — unreadable. Should be 11px minimum.
- Mistake context card: `text-sm` (14px) for both mistake detail and correction text — good.

---

### 9. Admin Dashboard (`admin-dashboard.component.ts`)
**Status: MODERATE — table issues**
- `grid-cols-2 lg:grid-cols-4` KPI grid — correct responsive approach. ✓
- KPI stat values: `text-2xl font-black` — good, readable. ✓
- KPI labels: `text-[10px] font-bold uppercase tracking-widest` — borderline, should be 11px.

**Issues (CRITICAL):**
- Uses HTML `<table>` for "Latest Activity" panel — **tables do not work on mobile**. On 360px screen with `overflow-x-auto`, users must horizontally scroll to see data. This is a known mobile UX failure pattern.
- Table headers: `text-[10px]` — unreadable at this size.
- Table cells: `font-size: 14px` via global override — acceptable but table layout itself is problematic.
- `grid-cols-1 lg:grid-cols-3` for main content grid — on mobile this stacks, which is correct, but the card takes full width and the table inside scrolls horizontally.

**Required transformation:** Admin tables → card-list pattern for screens < 768px.

---

### 10. Admin Layout (`admin-layout.component.scss`)
**Status: GOOD — well-structured shell**
- Footer nav with `env(safe-area-inset-bottom)` — correct. ✓
- Content padding-bottom: `calc(84px + env(safe-area-inset-bottom, 0px))` — correct. ✓
- Desktop centering at 700px/1024px breakpoints — correct. ✓
- Topbar `60px` fixed height — good.

**Issues:**
- Topbar height `60px` is hardcoded — should use `clamp(56px, 13vw, 64px)` for consistency with header component.
- `profile-meta` hides at 480px with `display: none` — correct but consider showing a shorter version (just role) at 360–480px.
- `.brand-name` font-size `14px` hardcoded — should use clamp or variable.

---

### 11. Auth Header / Shared Header (`header.component.scss`)
**Status: GOOD**
- Height `clamp(52px, 12vw, 60px)` — correct fluid approach. ✓
- Logo font `clamp(15px, 3.5vw, 18px)` — correct. ✓
- Padding `clamp(12px, 3vw, 16px)` — correct. ✓
- Avatar 36px — small touch target but wrapped in larger hitbox. Should verify tap area.

**Issues:**
- Streak badge: `font-size: 9px` — unreadable for streak number. Should be 11px minimum.
- Streak badge `padding: 1px 5px` — very tight. Should be `2px 6px`.

---

### 12. Bottom Navigation (`bottom-nav.component.scss`)
**Status: EXCELLENT — reference implementation**
- 68px height + safe-area-inset-bottom — correct. ✓
- `env(safe-area-inset-bottom)` applied to both height and padding-bottom — correct. ✓
- Desktop centering at 700px/1024px — correct. ✓
- `360px` override for extra-small phones — correctly shrinks icon wrap. ✓
- `-webkit-tap-highlight-color: transparent` — correct for mobile. ✓

**Issues (MINOR):**
- Nav label `font-size: 10px` — borderline. Fine for tab labels but should not go below this.

---

### 13. Login (`login.component.scss`)
See Section 1.

---

### 14. Voice Recorder (`voice-recorder.component.scss`)
**Status: GOOD — well-designed**
- Mic button `clamp(58px, 14vw, 70px)` — meets 44px minimum. ✓
- Waveform height `clamp(36px, 8vw, 44px)` — responsive. ✓
- `max-width: min(300px, 100%)` — correct mobile constraint. ✓

**Issues (MINOR):**
- State label `12px` — borderline but acceptable for secondary UI.
- Interim transcript `12px` — acceptable.

---

### 15. Session Create / Invite / History / Detail
*(Not fully read — but based on pattern)*  
- Session history likely uses a list/card pattern — verify no tables.
- Session invite form — likely similar to create form, same select sizing issues.

---

### 16. User Profile / Improvement Tracker / My Mistakes / Pronunciation Timeline
*(Inferred from module structure)*  
- These screens likely have data-heavy layouts that need mobile card patterns.
- My Mistakes in particular shows grammar tag badges + spoken text + correction — high information density that needs careful mobile treatment.

---

### 17. Script Library (`script-library.component.ts`)
*(Not read — inferred)*
- Script cards with filtering — likely a grid that needs to be single-column on mobile.
- Search and filter bar needs vertical stacking on mobile.

---

## Application-Wide Issues

### ISSUE A — Microscopic Text (CRITICAL)
**Affected text sizes:** `text-[7px]`, `text-[8px]`, `text-[9px]`, `text-[10px]`  
**Locations:** Lobby, Dashboard, Repractice, Speaker Screen, Admin Dashboard  
**Impact:** Unreadable on all Android screen sizes at normal viewing distance.  

Android minimum recommended text sizes:
- Secondary/caption: 12px (11px absolute minimum)
- Body: 14px
- Emphasis/heading: 16px+

The project's current design uses `font-black + uppercase + tracking-widest` for labels — this improves readability versus regular weight at the same size, but **7–9px is below the threshold of legibility regardless of weight**.

### ISSUE B — Touch Target Size (HIGH)
**Standard:** Android minimum 48dp × 48dp; iOS minimum 44pt × 44pt.  
**Recommended:** 44px × 44px as universal safe minimum.  

Violating elements found:
| Element | Current Size | Location |
|---|---|---|
| Close buttons (X) | `w-6 h-6` = 24px | Session room, alert banners |
| Presence toast dismiss | `w-6 h-6` = 24px | Session room |
| Pending repractice chevron | `w-7 h-7` = 28px | User dashboard |
| Grammar review chevron | `w-7 h-7` = 28px | User dashboard |
| Session score chip | varies | User dashboard |
| Settings button | `w-9 h-9` = 36px | Session room topbar |
| Leave button | `w-9 h-9` = 36px | Session room topbar |
| Select elements | `h-10` = 40px | Create session, form selects |
| Lobby "Remove" text link | inline text | Create session |
| Script card arrow | `w-8 h-8` = 32px | Dashboard learning path |

### ISSUE C — Missing Safe Area Bottom Padding (HIGH)
**Admin layout:** Correctly uses `calc(84px + env(safe-area-inset-bottom, 0px))` ✓  
**User screens:** Use hardcoded `pb-28` (112px) Tailwind class.  

On Android phones with gesture navigation (the default from Android 10+), the gesture indicator area is NOT part of `safe-area-inset-bottom` — it's handled by the system. However, on Android phones with punch-hole displays and on iOS (which Capacitor can target), `safe-area-inset-bottom` can be 20–34px.

The user content pages need the same safe-area treatment as admin:
```scss
padding-bottom: calc(#{$bottomnav-height} + env(safe-area-inset-bottom, 0px) + 16px);
```

### ISSUE D — Hardcoded Inline Color Values (MEDIUM)
Color values bypassing the CSS variable system, found in templates:
```
style="color:#F59E0B"           → should be: var(--gwf-warning)
style="background: rgba(245,158,11,0.1)"  → should be: rgba from CSS var
style="color:#E07B39"           → should be: var(--gwf-accent)
style="color:#3D5A99"           → should be: var(--gwf-primary) (wrong hex — actual primary is #5C35A8)
style="background: rgba(61,90,153,0.08)"  → should be: var-based
style="color:#F59E0B"           → var(--gwf-warning)
style="color:#2E7D32"           → var(--gwf-success)
```
Note: `#3D5A99` appears frequently as a "secondary blue" but is NOT in the color token system. This is a second undocumented color being used as an accent in live session. This needs a token: `--gwf-secondary: #3D5A99`.

### ISSUE E — Table Pattern on Mobile (MEDIUM)
HTML tables are used in:
- Admin dashboard "Latest Activity"
- Admin users list (inferred)
- Admin sessions list (inferred)
- Admin reports (inferred)

Tables with `overflow-x-auto` create a horizontally scrollable region — this is technically functional but it violates mobile UX patterns where vertical scrolling is the expected interaction.

**Standard solution:** Card-list pattern at `< 768px`, table at `>= 768px`.

### ISSUE F — No Standard Layout Wrapper for User Screens (MEDIUM)
Each user screen independently sets its layout:
```
class="min-h-screen bg-gw-bg"  // outer container
class="max-w-lg mx-auto px-4 pt-2 pb-28 space-y-4"  // inner wrapper
```
This is copied manually in every screen. Changes to:
- Bottom padding
- Max width
- Base padding
- Safe area

...require updating every component individually.

**Standard solution:** A shared CSS class (e.g., `.gwf-page-wrapper`) applied consistently.

### ISSUE G — Lobby Hero Card Overflow Risk (HIGH on narrow screens)
- Session name `text-3xl` with `font-black` can be 30–36px on some screens.
- Join code `text-3xl tracking-[0.3em] font-mono` is ~260px wide at base size.
- Card padding `p-7` (28px each side) = 56px horizontal padding consumed.
- At 360px viewport: 360 - 56px padding - 2px border = ~302px available width.
- A 6-char join code at 30px with 0.3em tracking = approximately 6 × (30 × 0.6) + 6 × (30 × 0.3) = ~162px base. OK.
- Session name: "Weekend Interview Preparation" = 28 chars × avg 17px = ~476px. **OVERFLOWS**.

### ISSUE H — No Standard for Scroll Containers (MEDIUM)
User screens use `min-h-screen` on the outer div but have `overflow: hidden` on body. The actual scrollable area is managed through flex layout. This works but creates inconsistency between screens.

The live session and admin layout use the correct pattern:
```
.shell { height: 100dvh; display: flex; flex-direction: column; overflow: hidden; }
.content { flex: 1; min-height: 0; overflow-y: auto; }
```
User layout wraps content in `<app-header>` + scrollable content + `<app-bottom-nav>`. This works but relies on each page having correct bottom padding.

---

## Mobile Design Standards

### Typography Scale

```
Level         Size              Use Case                       Current Issues
─────────────────────────────────────────────────────────────────────────────
Display       clamp(24px,6vw,32px)   Hero titles, scores           OK
H1 / Screen   clamp(18px,5vw,22px)   Page/section titles            OK  
H2 / Card     clamp(16px,4vw,20px)   Card headings                  OK
H3 / Sub      clamp(14px,3.5vw,16px) Sub-section headings           OK
Body          14px (min)            Main content text              OK
Body Small    13px (min)            Secondary content              OK
Label         12px (min)            Form labels, list sub-text     VIOLATED (10px used)
Caption       11px (min)            Metadata, dates, counts        VIOLATED (9px, 8px used)
Micro         11px (MIN ABSOLUTE)   ALL use cases                  VIOLATED (7px, 8px used)
```

**Rule:** `7px`, `8px`, `9px`, `10px` text ONLY acceptable when:
1. It is uppercase + bold + tracking-widest (improves legibility at small sizes), AND
2. It is not conveying critical information, AND
3. It is 11px on the internal scale (the rendering size after display density)

**Minimum sizes by role:**
- Any interactive label: 12px
- Any data value: 13px
- Any error/warning text: 13px
- Section labels (uppercase, bold): 11px (rendered)
- Status badges: 11px

**For the current text-[Xpx] pattern with uppercase + tracking:**
| Current | Replace With | Context |
|---|---|---|
| `text-[7px]` | `text-[11px]` | Ready/Waiting sub-labels |
| `text-[8px]` | `text-[11px]` | Stat sub-labels, metadata |
| `text-[9px]` | `text-[11px]` | Section headers, context tags |
| `text-[10px]` | `text-[11px]` | Secondary labels (keep tracking-widest) |

---

### Spacing System

**Standard page wrapper bottom padding:**
```scss
// For all user screens with bottom-nav
$page-bottom-padding: calc(#{$bottomnav-height} + env(safe-area-inset-bottom, 0px) + 16px);
// = calc(68px + env(...) + 16px) = 84px+ dynamically
```

**Consistent page wrapper class:**
```scss
.gwf-page-content {
  max-width: 512px;   // max-w-lg (32rem = 512px)
  margin: 0 auto;
  padding: 8px 16px calc(#{$bottomnav-height} + env(safe-area-inset-bottom, 0px) + 16px);
  // px-4 pt-2 pb-dynamic
}
```

**Card padding standard:**
- Mobile (< 480px): `clamp(12px, 3vw, 16px)` per side
- Default: `16px–20px` per side
- Large display cards (hero): `20px–28px`

**Stack spacing (between cards):**
- Tight: `gap-3` (12px) — related items
- Standard: `gap-4` (16px) — normal card rhythm
- Loose: `gap-5` (20px) — section separators

---

### Touch Target Standards

**Minimum sizes (apply universally):**
```
Primary CTA buttons:     height ≥ 56px (h-14)
Secondary buttons:       height ≥ 48px (h-12)
Icon buttons:            min 44×44px (w-11 h-11) with tap area
Close/dismiss:           min 44×44px — use padding to extend tap area
Nav tabs:                48×48px minimum tap area (already handled in bottom-nav)
Form inputs:             height ≥ 48px (h-12) — MUST include select elements
Toggle switches:         44px wide × 28px tall — existing 44px wide toggles ✓
```

**Tap area extension pattern for small visual elements:**
```html
<!-- Visual: 24px icon — Tap area: 44px -->
<button class="w-11 h-11 flex items-center justify-center">
  <i-lucide size="20"></i-lucide>
</button>
```

**Do NOT use:**
- `w-6 h-6` (24px) for interactive elements
- `w-7 h-7` (28px) for interactive elements
- `w-8 h-8` (32px) for interactive elements without tap-area extension
- `w-9 h-9` (36px) for important actions (leave, close, settings)

---

### Breakpoints & Safe Areas

**Device targets for GoWithFlow:**
| Category | Width Range | Example Devices |
|---|---|---|
| Ultra-small | 320–359px | Small Androids (legacy) |
| Small mobile | 360–399px | Redmi, Galaxy A-series |
| Standard mobile | 400–479px | Pixel 7a, OnePlus Nord |
| Large mobile | 480–639px | Large phones |
| Phablet | 640–767px | Tablets in portrait |
| Tablet | 768px+ | Tablets |

**Add breakpoint for ultra-small:**
```scss
$breakpoint-xxs: 360px;  // ADD to _variables.scss

@mixin respond-xxs {
  @media (max-width: #{$breakpoint-xxs}) { @content; }
}
```

**Safe area rules:**
```scss
// CORRECT — all screens with bottom nav
padding-bottom: calc(#{$bottomnav-height} + env(safe-area-inset-bottom, 0px) + 16px);

// CORRECT — all fixed bottom bars (nav, action bars)
height: calc(68px + env(safe-area-inset-bottom, 0px));
padding-bottom: env(safe-area-inset-bottom, 0px);

// INCORRECT — current user screens
padding-bottom: 112px; // pb-28: hardcoded, no safe area
```

**Viewport height:**
```scss
// CORRECT — full-screen views (live session, login)
height: 100dvh; // handles iOS Safari dynamic toolbar

// INCORRECT — some screens still use
min-height: 100vh; // ignores dynamic viewport
```

---

### Color Token System

**Existing tokens (keep):**
```css
var(--gwf-primary)        #5C35A8  purple
var(--gwf-primary-dark)   #4A2690
var(--gwf-primary-light)  #F0EBFA
var(--gwf-accent)         #E07B39  orange
var(--gwf-accent-dark)    #C5622A
var(--gwf-bg)             #F4F6F9
var(--gwf-card-bg)        #FFFFFF
var(--gwf-card-border)    #E0E4EC
var(--gwf-text)           #1A1A2E
var(--gwf-text-muted)     #6B7280
var(--gwf-success)        #2E7D32
var(--gwf-warning)        #F59E0B
var(--gwf-error)          #D32F2F
var(--gwf-focus-bg)       #1A1A2E  dark mode background
var(--gwf-focus-text)     #EAEAEA
```

**Missing token — ADD:**
```css
:root {
  --gwf-secondary:        #3D5A99;  /* secondary blue — used heavily in live session */
  --gwf-secondary-dark:   #2D4580;
  --gwf-secondary-light:  rgba(61, 90, 153, 0.08);
  --gwf-focus-border:     rgba(255, 255, 255, 0.08);
  --gwf-focus-muted:      rgba(255, 255, 255, 0.35);
  --gwf-focus-surface:    rgba(255, 255, 255, 0.05);
}
```

**Hardcoded values to replace:**
```
#3D5A99       → var(--gwf-secondary)
#2D4580       → var(--gwf-secondary-dark)
#E07B39       → var(--gwf-accent)
#F59E0B       → var(--gwf-warning)
#2E7D32       → var(--gwf-success)
#D32F2F       → var(--gwf-error)
#1A1A2E       → var(--gwf-focus-bg) or var(--gwf-text)
#121221       → [ADD token: --gwf-focus-bg-deep: #121221]
rgba(61,90,153,0.08)   → var(--gwf-secondary-light)
rgba(224,123,57,0.08)  → rgba(var(--gwf-accent-rgb), 0.08) [requires CSS RGB split]
```

---

## Component Standards

### Forms & Inputs

**Standard input:**
```html
<input class="w-full h-12 bg-gw-bg rounded-xl px-4 
              text-[15px] font-semibold text-gw-text 
              border-2 border-transparent focus:border-gw-primary 
              outline-none transition-colors" />
```
- Height: `h-12` (48px minimum) — already used in most inputs. ✓
- Font size: `text-[15px]` — prevents iOS zoom-on-focus (zoom triggers at < 16px). ✓
- Border style: transparent + focus:border — correct, no layout shift. ✓

**Standard select:**
```html
<select class="w-full h-12 bg-gw-bg rounded-xl px-3
               text-[14px] font-bold text-gw-text
               border-2 border-transparent focus:border-gw-primary 
               outline-none cursor-pointer">
```
- **Change from `h-10` to `h-12`** — meets 44px+ touch target.

**Form section card:**
```html
<div class="bg-white rounded-2xl border border-gw-card-border p-5">
  <label class="block text-[11px] font-bold uppercase tracking-[0.22em] text-gw-text-muted mb-3">
    Field Label
  </label>
  <!-- input -->
</div>
```
- Label: minimum `text-[11px]` (not 10px).

**Primary CTA button:**
```html
<button class="w-full h-14 rounded-2xl font-bold text-[13px] 
               uppercase tracking-[0.2em] flex items-center 
               justify-center gap-2.5 transition-all">
```
- Height: `h-14` (56px). ✓

**Secondary button:**
```html
<button class="h-12 px-5 rounded-2xl font-bold text-[12px]
               uppercase tracking-widest ...">
```
- Height: `h-12` (48px minimum).

---

### Cards

**Standard content card:**
```html
<div class="bg-white rounded-2xl border border-gw-card-border shadow-sm overflow-hidden">
  <!-- header -->
  <div class="flex items-center justify-between px-5 py-3.5 border-b border-gw-bg">
    <p class="text-[11px] font-black text-gw-text-muted uppercase tracking-widest">Title</p>
  </div>
  <!-- content -->
  <div class="px-5 py-4">...</div>
</div>
```

**Card header label minimum:** `text-[11px]` (currently `text-[10px]` in many places — bump to 11px).

**Card row item:**
```html
<div class="flex items-center gap-3 px-5 py-3.5 hover:bg-gw-bg/40 transition-colors">
  <!-- icon: w-10 h-10 rounded-xl (40px) or w-11 h-11 (44px) for tappable -->
  <!-- text: text-sm (14px) primary, text-[11px] secondary -->
  <!-- action: min w-11 h-11 -->
</div>
```

**Hero/feature card (e.g., lobby session card):**
```html
<div class="bg-gw-primary rounded-[28px] p-5 sm:p-7 text-white relative overflow-hidden">
  <!-- session name: clamp font, not fixed text-3xl -->
  <h2 class="font-black italic uppercase tracking-tighter leading-tight"
      style="font-size: clamp(18px, 5vw, 28px)">
    {{ sessionName }}
  </h2>
  <!-- join code: clamp font -->
  <span class="font-black tracking-[0.25em] font-mono"
        style="font-size: clamp(20px, 5.5vw, 28px)">
    {{ joinCode }}
  </span>
</div>
```

---

### Navigation

**Bottom nav (existing — keep as-is, reference implementation):**
- 68px + safe-area. ✓
- `env(safe-area-inset-bottom)` on padding and height. ✓
- `360px` breakpoint shrinks icon wrap. ✓
- Dark (#0D1526) background. ✓

**Content area offset (all pages with bottom nav):**
```scss
// In _variables.scss — add:
$page-bottom-safe: calc(#{$bottomnav-height} + env(safe-area-inset-bottom, 0px) + 16px);

// Apply to all user page wrappers instead of hardcoded pb-28
```

---

### Tables → Card Lists

**Standard:** Use tables only at `>= 768px`. Below that, use card-list pattern.

**Card list item template (replaces table row):**
```html
<div class="flex items-center justify-between px-4 py-3.5 border-b border-gw-bg last:border-0">
  <div class="flex-1 min-w-0">
    <p class="text-sm font-semibold text-gw-text truncate">{{ primaryField }}</p>
    <p class="text-[11px] text-gw-text-muted mt-0.5">{{ meta1 }} · {{ meta2 }}</p>
  </div>
  <div class="shrink-0 ml-3">
    <span class="badge badge-info">{{ status }}</span>
  </div>
</div>
```

**Responsive table wrapper:**
```html
<!-- Mobile: hide table, show card list -->
<div class="hidden sm:block overflow-x-auto">
  <table>...</table>
</div>
<div class="sm:hidden divide-y divide-gw-bg">
  <!-- card-list rows -->
</div>
```

---

### Dialogs & Confirmation

**Current pattern:** Uses `confirm()` browser dialog.  
**Issue:** `confirm()` looks different on every Android version and does not match app design.  
**Standard:** Replace with in-app bottom sheet or inline confirmation pattern.

**Confirmation pattern (no dialog box needed for most cases):**
```html
<!-- Replace confirm() for leave session, etc. -->
<div class="fixed inset-0 bg-black/50 z-50 flex items-end" *ngIf="showConfirm">
  <div class="w-full bg-white rounded-t-3xl p-6 space-y-4
              pb-[calc(1.5rem+env(safe-area-inset-bottom,0px))]">
    <p class="text-base font-bold text-gw-text">Are you sure?</p>
    <p class="text-sm text-gw-text-muted">...</p>
    <div class="flex gap-3">
      <button class="flex-1 h-12 ..." (click)="cancel()">Cancel</button>
      <button class="flex-1 h-12 ..." (click)="confirm()">Confirm</button>
    </div>
  </div>
</div>
```

---

### Buttons

**Hierarchy (enforce consistently):**
| Level | Size | Style | Use |
|---|---|---|---|
| Primary CTA | h-14 (56px), full-width | bg-gw-primary or bg-gw-accent, rounded-2xl | Main action per screen |
| Secondary | h-12 (48px), flex | bg-white border-2 border-gw-primary text-gw-primary | Alternative action |
| Danger | h-12 (48px), flex | border-2 border-gw-error text-gw-error | Destructive action |
| Ghost/Link | h-11 (44px), inline | text-gw-primary underline-on-hover | Tertiary navigation |
| Icon-only | min 44×44px visual tap area | bg-white/5 or bg-gw-bg rounded-xl | Toolbar actions |

**Icon button minimum (replace all <44px):**
```html
<button class="w-11 h-11 rounded-xl flex items-center justify-center bg-white/5
               hover:bg-white/10 transition-all active:scale-95">
  <i-lucide size="18"></i-lucide>
</button>
```

---

### Badges & Labels

**All badge text minimum:** `text-[11px]` (currently `text-[9px]` in many).

**Standard badge:**
```html
<span class="inline-flex items-center px-2 py-0.5 rounded-md
             text-[11px] font-black uppercase tracking-wider">
```

**Status pill (ready/waiting/completed):**
- Minimum `11px` text, minimum `24px` height.

---

## Screen-Specific Standards

### User Dashboard
```
Wrapper:          max-w-lg mx-auto px-4 pt-2 pb-[env-safe-area]
Quick actions:    grid-cols-3, icon 44px, label min 11px
Stat labels:      min 11px (not 8px–10px)
Card headers:     min 11px (not 10px)
Row items:        px-5 py-3.5
Tap targets:      All action buttons min 44px
Bottom padding:   calc(68px + env(safe-area-inset-bottom, 0px) + 16px)
```

### Lobby
```
Hero card:         p-5 (mobile) / p-7 (>= 480px)
Session name:      font-size: clamp(18px, 5vw, 28px) — NOT text-3xl
Join code:         font-size: clamp(20px, 5.5vw, 28px), tracking-[0.25em]
Member slot row:   min 60px tall for readability
Slot sub-labels:   min 11px (not 7px–9px)
Action buttons:    h-14 (56px) primary, h-14 secondary
Bottom padding:    16px (uses pb-10, this is page-level — OK since no bottom-nav on lobby)
                   CHECK: Does lobby have bottom-nav? If yes, use safe-area padding.
```

### Live Session (Focus Mode)
```
Topbar:            52px — keep
Session info:      Show context tag on mobile too (remove hidden sm:block)
Settings button:   w-11 h-11 (not w-9 h-9)
Leave button:      w-11 h-11 (not w-9 h-9)
Speaker label:     min 11px (not 10px)
Auto-submit msg:   min 12px
Content padding-bottom: max(24px, env(safe-area-inset-bottom, 24px)) — keep ✓
```

### Admin Screens
```
Table pattern:     Card-list on < 768px, table on >= 768px
Table headers:     min 11px (not 10px)
KPI stat labels:   min 11px (not 10px)
Content padding:   Keep calc(84px + env(safe-area-inset-bottom, 0px)) ✓
Topbar:            60px — keep (or update to clamp)
```

### Forms (Create Session, Invite, Settings, Profile)
```
Input height:      h-12 (48px minimum)
Select height:     h-12 (48px — change from h-10)
Label size:        min text-[11px] (not text-[10px])
Grid forms:        Single column on < 400px, 2-col on >= 400px
Button:            h-14 primary, h-12 secondary
```

### Script Library
```
Script cards:      Single column on < 640px
Search bar:        h-12 (48px)
Category filters:  Horizontal scroll chips, min 36px tall
Script detail:     Full-width card, no side-by-side on mobile
```

### Repractice / Correction Round
```
Mirrors live-session focus mode pattern
Context card labels: min 11px (not 9px)
Action buttons: h-14 primary, h-12 secondary
```

---

## Hardcoded Values to Replace

### In Templates (`.component.ts` inline styles)

| Hardcoded Value | Location | Replace With |
|---|---|---|
| `style="color:#F59E0B"` | Dashboard (flame icon) | `class="text-gw-warning"` |
| `style="background: rgba(245,158,11,0.1)"` | Dashboard (streak chip) | CSS class |
| `style="color:#E07B39"` | Speaker screen labels | `class="text-gw-accent"` |
| `style="color:#3D5A99"` | Dashboard alert icon | `class="text-gw-secondary"` (new token) |
| `style="background: rgba(61,90,153,0.08)"` | Multiple | CSS class |
| `style="color:#2E7D32"` | Multiple | `class="text-gw-success"` |
| `style="color:#166534"` | Session score (green) | `class="text-green-800"` (Tailwind) or new token |
| `style="color:#92400E"` | Session score (amber) | `class="text-amber-800"` (Tailwind) |
| `style="color:#991B1B"` | Session score (red) | `class="text-red-800"` (Tailwind) |
| `style="background: rgba(224,123,57,0.08)"` | Quick action icons | CSS class |
| `[style.background]="getScoreBg(score)"` | Session scores | Standardized Tailwind ngClass |

### In SCSS Files

| File | Hardcoded Value | Replace With |
|---|---|---|
| `login.component.scss` | `#3D5A99` button background | `var(--gwf-secondary)` |
| `login.component.scss` | `#2D4580` button hover | `var(--gwf-secondary-dark)` |
| `login.component.scss` | `#D32F2F` error | `var(--gwf-error)` |
| `login.component.scss` | `#6B7280` label color | `var(--gwf-text-muted)` |
| `login.component.scss` | `#1A1A2E` text | `var(--gwf-text)` |
| `admin-layout.component.scss` | `#0D1526` footer bg | Needs new token `--gwf-nav-bg` |
| `admin-layout.component.scss` | `60px` topbar height | `clamp(56px, 13vw, 64px)` or `$topbar-height` |
| `bottom-nav.component.scss` | `#0D1526` nav bg | `var(--gwf-nav-bg)` (new token) |

### Text Sizes to Bump

| Current | Bump To | Count (estimated) |
|---|---|---|
| `text-[7px]` | `text-[11px]` | ~5 occurrences |
| `text-[8px]` | `text-[11px]` | ~15 occurrences |
| `text-[9px]` | `text-[11px]` | ~25 occurrences |
| `text-[10px]` | `text-[11px]` | ~30 occurrences |

### Touch Target Sizes to Fix

| Current | Fix To | Elements |
|---|---|---|
| `w-6 h-6` | `w-11 h-11` (wrap icon) | Close buttons, dismiss |
| `w-7 h-7` | `w-11 h-11` | Chevron action buttons |
| `w-8 h-8` | `w-11 h-11` or keep w-8 with extended tap area | Arrow buttons |
| `w-9 h-9` | `w-11 h-11` | Settings, leave in session room |
| `h-10 select` | `h-12 select` | All select dropdowns |

---

## Implementation Priority

### Priority 1 — CRITICAL (do first, session experience)
1. Bump all `text-[7px]`/`text-[8px]`/`text-[9px]` to `text-[11px]` minimum
2. Fix `w-9 h-9` settings and leave buttons in session room to `w-11 h-11`
3. Fix `w-6 h-6` close buttons to `w-11 h-11` (with icon still at 13–16px)
4. Fix lobby hero card — clamp session name and join code font sizes
5. Fix select elements from `h-10` to `h-12`

### Priority 2 — HIGH (layout stability)
6. Replace `pb-28` in user pages with safe-area-aware padding
7. Add `--gwf-secondary: #3D5A99` token and replace all hardcoded `#3D5A99` values
8. Fix `text-[10px]` labels to `text-[11px]` (card section headers, form labels)
9. Add `$breakpoint-xxs: 360px` to variables and mixin
10. Remove `hidden sm:block` from session name in live session topbar

### Priority 3 — MEDIUM (admin & consistency)
11. Add card-list alternative to admin tables for < 768px
12. Replace hardcoded hex colors in SCSS files with CSS variables
13. Add `--gwf-nav-bg: #0D1526` token for nav background
14. Replace `confirm()` dialogs with in-app confirmation panels
15. Add `.gwf-page-content` utility class to replace per-screen inline padding

### Priority 4 — POLISH (tokens & system)
16. Replace remaining inline `style=` color values with Tailwind classes or CSS vars
17. Add `--gwf-focus-bg-deep: #121221` and `--gwf-secondary-light` tokens
18. Add portrait-only CSS for live session (prevent landscape layout on small screens)
19. Standardize all badge text sizes to `text-[11px]`
20. Audit and standardize `text-[10px]` → `text-[11px]` across all remaining components

---

## Reusable Design System Tokens

### Additions to `_variables.scss`
```scss
// Add to existing file
$breakpoint-xxs:    360px;    // Ultra-small Android phones
$page-bottom-nav:   68px;     // Aligns with $bottomnav-height (redundant — keep one)

// Page layout helpers
$page-max-width:    512px;    // max-w-lg = 32rem = 512px
$page-px:          16px;      // Standard horizontal padding on mobile
$page-pt:           8px;      // Standard top padding
```

### Additions to `styles.scss :root`
```css
:root {
  /* Add these tokens */
  --gwf-secondary:        #3D5A99;
  --gwf-secondary-dark:   #2D4580;
  --gwf-nav-bg:           #0D1526;
  --gwf-focus-bg-deep:    #121221;
}
```

### Additions to `styles.scss` utility classes
```scss
/* Standard user page content wrapper */
.gwf-page-content {
  max-width: $page-max-width;
  margin: 0 auto;
  padding: $page-pt $page-px
           calc(#{$bottomnav-height} + env(safe-area-inset-bottom, 0px) + 16px);
}

/* Standard card section header */
.gwf-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 20px;
  border-bottom: 1px solid var(--gwf-bg);
  
  .gwf-card-title {
    font-size: 11px;
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: 0.22em;
    color: var(--gwf-text-muted);
  }
}

/* Standard icon button (44px tap target) */
.gwf-icon-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  border-radius: 12px;
  transition: all 0.15s ease;
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
  
  &:active { transform: scale(0.9); }
}
```

### Additions to `_mixins.scss`
```scss
@mixin respond-xxs {
  @media (max-width: #{$breakpoint-xxs}) { @content; }
}

@mixin safe-area-bottom($extra: 16px) {
  padding-bottom: calc(#{$bottomnav-height} + env(safe-area-inset-bottom, 0px) + #{$extra});
}

@mixin gwf-icon-btn($size: 44px, $radius: 12px) {
  display: flex;
  align-items: center;
  justify-content: center;
  width: $size;
  height: $size;
  border-radius: $radius;
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
  transition: all 0.15s ease;
  &:active { transform: scale(0.9); }
}

@mixin clamp-text($min: 11px, $vw: 3vw, $max: 14px) {
  font-size: clamp(#{$min}, #{$vw}, #{$max});
}
```

---

## Summary Table: What Is Good vs. What Needs Fixing

| Area | Status | Notes |
|---|---|---|
| Design token system | ✓ GOOD | CSS vars + SCSS vars exist — just need consistent use |
| Breakpoints | ✓ GOOD | Add `360px` breakpoint |
| Bottom navigation | ✓ EXCELLENT | Reference implementation |
| Safe area (admin) | ✓ GOOD | Admin layout handles correctly |
| Safe area (user) | ✗ NEEDS FIX | pb-28 is not dynamic |
| Login screen | ✓ GOOD | Uses clamp() throughout |
| User dashboard | ⚠ MINOR | Microscopic labels, some small touch targets |
| Create session | ⚠ MINOR | Select height, some text sizes |
| Lobby | ✗ NEEDS FIX | Overflow risk, microscopic text, small targets |
| Live session room | ✓ GOOD | Well-designed, minor icon button sizing |
| Speaker screen | ✓ GOOD | Dynamic font sizing is correct |
| Admin layout | ✓ GOOD | Correct shell pattern |
| Admin dashboard | ⚠ MODERATE | Table-on-mobile, microscopic labels |
| Admin other screens | ⚠ LIKELY MODERATE | Tables, text sizes (not read) |
| Voice recorder | ✓ GOOD | clamp() mic button, good proportions |
| Typography | ✗ NEEDS FIX | 7–10px labels throughout |
| Touch targets | ✗ NEEDS FIX | Multiple elements below 44px |
| Color tokens | ⚠ PARTIAL | Missing --gwf-secondary, hardcoded values |
| Form inputs | ⚠ MINOR | Selects at h-10, need h-12 |
| Navigation (bottom) | ✓ EXCELLENT | — |
| Scroll layout pattern | ✓ GOOD | dvh used in focus mode, dynamic in admin |

---

*This document is the Mobile Design Standards reference. After approval, all responsive improvements will be implemented using this as the sole source of truth.*
