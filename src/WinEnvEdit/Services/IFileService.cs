using System.Collections.Generic;
using System.Threading.Tasks;

using WinEnvEdit.Models;

namespace WinEnvEdit.Services;

public interface IFileService {
  public Task ExportToFile(string filePath, IEnumerable<EnvironmentVariable> variables);
  public Task<IEnumerable<EnvironmentVariable>> ImportFromFile(string filePath);
}
