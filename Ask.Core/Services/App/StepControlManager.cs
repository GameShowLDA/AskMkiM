using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;

namespace Ask.Core.Services.App
{
  /// <summary>
  /// Источник активации пошагового режима.
  /// </summary>
  public enum StepModeActivationSource
  {
    Unknown = 0,
    ManualOrConfig = 1,
    Breakpoint = 2
  }

  /// <summary>
  /// Статический менеджер для управления режимами пошагового выполнения алгоритма (F10/F11)
  /// и вложенными блоками команд.
  /// Используется для организации поведения "шаг вглубь" (F11), "шаг поверх блока" (F10)
  /// и отслеживания текущего состояния при выполнении программы контроля.
  /// </summary>
  public static class StepControlManager
  {
    /// <summary>
    /// Флаг, указывающий, активен ли пошаговый режим.
    /// Если <c>true</c>, выполнение алгоритма будет останавливаться на каждом шаге.
    /// </summary>
    public static bool StepMode => _stepMode;

    /// <summary>
    /// Внутреннее поле для хранения состояния пошагового режима.
    /// </summary>
    private static bool _stepMode;

    /// <summary>
    /// Внутренний флаг запроса обхода пошагового режима (например, после выхода из него).
    /// </summary>
    private static bool _stepBypassRequested;

    /// <summary>
    /// Источник активации текущего пошагового режима.
    /// </summary>
    private static StepModeActivationSource _activationSource = StepModeActivationSource.Unknown;

    /// <summary>
    /// Контекст команды, которая активировала пошаговый режим через точку останова.
    /// </summary>
    private static IExecutionCommandInfo? _breakpointCommandInfo;

    /// <summary>
    /// Возвращает <c>true</c>, если после отключения пошагового режима требуется
    /// продолжить выполнение без остановок.
    /// </summary>
    public static bool StepBypassRequested => _stepBypassRequested;

    /// <summary>
    /// Флаг, определяющий тип пошагового выполнения.
    /// <para><c>true</c> — шаг вглубь (F11).</para>
    /// <para><c>false</c> — шаг поверх блока (F10).</para>
    /// </summary>
    public static bool IsStepInto { get; set; } = false;

    /// <summary>
    /// Флаг, указывающий, что выполнение находится внутри вложенного блока команд.
    /// Используется для правильной обработки режима F10/F11.
    /// </summary>
    public static bool InsideBlock { get; private set; } = false;

    /// <summary>
    /// Флаг F10 до следующей команды для программ контроля.
    /// Пока <c>true</c>, ожидание шага пропускается до момента появления
    /// заголовка следующей команды выполнения ПК.
    /// </summary>
    public static bool StepOverUntilNextControlCommand { get; private set; }

    /// <summary>
    /// Источник активации текущего пошагового режима.
    /// </summary>
    public static StepModeActivationSource ActivationSource => _activationSource;

    /// <summary>
    /// Команда, из-за которой был активирован пошаговый режим через брейкпоинт.
    /// </summary>
    public static IExecutionCommandInfo? BreakpointCommandInfo => _breakpointCommandInfo;

    /// <summary>
    /// Возвращает <c>true</c>, если пошаговый режим активен и был включен именно из брейкпоинта.
    /// </summary>
    public static bool IsBreakpointStepModeActive =>
      _stepMode &&
      _activationSource == StepModeActivationSource.Breakpoint &&
      _breakpointCommandInfo != null;

    /// <summary>
    /// Устанавливает состояние входа во вложенный блок команд.
    /// Обычно вызывается перед отображением сообщения (<c>ShowMessageAsync</c>).
    /// </summary>
    public static void EnterBlock()
    {
      InsideBlock = true;
    }

    /// <summary>
    /// Принудительно завершает состояние нахождения внутри блока команд.
    /// Обычно используется при выходе из блока вручную.
    /// </summary>
    public static void ExitBlock() => InsideBlock = false;

    /// <summary>
    /// Выполняет полный сброс всех состояний пошагового режима.
    /// Обычно вызывается после завершения выполнения алгоритма.
    /// </summary>
    public static void Reset()
    {
      IsStepInto = false;
      InsideBlock = false;
      StepOverUntilNextControlCommand = false;
      _activationSource = StepModeActivationSource.Unknown;
      _breakpointCommandInfo = null;
    }

