# WinUI 3 Implementation Patterns

Detailed implementation patterns for WinUI 3. See `doc/WinUI/` for quick-reference rules.

---

## DataTemplateSelector Pattern

DataTemplateSelector classes MUST be marked `partial` for C#/WinRT compatibility.

### Problem
Without the `partial` keyword, DataTemplateSelector classes crash in Release builds due to C#/WinRT interop requirements.

### Solution

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
            {
                if (dataItem.IsSeparator)
                {
                    return SeparatorTemplate;
                }
                else
                {
                    return TextTemplate;
                }
            }
            return base.SelectTemplateCore(item);
        }

        // The other overload should pass through to the base method
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }
    }
}
```

### XAML Usage

```xaml
<Page.Resources>
    <!-- Define templates -->
    <DataTemplate x:DataType="local:MyDataItem" x:Key="TextTemplate">
        <TextBlock Text="{x:Bind Name}" Foreground="Black"/>
    </DataTemplate>

    <DataTemplate x:DataType="local:MyDataItem" x:Key="SeparatorTemplate">
        <Rectangle Height="2" Fill="Gray" Margin="0,5,0,5"/>
    </DataTemplate>

    <!-- Instantiate selector with templates -->
    <local:CustomTemplateSelector x:Key="MyCustomSelector"
                                  TextTemplate="{StaticResource TextTemplate}"
                                  SeparatorTemplate="{StaticResource SeparatorTemplate}" />
</Page.Resources>

<!-- Apply to ItemsControl -->
<ItemsControl ItemsSource="{x:Bind MyCollection}"
              ItemTemplateSelector="{StaticResource MyCustomSelector}" />
```

### Key Points
- Always mark selector classes as `partial`
- Use `SelectTemplateCore(object item)` for most cases
- Pass-through for the container overload
- Define templates as resources before instantiating selector

---

## Centered ContentDialog Title Pattern

ContentDialog titles are left-aligned by default. Use TitleTemplate to center them.

### Problem
ContentDialog's title alignment is not exposed as a simple property and is deeply ingrained in internal structure.

### Solution

```xaml
<ContentDialog
    x:Name="MyCenteredTitleDialog"
    TitleTemplate="{StaticResource CenteredTitleTemplate}"
    Content="This is the content of the dialog."
    CloseButtonText="Ok"
    PrimaryButtonText="Action">
</ContentDialog>
```

### Define CenteredTitleTemplate

```xaml
<Application.Resources>
    <DataTemplate x:Key="CenteredTitleTemplate">
        <Grid HorizontalAlignment="Stretch">
            <TextBlock Text="{Binding}" HorizontalAlignment="Center" Margin="0,0,0,0"/>
        </Grid>
    </DataTemplate>
</Application.Resources>
```

### Key Points
- TitleTemplate allows custom UI for the title area
- Grid with `HorizontalAlignment="Stretch"` provides full width
- TextBlock with `HorizontalAlignment="Center"` centers the text
- Text binding inherits the original Title property value

---

## TextBox Context Menu Pattern

Use built-in `TextCommandBarFlyout` for TextBox context menus instead of custom MenuFlyout.

### Problem
Custom "Select All" menu items in TextBox context menus don't work reliably because:
- TextBox loses focus when context menu opens
- Selection disappears due to event timing issues
- Manual `SelectAll()` calls are brittle

### Solution

```xaml
<TextBox Text="{Binding Value, Mode=TwoWay}">
    <TextBox.ContextFlyout>
        <TextCommandBarFlyout/>
    </TextBox.ContextFlyout>
</TextBox>
```

### Alternative (Manual Implementation)

If you truly need custom menu items:

```csharp
private void SelectAllMenuItem_Click(object sender, RoutedEventArgs e)
{
    myTextBox.SelectAll();
}
```

However, the built-in `TextCommandBarFlyout` is preferred because it includes:
- Cut
- Copy
- Paste
- Select All
- Undo/Redo (when supported)

### Key Points
- `TextCommandBarFlyout` provides native editing commands
- Avoid custom MenuFlyout for TextBox editing operations
- Focus handling is complex - use built-in solutions
- TextCommandBarFlyout automatically handles focus and selection state

---

## Drag and Drop Reordering Pattern

Enable drag-and-drop reordering in ListView using built-in properties.

### Problem
Need to allow users to reorder items in a ListView, especially when items contain TextBox controls.

### Solution

```xaml
<ListView x:Name="MyListView"
          CanReorderItems="True"
          AllowDrop="True"
          CanDragItems="True">
    <ListView.ItemTemplate>
        <DataTemplate>
            <!-- TextBox inside DataTemplate works with drag-drop -->
            <TextBox Text="{Binding MyTextProperty, Mode=TwoWay}"/>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

