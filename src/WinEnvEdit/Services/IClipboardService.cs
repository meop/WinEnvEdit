using System.Threading.Tasks;

namespace WinEnvEdit.Services;

public interface IClipboardService {
  public void SetText(string text);
  public Task<string?> GetText();
}
