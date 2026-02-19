using Ask.Core.Services.Errors.Translation;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  public static class CheckPoints
  {
    public static RmCommandModel CheckRm(BaseCommandModel model ,int numberLine, string commandNumber, string mnemonic)
    {
      var rmCommandModel = CommandsModel.GetRMModel();

      if (rmCommandModel == null)
      {
        LogError($"Команда РМ не найдена");
        model.Errors.Add(EhtErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
      }
      return rmCommandModel;
    }
  }
}
