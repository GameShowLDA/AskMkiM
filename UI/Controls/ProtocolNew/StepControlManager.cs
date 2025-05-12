using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Controls.ProtocolNew
{
  /// <summary>
  /// Управляет режимами пошагового выполнения (F10/F11) и вложенными блоками.
  /// </summary>
  public static class StepControlManager
  {
    /// <summary>
    /// Флаг, указывающий, активен ли пошаговый режим.
    /// </summary>
    public static bool StepMode { get; set; } = false;

    /// <summary>
    /// True — если нажато F11 (вглубь), false — если F10 (поверх).
    /// </summary>
    public static bool IsStepInto { get; set; } = false;

    /// <summary>
    /// Внутри вложенного блока шагов.
    /// </summary>
    public static bool InsideBlock { get; private set; } = false;

    /// <summary>
    /// Войти в блок (до ShowMessageAsync).
    /// </summary>
    public static void EnterBlock() => InsideBlock = true;

    /// <summary>
    /// Выйти из блока (если нужно вручную).
    /// </summary>
    public static void ExitBlock() => InsideBlock = false;

    /// <summary>
    /// Сброс всех состояний (например, после завершения выполнения).
    /// </summary>
    public static void Reset()
    {
      StepMode = false;
      IsStepInto = false;
      InsideBlock = false;
    }
  }
}

