# XAML & WinUI Patterns

---

## DataTemplateSelector

DataTemplateSelector classes MUST be marked `partial` for C#/WinRT compatibility.

**Problem:** Without `partial`, DataTemplateSelector classes crash in Release builds due to C#/WinRT interop requirements.

```csharp
using Microsoft.UI.Xaml.Controls;

namespace YourAppName
{
    public partial class CustomTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate SeparatorTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is MyDataItem dataItem)
                return dataItem.IsSeparator ? SeparatorTemplate : TextTemplate;
            return base.SelectTemplateCore(item);
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            => SelectTemplateCore(item);
    }
}
```

```xaml
<Page.Resources>
    <DataTemplate x:DataType="local:MyDataItem" x:Key="TextTemplate">
        <TextBlock Text="{x:Bind Name}" Foreground="Black"/>
    </DataTemplate>
    <DataTemplate x:DataType="local:MyDataItem" x:Key="SeparatorTemplate">
        <Rectangle Height="2" Fill="Gray" Margin="0,5,0,5"/>
    </DataTemplate>
    <local:CustomTemplateSelector x:Key="MyCustomSelector"
                                  TextTemplate="{StaticResource TextTemplate}"
                                  SeparatorTemplate="{StaticResource SeparatorTemplate}" />
</Page.Resources>

<ItemsControl ItemsSource="{x:Bind MyCollection}"
              ItemTemplateSelector="{StaticResource MyCustomSelector}" />
```

---

## Centered ContentDialog Title

ContentDialog titles are left-aligned by default. Use `TitleTemplate` to center them.

```xaml
<Application.Resources>
    <DataTemplate x:Key="CenteredTitleTemplate">
        <Grid HorizontalAlignment="Stretch">
            <TextBlock Text="{Binding}" HorizontalAlignment="Center"/>
        </Grid>
    </DataTemplate>
</Application.Resources>
```

```xaml
<ContentDialog TitleTemplate="{StaticResource CenteredTitleTemplate}" .../>
```

---

## TextBox Context Menu

Use built-in `TextCommandBarFlyout` instead of a custom `MenuFlyout`.

**Problem:** Custom "Select All" items don't work reliably — TextBox loses focus when the menu opens, so selection disappears.

```xaml
<TextBox Text="{Binding Value, Mode=TwoWay}">
    <TextBox.ContextFlyout>
        <TextCommandBarFlyout/>
    </TextBox.ContextFlyout>
</TextBox>
```

`TextCommandBarFlyout` includes Cut, Copy, Paste, Select All, and Undo/Redo with correct focus handling.

---

## Drag and Drop Reordering

All three properties are required:

```xaml
<ListView CanReorderItems="True" AllowDrop="True" CanDragItems="True">
    <ListView.ItemTemplate>
        <DataTemplate>
            <TextBox Text="{Binding MyTextProperty, Mode=TwoWay}"/>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

Bind to `ObservableCollection<T>` — the collection updates automatically when the user reorders items.

For validation or logging:
```csharp
MyListView.DragItemsStarting += (s, e) => { if (ShouldCancel(e.Items)) e.Cancel = true; };
MyListView.DragItemsCompleted += (s, e) => LogReorder(e.Items, e.NewPosition);
```

---

## Non-Shared Context Menu

Define context menus in a `Style` to ensure each list item gets a unique instance.

**Problem:** A shared `MenuFlyout` resource can cause "empty menu" issues during rapid ListView recycling due to DataContext re-binding races.

```xaml
<Style x:Key="MyItemBorderStyle" TargetType="Border">
  <Setter Property="ContextFlyout">
    <Setter.Value>
      <MenuFlyout>
        <MenuFlyoutItem Text="Copy" Command="{Binding CopyCommand}"/>
      </MenuFlyout>
    </Setter.Value>
  </Setter>
</Style>
```

---

## CommandBar Height Stabilization

WinUI `CommandBar` resizes based on label visibility, causing layout jumps when search boxes toggle. Lock it with a style:

```xaml
<Style x:Key="StandardCommandBarStyle" TargetType="CommandBar">
  <Setter Property="Height" Value="48" />
  <Setter Property="DefaultLabelPosition" Value="Collapsed" />
  <Setter Property="OverflowButtonVisibility" Value="Collapsed" />
</Style>
```

---

## XAML Binding Rules

### x:Bind vs {Binding}

**Use `x:Bind` in Window/Page XAML** (e.g. `MainWindow.xaml`):
```xaml
<TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}"/>
```

**Use `{Binding}` in ResourceDictionary/DataTemplate** (e.g. `VariableTemplates.xaml`):
```xaml
<TextBlock Text="{Binding Name}"/>
```

Never use `x:Bind` in ResourceDictionary files — it won't work reliably without a code-behind compilation context.

### Binding Modes

| Syntax | Default Mode | Use for |
|--------|-------------|---------|
| `{x:Bind}` | OneTime | Static values, computed properties |
| `{x:Bind ..., Mode=OneWay}` | OneWay | Observable properties (read-only UI) |
| `{x:Bind ..., Mode=TwoWay}` | TwoWay | Editable controls (TextBox, CheckBox) |
| `{Binding}` | OneWay | ResourceDictionary templates |

### Converter Declaration Order

Converters MUST be declared BEFORE `MergedDictionaries` in `App.xaml`:

```xaml
<Application.Resources>
  <ResourceDictionary>
    <!-- 1. Converters FIRST -->
    <converters:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter"/>

    <!-- 2. THEN merged dictionaries that use them -->
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="Resources/VariableTemplates.xaml"/>
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

### ResourceDictionary Code-Behind

ResourceDictionaries using DataTemplates must have a code-behind class:

```csharp
namespace WinEnvEdit.Resources;

public partial class VariableTemplates : ResourceDictionary {
  public VariableTemplates() {
    InitializeComponent();
  }
}
```
