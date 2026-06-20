using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using Windows.Graphics;

using WinEnvEdit.Core.Constants;

using WinRT.Interop;

namespace WinEnvEdit;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application {
  /// <summary>
  /// Initializes the singleton application object.  This is the first line of authored code
  /// executed, and as such is the logical equivalent of main() or WinMain().
  /// </summary>
  public App() => InitializeComponent();

  /// <summary>
  /// Invoked when application is launched.
  /// </summary>
  /// <param name="args">Details about the launch request and process.</param>
  protected override void OnLaunched(LaunchActivatedEventArgs args) {
    var window = new MainWindow();
    SetDefaultWindowSize(window);
    window.Activate();
  }

  /// <summary>
  /// Sets the default window size based on available screen space.
  /// Chooses the largest resolution (1920x1080, 1600x900, or 1280x720) that fits within 80% of the screen.
  /// </summary>
  private static void SetDefaultWindowSize(Window window) {
    // Get window handle
    var hWnd = WindowNative.GetWindowHandle(window);
    var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
    var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);

    // Define 16:9 resolutions in order of preference
    var resolutions = new (int Width, int Height)[] {
      (Width: 1920, Height: 1080),
      (Width: 1600, Height: 900),
      (Width: WindowConstants.MinWindowWidth, Height: WindowConstants.MinWindowHeight)
    };

    // Get AppWindow and set minimum size constraints
    var appWindow = AppWindow.GetFromWindowId(windowId);

    // Set minimum window size (cannot resize smaller than WindowConstants.MinWindowWidth x WindowConstants.MinWindowHeight)
    if (appWindow.Presenter is OverlappedPresenter presenter) {
      presenter.IsResizable = true;
      presenter.IsMaximizable = true;
      presenter.IsMinimizable = true;
    }

    if (displayArea is not null) {
      var workArea = displayArea.WorkArea;
      var screenWidth = workArea.Width;
      var screenHeight = workArea.Height;

      // Choose the largest resolution that fits comfortably on the screen (80% of working area)
      var targetWidth = screenWidth * 0.8;
      var targetHeight = screenHeight * 0.8;

      foreach (var (Width, Height) in resolutions) {
        if (Width <= targetWidth && Height <= targetHeight) {
          appWindow.Resize(new SizeInt32(Width, Height));
          break;
        }
      }
    }
  }
}