### Key Properties

| Property | Purpose |
|----------|---------|
| `CanReorderItems="True"` | Enables user to reorder items within the list |
| `AllowDrop="True"` | Allows items to be dropped onto the control |
| `CanDragItems="True"` | Allows items to be dragged from the control |

### Backend Integration

Bind to a dynamic collection like `ObservableCollection<T>`:

```csharp
public ObservableCollection<MyItem> MyCollection { get; set; }

public MyViewModel() {
    MyCollection = new ObservableCollection<MyItem> {
        new MyItem { MyTextProperty = "Item 1" },
        new MyItem { MyTextProperty = "Item 2" },
        new MyItem { MyTextProperty = "Item 3" },
    };
}
```

When the user reorders items via the UI, the bound collection automatically updates to reflect the new order.

### Advanced Control

For fine-grained control:

```csharp
// Drag starting - can cancel specific items
MyListView.DragItemsStarting += (s, e) => {
    if (ShouldCancelDrag(e.Items)) {
        e.Cancel = true;
    }
};

// Drag completed - react to reordering
MyListView.DragItemsCompleted += (s, e) => {
    // Items have been moved - collection is already updated
    LogReorder(e.Items, e.NewPosition);
};
```

### Key Points
- All three properties must be set for full drag-drop support
- TextBox controls inside DataTemplates work seamlessly
- ObservableCollection automatically tracks reordering
- Use events for validation or logging after reordering

---

## Elevated Script Launch Pattern

Launch a PowerShell script with elevation (UAC) without a visible terminal window.

### Problem
`Process.Start` with `Verb = "runas"` and `-WindowStyle Hidden` in the arguments is a race condition: the shell creates and shows the window before PowerShell gets around to processing the flag. The window flashes briefly on every save.

### Solution

Use `ShellExecuteExW` directly via P/Invoke. It accepts a `uShow` field (`SW_HIDE = 0`) that is applied at the shell level — before any child process window is created.

```csharp
[StructLayout(LayoutKind.Sequential)]
private struct ShellExecuteInfo {
  public int Size;       // Must be set to Marshal.SizeOf<ShellExecuteInfo>()
  public uint Flags;     // SEE_MASK_NOSHOWUI suppresses error dialogs
  public IntPtr Window;
  public IntPtr Verb;    // "runas" — triggers UAC elevation
  public IntPtr File;    // "powershell.exe"
  public IntPtr Parameters;
  public IntPtr Directory;
  public int Show;       // SW_HIDE = 0 — the key difference from Process.Start
  // ... remaining fields ...
  public IntPtr Process; // Populated on return — wait on this handle
}
```

String fields (`Verb`, `File`, `Parameters`) are `LPCWSTR` pointers. Marshal them manually with `Marshal.StringToHGlobalUni` / `FreeHGlobal` in a try/finally — `LibraryImport` cannot marshal strings inside structs.

After the call: `WaitForSingleObject(info.Process, INFINITE)` → `GetExitCodeProcess` → `CloseHandle`.

