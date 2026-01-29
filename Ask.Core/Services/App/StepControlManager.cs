using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Services.App
{
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
    /// Устанавливает состояние входа во вложенный блок команд.
    /// Обычно вызывается перед отображением сообщения (<c>ShowMessageAsync</c>).
    /// </summary>
    public static void EnterBlock()
    {
      InsideBlock = true;
      IsStepInto = true;
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
    }

    /// <summary>
    /// Инициализирует состояние пошагового режима при старте приложения.
    /// Считывает настройки из <c>ExecutionConfig</c> и подписывается на событие
    /// <c>StepByStepModeChanged</c> для динамического изменения состояния.
    /// </summary>
    public static async Task InitializeAsync()
    {
      _stepMode = await ExecutionConfig.GetIsStepByStepModeEnabled();
      if (_stepMode)
      {
        IsStepInto = true;
      }

      EventAggregator.Subscribe<ExecutionEvents.StepByStepModeChanged>(e => EventAggregator_StepByStepModeChanged(e.IsEnabled));
    }

    /// <summary>
    /// Обработчик события изменения состояния пошагового режима.
    /// </summary>
    /// <param name="obj">Новое значение флага пошагового режима.</param>
    private static void EventAggregator_StepByStepModeChanged(bool obj)
    {
      if (obj)
      {
        EnableStepMode(true);
      }
      else
      {
        DisableStepMode();
      }
    }

    /// <summary>
    /// Включает пошаговый режим.
    /// </summary>
    /// <param name="isStepInto">
    /// <c>true</c> — шаг вглубь (F11), 
    /// <c>false</c> — шаг поверх блока (F10).
    /// </param>
    public static void EnableStepMode(bool isStepInto)
    {
      _stepMode = true;
      IsStepInto = isStepInto;
      _stepBypassRequested = false;
    }

    /// <summary>
    /// Отключает пошаговый режим и устанавливает флаг обхода
    /// для продолжения выполнения без остановок.
    /// </summary>
    public static void DisableStepMode()
    {
      _stepMode = false;
      _stepBypassRequested = true;
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
