using ToolPortal.Client.Models;
using ToolPortal.Shared.Models;

namespace ToolPortal.Client.Services;

public interface ILaunchService
{
    void Launch(ToolEntry tool, InstalledToolRecord record);
}
