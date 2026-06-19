# GoWithFlow — UI Standards (Authoritative, Rule-Based)

> **Status: BINDING.** This file is the single source of truth for how every screen in GoWithFlow must
> look and respond across device types. It is referenced by `Backend/Docs/CLAUDE.md` (Standards Gate).
> **Any UI change MUST comply with this file.** A change that violates a rule here is blocked at the QA
> gate exactly like an API/SQL standards violation. When the user names a page, follow the
> **§9 Page-Rephrase Protocol** — do not free-style.
>
> This file consolidates rules already proven in the codebase (see `ProjectOverview.md` →
> "Android Mobile Module — Mobile Design Standards" and `Backend/Docs/Dev/MobileDesignAnalysis.md`).
> On any conflict, the most specific rule wins; if still unclear, mark `[VERIFY]` and ask.

---

## 1. Core Principles

1. **Mobile-first.** Design for the smallest supported width first (360px), then enhance upward. The
   APK (Capacitor Android) is the primary surface; the web app shares the same components.
2. **One design system.** Use the `gw-*` / `gwf-*` design tokens and the shared components. Never
   hardcode hex colors in templates or component styles — use the CSS variables (§4).
3. **Rule over taste.** Every spacing, size, and breakpoint decision maps to a rule below. If a screen
   needs something not covered here, propose a rule addition — don't invent a one-off.
4. **Build ≠ render.** A green build is not acceptance. Per CLAUDE.md §5a, visual changes are verified
   rendered (web) and, for the APK, on-device before sign-off.

---

## 2. Device-Type Matrix (Responsive Tiers)

Tailwind breakpoints, mobile-first. The **`md` (768px)** boundary is the primary "phone vs. large
screen" switch already used across the app (e.g. `hidden md:block` tables).

| Tier | Width | Tailwind prefix | Layout intent |
|---|---|---|---|
| **XXS phone** | `< 360px` | (base) + `$breakpoint-xxs` mixin | Single column; tighten gaps; never overflow. No `text-3xl` heroes — use `clamp()`. |
| **Phone** | `360–767px` | (base), `xs`/`sm` | Single column, stacked cards, full-width controls, bottom-nav visible. **Default target.** |
| **Tablet** | `768–1023px` | `md:` | Two-column where it helps; data **tables allowed** (`md:block`); wider content max-width. |
| **Desktop** | `≥ 1024px` | `lg:` `xl:` | Centered content with max-width wrapper; multi-column; hover affordances. |

Rules:
- **Content max-width:** centered pages cap at `max-w-lg` (mobile content) or a documented wider wrapper
  on `lg:`; never let line length run edge-to-edge on desktop.
- **No horizontal scroll on list/data views at any phone width** (§5). This is non-negotiable.
- Anything hidden below a breakpoint (`hidden sm:block`) must have a visible mobile equivalent — never
  hide essential info on phones.
- **Immersive full-screen surfaces** (e.g. the Listen player, `fixed inset-0`): still wrap content in a
  **centered max-width column** (`max-w-md → md:max-w-lg → lg:max-w-xl`, `mx-auto`) so it never runs
  edge-to-edge on tablet/desktop; let the background fill behind it. Scale **icons** per tier from a
  `tier` signal (a `window.resize` listener mapping to the §2 widths) + an `iconSize(key)` lookup, and
  size **dynamic text** with `clamp()` — keep every touch target ≥44px at the smallest tier.

---

## 3. Typography

| Role | Size | Notes |
|---|---|---|
| Minimum readable | **11px** | Never smaller. No `text-[7px..10px]`. |
| Body / labels | **≤ 14px** | Default body caps at 14px on mobile. |
| Scores / key numbers | **≤ 22px** | Score callouts cap at 22px. |
| Hero / long dynamic text (utterances, session names, join codes) | `clamp()` | Conservative `clamp(min, vw, max)` so it never overflows 320–360px. Never fixed `text-3xl`. |

- Weight/emphasis via the existing utility patterns (`font-black uppercase tracking-widest italic` for
  section eyebrows, `font-bold` for content) — match the surrounding screen, don't introduce a new scale.

---

## 4. Color Tokens (no raw hex in templates)

Use CSS variables / Tailwind `gw-*` classes only. Known tokens (see `styles.scss` / `_variables.scss`):

```
--gwf-bg / gw-bg              page background (#F4F6F9)
--gw-primary                  primary accent (purple)
--gwf-secondary  #3D5A99      second accent   (--gwf-secondary-dark #2D4580)
--gwf-nav-bg     #0D1526      dark nav surface
--gwf-focus-bg-deep #121221
gw-text / gw-text-muted / gw-card-border / gw-error / gw-warning / gw-success / gw-accent
```

- New color needed → add a token, then use it. Inline `style="background:#..."` is only allowed for
  data-driven values (e.g. per-category/per-role colors computed in TS), never for static theming.

---

