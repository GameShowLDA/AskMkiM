namespace Ask.UI.Features.ProtocolNew.Protocol
{
  /// <summary>
  /// Размещает итоговый протокол во внешней области интерфейса владельца.
  /// </summary>
  public interface IInspectionProtocolHost
  {
    /// <summary>
    /// Показывает сформированный итоговый протокол.
    /// </summary>
    void ShowInspectionProtocol(string protocolText);

    /// <summary>
    /// Удаляет представление итогового протокола и очищает его содержимое.
    /// </summary>
    void ClearInspectionProtocol();
  }
}
