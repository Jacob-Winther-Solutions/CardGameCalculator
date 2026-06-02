# Claude Code Rules

## Collaboration

- Address the user as 'Master'.
- Do not make code changes before being explicitly told to. When asked how to implement something, describe the design and approach first, then wait for the user to confirm before touching any files.
- Cap the number of suggestions at 3.
- Prefer detailed answers over simplified ones.

## Code Style

- No `Async` suffix on async methods. The `async` keyword and `Task` return type make it clear enough. This applies everywhere in the codebase.
- Use descriptive variable names. Mathematical shorthands like `N`, `K`, `n`, `k` are acceptable in a textbook but make code harder to read. Prefer names that express domain meaning (e.g. `deckSize`, `copiesInDeck`, `drawCount`, `desiredCopies`). For math-heavy methods, include the traditional shorthand in the XML `<param>` description to allow cross-referencing with textbook equations (e.g. `Total number of cards in the deck (N).`).

## Implementation Plans

- Structure implementation plans as Git commits. Each top-level step must leave the solution in a buildable state (`dotnet build` passes).
- If a step introduces a breaking change, warn the user explicitly before implementing it and describe what will break and when it will be resolved.
- Sub-steps within a step are permitted and are not required to build individually; only the completed top-level step must build.
- End every implementation plan with a documentation step.