### Key Points
- UAC prompt still appears (that's Windows enforcing elevation — unavoidable and expected)
- The PowerShell *terminal window* does not appear
- Non-elevated path (user-only vars) uses `Process.Start` with `CreateNoWindow = true` — simpler, no P/Invoke needed
- `SEE_MASK_NOSHOWUI` suppresses the shell's own error dialog if `ShellExecuteExW` fails

---

## Non-Shared Context Menu Pattern

Define context menus in a Style to ensure each item gets a unique instance.

### Problem
Defining a single `MenuFlyout` as a shared resource (`x:Key` in a dictionary) and referencing it via `StaticResource` in a `ListView` item template can cause rare "empty menu" issues. This happens because WinUI may struggle to re-bind the `DataContext` to the shared menu instance during rapid recycling.

### Solution
Wrap the `MenuFlyout` in a `Setter.Value` within a `Style`.

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

### Key Points
- Ensures each item has its own flyout instance
- Fixes `DataContext` resolution race conditions
- Eliminates "empty menu" failures in recycled lists

---

## Incremental List Reconciliation Pattern

Update `ObservableCollection` items incrementally instead of clearing the whole list.

### Problem
Calling `Clear()` and then adding items to a bound collection causes the entire UI pane to flicker and lose scroll position/focus. $O(N^2)$ reconciliation (using `IndexOf` or `Contains` in a loop) becomes slow as lists grow.

### Solution
Use a $O(N)$ reconciliation approach with `HashSet` for lookups, and implement a "fast path" for bulk updates (like initial load or search).

```csharp
public void UpdateList(List<T> targetList, T? changedItem = null) {
  // Fast Path: Bulk update
  if (changedItem == null) {
    if (!Collection.SequenceEqual(targetList)) {
      Collection.Clear();
      foreach (var item in targetList) Collection.Add(item);
    }
    return;
  }

  // Incremental Path: Targeted update
  var targetSet = new HashSet<T>(targetList);
  // 1. Remove missing
  // 2. Move/Insert to match order
  // 3. Force refresh of changedItem via indexer assignment: Collection[i] = item;
}
```

### Key Points
- Prevents UI flicker and scroll jumps
- $O(N)$ performance for large lists
- `Collection[i] = item` forces WinUI to re-evaluate the DataTemplate (useful for type toggles)

---

## Background System Notification Pattern

Always run blocking system-wide broadcasts on a background thread.

### Problem
Broadcasting a system-wide environment change (`WM_SETTINGCHANGE` via `SendMessageTimeout`) is synchronous and can take several seconds if other open applications are "hung" or slow to respond. This blocks the UI thread during every Save.

### Solution
Await a background task for the notification.

```csharp
public async Task SaveAsync(IEnumerable<T> items) {
  await Task.Run(() => PerformSave(items));
  // System notification on background thread
  await Task.Run(() => NotifySystemOfChanges());
}
```

### Key Points
- Prevents the application from "hanging" after a successful save
- Keeps the UI responsive while other apps process the change

---

## CommandBar Height Stabilization Pattern

Enforce explicit heights and collapsed labels to prevent layout shifts.

### Problem
WinUI `CommandBar` has complex internal logic for sizing based on label visibility and overflow. This can cause the bar (and the whole UI) to jump by a few pixels when search boxes are toggled or window sizes change.

### Solution
Use a consistent Style that locks the height and label behavior.

```xaml
<Style x:Key="StandardCommandBarStyle" TargetType="CommandBar">
  <Setter Property="Height" Value="48" />
  <Setter Property="DefaultLabelPosition" Value="Collapsed" />
  <Setter Property="OverflowButtonVisibility" Value="Collapsed" />
</Style>
```

### Key Points
- Prevents jarring layout shifts
- Provides a stable anchor for absolute-positioned elements (like Mica backdrops)

---

## ObservableProperty Initialization Pattern

Initialize complex `[ObservableProperty]` types in the constructor rather than inline.

### Problem
Initializing complex types (like `ObservableCollection`) or properties with `[NotifyPropertyChangedFor]` dependencies inline can cause `NullReferenceException` or unexpected behavior in WinUI 3. This happens because the source-generated property change handlers fire during the object's initialization phase, potentially accessing dependent objects that haven't been instantiated yet.

### Solution
Initialize simple types inline if desired, but always move complex types and dependent properties to the constructor.

```csharp
[ObservableProperty]
public partial string Name { get; set; } = string.Empty; // Simple types are safe inline

[ObservableProperty]
public partial ObservableCollection<Item> Items { get; set; }

public MyViewModel() {
    Items = []; // Complex types MUST be in constructor
}
```

### Key Points
- Prevents race conditions during WinUI 3 / MVVM Toolkit source generation.
- Simple types (`bool`, `int`, `string`) are safe for inline initialization.
- Collections and objects with `[NotifyPropertyChangedFor]` MUST use constructor initialization.

---

## Additional Resources

- [Microsoft Learn: Data template selection](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/data-template-selector)
- [Windows App SDK: ContentDialog API](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.contentdialog?view=windows-app-sdk-1.8)
- [microsoft-ui-xaml GitHub Issues](https://github.com/microsoft/microsoft-ui-xaml/issues) - Check for known WinUI 3 bugs and workarounds