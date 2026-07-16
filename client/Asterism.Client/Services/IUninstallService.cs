using Asterism.Client.Models;
using Asterism.Shared.Models;

namespace Asterism.Client.Services;

public interface IUninstallService
{
    void Uninstall(ToolEntry tool, InstalledToolRecord record);
}
