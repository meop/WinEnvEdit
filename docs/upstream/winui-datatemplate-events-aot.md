# Event handlers wired in a DataTemplate never fire under Native AOT

- **Target repo:** microsoft/microsoft-ui-xaml
- **Kind:** bug report + minimal repro
- **Confidence:** High (reproduced in the product; consistent with x:Bind-in-template being dead)

## Summary

Under `PublishAot=true`, FrameworkElement events wired **in XAML inside a `DataTemplate`** —
`Loaded`, `DataContextChanged`, and `ContextRequested` — **never invoke their handlers**. The same handlers
fire when wired on a **page/window-level** element (outside a template). No build warning, no exception.

This is the lifecycle-event sibling of the well-known "`x:Bind` is dead inside a `DataTemplate` under AOT":
the per-template connect/codegen the XAML compiler would emit appears to be absent.

## Environment

Windows 11 25H2 · .NET 10 · Windows App SDK 2.1.x · `PublishAot=true` · self-contained · full trim · x64.
(JIT build of the identical XAML fires the handlers.)

## Minimal repro

1. WinUI 3 unpackaged app, `PublishAot=true`.
2. A `ListView` whose `ItemTemplate` root wires events:
   ```xml
   <DataTemplate x:DataType="local:Item">
     <Border Loaded="OnRowLoaded"
             DataContextChanged="OnRowDcc"
             ContextRequested="OnRowContext">
       <TextBlock Text="{Binding Name}"/>
     </Border>
   </DataTemplate>
   ```
3. In the handlers, append a line to a temp log file.
4. Publish AOT, run, scroll/right-click.

**Expected:** `OnRowLoaded`/`OnRowDcc` fire as rows realize; `OnRowContext` fires on right-click.
**Actual:** the log file is never written — none of the handlers run. Wiring the same handler on a
page-level element (not in a template) works.

## Workaround in WinEnvEdit

- Replaced template-level `ContextRequested` (per-card menu) with a **ListView-level** `ContextRequested`
  handler that resolves the right-clicked item from the visual tree
  (`WinEnvEdit/MainWindow.xaml.cs` → `ListView_ContextRequested` / `FindVariable`).
- Replaced template-level `Loaded`/`DataContextChanged` (used to build the nested list) with a **binding to an
  attached `DependencyProperty`** (a binding fires where events don't):
  `WinEnvEdit/Resources/VariableTemplates.xaml.cs` → `PathExpandedProperty`.

See `docs/aot.md` for the narrative.

## Could this be working-as-designed?

Same caveat as the nested-list report — may be part of the in-progress WinUI AOT work. Confirm whether
in-template event wiring is expected to work under AOT.

## The ask

Generate the in-template event-connection code under AOT, or document the limitation alongside the
`x:Bind`-in-template guidance so the binding-based workaround is discoverable.