    /// <summary>
    /// Переводит пошаговый режим в F10 до начала следующей команды ПК.
    /// </summary>
    public static void RequestStepOverUntilNextControlCommand()
    {
      IsStepInto = false;
      StepOverUntilNextControlCommand = true;
    }

    /// <summary>
    /// Переводит режим в F11 шаг вглубь и сбрасывает режим F10-обхода.
    /// </summary>
    public static void SetStepIntoMode()
    {
      IsStepInto = true;
      StepOverUntilNextControlCommand = false;
    }

    /// <summary>
    /// Сбрасывает флаг ожидания следующей команды для F10.
    /// </summary>
    public static void CompleteStepOverUntilNextControlCommand()
    {
      StepOverUntilNextControlCommand = false;
    }

    /// <summary>
    /// Инициализирует состояние пошагового режима при старте приложения.
    /// Считывает настройки из <c>ExecutionConfig</c> и подписывается на событие
    /// <c>StepByStepModeChanged</c> для динамического изменения состояния.
    /// </summary>
    public static async Task InitializeAsync()
    {
      _stepMode = ExecutionConfig.GetIsStepByStepModeEnabled();
      if (_stepMode)
      {
        EnableStepMode(true);
      }
      else
      {
        DisableStepMode();
      }
    }

    /// <summary>
    /// Включает пошаговый режим как ручной/конфигурационный.
    /// </summary>
    /// <param name="isStepInto">
    /// <c>true</c> — шаг вглубь (F11),
    /// <c>false</c> — шаг поверх блока (F10).
    /// </param>
    public static void EnableStepMode(bool isStepInto)
    {
      EnableStepModeCore(isStepInto, StepModeActivationSource.ManualOrConfig);
    }

    /// <summary>
    /// Включает пошаговый режим от точки останова и сохраняет контекст команды.
    /// Если режим уже включен не из брейкпоинта, источник не меняется.
    /// </summary>
    /// <param name="commandInfo">Контекст команды, на которой сработал брейкпоинт.</param>
    /// <param name="isStepInto">
    /// <c>true</c> — шаг вглубь (F11),
    /// <c>false</c> — шаг поверх блока (F10).
    /// </param>
    public static void EnableStepModeByBreakpoint(IExecutionCommandInfo commandInfo, bool isStepInto = true)
    {
      if (!_stepMode)
      {
        EnableStepModeCore(isStepInto, StepModeActivationSource.Breakpoint, commandInfo);
        return;
      }

      IsStepInto = isStepInto;

      if (_activationSource == StepModeActivationSource.Breakpoint)
      {
        _breakpointCommandInfo = commandInfo;
      }
    }

    /// <summary>
    /// Включает пошаговый режим с указанием источника активации.
    /// </summary>
    /// <param name="isStepInto">
    /// <c>true</c> — шаг вглубь (F11),
    /// <c>false</c> — шаг поверх блока (F10).
    /// </param>
    /// <param name="activationSource">Источник активации пошагового режима.</param>
    /// <param name="breakpointCommandInfo">
    /// Контекст команды для источника <see cref="StepModeActivationSource.Breakpoint"/>.
    /// </param>
    private static void EnableStepModeCore(
      bool isStepInto,
      StepModeActivationSource activationSource,
      IExecutionCommandInfo? breakpointCommandInfo = null)
    {
      _stepMode = true;
      IsStepInto = isStepInto;
      StepOverUntilNextControlCommand = !isStepInto;
      _stepBypassRequested = false;
      _activationSource = activationSource;
      _breakpointCommandInfo = activationSource == StepModeActivationSource.Breakpoint
        ? breakpointCommandInfo
        : null;

      ExecutionEventAdapter.RaiseStepByStepModeChanged(true);
    }

    /// <summary>
    /// Отключает пошаговый режим и устанавливает флаг обхода
    /// для продолжения выполнения без остановок.
    /// </summary>
    public static void DisableStepMode()
    {
      _stepMode = false;
      _stepBypassRequested = true;
      StepOverUntilNextControlCommand = false;
      _activationSource = StepModeActivationSource.Unknown;
      _breakpointCommandInfo = null;
      ExecutionEventAdapter.RaiseStepByStepModeChanged(false);
    }

    /// <summary>
    /// Статический конструктор, выполняющий инициализацию
    /// состояния пошагового режима при первом обращении к классу.
    /// </summary>
    static StepControlManager()
    {
      InitializeAsync().ConfigureAwait(true);
    }
  }
}

