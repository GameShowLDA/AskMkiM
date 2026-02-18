using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  internal static class ProtocolModelExtensions
  {
    public static void AddErrors(this ProtocolModel protocolModel, string commandKey, List<ShowMessageModel> errors)
    {
      if (errors == null || errors.Count == 0)
      {
        return;
      }

      if (protocolModel.Errors.TryGetValue(commandKey, out var existing))
      {
        existing.AddRange(errors);
        return;
      }

      protocolModel.Errors[commandKey] = new List<ShowMessageModel>(errors);
    }

    public static void AddInfo(this ProtocolModel protocolModel, string commandKey, List<ShowMessageModel> info)
    {
      if (info == null || info.Count == 0)
      {
        return;
      }

      if (protocolModel.Info.TryGetValue(commandKey, out var existing))
      {
        existing.AddRange(info);
        return;
      }

      protocolModel.Info[commandKey] = new List<ShowMessageModel>(info);
    }
  }
}
