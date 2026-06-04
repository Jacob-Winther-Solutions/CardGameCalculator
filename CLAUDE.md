# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

- Build: `dotnet build`
- Run: `dotnet run` — serves on `https://localhost:7146` (HTTP fallback: `http://localhost:5062`)
- No test projects exist in this solution.

## Architecture

Single Blazor WebAssembly project (net10.0) targeting Azure Static Web Apps. No server-side component.

**Pages** — four calculators, each split into `.razor` (markup) and `.razor.cs` (code-behind):
- `HypergeometricCalculator` (`/hypergeometric`) — single-card draw probability using exact hypergeometric math; includes the Optimal Instances Advisor (inverse hypergeometric: finds minimum instances in deck to meet a target probability)
- `MultivariateCalculator` (`/multivariate`) — joint probability across multiple card groups, with Monte Carlo probability curve
- `MulliganLootingCalculator` (`/mulligan-looting`) — London Mulligan simulation with per-turn card selection effects; supports free first mulligan toggle (auto-applied from format preset)
- `ManaBaseAnalyzer` (`/mana-base`) — turn-by-turn land-drop probability table and chart; models cumulative draw count as `handSize + turn` and computes P(≥N lands) via hypergeometric CDF

**Services:**
- `ProbabilityCalculator` — static class; closed-form math (hypergeometric PMF/CDF, multivariate hypergeometric, binomial coefficients)
- `SimulationEngine` — static class; Monte Carlo simulations (single-draw and full mulligan/card-selection game sequence), Fisher-Yates shuffle, Wilson confidence intervals; `RunWithMulligans` accepts `freeFirstMulligan` to model the Commander free-first-mulligan rule
- `FormatService` — singleton (DI); holds the active `FormatPreset`, persists selection to `localStorage` via JS interop, exposes a `Changed` event; all calculator pages inject it and call `Initialize()` in `OnInitializedAsync`

**Models:**
- `CardGroup` — a named group of cards defined by `CopiesInDeck` and `DesiredCopies`; shared across Multivariate and Mulligan pages
- `LootingEffect` — a per-turn draw/discard effect (`Turn`, `DrawCount`, `DiscardCount`)
- `LookEffect` — a per-turn inspect-and-reorder effect (`Turn`, `LookCount`, `BottomCount`)
- `SimulationResult` / `MulliganSimulationResult` — records holding Monte Carlo output including Wilson CI bounds
- `FormatPreset` — record with `Name`, `DeckSize`, `HandSize`, `MaxCopiesPerCard`, `DefaultLandCount`, `FreeFirstMulligan`; three built-in presets: 60-card Traditional (false), 100-card Singleton — 99 playable cards (true), 40-card Limited (false)
- `AdvisorRow` — internal record used by `HypergeometricCalculator` to hold a single row of the Optimal Instances Advisor results (`Instances`, `Probability`, `MeetsTarget`)

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

### ~~Optimal Copies Advisor~~ — COMPLETE

### ~~Global Format Type Picker~~ — COMPLETE

### ~~Full Page Descriptions (Theory + How-To-Use)~~ — COMPLETE

### ~~Scry / Surveil / Ponder Effects~~ — COMPLETE

### Ponder Shuffle Path

The scry/surveil simulation uses the no-shuffle path only (always optimal under the greedy keep policy). A future upgrade could model the optional reshuffle on Ponder-style effects.

### Unified Effects Ordering

When a turn has both looting and look effects, looting fires first (deeper deck penetration before looking). A future upgrade could replace the two separate effect sections on the Mulligan page with a single ordered list where each row has a type dropdown (Loot / Look), giving the user full control over ordering.
