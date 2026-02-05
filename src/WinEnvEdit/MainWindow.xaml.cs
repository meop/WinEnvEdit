using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Windows.ApplicationModel.DataTransfer;

using WinEnvEdit.Helpers;
using WinEnvEdit.ViewModels;

using WinRT.Interop;

namespace WinEnvEdit;

public sealed partial class MainWindow : Window {
  private readonly IntPtr hwnd;
  private readonly SUBCLASSPROC? wndProcDelegate;

  public MainWindowViewModel ViewModel { get; }

  public MainWindow() {
    var environmentService = new Services.EnvironmentService();
    var fileService = new Services.FileService();
    var stateSnapshotService = new Services.StateSnapshotService();
    var undoRedoService = new Services.UndoRedoService();
    ViewModel = new MainWindowViewModel(environmentService, this, fileService, stateSnapshotService, undoRedoService);
    InitializeComponent();

    // Set minimum window size at Win32 level to prevent flickering
    hwnd = WindowNative.GetWindowHandle(this);
    wndProcDelegate = WndProc;
    SetWindowSubclass(hwnd, wndProcDelegate, 0, IntPtr.Zero);
  }

  private void SearchButton_Click(object sender, RoutedEventArgs e) {
    ViewModel.IsSearchVisible = true;
    SearchBox.Focus(FocusState.Programmatic);
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

      // Prevent dropping if name already exists in target list
      var targetVariables = ReferenceEquals(sender, SystemListView)
        ? ViewModel.SystemVariables.FilteredVariables
        : ViewModel.UserVariables.FilteredVariables;

      var nameExists = targetVariables.Any(v =>
        string.Equals(v.Name, variable.Name, StringComparison.OrdinalIgnoreCase));

      if (nameExists) {
        e.AcceptedOperation = DataPackageOperation.None;
        return;
      }

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
      TransferVariable(variable, ViewModel.UserVariables, ViewModel.SystemVariables);
    }
  }

  private void UserListView_Drop(object sender, DragEventArgs e) {
    SetNestedListViewsAllowDrop(UserListView, true);

    if (e.DataView.Properties.TryGetValue("VariableViewModel", out var varObj) &&
        varObj is VariableViewModel variable &&
        e.DataView.Properties.TryGetValue("SourceListView", out var sourceObj) &&
        ReferenceEquals(sourceObj, SystemListView)) {
      TransferVariable(variable, ViewModel.SystemVariables, ViewModel.UserVariables);
    }
  }

  private static void TransferVariable(VariableViewModel variable, VariableScopeViewModel source, VariableScopeViewModel target) {
    if (variable.Model.IsRemoved) {
      return;
    }

    var nameExists = target.FilteredVariables.Any(v =>
      string.Equals(v.Name, variable.Name, StringComparison.OrdinalIgnoreCase));

    if (!nameExists) {
      source.RemoveVariable(variable);
      target.AddVariable(variable.Name, variable.Data, variable.Model.Type);
    }
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
