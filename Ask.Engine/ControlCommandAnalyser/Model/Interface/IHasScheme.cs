using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;

namespace Ask.Engine.ControlCommandAnalyser.Model.Interface
{
  public interface IHasScheme
  {
    /// <summary>
    /// Схема измерений.
    /// </summary>
    SchemeModel Scheme { get; set; }
  }
}
