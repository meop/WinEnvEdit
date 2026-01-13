using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;

using WinEnvEdit.ViewModels;

namespace WinEnvEdit;

public sealed partial class MainWindow : Window
{
    private const int _minWindowWidth = 1280;
    private const int _minWindowHeight = 720;

    private readonly IntPtr _hwnd;
    private readonly SUBCLASSPROC? _wndProcDelegate;

    public MainWindowViewModel ViewModel { get; }

    public MainWindow()
    {
        ViewModel = new MainWindowViewModel();
        InitializeComponent();

        // Set minimum window size at Win32 level to prevent flickering
        _hwnd = WindowNative.GetWindowHandle(this);
        _wndProcDelegate = new SUBCLASSPROC(WndProc);
        SetWindowSubclass(_hwnd, _wndProcDelegate, 0, IntPtr.Zero);
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
    {
        const int WM_GETMINMAXINFO = 0x0024;

        if (msg == WM_GETMINMAXINFO)
        {
            var dpi = GetDpiForWindow(hWnd);
            var scaleX = dpi / 96.0;
            var scaleY = dpi / 96.0;

            MINMAXINFO info = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            info.ptMinTrackSize.X = (int)(_minWindowWidth * scaleX);
            info.ptMinTrackSize.Y = (int)(_minWindowHeight * scaleY);
            Marshal.StructureToPtr(info, lParam, true);
        }

        return DefSubclassProc(hWnd, msg, wParam, lParam);
    }

    #region Win32 Interop

    private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, IntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    #endregion
}
