using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Atributes;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;
using Ask.Engine.ControlCommandExecutor.Execution;
using System.Reflection;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  internal abstract class ExecutorContext
  {
    /// <summary>
    /// Тип измерительной команды, определяющий режим или метод выполнения измерения.
    /// Используется для выбора логики обработки параметров и запуска соответствующего алгоритма.
    /// </summary>
    internal MeasurementTypeCommand TypeCommand { get; set; }

    /// <summary>
    /// Модель схемы подключения, используемая для выполнения измерения.
    /// Определяет точки, пары и соединения, участвующие в текущем методе.
    /// </summary>
    internal SchemeModel SchemeModel { get; set; }

    /// <summary>
    /// Менеджер выполнения команд, обеспечивающий координацию выполнения,
    /// обработку ошибок и последовательность выполнения операций.
    /// </summary>
    internal CommandExecutionManager CommandManager { get; set; }

    /// <summary>
    /// Модель команды, содержащая параметры,
    /// структуру и настройки текущей команды измерения.
    /// </summary>
    internal BaseCommandModel CommandModel { get; set; }

    /// <summary>
    /// Сервис пользовательских сообщений, используемый для отображения
    /// информационных сообщений, предупреждений и ошибок в процессе выполнения измерения.
    /// </summary>
    internal IUserInteractionService MessageService { get; set; }

    /// <summary>
    /// Номинальное значение, используемое при выполнении измерения
    /// или сравнении результата с заданными допусками.
    /// </summary>
    internal double Value { get; set; }

    /// <summary>
    /// Нижний предел допустимого значения измеряемого параметра.
    /// Применяется для проверки результата после выполнения измерения.
    /// </summary>
    public double LowerLimit { get; set; }

    /// <summary>
    /// Верхний предел допустимого значения измеряемого параметра.
    /// Представлен в текстовом виде, если значение содержит специальные обозначения
    /// или форматируется в нестандартной форме.
    /// </summary>
    public double HigherLimit { get; set; }

    /// <summary>
    /// Указывает, что текущая команда была вызвана другой командой,
    /// а не инициирована напрямую внешним источником.
    /// </summary>
    public bool IsInvokedByAnotherCommand { get; set; }

    /// <summary>
    /// Собственное сопротивление релейного коммутационного модуля,
    /// вносимое в цепь при прохождении сигнала через его коммутируемый путь.
    /// Значение выражено в единицах, указанных в <see cref="Unit"/>.
    /// </summary>
    public double InternalResistance { get; set; } = 0;

    /// <summary>
    /// Мнемоническое обозначение измеряемой физической величины
    /// Используется для краткой идентификации параметра.
    /// </summary>
    public string UnitMnemonic { get; set; }

    /// <summary>
    /// Единица измерения параметра (например: Ом, В, А).
    /// Соответствует текущей конфигурации измерительной подсистемы.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// Признак переполюсовки точек (инвертированной полярности) в текущей конфигурации (Ключ И).
    /// <c>true</c> — если полярность точек инвертирована; иначе <c>false</c>.
    /// </summary>
    public bool IsPolarityReversed { get; set; }

    /// <summary>
    /// Атрибут вывода информации в протокол и на печать (Ключ Д).
    /// </summary>
    public bool IsProtocolAttribute { get; set; }

    internal ExecutorContext() { }
    internal ExecutorContext(
      CommandExecutionContext context,
      BaseCommandModel command,
      IHasScheme hasScheme,
      double value = 0,
      double lowerLimit = 0,
      double higherLimit = 0)
    {
      TypeCommand = command.TypeCommand;

      var member = typeof(MeasurementTypeCommand)
        .GetMember(TypeCommand.ToString())
        .FirstOrDefault();

      var attr = member?
        .GetCustomAttribute<CommandDisplayInfoAttribute>();

      if (attr == null)
        throw new InvalidOperationException(
          $"CommandDisplayInfoAttribute not found for {TypeCommand}");

      SchemeModel = hasScheme.Scheme;
      CommandManager = context.CommandExecutionManager;
      CommandModel = command;
      MessageService = context.Console;
      Value = value;
      LowerLimit = lowerLimit;
      HigherLimit = higherLimit;

      Unit = attr.Unit;
      UnitMnemonic = attr.Symbol.ToString();
    }
    protected void CopyFrom(ExecutorContext other)
    {
      SchemeModel = other.SchemeModel;
      CommandManager = other.CommandManager;
      CommandModel = other.CommandModel;
      MessageService = other.MessageService;
      Value = other.Value;
      LowerLimit = other.LowerLimit;
      HigherLimit = other.HigherLimit;
      Unit = other.Unit;
      UnitMnemonic = other.UnitMnemonic;
      TypeCommand = other.TypeCommand;
      IsInvokedByAnotherCommand = other.IsInvokedByAnotherCommand;
      IsPolarityReversed = other.IsPolarityReversed;
      IsProtocolAttribute = other.IsProtocolAttribute;
    }

    public T CreateChild<T>() where T : ExecutorContext, new()
    {
      var child = new T();
      child.CopyFrom(this);
      return child;
    }
  }
}
