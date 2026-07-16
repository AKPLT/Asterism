using Asterism.Client.Models;
using Asterism.Shared.Models;

namespace Asterism.Client.Services;

public enum UninstallOutcome
{
    Removed,
    ManualActionRequired
}

public interface IUninstallService
{
    UninstallOutcome Uninstall(ToolEntry tool, InstalledToolRecord record);
}
