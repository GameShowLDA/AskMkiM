using Utilities.Errors;
using Utilities.Models;

namespace AppConfiguration.Error.Translation
{
  /// <summary>
  /// Предоставляет набор методов для генерации общих ошибок,
  /// возникающих при анализе и проверке последовательности команд
  /// в управляющих программах, не зависящих от конкретной команды.
  /// Каждый метод возвращает объект <see cref="ErrorItem"/>,
  /// описывающий конкретную ошибочную ситуацию с указанием строки,
  /// команды и дополнительного описания.
  /// </summary>
  public static class GeneralErrors
  {
    /// <summary>
    /// Возвращает ошибку, если первой командой не является ОК.
    /// </summary>
    /// <param name="lineNumber">Номер строки, в которой ожидалась команда ОК.</param>
    /// <param name="command">Текст команды, найденной на первой строке.</param>
    /// <returns>
    /// Объект <see cref="ErrorItem"/>, описывающий ошибку: первая команда должна быть ОК.
    /// </returns>
    public static ErrorItem FirstCommandMustBeOk(int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Gen_FirstMustBeOk,
      Description = "Первая команда должна быть ОК"
    };

    /// <summary>
    /// Возвращает ошибку, если последней командой не является КЦ.
    /// </summary>
    /// <param name="lineNumber">Номер строки, в которой ожидалась команда КЦ.</param>
    /// <param name="command">Текст команды, найденной на последней строке.</param>
    /// <returns>
    /// Объект <see cref="ErrorItem"/>, описывающий ошибку: последняя команда должна быть КЦ.
    /// </returns>
    public static ErrorItem LastCommandMustBeKc(int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Gen_LastMustBeKc,
      Description = "Последняя команда должна быть КЦ"
    };

    /// <summary>
    /// Возвращает ошибку, если в программе отсутствует обязательная команда с указанной мнемоникой.
    /// </summary>
    /// <param name="mnemonic">Мнемоника обязательной команды.</param>
    /// <param name="lineNumber">Номер строки, к которой относится ошибка (обычно место проверки).</param>
    /// <param name="command">Текст команды (или контекст), для которой возникает ошибка.</param>
    /// <returns>
    /// Объект <see cref="ErrorItem"/>, описывающий ошибку отсутствия требуемой команды.
    /// </returns>
    public static ErrorItem MissingRequiredCommand(string mnemonic, int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Gen_MissingRequiredCommand,
      Description = $"Команда {mnemonic} должна присутствовать в программе"
    };

    /// <summary>
    /// Возвращает ошибку, если команда с указанной мнемоникой встречается в программе более одного раза.
    /// </summary>
    /// <param name="mnemonic">Мнемоника команды, которая продублирована.</param>
    /// <param name="lineNumber">Номер строки, где обнаружен дубликат.</param>
    /// <param name="command">Текст дублирующей команды.</param>
    /// <returns>
    /// Объект <see cref="ErrorItem"/>, описывающий ошибку дублирования команды.
    /// </returns>
    public static ErrorItem DuplicateCommand(string mnemonic, int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Gen_DuplicateCommand,
      Description = $"Команда {mnemonic} должна быть только одна"
    };

    /// <summary>
    /// Возвращает ошибку, если карта точек (РМ) отсутствует и невозможна дальнейшая проверка точек.
    /// </summary>
    /// <param name="lineNumber">Номер строки, где требуется карта точек.</param>
    /// <param name="command">Текст команды, для которой необходима карта точек.</param>
    /// <returns>
    /// Объект <see cref="ErrorItem"/>, описывающий ошибку отсутствия карты точек.
    /// </returns>
    public static ErrorItem MissingPointsMap(int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Gen_MissingPointsMap,
      Description = "Карта точек (РМ) отсутствует — невозможно проверить точки"
    };

    /// <summary>
    /// Возвращает ошибку, если указанная точка не найдена в карте точек RM.
    /// </summary>
    /// <param name="point">Имя точки, не найденной в карте RM.</param>
    /// <param name="lineNumber">Номер строки, где была использована неизвестная точка.</param>
    /// <param name="command">Текст команды, содержащей неизвестную точку.</param>
    /// <returns>
    /// Объект <see cref="ErrorItem"/>, описывающий ошибку отсутствия точки в RM.
    /// </returns>
    public static ErrorItem UnknownPoint(string point, int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Gen_UnknownPoint,
      Description = $"Точка '{point}' не найдена в RM"
    };

    /// <summary>
    /// Возвращает ошибку, если точка назначения используется более одного раза.
    /// </summary>
    /// <param name="point">Имя дублирующейся точки назначения.</param>
    /// <param name="lineNumber">Номер строки, где обнаружен повтор.</param>
    /// <param name="command">Текст команды, в которой повторяется точка назначения.</param>
    /// <returns>
    /// Объект <see cref="ErrorItem"/>, описывающий ошибку дублирования точки назначения.
    /// </returns>
    public static ErrorItem DuplicateDestinationPoint(string point, int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Gen_DuplicateDestination,
      Description = $"Точка назначения '{point}' используется более одного раза"
    };

    /// <summary>
    /// Возвращает ошибку, если совершена попытка добавить схему с дублирующими точками.
    /// </summary>
    /// <param name="lineNumber">Номер строки, где обнаружен повтор.</param>
    /// <param name="command">Текст команды, в которой повторяется точка назначения.</param>
    /// <returns>
    /// Объект <see cref="ErrorItem"/>, описывающий ошибку дублирования точки(-ек) в схеме назначения.
    /// </returns>
    public static ErrorItem SchemeConflict(int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Gen_SchemeConflict,
      Description = $"Попытка добавить схему из предыдущей команды проверки, в которой дублируются уже указанные точки."
    };

