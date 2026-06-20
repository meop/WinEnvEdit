namespace WinEnvEdit.Services;

public interface IDialogService {
  // primaryButtonText defaults to null so the implementation can supply a localized "Okay".
  public Task<bool> ShowConfirmation(string title, string message, string? primaryButtonText = null);
  public Task<string?> PickOpenFile(string extension);
  public Task<string?> PickSaveFile(string description, string extension, string suggestedFileName);
  public Task ShowError(string title, string message, string detailedError);
}
