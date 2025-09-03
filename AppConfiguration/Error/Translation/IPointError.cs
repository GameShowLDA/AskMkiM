using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Models;

namespace AppConfiguration.Error.Translation
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
    public ErrorItem PairError(string command, string pointFirst, string pointLast);

    /// <summary>
    /// Ошибка: Ошибка замкнутой цепи.
    /// </summary>
    /// <param name="command">Номер команды и мнемоника.</param>
    /// <param name="pointFirst">Первая точка.</param>
    /// <param name="pointLast">Вторая точка.</param>
    /// <returns></returns>
    public ErrorItem ChainPairError(string command, List<PointModel> pointFirst, List<PointModel> pointLast);

    /// <summary>
    /// Ошибка: Ошибка замкнутой цепи.
    /// </summary>
    /// <param name="command">Номер команды и мнемоника.</param>
    /// <param name="step">Номер разряда.</param>
    /// <param name="countStep">Кол-во разрядов.</param>
    /// <param name="resultMeasure">Результат измерения.</param>
    /// <returns></returns>
    public ErrorItem ChainError(string command, string chain);

    /// <summary>
    /// Ошибка: Ошибка при проверке одно из разряда в групповом методе.
    /// </summary>
    /// <param name="command">Номер команды и мнемоника.</param>
    /// <param name="step">Номер разряда.</param>
    /// <param name="countStep">Кол-во разрядов.</param>
    /// <param name="resultMeasure">Результат измерения.</param>
    /// <returns></returns>
    public ErrorItem NodeExecutePointError(string command, List<PointModel> point, string resultMeasure);

  }
}
