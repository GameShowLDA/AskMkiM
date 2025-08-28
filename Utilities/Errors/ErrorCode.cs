using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Utilities.Models;

namespace Utilities.Errors
{
  /// <summary>
  /// Коды ошибок, используемые при анализе и разборе управляющих программ.
  /// Каждый код помечен атрибутом <see cref="ErrorCodeTagAttribute"/>,
  /// соответствует уникальной ситуации, выявленной при валидации команд.
  /// </summary>
  public enum ErrorCode
  {
    #region Транслятор

    #region Общие ошибки

    /// <summary> Первая команда в управляющей программе должна быть ОК. </summary>
    [ErrorCodeTag("GEN001")]
    Gen_FirstMustBeOk,

    /// <summary> Последняя команда в управляющей программе должна быть КЦ. </summary>
    [ErrorCodeTag("GEN002")]
    Gen_LastMustBeKc,

    /// <summary> В программе отсутствует обязательная команда с указанной мнемоникой. </summary>
    [ErrorCodeTag("GEN003")]
    Gen_MissingRequiredCommand,

    /// <summary> Команда с данной мнемоникой встречается более одного раза. </summary>
    [ErrorCodeTag("GEN004")]
    Gen_DuplicateCommand,

    /// <summary> Не удалось найти карту точек (РМ), необходимую для проверки точек подключения. </summary>
    [ErrorCodeTag("GEN005")]
    Gen_MissingPointsMap,

    /// <summary> В команде используется точка, отсутствующая в карте точек RM. </summary>
    [ErrorCodeTag("GEN006")]
    Gen_UnknownPoint,

    /// <summary> Одна и та же точка назначения используется в RM более одного раза. </summary>
    [ErrorCodeTag("GEN007")]
    Gen_DuplicateDestination,

    /// <summary> Обнаружена команда с нераспознанной или неизвестной мнемоникой. </summary>
    [ErrorCodeTag("GEN008")]
    Gen_UnknownCommand,

    /// <summary> В строке команды найдены нераспознанные или лишние параметры. </summary>
    [ErrorCodeTag("GEN009")]
    Gen_UnrecognizedParameters,

    /// <summary> После команды ЦУ с вопросом ("?" или "??") ожидается команда УП, но она отсутствует. </summary>
    [ErrorCodeTag("GEN010")]
    Gen_ExpectedConditionalJumpAfterCu,

    /// <summary> Команда УП следует после информационной ЦУ (без вопроса), что недопустимо по синтаксису. </summary>
    [ErrorCodeTag("GEN011")]
    Gen_ConditionalJumpAfterInformationCu,

    /// <summary> Недопустимые отступы в начале строки перед номером команды или наоборот отсутсвуют необходимые отступы в начале строки тела команды при переносе. </summary>
    [ErrorCodeTag("GEN012")]
    Gen_IndentationError,

    /// <summary> Нарушен порядок параметров команды. </summary>
    [ErrorCodeTag("GEN013")]
    Gen_InvalidParameterOrder,

    /// <summary> Неверне использование одиночной точки. </summary>
    [ErrorCodeTag("GEN014")]
    Gen_InvalidOnePointUse,

    #endregion

    #region Режим УМ

    /// <summary> В команде УП отсутствует или некорректно указана метка перехода. </summary>
    [ErrorCodeTag("UP001")]
    Up_MissingOrInvalidUpLabel,

    /// <summary> Метка перехода, указанная в команде УП, не найдена среди доступных команд. </summary>
    [ErrorCodeTag("UP002")]
    Up_UpLabelNotFound,

    #endregion

    #region Режим ОК

    /// <summary> Не удалось корректно разобрать первую строку команды ОК. </summary>
    [ErrorCodeTag("OK001")]
    Ok_CannotParseFirstLine,

    /// <summary> В команде ОК отсутствует код объекта. </summary>
    [ErrorCodeTag("OK002")]
    Ok_MissingObjectCode,

    /// <summary> В команде ОК отсутствует наименование объекта. </summary>
    [ErrorCodeTag("OK003")]
    Ok_MissingObjectName,

