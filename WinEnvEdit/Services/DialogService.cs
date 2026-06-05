using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

using WinEnvEdit.Helpers;

namespace WinEnvEdit.Services;

public class DialogService(Window window) : IDialogService {

  public async Task<bool> ShowConfirmation(string title, string message, string primaryButtonText = "Okay") {
    if (window?.Content?.XamlRoot == null) {
      return false;
    }

    var messageText = new TextBlock {
      Text = message,
      TextWrapping = TextWrapping.Wrap,
      VerticalAlignment = VerticalAlignment.Top,
      HorizontalAlignment = HorizontalAlignment.Left,
      TextAlignment = TextAlignment.Left,
    };

    var contentPanel = DialogHelper.CreateDialogPanel([messageText]);
    var dialog = DialogHelper.CreateStandardDialog(window.Content.XamlRoot, title, contentPanel, primaryButtonText, "Cancel");
    var result = await dialog.ShowAsync();
    return result == ContentDialogResult.Primary;
  }

  public async Task<string?> PickOpenFile(string extension) {
    var openPicker = new FileOpenPicker(window.AppWindow.Id);
    openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
    openPicker.FileTypeFilter.Add(extension);

    var file = await openPicker.PickSingleFileAsync();
    return file?.Path;
  }

  public async Task<string?> PickSaveFile(string description, string extension, string suggestedFileName) {
    var savePicker = new FileSavePicker(window.AppWindow.Id);
    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
    // Explicit List<string> (not a collection expression) — required for the WinRT picker under AOT.
    savePicker.FileTypeChoices.Add(description, new List<string> { extension });
    savePicker.SuggestedFileName = suggestedFileName;

    var file = await savePicker.PickSaveFileAsync();
    return file?.Path;
  }

  public async Task ShowError(string title, string message, string detailedError) {
    if (window?.Content?.XamlRoot == null) {
      return;
    }

    var contentPanel = DialogHelper.CreateDialogPanel([
      new TextBlock {
        Style = Application.Current.Resources["DialogErrorHeaderStyle"] as Style,
        Text = message,
      },
      new TextBox {
        Style = Application.Current.Resources["DialogErrorDetailStyle"] as Style,
        Text = detailedError,
      },
    ]);

    var dialog = DialogHelper.CreateStandardDialog(window.Content.XamlRoot, title, contentPanel, closeButtonText: "Close");
    await dialog.ShowAsync();
  }
}
