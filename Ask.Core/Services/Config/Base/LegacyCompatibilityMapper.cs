using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Services.Config.Base
{
  static public class LegacyCompatibilityMapper
  {
    static public void SetCompatibilityPointsMap(Dictionary<PointModel, PointModel> compatibilityPointsMap)
    {
      CompatibilityPointsMap = compatibilityPointsMap;
    }

    /// <summary>
    /// Возвращает реальный адрес точки подключения
    /// по адресу точки переходной панели.
    /// </summary>
    static public string GetRealAddressByCompatibilityPoint(string compatibilityPoint)
    {
      PointModel pointModel = PointModel.ParsePointString(compatibilityPoint);

      var pair = CompatibilityPointsMap.FirstOrDefault(x => x.Value.Equals(pointModel));

      if (string.IsNullOrWhiteSpace(pair.Key.ToString()))
      {
        return compatibilityPoint;
      }

      return pair.Key.ToString();
    }

    /// <summary>
    /// Возвращает адрес точки переходной панели
    /// по реальному адресу точки подключения.
    /// </summary>
    static public string GetCompatibilityPointByRealAddress(string realAddress)
    {
      PointModel pointModel = PointModel.ParsePointString(realAddress);

      if (CompatibilityPointsMap.TryGetValue(pointModel, out PointModel? compatibilityPoint))
      {
        return compatibilityPoint.ToString();
      }

      return realAddress;
    }

    static Dictionary<PointModel, PointModel> CompatibilityPointsMap { get; set; } = null;
  }
}
