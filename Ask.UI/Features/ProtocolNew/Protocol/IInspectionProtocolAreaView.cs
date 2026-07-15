namespace Ask.UI.Features.ProtocolNew.Protocol
{
  /// <summary>
  /// Предоставляет контроллеру доступ к встроенной области итогового протокола.
  /// </summary>
  internal interface IInspectionProtocolAreaView
  {
    /// <summary>Получает или задаёт текст встроенного редактора итогового протокола.</summary>
    string ProtocolText { get; set; }

    /// <summary>
    /// Показывает или скрывает встроенный редактор вместе с занимаемой им колонкой и разделителем.
    /// </summary>
    /// <param name="isVisible">Признак видимости итоговой области.</param>
    void SetAreaVisible(bool isVisible);
  }
}
