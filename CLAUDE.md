# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

- Build: `dotnet build`
- Run: `dotnet run` — serves on `https://localhost:7146` (HTTP fallback: `http://localhost:5062`)
- No test projects exist in this solution.

## Architecture

Single Blazor WebAssembly project (net10.0) targeting Azure Static Web Apps. No server-side component.

**Pages** — four calculators, each split into `.razor` (markup) and `.razor.cs` (code-behind):
- `HypergeometricCalculator` (`/hypergeometric`) — single-card draw probability using exact hypergeometric math
- `MultivariateCalculator` (`/multivariate`) — joint probability across multiple card groups, with Monte Carlo probability curve
- `MulliganLootingCalculator` (`/mulligan-looting`) — London Mulligan simulation with per-turn looting effects
- `ManaBaseAnalyzer` (`/mana-base`) — turn-by-turn land-drop probability table and chart; models cumulative draw count as `handSize + turn` and computes P(≥N lands) via hypergeometric CDF

**Services:**
- `ProbabilityCalculator` — static class; closed-form math (hypergeometric PMF/CDF, multivariate hypergeometric, binomial coefficients)
- `SimulationEngine` — static class; Monte Carlo simulations (single-draw and full mulligan/looting game sequence), Fisher-Yates shuffle, Wilson confidence intervals
- `FormatService` — singleton (DI); holds the active `FormatPreset`, persists selection to `localStorage` via JS interop, exposes a `Changed` event; all calculator pages inject it and call `Initialize()` in `OnInitializedAsync`

**Models:**
- `CardGroup` — a named group of cards defined by `CopiesInDeck` and `DesiredCopies`; shared across Multivariate and Mulligan pages
- `LootingEffect` — a per-turn draw/discard effect (`Turn`, `DrawCount`, `DiscardCount`)
- `SimulationResult` / `MulliganSimulationResult` — records holding Monte Carlo output including Wilson CI bounds
- `FormatPreset` — record with `Name`, `DeckSize`, `HandSize`, `MaxCopiesPerCard`, `DefaultLandCount`; three built-in presets: 60-card Traditional, 100-card Singleton (99 playable cards), 40-card Limited

# Claude Code Rules

## Collaboration

- Address the user as 'Master'.
- Do not make code changes before being explicitly told to. When asked how to implement something, describe the design and approach first, then wait for the user to confirm before touching any files.
- Cap the number of suggestions at 3.
- Prefer detailed answers over simplified ones.

## Code Style

- No `Async` suffix on async methods. The `async` keyword and `Task` return type make it clear enough. This applies everywhere in the codebase.
- Use descriptive variable names. Mathematical shorthands like `N`, `K`, `n`, `k` are acceptable in a textbook but make code harder to read. Prefer names that express domain meaning (e.g. `deckSize`, `copiesInDeck`, `drawCount`, `desiredCopies`). For math-heavy methods, include the traditional shorthand in the XML `<param>` description to allow cross-referencing with textbook equations (e.g. `Total number of cards in the deck (N).`).

## Project Conventions

- Do not add `@using Plotly.Blazor.LayoutLib` to `_Imports.razor`. It causes an ambiguous reference between `Plotly.Blazor.LayoutLib.Margin` and `MudBlazor.Margin` in any razor file that also uses MudBlazor layout components. Import `Plotly.Blazor.LayoutLib` only in `.razor.cs` code-behind files where it is needed.
- Plotly charts require explicit update calls to re-render after parameter changes. The correct pattern is: add `@ref="_chart"` to the `<PlotlyChart>` component, hold a `private bool _pendingChartUpdate` flag, set it to `true` whenever chart data is rebuilt, and call `await _chart.React(CancellationToken.None)` inside `OnAfterRenderAsync` when the flag is set. This ensures the call happens after Blazor has propagated the new `Data` and `Layout` values into the component.

## Implementation Plans

- Structure implementation plans as Git commits. Each top-level step must leave the solution in a buildable state (`dotnet build` passes).
- If a step introduces a breaking change, warn the user explicitly before implementing it and describe what will break and when it will be resolved.
- Sub-steps within a step are permitted and are not required to build individually; only the completed top-level step must build.
- End every implementation plan with a documentation step.

## Feature Backlog

### ~~Mana Base Analyzer (`/mana-base`)~~ — COMPLETE

### Optimal Copies Advisor

Inverse hypergeometric: given a target probability, draw window, and deck size, back-calculate the minimum copies needed.

- **Math:** Iterate over copy counts 1..4 (or 1..deck size), compute `HypergeometricAtLeast()` for each, return first that meets threshold. Could also display a curve of probability vs copy count.
- **UI:** Companion panel on the existing Hypergeometric Calculator page, or a standalone page. Inputs: deck size, draw count, desired probability. Output: recommended copy count + a table showing probability for 1–4 copies.
- **Models:** No new models needed.

### ~~Global Format Type Picker~~ — COMPLETE

### Full Page Descriptions (Theory + How-To-Use)

Each calculator page should contain two documentation sections visible to the user:

1. **Theory / Math explanation** — describes the statistical model behind the calculator, the formula used, and any assumptions made. The goal is that a mathematically curious user can "check the math" independently.
2. **How to use** — step-by-step guide for filling in the inputs, with at least one concrete worked example (e.g., "I want to know the probability of drawing my 4 Sol Rings in my first 7 cards of a 100-card Commander deck…").

- **UI:** Collapsible `MudExpansionPanel` sections beneath the calculator, or a tabbed layout (Calculator / Theory / How To Use). Keep them out of the way for returning users but discoverable for new ones.
- **Scope:** Applies to all existing pages: Hypergeometric, Multivariate, Mulligan & Looting, and Mana Base Analyzer.

### Scry / Surveil / Ponder Effects

Extends the Mulligan & Looting simulator to model ordered-look effects (inspect top N cards, selectively bottom some).

- **Math:** Requires a new simulation mode in `SimulationEngine`. Unlike looting (blind draw-discard), scry/ponder lets the player inspect and choose. Decision policy: bottom a card if it doesn't contribute to any combo group, prefer bottoming excess lands over non-combo spells.
- **UI:** New `LookEffect` model (Name, Turn, LookCount, BottomCount). Toggle or new section on the Mulligan & Looting page, or a standalone page.
- **Models:** New `LookEffect` model alongside `LootingEffect`.
