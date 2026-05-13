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
  /// Содержит состояние и логику управления пошаговым выполнением (F10/F11)
  /// во время исполнения сценария.
  /// Используется для реализации режимов "шаг с заходом" (F11), "шаг с обходом" (F10)
  /// и временного приостановления выполнения при достижении точки останова.
  /// </summary>
  public static class StepControlManager
  {
    /// <summary>
    /// Флаг, показывающий, включён ли пошаговый режим.
    /// Если <c>true</c>, выполнение сценария приостанавливается после каждого шага.
    /// </summary>
    public static bool StepMode => _stepMode;

    /// <summary>
    /// Внутренний флаг состояния пошагового режима.
    /// </summary>
    private static bool _stepMode;

    /// <summary>
    /// Внутренний флаг запроса пропуска пошагового режима (например, после выхода из него).
    /// </summary>
    private static bool _stepBypassRequested;

    /// <summary>
    /// Источник текущей активации пошагового режима.
    /// </summary>
    private static StepModeActivationSource _activationSource = StepModeActivationSource.Unknown;

    /// <summary>
    /// Информация о команде, которая инициировала остановку через breakpoint.
    /// </summary>
    private static IExecutionCommandInfo? _breakpointCommandInfo;

    /// <summary>
    /// Возвращает <c>true</c>, если после отключения пошагового режима требуется
    /// временно пропустить выполнение остановок.
    /// </summary>
    public static bool StepBypassRequested => _stepBypassRequested;

    /// <summary>
    /// Флаг, определяющий режим выполнения.
    /// <para><c>true</c> — шаг с заходом (F11).</para>
    /// <para><c>false</c> — шаг с обходом (F10).</para>
    /// </summary>
    public static bool IsStepInto { get; set; } = false;

    /// <summary>
    /// Флаг, показывающий, что выполнение находится внутри блока.
    /// Используется для корректной обработки режимов F10/F11.
    /// </summary>
    public static bool InsideBlock { get; private set; } = false;

    /// <summary>
    /// Флаг режима F10 до следующей управляющей команды.
    /// Если <c>true</c>, выполнение будет продолжаться до появления
    /// следующей команды управления без остановки.
    /// </summary>
    public static bool StepOverUntilNextControlCommand { get; private set; }

    /// <summary>
    /// Источник текущей активации пошагового режима.
    /// </summary>
    public static StepModeActivationSource ActivationSource => _activationSource;

    /// <summary>
    /// Команда, из-за которой был активирован пошаговый режим breakpoint.
    /// </summary>
    public static IExecutionCommandInfo? BreakpointCommandInfo => _breakpointCommandInfo;

    /// <summary>
    /// Возвращает <c>true</c>, если пошаговый режим активирован через breakpoint
    /// и точка останова ещё не сброшена.
    /// </summary>
    public static bool IsBreakpointStepModeActive =>
      _stepMode &&
      _activationSource == StepModeActivationSource.Breakpoint &&
      _breakpointCommandInfo != null;

    /// <summary>
    /// Помечает вход в исполняемый блок.
    /// Обычно вызывается перед выполнением вложенных операций.
    /// </summary>
    public static void EnterBlock()
    {
      InsideBlock = true;
    }

    /// <summary>
    /// Помечает завершение выполнения блока.
    /// Обычно используется при выходе из вложенного контекста.
    /// </summary>
    public static void ExitBlock() => InsideBlock = false;

    /// <summary>
    /// Сбрасывает состояние пошагового режима.
    /// Вызывается после завершения выполнения сценария.
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
    /// Активирует режим F10 до следующей управляющей команды.
    /// </summary>
    public static void RequestStepOverUntilNextControlCommand()
    {
      IsStepInto = false;
      StepOverUntilNextControlCommand = true;
    }

    /// <summary>
    /// Переключает режим в F11 или отменяет F10-режим.
    /// </summary>
    public static void SetStepIntoMode()
    {
      IsStepInto = true;
      StepOverUntilNextControlCommand = false;
    }

    /// <summary>
    /// Сбрасывает флаг ожидания следующей управляющей команды для F10.
    /// </summary>
    public static void CompleteStepOverUntilNextControlCommand()
    {
      StepOverUntilNextControlCommand = false;
    }

    /// <summary>
    /// Инициализирует состояние пошагового режима.
    /// Загружает настройки из <c>ExecutionConfig</c>
    /// и уведомляет подписчиков о текущем состоянии.
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
    /// Включает пошаговый режим через пользовательский или конфигурационный источник.
    /// </summary>
    /// <param name="isStepInto">
    /// <c>true</c> — шаг с заходом (F11),
    /// <c>false</c> — шаг с обходом (F10).
    /// </param>
    public static void EnableStepMode(bool isStepInto)
    {
      EnableStepModeCore(isStepInto, StepModeActivationSource.ManualOrConfig);
    }

    /// <summary>
    /// Активирует пошаговый режим по достижении breakpoint.
    /// Если режим уже активен и был включён breakpoint,
    /// обновляется информация о текущей команде.
    /// </summary>
    /// <param name="commandInfo">
    /// Команда, на которой произошло срабатывание breakpoint.
    /// </param>
    /// <param name="isStepInto">
    /// <c>true</c> — шаг с заходом (F11),
    /// <c>false</c> — шаг с обходом (F10).
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
    /// Внутренний метод включения пошагового режима.
    /// </summary>
    /// <param name="isStepInto">
    /// <c>true</c> — шаг с заходом (F11),
    /// <c>false</c> — шаг с обходом (F10).
    /// </param>
    /// <param name="activationSource">
    /// Источник активации пошагового режима.
    /// </param>
    /// <param name="breakpointCommandInfo">
    /// Команда breakpoint для режима
    /// <see cref="StepModeActivationSource.Breakpoint"/>.
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
    /// Отключает пошаговый режим и устанавливает флаг пропуска
    /// остановок до следующего шага выполнения.
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
    /// Выполняет начальную инициализацию,
    /// загружая настройки пошагового режима при старте.
    /// </summary>
    static StepControlManager()
    {
      InitializeAsync().ConfigureAwait(true);
    }
  }
}