    /// <summary> Тело команды ОК отсутствует или пустое. </summary>
    [ErrorCodeTag("OK004")]
    Ok_EmptyCommandBody,

    /// <summary> Не удалось корректно разобрать параметр команды ОК. </summary>
    [ErrorCodeTag("OK005")]
    Ok_CannotParseParameter,

    /// <summary> Ключ параметра в команде ОК превышает максимально допустимую длину. </summary>
    [ErrorCodeTag("OK006")]
    Ok_ParameterKeyTooLong,

    /// <summary> Значение параметра в команде ОК превышает максимально допустимую длину. </summary>
    [ErrorCodeTag("OK007")]
    Ok_ParameterValueTooLong,

    /// <summary> В команде ОК обнаружен дублирующийся ключ параметра. </summary>
    [ErrorCodeTag("OK008")]
    Ok_DuplicateKey,

    /// <summary> Код объекта в команде ОК превышает максимально допустимую длину. </summary>
    [ErrorCodeTag("OK009")]
    Ok_ObjectCodeTooLong,

    /// <summary> Наименование объекта в команде ОК превышает максимально допустимую длину. </summary>
    [ErrorCodeTag("OK010")]
    Ok_ObjectNameTooLong,

    #endregion

    #region Режим РМ

    /// <summary> Не удалось корректно разобрать выражение в команде РМ. </summary>
    [ErrorCodeTag("RM001")]
    Rm_CannotParseExpression,

    /// <summary> Левая или правая часть выражения в команде РМ отсутствует или пуста. </summary>
    [ErrorCodeTag("RM002")]
    Rm_EmptyLeftOrRight,

    /// <summary> Количество элементов слева и справа в выражении команды РМ не совпадает. </summary>
    [ErrorCodeTag("RM003")]
    Rm_MismatchedCounts,

    /// <summary> Группы точек в команде РМ не совпадают по составу или названию. </summary>
    [ErrorCodeTag("RM004")]
    Rm_GroupMismatch,

    /// <summary> Группа точек в команде РМ содержит недостаточно элементов. </summary>
    [ErrorCodeTag("RM005")]
    Rm_GroupTooShort,

    /// <summary> Диапазоны шагов в команде РМ не совпадают по количеству или значениям. </summary>
    [ErrorCodeTag("RM006")]
    Rm_StepRangeMismatch,

    /// <summary> Тело команды РМ отсутствует или пустое. </summary>
    [ErrorCodeTag("RM007")]
    Rm_EmptyCommandBody,

    /// <summary> При парсинге команды РМ найден лишний пробел. </summary>
    [ErrorCodeTag("RM008")]
    Rm_ExtraSpace,

    /// <summary> При парсинге команды РМ найдены недопустимые символы. </summary>
    [ErrorCodeTag("RM009")]
    Rm_UnacceptableSymbol,

    #endregion

    #region Режим СИ

    /// <summary> Не удалось корректно разобрать выражение в команде СИ. </summary>
    [ErrorCodeTag("SI001")]
    Si_CannotParseExpression,

    /// <summary> Не удалось корректно разобрать параметры команды СИ. </summary>
    [ErrorCodeTag("SI002")]
    Si_CannotParseParameters,

    /// <summary> В команде СИ отсутствует список точек. </summary>
    [ErrorCodeTag("SI003")]
    Si_EmptyPoints,

    /// <summary> Тело команды СИ отсутствует или пустое. </summary>
    [ErrorCodeTag("SI004")]
    Si_EmptyCommandBody,

    /// <summary> Ошибка при проверке разряда при групповом методе. </summary>
    [ErrorCodeTag("SI005")]
    Si_WrongDigitCheckForGroupedMethod,

    /// <summary> Ошибка при проверке точки про методе полного узла. </summary>
    [ErrorCodeTag("SI006")]
    Si_NodeExecutePointError,

    /// <summary> Ошибка при замкнутной цепи. </summary>
    [ErrorCodeTag("SI007")]
    Si_ChainError,

    /// <summary> Ошибка при замкнутной паре. </summary>
    [ErrorCodeTag("SI008")]
    Si_PairError,

    #endregion

    #region Режим ПИ

