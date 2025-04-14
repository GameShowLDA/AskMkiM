using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfiguration.MeasurementError
{
  /// <summary>
  /// Класс конфигурации для <see cref="MeasurementErrorModel"/>.
  /// </summary>
  public static class MeasurementErrorConfig
  {
    #region Properties.

    /// <summary>
    /// Модель данных <see cref="MeasurementErrorModel"/> для режима КС.
    /// </summary>
    static private MeasurementErrorModel KcMode;

    /// <summary>
    /// Модель данных <see cref="MeasurementErrorModel"/> для режима ИЕ.
    /// </summary>
    static private MeasurementErrorModel IeMode;

    /// <summary>
    /// Модель данных <see cref="MeasurementErrorModel"/> для режима СИ.
    /// </summary>
    static private MeasurementErrorModel CiMode;

    /// <summary>
    /// Модель данных <see cref="MeasurementErrorModel"/> для режима ПР.
    /// </summary>
    static private MeasurementErrorModel PrMode;

    #endregion

    #region Set.

    /// <summary>
    /// Установить объект MeasurementErrorModel по TypeCommand.
    /// </summary>
    /// <param name="model">Объект MeasurementErrorModel.</param>
    public static void SetMeasurementErrorModel(MeasurementErrorModel model)
    {
      switch (model.Type)
      {
        case MeasurementErrorModel.TypeCommand.KC:
          KcMode = model;
          break;
        case MeasurementErrorModel.TypeCommand.IE:
          IeMode = model;
          break;
        case MeasurementErrorModel.TypeCommand.CI:
          CiMode = model;
          break;
        case MeasurementErrorModel.TypeCommand.PR:
          PrMode = model;
          break;
        default:
          throw new ArgumentException($"Неизвестный тип команды: {model.Type}", nameof(model.Type));
      }
    }

    #endregion

    #region Get.

    /// <summary>
    /// Получить значение погрешности в процентах.
    /// </summary>
    /// <param name="type">Тип команды.</param>
    /// <returns>Погрешность в процентах.</returns>
    public static double GetPercentageError(MeasurementErrorModel.TypeCommand type)
    {
      var result = GetMeasurementErrorModel(type);
      if (result != null)
      {
        return result.PercentageError;
      }

      return 0;
    }

    /// <summary>
    /// Получить значение погрешности в числовом значении.
    /// </summary>
    /// <param name="type">Тип команды.</param>
    /// <returns>Погрешность в процентах.</returns>
    public static double GetNumericError(MeasurementErrorModel.TypeCommand type)
    {
      var result = GetMeasurementErrorModel(type);
      if (result != null)
      {
        return result.NumericError;
      }

      return 0;
    }

    /// <summary>
    /// Получить объект MeasurementErrorModel по TypeCommand.
    /// </summary>
    /// <param name="type">Тип команды.</param>
    /// <returns>Объект MeasurementErrorModel.</returns>
    private static MeasurementErrorModel GetMeasurementErrorModel(MeasurementErrorModel.TypeCommand type)
    {
      switch (type)
      {
        case MeasurementErrorModel.TypeCommand.KC:
          return KcMode;
        case MeasurementErrorModel.TypeCommand.IE:
          return IeMode;
        case MeasurementErrorModel.TypeCommand.CI:
          return CiMode;
        case MeasurementErrorModel.TypeCommand.PR:
          return PrMode;
        default:
          throw new ArgumentException($"Неизвестный тип команды: {type}", nameof(type));
      }
    }

    #endregion
  }
}
