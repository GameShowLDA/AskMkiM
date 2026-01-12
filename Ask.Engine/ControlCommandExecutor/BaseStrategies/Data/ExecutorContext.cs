using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  internal abstract class ExecutorContext
  {

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

    public string UnitMnemonic { get; set; }
    public string Unit { get; set; }

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
    }

    public T CreateChild<T>() where T : ExecutorContext, new()
    {
      var child = new T();
      child.CopyFrom(this);
      return child;
    }
  }
}
