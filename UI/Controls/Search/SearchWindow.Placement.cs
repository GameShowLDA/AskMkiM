using System.Windows;

namespace UI.Controls.Search;

public partial class SearchWindow
{
  private FrameworkElement? _placementAnchor;

  /// <summary>
  /// Верхняя панель владельца, к нижнему краю которой прикрепляется окно поиска.
  /// При отсутствии используется прежний отступ от верхнего края владельца.
  /// </summary>
  public FrameworkElement? PlacementAnchor
  {
    get => _placementAnchor;
    set
    {
      if (ReferenceEquals(_placementAnchor, value)) return;
      if (_placementAnchor != null) _placementAnchor.SizeChanged -= PlacementAnchor_SizeChanged;
      _placementAnchor = value;
      if (_placementAnchor != null) _placementAnchor.SizeChanged += PlacementAnchor_SizeChanged;
      if (IsVisible) UpdatePosition();
    }
  }

  /// <summary>Обновляет положение после перерасчёта размеров верхней панели.</summary>
  /// <param name="sender">Верхняя панель владельца.</param>
  /// <param name="args">Изменение размеров панели.</param>
  private void PlacementAnchor_SizeChanged(object sender, SizeChangedEventArgs args) => UpdatePosition();

  /// <summary>Удаляет подписки на прежнее окно владельца перед повторным подключением.</summary>
  private void DetachPlacementOwner()
  {
    if (_parentWindow == null) return;
    _parentWindow.LocationChanged -= ParentWindow_LocationChanged;
    _parentWindow.SizeChanged -= ParentWindow_SizeChanged;
    _parentWindow.StateChanged -= ParentWindow_StateChanged;
  }

  /// <summary>Освобождает привязку к верхней панели после окончательного закрытия окна.</summary>
  /// <param name="sender">Закрытое окно поиска.</param>
  /// <param name="args">Событие закрытия.</param>
  private void SearchWindow_Closed(object? sender, EventArgs args)
  {
    IsLoaded = false;
    Ask.Core.Services.EventCore.Services.EventAggregator.Unsubscribe<Ask.Core.Services.EventCore.Events.SearchEvents.SearchProgressChanged>(OnLegacySearchProgressChanged);
    DetachPlacementOwner();
    if (_placementAnchor != null) _placementAnchor.SizeChanged -= PlacementAnchor_SizeChanged;
    _placementAnchor = null;
    _parentWindow = null;
  }
}
