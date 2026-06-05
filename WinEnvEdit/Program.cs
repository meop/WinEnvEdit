using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using WinEnvEdit.Core.Services;

using WinRT;

namespace WinEnvEdit;

public static class Program {
  [STAThread]
  private static int Main(string[] args) {
    // Headless mode: an elevated relaunch applies system variables and exits without starting the UI.
    if (args.Length >= 2 && args[0] == EnvironmentService.ElevatedApplyArg) {
      return EnvironmentService.ApplySystemVariablesFromFile(args[1]);
    }

    ComWrappersSupport.InitializeComWrappers();
    Application.Start(p => {
      var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
      SynchronizationContext.SetSynchronizationContext(context);
      _ = new App();
    });

    return 0;
  }
}
