using System.Runtime.InteropServices;
using System.Windows.Input;

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;

using WinEnvEdit.Core.Constants;
using WinEnvEdit.Core.Services;
using WinEnvEdit.Services;
using WinEnvEdit.ViewModels;

using WinRT.Interop;

namespace WinEnvEdit;

public sealed partial class MainWindow : Window {
  private readonly IntPtr hwnd;
  private readonly SUBCLASSPROC? wndProcDelegate;

  public MainWindowViewModel ViewModel { get; }

  public MainWindow() {
    var environmentService = new EnvironmentService();
    var fileService = new FileService();
    var stateSnapshotService = new StateSnapshotService();
    var undoRedoService = new UndoRedoService();
    var clipboardService = new ClipboardService();
    var dialogService = new DialogService(this);
    ViewModel = new MainWindowViewModel(environmentService, this, fileService, stateSnapshotService, undoRedoService, clipboardService, dialogService);
    InitializeComponent();

    // Set minimum window size at Win32 level to prevent flickering
    hwnd = WindowNative.GetWindowHandle(this);
    wndProcDelegate = WndProc;
    SetWindowSubclass(hwnd, wndProcDelegate, 0, IntPtr.Zero);

    // Set window icon
    SetWindowIcon();

    // Flat title bar to match the solid window background (no caption backdrop tint).
    ApplyFlatTitleBar();
    if (Content is FrameworkElement root) {
      root.ActualThemeChanged += (_, _) => ApplyFlatTitleBar();
    }

    // Keep Paste availability in sync with the clipboard.
    Clipboard.ContentChanged += (_, _) => UpdateClipboardText();
    Activated += (_, _) => UpdateClipboardText();
    UpdateClipboardText();
  }

  private void ApplyFlatTitleBar() {
    var titleBar = AppWindow.TitleBar;
    var isDark = (Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark;
    var background = isDark ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);
    var foreground = isDark ? Colors.White : Colors.Black;
    var hover = isDark ? Color.FromArgb(255, 45, 45, 45) : Color.FromArgb(255, 229, 229, 229);

    titleBar.BackgroundColor = background;
    titleBar.InactiveBackgroundColor = background;
    titleBar.ForegroundColor = foreground;
    titleBar.InactiveForegroundColor = foreground;
    titleBar.ButtonBackgroundColor = background;
    titleBar.ButtonInactiveBackgroundColor = background;
    titleBar.ButtonForegroundColor = foreground;
    titleBar.ButtonInactiveForegroundColor = foreground;
    titleBar.ButtonHoverBackgroundColor = hover;
    titleBar.ButtonHoverForegroundColor = foreground;
    titleBar.ButtonPressedBackgroundColor = hover;
    titleBar.ButtonPressedForegroundColor = foreground;
  }

  private async void UpdateClipboardText() {
    try {
      var content = Clipboard.GetContent();
      ViewModel.ClipboardText = content.Contains(StandardDataFormats.Text) ? await content.GetTextAsync() : string.Empty;
    }
    catch {
      ViewModel.ClipboardText = string.Empty;
    }
  }

