using Ask.Core.Shared.Metadata.Enums.MetrologyEnums;

namespace Ask.Core.Shared.Metadata.View
{
  public interface IMetrologyServiceView
  {
    /// <summary>
    /// Открывает пользовательский элемент управления режима метрологии.
    /// </summary>
    public void OpenMetrologyMode(MetrologyType metrologyType);
  }
}
