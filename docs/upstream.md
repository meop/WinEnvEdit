# Upstream reports & PRs (drafts)

Findings from making WinEnvEdit run under Native AOT that are worth giving back. Each file is a
self-contained draft an agent (or you) can turn into a GitHub issue or PR on the target repo. They are
**drafts living in this repo** — nothing has been filed yet.

| Draft | Target repo | Kind | Confidence it's a real gap |
|-------|-------------|------|----------------------------|
| [winui-nested-list-in-datatemplate-aot.md](upstream/winui-nested-list-in-datatemplate-aot.md) | microsoft/microsoft-ui-xaml | issue + repro | High |
| [winui-datatemplate-events-aot.md](upstream/winui-datatemplate-events-aot.md) | microsoft/microsoft-ui-xaml | issue + repro | High |

Before filing any of these, **search the target repo** — WinUI 3 + AOT is actively evolving and some may
already be tracked or marked "by design / tracked by the AOT effort." Each draft has a "Could this be
working-as-designed?" note.

Environment for all reports: Windows 11 (25H2), .NET 10, Windows App SDK 2.3.x, `PublishAot=true`,
self-contained, full trim, x64. A `WinEnvEdit/Resources/VariableTemplates.xaml(.cs)` is the in-product
example of each workaround; `docs/aot.md` is the narrative.

## Filed & resolved

Submitted upstream (open or closed), or fixed before filing — kept here as a record, not a draft to file.

| Record | Target repo | Outcome |
|--------|-------------|---------|
| [cswinrt-collection-expression-ccw.md](upstream/cswinrt-collection-expression-ccw.md) | microsoft/CsWinRT | **filed [#2475](https://github.com/microsoft/CsWinRT/issues/2475)** — closed "expected": an analyzer warns about it from CsWinRT 2.3, and it is only made to *work* in CsWinRT 3.0. Windows App SDK 2.3.2 still carries CsWinRT 2.2, so the `new List<string>` workaround stays in `DialogService` |
| [tomlyn-sourcegen.md](upstream/tomlyn-sourcegen.md) | xoofx/Tomlyn | `#128` array-of-tables headers — **accepted** (fixed in 2.7); `#129`/PR `#130` public ctors — **declined** (won't fix) |
| resourcedictionary-indexer-null-aot | microsoft-ui-xaml | **no longer reproduces on Windows App SDK 2.2** — `Application.Current.Resources[key]` resolves merged-dictionary keys under AOT again; the `FindResource`/`FindIn` workaround was removed and it was never filed |
