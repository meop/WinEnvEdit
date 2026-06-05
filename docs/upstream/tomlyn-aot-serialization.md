# Tomlyn — AOT / source-generation PRs & issue

- **Target repo:** xoofx/Tomlyn (examined at 2.4.1)
- **Kind:** 1 PR-ready, 1 feature issue, 1 docs follow-up
- **Confidence:** High — work already exists in a local clone

The full write-up (root cause, repros, line refs) is `AOT-FINDINGS.md` in the local Tomlyn clone at
`C:\Users\marshall\code\xoofx\Tomlyn`, on branch `docs/aot-remaining-work`. There is also a
`src/Tomlyn.AotTests` project. Summary so an agent can act:

## 1. PR-ready — generated `TomlSerializerContext` has no public constructors
- **Branch:** `feature/generated-context-public-constructors` (one commit ahead of `main`).
- The source generator emits only a private ctor, so `new MyContext(options)` doesn't compile (`CS1729`);
  STJ's `JsonSerializerContext` exposes public ctors. The branch emits a public parameterless + an
  options-accepting ctor and adds tests.
- **Action:** push the branch as a PR to xoofx/Tomlyn (STJ parity, low risk). `origin` on the clone points at
  upstream; create a fork remote first.

## 2. Issue — source-gen serialization ignores `TableArrayStyle` (`[[array-of-tables]]`)
- With the source generator (the AOT path), `List<ComplexType>` **writes** as inline tables and **reads back
  empty** for `[[key]]` sections, regardless of `TableArrayStyle.Headers`. Only the dynamic model writer
  honors the option, so the two paths diverge silently. Root causes + minimal repro are in `AOT-FINDINGS.md` §2.
- Maintainer-owned (table-arrays are only valid at document/table level → must fall back to inline when
  nested), so **file as an issue with the repro**, not a drive-by PR.
- This is what forces WinEnvEdit's workaround: build a `Tomlyn.Model.TomlTable` by hand and serialize via a
  context registered for `TomlTable` (`WinEnvEdit.Core/Services/FileService.cs` + `TomlExportContext.cs`).

## 3. Docs follow-up (gated on #2)
- `site/docs/migration.md` / `source-generation.md` don't mention the inline-only source-gen limitation or the
  `TomlTable`-via-context recipe. Author after #2 is resolved (decides whether the recipe is recommended or
  obsolete).

## Note
These currently live only in the local clone — nothing is pushed. `origin` is upstream, so filing needs a fork.
