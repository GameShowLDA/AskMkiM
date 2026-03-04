using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using System.Reflection;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// Контекст выполнения команды, содержащий модель команды и инструменты вывода.
  /// </summary>
  public class CommandExecutionContext
  {
    /// <summary>
    /// Модель текущей выполняемой команды.
    /// </summary>
    public BaseCommandModel Command { get; }

    /// <summary>
    /// Сервис взаимодействия с пользователем,
    /// используемый для вывода сообщений и запросов.
    /// </summary>
    public IUserInteractionService Console { get; }

    /// <summary>
    /// Адаптер текстового редактора,
    /// используемый для подсветки, навигации и отображения команд.
    /// </summary>
    public ITextEditorAdapter TranslationControl { get; }

    /// <summary>
    /// Менеджер выполнения команд,
    /// управляющий общим процессом исполнения программы контроля.
    /// </summary>
    public CommandExecutionManager CommandExecutionManager { get; }

    /// <summary>
    /// Делегат перехода к команде по её номеру (метке).
    /// Заполняется менеджером выполнения команд и используется для реализации переходов и циклов.
    /// </summary>
    public Action<string> JumpToCommandNumber { get; set; }

    /// <summary>
    /// Коллекция дополнительных данных,
    /// общих между исполн
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    /// <summary>
    /// Путь к файлу управляющей программы (ОПК),
    /// используемый при выполнении команды.
    /// </summary>
    public string? OpkFilePath { get; set; }

    /// <summary>
    /// Указывает, что текущая команда была вызвана
    /// другой командой, а не инициирована напрямую внешним источником выполнения.
    /// </summary>
    public bool IsInvokedByAnotherCommand { get; set; }

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

    /// <summary>
    /// Возвращает список всех уникальных измерительных приборов, используемых в текущей программе контроля,
    /// исключая значение <see cref="MeasurementDevice.None"/>.
    /// </summary>
    /// <returns>
    /// Список уникальных измерительных приборов, реально задействованных в программе контроля.
    /// </returns>
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
