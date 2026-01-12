using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Reflection;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// Контекст выполнения команды, содержащий модель команды и инструменты вывода.
  /// </summary>
  public class CommandExecutionContext
  {
    public BaseCommandModel Command { get; }
    public IUserInteractionService Console { get; }
    public ITextEditorAdapter TranslationControl { get; }

    public CommandExecutionManager CommandExecutionManager { get; }


    /// <summary>
    /// Делегат для перехода к команде по номеру (метке). 
    /// Заполняется менеджером выполнения команд.
    /// </summary>
    public Action<string> JumpToCommandNumber { get; set; }

    /// <summary>
    /// Дополнительные данные, общие между исполнителями.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    public string? OpkFilePath { get; set; }


    public CommandExecutionContext(CommandExecutionManager commandExecutionManager, BaseCommandModel command, IUserInteractionService console, ITextEditorAdapter editorAdapter, string opkFileName)
    {
      Command = command;
      Console = console;
      TranslationControl = editorAdapter;
      CommandExecutionManager = commandExecutionManager;
      OpkFilePath = opkFileName;
    }

    /// <summary>
    /// Определяет тип измерительного прибора для данной команды по атрибуту.
    /// Если атрибут отсутствует — возвращает MeasurementDevice.None.
    /// </summary>
    public MeasurementDevice GetDeviceForCommand(BaseCommandModel command)
    {
      var type = command.GetType();
      var attr = type.GetCustomAttribute<MeasurementDeviceAttribute>();
      return attr?.Device ?? MeasurementDevice.None;
    }

    /// <summary>
    /// Возвращает список всех уникальных измерительных приборов,
    /// которые используются в текущей программе контроля.
    /// </summary>
    public List<MeasurementDevice> GetUniqueDevices()
    {
      return CommandExecutionManager.CommandsToExecute
          .Select(cmd => GetDeviceForCommand(cmd))
          .Distinct()
          .ToList();
    }

    public List<MeasurementDevice> GetUniqueMeasurementDevices()
    {
      return CommandExecutionManager.CommandsToExecute
          .Select(cmd => GetDeviceForCommand(cmd))
          .Where(d => d != MeasurementDevice.None)
          .Distinct()
          .ToList();
    }
  }
}
