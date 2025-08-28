using System;
using System.Collections.Generic;
using System.Linq;
using Mode.Models;
using NewCore.Base.Interface.Main;
using Utilities.Models;
using static NewCore.Enum.DeviceEnum;

namespace Mode.TestSuite.Metrology.MethodExecutor
{
  /// <summary>
  /// Отвечает за группировку точек по модулям и определение шины подключения для каждой точки.
  /// </summary>
  public class PointGroupingService
  {
    private readonly IEnumerable<IRelaySwitchModule> _modules;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PointGroupingService"/>.
    /// </summary>
    /// <param name="modules">Список доступных модулей коммутации реле.</param>
    public PointGroupingService(IEnumerable<IRelaySwitchModule> modules)
    {
      _modules = modules ?? throw new ArgumentNullException(nameof(modules));
    }

    /// <summary>
    /// Группирует точки по модулям, сопоставляя перевёрнутые бинарные строки.
    /// </summary>
    /// <param name="pointsWithBinary">
    /// Список кортежей, содержащих точку и соответствующее ей перевёрнутое двоичное представление.
    /// </param>
    /// <returns>
    /// Список кортежей, где каждая группа содержит модуль, список точек и список бинарных строк.
    /// </returns>
    public List<(IRelaySwitchModule module, List<PointModel> points, List<string> reversedBinary)> GroupByModulesWithBinary(
        List<(PointModel point, string reversedBinary)> pointsWithBinary)
    {
      return pointsWithBinary
          .Select(p => new
          {
            Module = _modules.FirstOrDefault(m =>
                m.Number == p.point.ModuleNumber &&
                m.NumberChassis == p.point.DeviceNumber),
            p.point,
            p.reversedBinary,
          })
          .Where(x => x.Module != null)
          .GroupBy(x => x.Module!)
          .Select(g => (
              module: g.Key,
              points: g.Select(x => x.point).ToList(),
              reversedBinary: g.Select(x => x.reversedBinary).ToList()))
          .ToList();
    }

    /// <summary>
    /// Назначает каждой точке шину подключения в зависимости от текущего разряда бинарного значения.
    /// </summary>
    /// <param name="grouped">
    /// Группированный список точек с перевёрнутыми бинарными строками по модулям.
    /// </param>
    /// <param name="currentStep">Текущий индекс проверяемого бита (начиная с младшего).</param>
    /// <param name="assignedBus">Шина, соответствующая биту со значением '1'.</param>
    /// <param name="oppositeBus">Шина, соответствующая биту со значением '0'.</param>
    /// <returns>
    /// Список кортежей, содержащих модуль, список точек, их бинарные строки и соответствующие шины подключения.
    /// </returns>
    public List<(IRelaySwitchModule module, List<PointModel> points, List<string> reversedBinary, List<BusPoint> buses)>
        AssignBusConnections(
            List<(IRelaySwitchModule module, List<PointModel> points, List<string> reversedBinary)> grouped,
            int currentStep,
            BusPoint assignedBus,
            BusPoint oppositeBus)
    {
      var result = new List<(IRelaySwitchModule, List<PointModel>, List<string>, List<BusPoint>)>();

      foreach (var (module, points, reversedBinary) in grouped)
      {
        var busAssignments = new List<BusPoint>();

        for (int i = 0; i < points.Count; i++)
        {
          bool isOne = currentStep < reversedBinary[i].Length &&
                       reversedBinary[i][currentStep] == '1';

          busAssignments.Add(isOne ? assignedBus : oppositeBus);
        }

        result.Add((module, points, reversedBinary, busAssignments));
      }

      return result;
    }
  }
}
