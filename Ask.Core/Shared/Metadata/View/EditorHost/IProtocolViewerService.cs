using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Core.Shared.Metadata.View.EditorHost
{
  /// <summary>
  /// Отвечает за отображение протоколов и отчётов в режиме просмотра.
  /// </summary>
  public interface IProtocolViewerService
  {
    /// <summary>
    /// Открывает протокол в просмотрщике.
    /// </summary>
    /// <param name="protocol">Модель протокола.</param>
    /// <param name="showInSoftware">Показывать в ПО или во внешнем виде.</param>
    void ViewProtocol(ProtocolModel protocol, bool showInSoftware);
  }
}
