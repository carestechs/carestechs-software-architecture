---
category: angular
stack: angular
status: Active
requires: []
conflicts_with:
  - adrs/react/tanstack-query.md
last_reviewed: 2026-07-29
---

# Signals for Reactive State Management

## Decision
All reactive component state uses Angular Signals. RxJS is reserved for HTTP calls and complex async streams only.

## Rationale
- Signals provide fine-grained reactivity with simpler mental model than RxJS for synchronous state
- Reduces subscription management bugs (memory leaks, missing unsubscribe)
- Alternatives considered: RxJS BehaviorSubjects for all state, NgRx store — both rejected as overengineered for component-level state

## Constraints (non-negotiable for AI)
- Use `signal()` for all writable component state
- Use `computed()` for all derived/calculated state
- Never use `BehaviorSubject` or `ReplaySubject` for component state
- RxJS is acceptable only for: HTTP calls (`HttpClient`), complex async streams (websockets, debounced inputs, merge/race conditions)
- Use `toSignal()` to bridge RxJS observables into signal-based templates
- Use `effect()` sparingly — prefer `computed()` for derived values
