using Ask.Core.Shared.Metadata.Enums.MetrologyEnums;

namespace Ask.Core.Shared.Metadata.View
{
  public interface IMetrologyServiceView
  {
    /// <summary>
    /// Открывает пользовательский элемент управления режима метрологии.
    /// </summary>
    public void OpenMetrologyMode(MetrologyType metrologyType);

    /// <summary>
    /// Открывает тест погрешности измерения старого тестера АСК по коду теста MKI.
    /// </summary>
    /// <param name="testCode">Код теста старой программы MKI.</param>
    public void OpenLegacyAskMeasurementAccuracyTest(string testCode);
  }
}
