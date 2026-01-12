using Ask.Core.Services.Errors.Models;
using System.Runtime.CompilerServices;

namespace Ask.Core.Shared.Interfaces.ErrorInterfaces
{
  public interface IPointError
  {
    /// <summary>
    /// Ошибка: Ошибка замкнутой цепи.
    /// </summary>
    /// <param name="command">Номер команды и мнемоника.</param>
    /// <param name="pointFirst">Первая точка.</param>
    /// <param name="pointLast">Вторая точка.</param>
    /// <returns></returns>
    ErrorItem PairError(string command, string pointFirst, string pointLast, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0);

    /// <summary>
    /// Ошибка: Ошибка замкнутой цепи.
    /// </summary>
    /// <param name="command">Номер команды и мнемоника.</param>
    /// <param name="pointFirst">Первая точка.</param>
    /// <param name="pointLast">Вторая точка.</param>
    /// <returns></returns>
    ErrorItem ChainPairError(string command, List<string> pointFirst, List<string> pointLast, string value, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0);

    /// <summary>
    /// Ошибка: Ошибка замкнутой цепи.
    /// </summary>
    /// <param name="command">Номер команды и мнемоника.</param>
    /// <param name="step">Номер разряда.</param>
    /// <param name="countStep">Кол-во разрядов.</param>
    /// <param name="resultMeasure">Результат измерения.</param>
    /// <returns></returns>
    ErrorItem ChainError(string command, string chain, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0);

    /// <summary>
    /// Ошибка: Ошибка разрыва цепи.
    /// </summary>
    /// <param name="command">Номер команды и мнемоника.</param>
    /// <param name="step">Номер разряда.</param>
    /// <param name="countStep">Кол-во разрядов.</param>
    /// <param name="resultMeasure">Результат измерения.</param>
    /// <returns></returns>
    ErrorItem DisconnectChainError(string command, string chain, string measureResult, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0);

    /// <summary>
    /// Ошибка: Ошибка при проверке одно из разряда в групповом методе.
    /// </summary>
    /// <param name="command">Номер команды и мнемоника.</param>
    /// <param name="step">Номер разряда.</param>
    /// <param name="countStep">Кол-во разрядов.</param>
    /// <param name="resultMeasure">Результат измерения.</param>
    /// <returns></returns>
    ErrorItem NodeExecutePointError(string command, List<string> point, string resultMeasure, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0);
  }
}
