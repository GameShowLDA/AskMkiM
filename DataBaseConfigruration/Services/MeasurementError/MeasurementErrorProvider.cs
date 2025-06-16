using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Enums;
using AppConfiguration.MeasurementError;
using DataBaseConfiguration.Models.MeasurementError;

namespace DataBaseConfiguration.Services.MeasurementError
{
  public class MeasurementErrorProvider : IMeasurementErrorProvider
  {
    private readonly MeasurementErrorServices _service;

    public MeasurementErrorProvider(MeasurementErrorServices service)
    {
      _service = service;
    }

    public (double Numeric, double Percent) GetErrorParameters(TypeCommand type)
    {
      // Маппинг типов
      var dbType = type switch
      {
        TypeCommand.KC => TypeCommand.KC,
        TypeCommand.PR => TypeCommand.PR,
        TypeCommand.CI => TypeCommand.CI,
        TypeCommand.IE => TypeCommand.IE,
        _ => throw new ArgumentOutOfRangeException(nameof(type), $"Неизвестный тип {type}")
      };

      return _service.GetErrorParameters(dbType);
    }

    public (double Min, double Max) GetRange(TypeCommand typeCommand, double expectedValue)
    {
      // Маппинг типов
      var dbType = typeCommand switch
      {
        TypeCommand.KC => TypeCommand.KC,
        TypeCommand.PR => TypeCommand.PR,
        TypeCommand.CI => TypeCommand.CI,
        TypeCommand.IE => TypeCommand.IE,
        _ => throw new ArgumentOutOfRangeException(nameof(typeCommand), $"Неизвестный тип {typeCommand}")
      };

      return _service.GetRange(dbType, expectedValue);
    }
  }
}
