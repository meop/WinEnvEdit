using System.Threading.Tasks;

namespace WinEnvEdit.Services;

public interface IDialogService {
  public Task<bool> ShowConfirmation(string title, string message, string primaryButtonText = "Okay");
  public Task<string?> PickOpenFile(string extension);
  public Task<string?> PickSaveFile(string description, string extension, string suggestedFileName);
  public Task ShowError(string title, string message, string detailedError);
}