## 5. Lists, Tables & "No Horizontal Scroll" (ENFORCED)

- A list/grid item must **never** require sideways scrolling to be read on a phone.
- Wide multi-column HTML `<table>` (anything with `overflow-x-auto`) is **desktop/tablet-only**: gate it
  `hidden md:block`, and provide a `md:hidden` **stacked-card** alternative — one card per row: header
  (title + chips + action) over a `grid-cols-2` label/value block. Preserve per-cell color logic in cards.
- Reuse one `@for (...) {} @empty {}` empty-state across both the table and the card list.
- Filter chips rows may scroll horizontally (they're controls, not content).

---

## 6. Spacing, Touch Targets & Safe Area

- **Touch targets ≥ 44px** for primary/interactive controls (`w-11 h-11`). Secondary icon buttons ≥ 40px
  (`w-10 h-10`). Form inputs/selects ≥ 44px (`h-11`), 48px (`h-12`) for prominent create/session forms.
- **One page gutter, owned by the shell.** The horizontal/top gutter is provided **once** by the user
  shell `.user-content-area` (`app.component`): `16px` sides + top on phones, `24px`/`20px` on tablet+
  (`@media ≥768px`). **Page root containers must NOT add their own `px-4`/`pt-*`** — doing so double-pads
  (content was being inset 32px/side and squeezed). Page roots are `max-w-lg mx-auto gwf-page-bottom …`
  (max-width cap + centering + bottom clearance only). If an inner element needs to break out, do it
  inside the page, not by re-adding a page gutter.
- **Bottom padding** on scrollable user pages uses the safe-area-aware utility **`.gwf-page-bottom`**
  (`calc(68px + env(safe-area-inset-bottom) + 16px)`) — never hardcoded `pb-28`. This is the **sole**
  bottom/nav clearance; the shell no longer adds its own `padding-bottom` (that previously stacked ~164px
  above the nav).
- Full-screen / docked surfaces use `env(safe-area-inset-*)` (e.g. `pb-[max(1rem,env(safe-area-inset-bottom))]`).
- Admin shell already handles safe area (`calc(84px + env(safe-area-inset-bottom))`) — match it.

---

## 7. Shell, Navigation & Loading

- **Full-bleed routes** (`/auth`, `/live-session`, `/repractice`) own their entire layout; the user shell
  removes padding via `[class.flush]`. Don't add a shell frame to these.
- **Bottom nav** (`bottom-nav.component`) is the shared footer for user + admin shells. **Icons-only:**
  the bar shows icons with no visible text labels (labels kept in the DOM as screen-reader-only +
  `aria-label`; the active tab's name is surfaced elsewhere on tap). Active tab uses the primary color +
  light pill behind the icon. Change it only by an explicit, documented design decision (update this file).
- **Loading:** use the Frontend Loading Framework — branded `LoaderService` for full-screen/route
  transitions, `TopProgressBar` + `loadingInterceptor` for background calls, and the skeleton kit
  (`@shared/ui/skeleton`) / `LoadingStateComponent` per section. No ad-hoc spinners; no layout shift.

---

## 8. Component Conventions

- **Standalone components**, Angular **signals** for state, `inject()` for DI.
- Icons: **lucide-angular** (declare each icon as a `readonly X = Icon` field).
- Bottom sheets: Angular Material `MatBottomSheet`; menus: `MatMenu`.
- Business logic lives in services, never components. Components never call the DB/HTTP directly except
  through the project's services.
- Every interactive/visual change is verified rendered (and on-device for the APK) before sign-off.

---

## 9. Page-Rephrase Protocol (when the user names a page)

When the user gives a **page name** ("rephrase the X page"), follow these steps every time:

1. **Locate** the page component(s) (`.ts` + `.html`/`.scss`).
2. **Audit** it against §1–§8 and list concrete violations (typography, touch targets, tokens, scroll,
   safe area, responsive tiers, loading).
3. **Propose** the redesign as options with a recommended one (per CLAUDE.md §4a Clarify-&-Confirm),
   including how it renders per device tier (§2). Wait for the user's verdict.
4. **Apply** only the confirmed option, using tokens + shared components; keep the backend/API contract
   and route untouched unless explicitly asked.
5. **Verify** rendered (web) and on-device (APK) per §5a; report honestly if unverified.
6. **Document** any new rule introduced back into THIS file, and note the page in `ProjectOverview.md` if
   a flow/contract was touched.

Output format for a page audit:
```
PAGE: <name> (<route>, <files>)
VIOLATIONS: [rule § → what's wrong]
PROPOSAL (recommended first): [option + per-tier rendering]
```

---

## 10. Change Control

- Editing a rule here is a design-system decision: state the reason, get the user's confirmation, and
  keep `ProjectOverview.md` cross-references in sync.
- This file overrides scattered/older UI notes; if `ProjectOverview.md` and this file disagree on a UI
  rule, **this file wins** and the other is corrected.
