using Asterism.Client.Models;

namespace Asterism.Client.Services;

public interface ILaunchService
{
    void Launch(ToolEntry tool, InstalledToolRecord record);
}
