# Tomlyn source-gen serialization (filed & resolved)

- **Target repo:** xoofx/Tomlyn
- **Kind:** record of two filed interactions (one accepted, one declined) — **not a draft**
- **Status:** closed upstream; WinEnvEdit ships on the released package with no fork.

Background: WinEnvEdit's TOML export/import moved to an AOT-safe, source-generated `TomlSerializerContext`
(`WinEnvEdit.Core/Services/TomlExportContext.cs`, consumed via `.Default` in `FileService`). Two rough edges
surfaced while doing that; both were filed upstream.

## 1. Array-of-tables didn't emit `[[Foo]]` (issue #128) — ACCEPTED

Source-generated `List<T>` of table-shaped objects serialized as an inline array of inline tables
(`Foo = [{...}, ...]`) even with `TomlTableArrayStyle.Headers`, instead of the `[[Foo]]` header layout.
WinEnvEdit needs `[[System]]` / `[[User]]` headers.

- **Outcome:** fixed upstream (Tomlyn 2.7, commit `2ce2271`). WinEnvEdit's earlier hand-built `TomlTable`
  workaround was removed once 2.7 shipped — the export now uses the plain POCO source-gen context.

## 2. Generated context has no public constructors (issue #129 / PR #130) — DECLINED (won't fix)

The generated `TomlSerializerContext` is emitted with only a private constructor (used by the static
`Default`), so `new MyContext()` / `new MyContext(options)` don't compile (`CS1729`). The ask was parity with
`System.Text.Json`'s `JsonSerializerContext`, which exposes a public parameterless ctor and an
options-accepting ctor. A PR (`#130`) added exactly those, purely additively.

- **Outcome:** closed as won't-fix. Maintainer's reasoning (paraphrased): Tomlyn's generator bakes many
  `TomlSerializerOptions` into the emitted code at **compile** time — `PropertyNamingPolicy` (names become
  string literals), `PropertyNameCaseInsensitive` (selects the member-dispatch branch),
  `DefaultIgnoreCondition`, `DuplicateKeyHandling`, `MappingOrder`, `PreferredObjectCreationHandling`. An
  options-accepting ctor would therefore be a *misleading contract*: `context.Options` could say
  case-insensitive while the generated reader still runs the case-sensitive branch baked in at generation. The
  supported way to change compile-time-sensitive behavior is to generate a different context with different
  `[TomlSourceGenerationOptions]`.

- **Our assessment (reconciled):** the technical objection to the *arbitrary-options* ctor is valid — that
  ctor is a real trap given how aggressively Tomlyn bakes. But the rejection is stronger than the argument
  earns in two ways: (a) STJ ships the same ctor with the same class of caveat and documents it, so "not
  viable" is really "a trade-off chosen differently," and (b) the PR also added a harmless **parameterless**
  ctor (identical to `Default`, no ambiguity) that was closed along with the contested half. A middle path
  existed (ship the parameterless ctor; document or diagnostic-warn on the options ctor). Filing the papercut
  was still correct; the disposition is the maintainer's call, and we don't carry a fork over it.

## What this means for WinEnvEdit (no code change needed)

- We use `TomlExportContext.Default` exclusively and never need a runtime-options ctor, so the decline does
  not affect shipping code. We consume the **released** Tomlyn package (see `Directory.Packages.props`), not
  the `meop/Tomlyn` fork — do **not** pin to the fork to regain the declined ctors.
- Any future serialization customization (naming policy, case-insensitive import, ignore conditions, mapping
  order) must go on the context as `[TomlSourceGenerationOptions(...)]` — a **compile-time** decision — not a
  runtime options object. This is exactly the path the maintainer endorsed, and it's the only one that's
  honest under source generation. The existing `[TomlPropertyName]` attributes already follow this model.
