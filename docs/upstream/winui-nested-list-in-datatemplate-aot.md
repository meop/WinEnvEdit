# A ListView/ItemsControl declared inside a DataTemplate does not realize items under Native AOT

- **Target repo:** microsoft/microsoft-ui-xaml (cross-link CsWinRT if they redirect)
- **Kind:** bug report + minimal repro
- **Confidence:** High (reproduced in isolation)

## Summary

Under `PublishAot=true` (full trim, self-contained), an items control (`ListView` or `ItemsControl`)
**declared in XAML inside a `DataTemplate`** binds its `ItemsSource` but **never generates item containers** —
zero rows render — even though the bound collection is populated. The same control works when **created in
code** and added to the tree. No build warning or runtime exception; it silently shows nothing.

## Environment

Windows 11 25H2 · .NET 10 · Windows App SDK 2.1.x · `PublishAot=true` · self-contained · full trim · x64.
(JIT / framework-dependent build of the identical XAML works.)

## Minimal repro

1. New WinUI 3 (unpackaged) app; set `<PublishAot>true</PublishAot>`, `<SelfContained>true</SelfContained>`.
2. A VM `Outer` with `ObservableCollection<Child> Items` (populated, ≥3); `Child` has a `string Text`.
   Mark both `partial` + `[WinRT.GeneratedBindableCustomProperty]`.
3. An outer `ListView` (page level) whose `ItemTemplate` contains a **nested** `ListView`:
   ```xml
   <DataTemplate x:DataType="local:Outer">
     <ListView ItemsSource="{Binding Items}">
       <ListView.ItemTemplate>
         <DataTemplate x:DataType="local:Child">
           <TextBlock Text="{Binding Text}"/>
         </DataTemplate>
       </ListView.ItemTemplate>
     </ListView>
   </DataTemplate>
   ```
4. Bind the outer list to one `Outer`. Publish AOT and run.

**Expected:** nested rows render.
**Actual:** nested list is empty (no containers). `ItemsSource` is set and non-empty; a marker bound to
`Items.Count` shows the right number; even a **literal** inner template (`<TextBlock Text="ROW"/>`) produces
no rows. Replacing the nested XAML `ListView` with a `ListView` **created in code-behind** (same template via
`ContainerFromItem`/`ItemTemplate`) realizes rows correctly.

A focused isolation project (`aot-probe`) demonstrated this; it can be reduced to the above for filing.

## Workaround in WinEnvEdit

Host the nested list from code: an attached `DependencyProperty` bound to a trigger builds the `ListView` in
code on the dispatcher (after the panel is visible) and assigns `ItemsSource`. See
`WinEnvEdit/Resources/VariableTemplates.xaml.cs` (`PathExpandedProperty` / `OnPathExpandedChanged`) and
`docs/aot.md` → "The nested path-list: the hard case".

## Could this be working-as-designed?

Possibly tracked under the broader "WinUI 3 AOT support" effort. Worth confirming whether nested
item-container generation in a templated/virtualized parent is expected to work under AOT, or is a known gap.

## The ask

Either generate the item-container code for templated nested items under AOT, or document that code-hosting is
required and why — so apps don't discover it via silent empty lists.