    /// <summary>
    /// Возвращает ошибку, если указанный в команде вольтаж выше максимально допустимого вольтажа пробойной установки.
    /// </summary>
    /// <param name="lineNumber">Номер строки, где обнаружен повтор.</param>
    /// <param name="command">Текст команды, в которой повторяется точка назначения.</param>
    /// <param name="commandVoltage">Вольтаж, указанный в команде.</param>
    /// <param name="breakdowmMaxVoltage">Максимально дпустимый вольтаж пробойной установки.</param>
    /// <returns>
    /// Объект <see cref="ErrorItem"/>, описывающий ошибку дублирования точки(-ек) в схеме назначения.
    /// </returns>
    public static ErrorItem VoltageConflict(int lineNumber, string command, int commandVoltage, int breakdowmMaxVoltage) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Gen_VoltageConflict,
      Description = $"В команде {command} указан вольтаж({commandVoltage}), превышающий максимально допустимый вольтаж пробойной установки({breakdowmMaxVoltage})."
    };

    /// <summary>
    /// Возвращает ошибку, если команда не распознана (неизвестная мнемоника).
    /// </summary>
    /// <param name="mnemonic">Мнемоника неизвестной команды.</param>
    /// <param name="lineNumber">Номер строки, где обнаружена неизвестная команда.</param>
    /// <param name="command">Полный текст команды, вызвавшей ошибку.</param>
    /// <returns>
    /// Объект <see cref="ErrorItem"/>, описывающий ошибку: команда не распознана.
    /// </returns>
    public static ErrorItem UnknownCommand(string mnemonic, int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Description = $"Неизвестная команда {mnemonic}",
      Code = ErrorCode.Gen_UnknownCommand
    };

    /// <summary>
    /// Возвращает ошибку, если в строке команды обнаружены нераспознанные или лишние параметры.
    /// </summary>
    /// <param name="unparsed">Сами нераспознанные параметры.</param>
    /// <param name="lineNumber">Номер строки, где найдены нераспознанные параметры.</param>
    /// <param name="command">Текст команды, в которой присутствуют лишние или нераспознанные параметры.</param>
    /// <returns>
    /// Объект <see cref="ErrorItem"/>, описывающий ошибку: нераспознанные параметры в строке.
    /// </returns>
    public static ErrorItem UnrecognizedParameters(string unparsed, int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Gen_UnrecognizedParameters,
      Description = $"Обнаружены нераспознанные параметры: {unparsed}"
    };

    /// <summary>
    /// Возвращает ошибку, если после вопросительной ЦУ (с "?" или "??") отсутствует ожидаемая команда УП.
    /// </summary>
    /// <param name="lineNumber">Номер строки, в которой находится команда ЦУ.</param>
    /// <param name="command">Текст команды ЦУ.</param>
    /// <returns>Объект <see cref="ErrorItem"/>, описывающий ошибку отсутствия УП после ЦУ-вопроса.</returns>
    public static ErrorItem ExpectedConditionalJumpAfterCu(int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Gen_ExpectedConditionalJumpAfterCu,
      Description = "После команды ЦУ с вопросом ожидается команда УП (условный переход)"
    };

    /// <summary>
    /// Возвращает ошибку, если команда УП стоит после информационной ЦУ (без вопроса), что недопустимо.
    /// </summary>
    /// <param name="lineNumber">Номер строки, где находится команда УП.</param>
    /// <param name="command">Текст команды УП.</param>
    /// <returns>Объект <see cref="ErrorItem"/>, описывающий ошибку положения УП.</returns>
    public static ErrorItem ConditionalJumpAfterInformationCu(int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Gen_ConditionalJumpAfterInformationCu,
      Description = "Команда УП не должна следовать за информационной ЦУ (без вопроса)"
    };

    /// <summary>
    /// Возвращает ошибку, если имеются отступы в начале строки перед номером команды.
    /// </summary>
    /// <param name="mnemonic">Мнемоника неизвестной команды.</param>
    /// <param name="lineNumber">Номер строки, где обнаружена неизвестная команда.</param>
    /// <param name="command">Полный текст команды, вызвавшей ошибку.</param>
    /// <returns>
    /// Объект <see cref="ErrorItem"/>, описывающий ошибку: команда не распознана.
    /// </returns>
    public static ErrorItem IndentationError(string mnemonic, int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Description = $"Возможно присутсвуют лишние пробелы перед номером команды или отсутсвуют необходимые отступы при переносе тела команды {mnemonic}.",
      Code = ErrorCode.Gen_UnknownCommand
    };

    /// <summary>
    /// Возвращает ошибку, если нарушен порядок параметров команды.
    /// </summary>
    /// <param name="mnemonic">Мнемоника неизвестной команды.</param>
    /// <param name="lineNumber">Номер строки, где обнаружена неизвестная команда.</param>
    /// <param name="command">Полный текст команды, вызвавшей ошибку.</param>
    /// <returns>
    /// Объект <see cref="ErrorItem"/>, описывающий ошибку: команда не распознана.
    /// </returns>
    public static ErrorItem InvalidParameterOrder(string mnemonic, int lineNumber, string command, string descr) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Description = $"{descr}",
      Code = ErrorCode.Gen_InvalidParameterOrder
    };

  }
}
