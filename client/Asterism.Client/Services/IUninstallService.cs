using Asterism.Client.Models;

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
