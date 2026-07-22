using ToolPortal.Client.Models;
using ToolPortal.Shared.Models;

namespace ToolPortal.Client.Services;

public interface IUninstallService
{
    void Uninstall(ToolEntry tool, InstalledToolRecord record);
}