    /// <summary> Не удалось корректно разобрать выражение в команде ПИ. </summary>
    [ErrorCodeTag("PI001")]
    Pi_CannotParseExpression,

    /// <summary> Не удалось корректно разобрать параметры команды ПИ. </summary>
    [ErrorCodeTag("PI002")]
    Pi_CannotParseParameters,

    /// <summary> В команде ПИ отсутствует список точек. </summary>
    [ErrorCodeTag("PI003")]
    Pi_EmptyPoints,

    /// <summary> Тело команды ПИ отсутствует или пустое. </summary>
    [ErrorCodeTag("PI004")]
    Pi_EmptyCommandBody,

    #endregion

    #region Режим КС

    /// <summary> Не указаны границы сопротивления для команды КС. </summary>
    [ErrorCodeTag("KS001")]
    Ks_EmptyResistance,

    /// <summary> Не удалось корректно разобрать параметры команды КС. </summary>
    [ErrorCodeTag("KS002")]
    Ks_CannotParseParameters,

    /// <summary> В команде КС отсутствует список точек. </summary>
    [ErrorCodeTag("KS003")]
    Ks_EmptyPoints,

    /// <summary> В команде КС пустое тело метода. </summary>
    [ErrorCodeTag("KS004")]
    Ks_EmptyCommandBody,

    #endregion

    #region Режим ИЕ

    /// <summary> Не указаны границы емкости для команды ИЕ. </summary>
    [ErrorCodeTag("IE001")]
    Ie_EmptyCapacity,

    /// <summary> Не удалось корректно разобрать параметры команды ИЕ. </summary>
    [ErrorCodeTag("IE002")]
    Ie_CannotParseParameters,

    /// <summary> В команде ИЕ отсутствует список точек. </summary>
    [ErrorCodeTag("IE003")]
    Ie_EmptyPoints,

    /// <summary> В команде ИЕ пустое тело метода. </summary>
    [ErrorCodeTag("IE004")]
    Ie_EmptyCommandBody,

    #endregion

    #region Режим ПР

    /// <summary> Ошибка при проверке точки про методе полного узла в команде ПР. </summary>
    [ErrorCodeTag("PR006")]
    Pr_NodeExecutePointError,

    /// <summary> Ошибка при замкнутной цепи в команде ПР. </summary>
    [ErrorCodeTag("PR007")]
    Pr_ChainError,

    /// <summary> В команде ПР замкнутые точки. </summary>
    [ErrorCodeTag("PR008")]
    Pr_PairError,
    #endregion

    #region Ключи команд

    /// <summary> Использованный ключ не разрешён для данной команды. </summary>
    [ErrorCodeTag("KEY001")]
    Key_NotAllowedForCommand,

    /// <summary> Ключ не распознан среди допустимых. </summary>
    [ErrorCodeTag("KEY002")]
    Key_NotRecognized,

    /// <summary> Использована конфликтная пара ключей, несовместимая в рамках одной команды. </summary>
    [ErrorCodeTag("KEY003")]
    Key_ConflictPair,

    /// <summary> Ключ не ожидается в данной команде. </summary>
    [ErrorCodeTag("KEY004")]
    Key_NotExpectedInThisCommand,
    #endregion

    #endregion

    #region Оборудование

    #region МКР
    /// <summary> Первая команда в управляющей программе должна быть ОК. </summary>
    
    [ErrorCodeTag("MKR001")]
    MKR_PointError,

    #endregion

    #region УКШ

    #endregion

    #region ППУ

    #endregion

    #region Мультиметр

    #endregion

    #endregion
  }

  /// <summary>
  /// Расширения для <see cref="ErrorCode"/>.
  /// </summary>
  public static class ErrorCodeExtensions
  {
    /// <summary>
    /// Возвращает строковой тэг (например, TRN001), заданный через атрибут <see cref="ErrorCodeTagAttribute"/>.
    /// </summary>
    public static string? GetTag(this ErrorCode code)
    {
      var member = typeof(ErrorCode).GetMember(code.ToString()).FirstOrDefault();
      var attr = member?.GetCustomAttribute<ErrorCodeTagAttribute>();
      return attr?.Tag;
    }
  }
}
