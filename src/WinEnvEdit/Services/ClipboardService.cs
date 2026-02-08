using System;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;

namespace WinEnvEdit.Services;

public class ClipboardService : IClipboardService {
  public void SetText(string text) {
    var dataPackage = new DataPackage();
    dataPackage.SetText(text);
    Clipboard.SetContent(dataPackage);
  }

  public async Task<string?> GetText() {
    var dataPackageView = Clipboard.GetContent();
    if (dataPackageView.Contains(StandardDataFormats.Text)) {
      return await dataPackageView.GetTextAsync();
    }
    return null;
  }
}
