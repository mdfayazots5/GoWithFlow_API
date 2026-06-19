# Next Phase — UI Polish (deferred 2026-06-19)

Captured during the v1.8 session. Issues 1, 3, 4 below were **fixed and shipped in v1.8**; issue 2 and
the latent build finding are **deferred to the next phase**.

## Shipped in v1.8 (fixed — needs final visual confirmation on device)

1. **Listen settings — every role showed the same voice.** `ScriptPlaybackService` assigns distinct
   voices per role, but the sheet's `<select [value]=…>` didn't reflect the bound value (Angular renders
   the `@for` options around it), so each dropdown showed the first option (aarav). **Fix:** moved the
   selection to `[selected]` on each `<option>` in `listen-settings.sheet.ts`.
3. **Empty pages still scrolled.** 17 user-shell pages wrap content in `min-h-screen` (`min-height:100vh`).
   Inside the scroll container (`.user-content-area`, which already fills the space), that forces every
   page taller than the viewport → even empty pages scroll. **Fix:** scoped CSS override in
   `app.component.ts` — `.user-content-area:not(.flush) .min-h-screen { min-height: 0; }` (full-bleed
   routes keep their full-height layout).
4. **Dashboard last panel cut off behind the bottom nav.** Root cause: the bottom nav is
   `position: fixed` (overlay), and the page-level `.gwf-page-bottom` utility (the documented nav
   clearance) is **stripped from the built CSS** (see latent finding) so it adds 0px. The clearance was
   actually coming from the shell `padding-bottom` removed in the prior padding change. **Fix:** put the
   clearance back on the shell — `.user-content-area` `padding-bottom: calc(68px + env(safe-area-inset-bottom) + 16px)`
   (component-inline styles aren't purged, so reliable). Verified: last panel bottom 680px now clears the
   nav top at 696px.

## Deferred to next phase

### 2. Header-icon consistency across the tab/list pages
The list-page headers are inconsistent three ways:
- **Listen** (`listen-picker`): rounded **icon badge on the LEFT**, before the title.
- **Session History**: icon badge on the **RIGHT**, after the title.
- **Script Library** (and several others): **no icon**, title only.

**Decision (user, 2026-06-19):** standardize by **adding the icon badge to all** main tab/list pages,
using the **Listen-style left-aligned badge** (`w-10 h-10 rounded-xl bg-gw-primary/10` + `i-lucide`
`text-gw-primary`). Each page needs an appropriate lucide icon + the import/field. Pages to update
(approx.): script-library (Book/Library), session-list + session-history (History — already right-side,
move to left), my-mistakes (AlertCircle), learning-goals (Target), vocabulary-bank (BookOpen),
improvement-tracker/progress (TrendingUp), interview-performance, pronunciation-timeline, my-invitations,
settings, profile. Verify rendered per page (§5a).

### Latent build finding — `styles.scss` custom classes stripped from the built CSS
The entire custom-class layer of `Frontend/src/styles/styles.scss` (`.gwf-card`, `.gwf-page-bottom`,
`.gwf-page-content`, `.gwf-page-header`, `@keyframes gwf-shimmer`, …) is **absent from the loaded client
bundle** (`dist/analog/public/assets/index-*.css` — 0 occurrences), while CSS vars/Tailwind work. So
`.gwf-page-bottom` (referenced by 17 templates + documented in `UIStandards.md §6`) is a **no-op**.
Investigate the Analog/Vite + Tailwind v4 style pipeline (is `styles.scss` actually included in the
client build, or is its non-`:root` content being dropped?). Until fixed, the **shell owns bottom
clearance** (done in #4) — update `UIStandards.md §6` to match, or restore the utility once the build is
fixed. `calc(env(safe-area-inset-bottom))` itself resolves fine on-device (tested = 84px), so this is a
bundling issue, not a CSS-support issue.
