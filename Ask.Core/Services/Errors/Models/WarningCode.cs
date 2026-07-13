using System.Reflection;

namespace Ask.Core.Services.Errors.Models
{
  /// <summary>
  /// Коды предупреждений, используемые при анализе и разборе управляющих программ.
  /// Каждый код помечен атрибутом <see cref="WarningCodeTagAttribute"/>,
  /// соответствует уникальной ситуации, выявленной при валидации команд.
  /// </summary>
  public enum WarningCode
  {
    /// <summary>
    /// Неизвестное или не классифицированное предупреждение.
    /// Используется по умолчанию, если код не определён явно.
    /// </summary>
    [WarningCodeTag("WARNING_ERROR")]
    Unknown,

    #region Транслятор

    #region Общие ошибки

    /// <summary> Время выполнения установлено по умолчанию. </summary>
    [WarningCodeTag("WRNGEN001")]
    Gen_DefaultTime,

    /// <summary> Напряжение установлено по умолчанию. </summary>
    [WarningCodeTag("WRNGEN002")]
    Gen_DefaultVoltage,

    /// <summary> Нижняя граница сопротивления установлена по умолчанию. </summary>
    [WarningCodeTag("WRNGEN003")]
    Gen_DefaultResistainceLowLimit,

    /// <summary> Верхняя граница сопротивления установлена по умолчанию. </summary>
    [WarningCodeTag("WRNGEN004")]
    Gen_DefaultResistainceHighLimit,

    /// <summary> Сопротивление установлено по умолчанию. </summary>
    [WarningCodeTag("WRNGEN005")]
    Gen_DefaultResistaince,

    /// <summary> Найден и удален дублирующийся ключ. </summary>
    [WarningCodeTag("WRNGEN006")]
    Gen_DuplicateKey,

    /// <summary> Количество разобщенных цепей было меньше одной. Ключ ЗР был добавлен по умолчанию. </summary>
    [WarningCodeTag("WRNGEN007")]
    Gen_KeyZR,

    /// <summary> Команда ВШ не была указана в программе контроля. Команда ВШ была добавлена автоматически. </summary>
    [WarningCodeTag("WRNGEN008")]
    Gen_VshCommandAddedAutomatically,

    /// <summary> Значение напряжения игнорируется. </summary>
    [WarningCodeTag("WRNGEN009")]
    Gen_IgnoreVoltage,

    /// <summary> Значение силы тока игнорируется. </summary>
    [WarningCodeTag("WRNGEN010")]
    Gen_IgnoreAmperage,

    /// <summary> Пустое сообщение в ЦУ. </summary>
    [WarningCodeTag("WRNGEN011")]
    Gen_EmptyMessage,

    #endregion

    #region Режим УМ

    #endregion

    #region Режим ОК

    /// <summary> Не удалось корректно разобрать первую строку команды ОК. </summary>
    [WarningCodeTag("WARNOK001")]
    Ok_CannotParseFirstLine,

    #endregion

    #region Режим РМ

    /// <summary> Не удалось корректно разобрать выражение в команде РМ. </summary>
    [WarningCodeTag("WARNRM001")]
    Rm_CannotParseExpression,

    #endregion

    #region Режим СИ

    /// <summary> Не удалось корректно разобрать выражение в команде СИ. </summary>
    [WarningCodeTag("WARNSI001")]
    Si_CannotParseExpression,

    #endregion

    #region Режим ПИ

    /// <summary> Не удалось корректно разобрать выражение в команде ПИ. </summary>
    [WarningCodeTag("WARNPI001")]
    Pi_CannotParseExpression,

    #endregion

    #region Режим КС

    /// <summary> Не указаны границы сопротивления для команды КС. </summary>
    [WarningCodeTag("WARNKS001")]
    Ks_EmptyResistance,

    #endregion

    #region Режим ИЕ

    /// <summary> Не указаны границы емкости для команды ИЕ. </summary>
    [WarningCodeTag("WARNIE001")]
    Ie_EmptyLowerCapacity,

    #endregion

    #region Режим ПР

    /// <summary> Ошибка при проверке точки про методе полного узла в команде ПР. </summary>
    [WarningCodeTag("WARNPR006")]
    Pr_NodeExecutePointError,

    #endregion

    #region Режим ЭТ

    /// <summary> В команде ПР не удалось распознать параметры. </summary>
    [WarningCodeTag("WARNEHT001")]
    Eht_CannotParseParameters,

    #endregion

    #region Ключи команд

    /// <summary> Использованный ключ не разрешён для данной команды. </summary>
    [WarningCodeTag("WARNKEY001")]
    Key_NotAllowedForCommand,

    #endregion

    #endregion

    #region Оборудование

    #region МКР (модуль коммутации реле)

    /// <summary>
    /// Ошибка: не найдено шасси с указанным номером.
    /// </summary>
    [WarningCodeTag("WARNMKR001")]
    Equipment_ChassisNotFound,

    #endregion

    #region УКШ

    #endregion

    #region ППУ

    #endregion

    #region Мультиметр

    #endregion

    #endregion

    #region Метрология

    #region Валидация данных.

    #endregion

    #endregion

  }
  /// <summary>
  /// Расширения для <see cref="WarningCode"/>.
  /// </summary>
  public static class WarningCodeExtensions
  {
    /// <summary>
    /// Возвращает строковой тэг (например, TRN001), заданный через атрибут <see cref="WarningCodeTagAttribute"/>.
    /// </summary>
    public static string? GetTag(this WarningCode code)
    {
      var member = typeof(WarningCode).GetMember(code.ToString()).FirstOrDefault();
      var attr = member?.GetCustomAttribute<WarningCodeTagAttribute>();
      return attr?.Tag;
    }
  }
}
