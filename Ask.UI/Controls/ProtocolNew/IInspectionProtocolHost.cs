namespace Ask.UI.Controls.ProtocolNew
{
  /// <summary>
  /// Размещает итоговый протокол в интерфейсе владельца <see cref="ProtocolUI"/>.
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
