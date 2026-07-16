using Asterism.Client.Models;
using Asterism.Shared.Models;

namespace Asterism.Client.Services;

public interface ILaunchService
{
    void Launch(ToolEntry tool, InstalledToolRecord record);
}
