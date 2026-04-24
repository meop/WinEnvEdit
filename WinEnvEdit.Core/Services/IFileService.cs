using WinEnvEdit.Core.Models;

namespace WinEnvEdit.Core.Services;

public interface IFileService {
  public Task ExportToFile(string filePath, IEnumerable<EnvironmentVariableModel> variables);
  public Task<IEnumerable<EnvironmentVariableModel>> ImportFromFile(string filePath);
  public Task ExportToStream(Stream stream, IEnumerable<EnvironmentVariableModel> variables);
  public Task<IEnumerable<EnvironmentVariableModel>> ImportFromStream(Stream stream);
}
