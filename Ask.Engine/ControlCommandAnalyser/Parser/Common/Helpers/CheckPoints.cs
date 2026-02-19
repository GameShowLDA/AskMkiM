using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  /// <summary>
  /// Класс для проверки наличия контрольных точек (РМ).
  /// </summary>
  public static class CheckPoints
  {
    /// <summary>
    /// Проверяет наличие модели команды РМ и возвращает её.
    /// </summary>
    /// <param name="model">Текущая модель команды.</param>
    /// <param name="numberLine">Номер строки.</param>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <returns>
    /// Модель команды РМ, если она найдена; иначе <c>null</c>.
    /// </returns>
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
