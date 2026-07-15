using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.UI.Features.ProtocolNew.Protocol
{
  /// <summary>
  /// Управляет отображением итогового протокола во встроенной области
  /// либо во внешнем хосте исполнительного окна.
  /// </summary>
  internal sealed class InspectionProtocolAreaController
  {
    /// <summary>Хранилище текста текущего итогового протокола.</summary>
    private readonly ProtocolStorageService _storage;

    /// <summary>Представление встроенной итоговой области.</summary>
    private readonly IInspectionProtocolAreaView _view;

    /// <summary>
    /// Создаёт контроллер итоговой области протокола.
    /// </summary>
    /// <param name="storage">Хранилище текущей пары протоколов.</param>
    /// <param name="view">Представление встроенной итоговой области.</param>
    public InspectionProtocolAreaController(
      ProtocolStorageService storage,
      IInspectionProtocolAreaView view)
    {
      _storage = storage;
      _view = view;
    }

    /// <summary>
    /// Отображает итоговый протокол во внешнем хосте или во встроенной правой области.
    /// </summary>
    /// <param name="protocolText">Сформированный текст итогового протокола.</param>
    /// <param name="externalHost">Необязательный внешний владелец итоговой области.</param>
    public void Show(string? protocolText, IInspectionProtocolHost? externalHost)
    {
      _storage.SetInspectionProtocol(protocolText);

      if (externalHost != null)
      {
        externalHost.ShowInspectionProtocol(_storage.InspectionProtocolText);
        return;
      }

      _view.ProtocolText = _storage.InspectionProtocolText;
      _view.SetAreaVisible(isVisible: true);
    }

    /// <summary>
    /// Очищает текст и скрывает встроенную либо внешнюю итоговую область перед новым выполнением.
    /// </summary>
    /// <param name="externalHost">Необязательный внешний владелец итоговой области.</param>
    public void Clear(IInspectionProtocolHost? externalHost)
    {
      _storage.ClearInspectionProtocol();
      _view.ProtocolText = string.Empty;
      _view.SetAreaVisible(isVisible: false);
      externalHost?.ClearInspectionProtocol();
    }
  }
}