  private void SetWindowIcon() {
    var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
    var appWindow = AppWindow.GetFromWindowId(windowId);

    // Try multiple possible locations for the icon
    var possiblePaths = new[] {
      Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"),
      Path.Combine(AppContext.BaseDirectory, "app.ico")
    };

    foreach (var path in possiblePaths) {
      if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
        appWindow.SetIcon(path);
        break;
      }
    }
  }

  private void SearchButton_Click(object sender, RoutedEventArgs e) {
    // Toggle visibility if search box is already visible and empty
    if (ViewModel.IsSearchVisible && string.IsNullOrEmpty(ViewModel.SearchText)) {
      ViewModel.IsSearchVisible = false;
    }
    else {
      ViewModel.IsSearchVisible = true;
      SearchBox.Focus(FocusState.Programmatic);
    }
  }

  // Only hide on blur when search text is empty — keeps the filter visible
  // while an active search is in place.
  private void SearchBox_LostFocus(object sender, RoutedEventArgs e) {
    if (string.IsNullOrEmpty(ViewModel.SearchText)) {
      ViewModel.IsSearchVisible = false;
    }
  }

  private void ListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e) {
    if (e.Items.FirstOrDefault() is VariableViewModel variable) {
      // Don't allow dragging volatile variables between lists
      if (variable.Model.IsVolatile) {
        e.Cancel = true;
        return;
      }
      e.Data.Properties["VariableViewModel"] = variable;
      e.Data.Properties["SourceListView"] = sender;
      e.Data.RequestedOperation = DataPackageOperation.Move;
    }
  }

  private void ListView_DragEnter(object sender, DragEventArgs e) {
    // When a cross-list drag enters, disable AllowDrop on all nested path ListViews
    // to prevent the visual "spreading apart" effect
    if (e.DataView.Properties.TryGetValue("SourceListView", out var sourceObj) &&
        sourceObj is ListView sourceList &&
        !ReferenceEquals(sourceList, sender) &&
        sender is ListView targetList) {
      SetNestedListViewsAllowDrop(targetList, false);
    }
  }

  private void ListView_DragLeave(object sender, DragEventArgs e) {
    // Re-enable AllowDrop on nested path ListViews when cross-list drag leaves
    if (e.DataView.Properties.ContainsKey("SourceListView") && sender is ListView targetList) {
      SetNestedListViewsAllowDrop(targetList, true);
    }
  }

  private void ListView_DragOver(object sender, DragEventArgs e) {
    // Only allow drop if dragging from a different list and not volatile
    if (e.DataView.Properties.TryGetValue("SourceListView", out var sourceObj) &&
        sourceObj is ListView sourceList &&
        !ReferenceEquals(sourceList, sender) &&
        e.DataView.Properties.TryGetValue("VariableViewModel", out var varObj) &&
        varObj is VariableViewModel variable &&
        !variable.Model.IsVolatile) {

      // Prevent dropping deleted variables
      if (variable.Model.IsRemoved) {
        e.AcceptedOperation = DataPackageOperation.None;
        return;
      }

      // Can't overwrite a same-named volatile (read-only) variable on the target.
      var target = ReferenceEquals(sender, SystemListView) ? ViewModel.SystemVariables : ViewModel.UserVariables;
      if (target.Variables.Any(v => v.Model.IsVolatile && string.Equals(v.Name, variable.Name, StringComparison.OrdinalIgnoreCase))) {
        e.AcceptedOperation = DataPackageOperation.None;
        return;
      }

      // A same-named non-volatile variable on the target is overwritten on drop.
      e.AcceptedOperation = DataPackageOperation.Move;
      e.DragUIOverride.Caption = ReferenceEquals(sender, SystemListView) ? "Move to System" : "Move to User";
    }
    else {
      e.AcceptedOperation = DataPackageOperation.None;
    }
  }

  private void SystemListView_Drop(object sender, DragEventArgs e) {
    SetNestedListViewsAllowDrop(SystemListView, true);

    if (e.DataView.Properties.TryGetValue("VariableViewModel", out var varObj) &&
        varObj is VariableViewModel variable &&
        e.DataView.Properties.TryGetValue("SourceListView", out var sourceObj) &&
        ReferenceEquals(sourceObj, UserListView)) {
      // Remove-from-source + add-to-target is one logical move → one undo step.
      ViewModel.RunBatch(() => TransferVariable(variable, ViewModel.UserVariables, ViewModel.SystemVariables));
      // Re-render after the drop settles so the moved item shows in sorted position, not where it landed.
      DispatcherQueue.TryEnqueue(ViewModel.SystemVariables.ResyncFilteredVariables);
    }
  }

  private void UserListView_Drop(object sender, DragEventArgs e) {
    SetNestedListViewsAllowDrop(UserListView, true);

    if (e.DataView.Properties.TryGetValue("VariableViewModel", out var varObj) &&
        varObj is VariableViewModel variable &&
        e.DataView.Properties.TryGetValue("SourceListView", out var sourceObj) &&
        ReferenceEquals(sourceObj, SystemListView)) {
      ViewModel.RunBatch(() => TransferVariable(variable, ViewModel.SystemVariables, ViewModel.UserVariables));
      DispatcherQueue.TryEnqueue(ViewModel.UserVariables.ResyncFilteredVariables);
    }
  }

  private static void TransferVariable(VariableViewModel variable, VariableScopeViewModel source, VariableScopeViewModel target) {
    if (variable.Model.IsRemoved) {
      return;
    }

    // Can't overwrite a same-named volatile (read-only) variable on the target.
    var targetExisting = target.Variables.FirstOrDefault(v =>
      string.Equals(v.Name, variable.Name, StringComparison.OrdinalIgnoreCase));
    if (targetExisting?.Model.IsVolatile == true) {
      return;
    }

    // AddVariable overwrites an active target (data) or restores a deleted original in place, so a move and a
    // move-back return to the clean snapshot; RemoveVariable cleans up the source.
    source.RemoveVariable(variable);
    target.AddVariable(variable.Name, variable.Data, variable.Model.Type);
  }

  // Clicks through a tagged card show the per-variable menu; clicks in the gap fall through to the panel flyout.
  private void ListView_ContextRequested(UIElement sender, ContextRequestedEventArgs e) {
    var vm = FindVariable(e.OriginalSource as DependencyObject);
    if (vm is null) {
      return;
    }

    var menu = BuildCardContextMenu(vm);
    if (e.TryGetPosition(sender, out var point)) {
      menu.ShowAt((FrameworkElement)sender, new FlyoutShowOptions { Position = point });
    }
    else if (sender is FrameworkElement element) {
      menu.ShowAt(element);
    }

    e.Handled = true;
  }

  private static VariableViewModel? FindVariable(DependencyObject? source) {
    while (source is not null) {
      if (source is FrameworkElement { Tag: "VariableCard", DataContext: VariableViewModel vm }) {
        return vm;
      }
      source = VisualTreeHelper.GetParent(source);
    }
    return null;
  }

  private MenuFlyout BuildCardContextMenu(VariableViewModel vm) {
    var menu = new MenuFlyout();
    menu.Items.Add(CreateMenuItem("Copy", vm.CopyDataCommand, VirtualKey.C, enabled: true));
    menu.Items.Add(CreateMenuItem("Paste", vm.PasteDataCommand, VirtualKey.V, enabled: ViewModel.CanPasteValue && !vm.IsLocked));

    if (!vm.IsLocked) {
      menu.Items.Add(new MenuFlyoutSeparator());
      menu.Items.Add(CreateMenuItem("Toggle type", vm.ToggleTypeCommand, VirtualKey.T, enabled: true));
    }

    return menu;
  }

  private static MenuFlyoutItem CreateMenuItem(string text, ICommand command, VirtualKey acceleratorKey, bool enabled) {
    var item = new MenuFlyoutItem { Text = text, Command = command, IsEnabled = enabled };
    item.KeyboardAccelerators.Add(new KeyboardAccelerator { Key = acceleratorKey, Modifiers = VirtualKeyModifiers.Control });
    return item;
  }

  private static void SetNestedListViewsAllowDrop(DependencyObject parent, bool allowDrop) {
    foreach (var listView in FindNestedListViews(parent)) {
      listView.AllowDrop = allowDrop;
    }
  }

  private static IEnumerable<ListView> FindNestedListViews(DependencyObject parent) {
    var count = VisualTreeHelper.GetChildrenCount(parent);
    for (var i = 0; i < count; i++) {
      var child = VisualTreeHelper.GetChild(parent, i);
      if (child is ListView listView && listView.Name != "SystemListView" && listView.Name != "UserListView") {
        yield return listView;
      }
      foreach (var nested in FindNestedListViews(child)) {
        yield return nested;
      }
    }
  }

  private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData) {
    const int WM_GETMINMAXINFO = 0x0024;

    if (msg == WM_GETMINMAXINFO) {
      var dpi = GetDpiForWindow(hWnd);
      var scaleX = dpi / 96.0;
      var scaleY = dpi / 96.0;

      var info = Marshal.PtrToStructure<MINMAXINFO>(lParam);
      info.ptMinTrackSize.X = (int)(WindowConstants.MinWindowWidth * scaleX);
      info.ptMinTrackSize.Y = (int)(WindowConstants.MinWindowHeight * scaleY);
      Marshal.StructureToPtr(info, lParam, true);
    }

    return DefSubclassProc(hWnd, msg, wParam, lParam);
  }

  #region Win32 Interop

  private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);

  [LibraryImport("comctl32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, IntPtr uIdSubclass, IntPtr dwRefData);

  [LibraryImport("comctl32.dll", SetLastError = true)]
  private static partial IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

  [LibraryImport("user32.dll")]
  private static partial uint GetDpiForWindow(IntPtr hwnd);

  [StructLayout(LayoutKind.Sequential)]
  private struct POINT {
    public int X;
    public int Y;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct MINMAXINFO {
    public POINT ptReserved;
    public POINT ptMaxSize;
    public POINT ptMaxPosition;
    public POINT ptMinTrackSize;
    public POINT ptMaxTrackSize;
  }

  #endregion
}
