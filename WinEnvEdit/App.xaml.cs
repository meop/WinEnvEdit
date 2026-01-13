using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace WinEnvEdit;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        SetDefaultWindowSize(_window);
        _window.Activate();
    }

    /// <summary>
    /// Sets the default window size based on available screen space.
    /// Chooses the largest resolution (1920x1080, 1600x900, or 1280x720) that fits within 80% of the screen.
    /// </summary>
    private static void SetDefaultWindowSize(Window window)
    {
        // Get the window handle
        var hWnd = WindowNative.GetWindowHandle(window);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);

        // Define 16:9 resolutions in order of preference
        (int Width, int Height)[] resolutions = new[]
        {
            (Width: 1920, Height: 1080),
            (Width: 1600, Height: 900),
            (Width: 1280, Height: 720)
        };

        // Get the AppWindow and set minimum size constraints
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // Set minimum window size (cannot resize smaller than 1280x720)
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
        }

        if (displayArea != null)
        {
            RectInt32 workArea = displayArea.WorkArea;
            var screenWidth = workArea.Width;
            var screenHeight = workArea.Height;

            // Choose the largest resolution that fits comfortably on the screen (80% of working area)
            var targetWidth = screenWidth * 0.8;
            var targetHeight = screenHeight * 0.8;

            foreach ((int Width, int Height) resolution in resolutions)
            {
                if (resolution.Width <= targetWidth && resolution.Height <= targetHeight)
                {
                    appWindow.Resize(new SizeInt32(resolution.Width, resolution.Height));
                    break;
                }
            }
        }
    }
}
