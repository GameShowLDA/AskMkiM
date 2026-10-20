using Ask.Core.Services.TextSearch;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.Search;

public partial class SearchWindow
{
  private ITextSearchTarget? _target;
  private TextSearchSession? _session;
  private object[]? _legacyAreas;
  private int _legacyAreaIndex;
  private bool _updatingAreas;

  /// <summary>
  /// Предоставляет адаптер активного компонента перед каждым действием поиска.
  /// Отсутствие адаптера сохраняет существующий поиск редактора и транслятора.
  /// </summary>
  public Func<ITextSearchTarget?>? TargetProvider { get; set; }

  /// <summary>Подключает компонент к существующему окну поиска.</summary>
  /// <param name="target">Адаптер компонента; отсутствие включает прежний маршрут редактора.</param>
  public void AttachTarget(ITextSearchTarget? target)
  {
    if (!ReferenceEquals(_target, target))
    {
      _session?.Dispose();
      if (_target == null && target != null) ClearHighlights?.Invoke();
      _target = target;
      _session = target == null ? null : new TextSearchSession(target);
      SearchStatus.Text = string.Empty;
    }

    bool replaceAllowed = target == null || target is IReplaceTextSearchTarget;
    ToggleArrow.Visibility = replaceAllowed ? Visibility.Visible : Visibility.Hidden;
    if (!replaceAllowed && _isExpanded) _ = CollapseReplaceRowAsync();
    UpdateTargetAreas();
  }

  /// <summary>Обновляет области подключённого компонента или восстанавливает области редактора.</summary>
  private void UpdateTargetAreas()
  {
    _updatingAreas = true;
    try
    {
      if (_target == null)
      {
        if (_legacyAreas == null) return;
        searchAreaParameters.Items.Clear();
        foreach (var item in _legacyAreas) searchAreaParameters.Items.Add(item);
        searchAreaParameters.SelectedIndex = _legacyAreaIndex;
        _legacyAreas = null;
        SearchStatus.Visibility = Visibility.Visible;
        return;
      }

      if (_legacyAreas == null)
      {
        _legacyAreas = searchAreaParameters.Items.Cast<object>().ToArray();
        _legacyAreaIndex = searchAreaParameters.SelectedIndex;
      }
      var documents = _target.GetSearchDocuments();
      var scopeProvider = _target as ITextSearchScopeProvider;
      string? selected = scopeProvider != null ? scopeProvider.SelectedSearchDocumentId
        : (searchAreaParameters.SelectedItem as ComboBoxItem)?.Tag as string;
      var areas = new[] { new SearchDocument("", scopeProvider?.AllDocumentsTitle ?? "Все области компонента", "") }
        .Concat(documents).ToArray();
      var current = searchAreaParameters.Items.Cast<ComboBoxItem>().ToArray();
      if (current.Length != areas.Length || current.Where((item, index) =>
        !Equals(item.Tag, areas[index].Id) || !Equals(item.Content, areas[index].Title)).Any())
      {
        searchAreaParameters.Items.Clear();
        foreach (var area in areas) searchAreaParameters.Items.Add(new ComboBoxItem { Content = area.Title, Tag = area.Id });
        searchAreaParameters.SelectedIndex = Math.Max(0, Array.FindIndex(areas, area => area.Id == selected));
      }
      SearchStatus.Visibility = Visibility.Visible;
    }
    finally { _updatingAreas = false; }
  }

  /// <summary>Сохраняет выбор области только для компонента, явно поддерживающего эту возможность.</summary>
  private void RememberTargetArea()
  {
    if (_target is not ITextSearchScopeProvider scopeProvider) return;
    scopeProvider.SelectedSearchDocumentId = GetTargetArea();
    _session?.Dispose();
    _session = new TextSearchSession(_target);
    SearchStatus.Text = string.Empty;
  }

  /// <summary>Переключает сессию на текущий компонент рабочей области.</summary>
  private void SynchronizeTarget()
  {
    if (TargetProvider != null) AttachTarget(TargetProvider());
  }

  /// <summary>Создаёт запрос из полей окна.</summary>
  /// <returns>Текст и параметры буквального поиска.</returns>
  private TextSearchQuery GetTargetQuery() => new(SearchTextBox.Text, caseButton.IsChecked == true, wholeWordButton.IsChecked == true);

  /// <summary>Возвращает выбранную область подключённого компонента.</summary>
  /// <returns>Идентификатор документа или все документы.</returns>
  private string? GetTargetArea() => (searchAreaParameters.SelectedItem as ComboBoxItem)?.Tag is string id && id.Length > 0 ? id : null;

  /// <summary>Выполняет поиск подключённого компонента и отображает состояние операции.</summary>
  /// <param name="action">Действие кнопки поиска.</param>
  private async Task SearchTargetAsync(string action)
  {
    var session = _session;
    if (session == null) return;
    try
    {
      var searchAction = action == "FindPrevious" ? TextSearchAction.Previous
        : action == "FindAll" ? TextSearchAction.FindAll : TextSearchAction.Next;
      if (await session.SearchAsync(GetTargetQuery(), searchAction, GetTargetArea()) && ReferenceEquals(session, _session))
        UpdateTargetStatus();
    }
    catch (Exception exception)
    {
      if (ReferenceEquals(session, _session)) SearchStatus.Text = "Ошибка поиска: " + exception.Message;
      Ask.LogLib.LoggerUtility.LogException("Ошибка поиска в подключённом компоненте", exception);
    }
  }

  /// <summary>Выполняет замену, если подключённый компонент поддерживает редактирование.</summary>
  /// <param name="replaceAll">Признак замены всех совпадений.</param>
  private async Task ReplaceTargetAsync(bool replaceAll)
  {
    var session = _session;
    if (session == null || _target is not IReplaceTextSearchTarget) return;
    try
    {
      await session.ReplaceAsync(GetTargetQuery(), ReplaceTextBox.Text, replaceAll, GetTargetArea());
      if (ReferenceEquals(session, _session)) UpdateTargetStatus();
    }
    catch (Exception exception)
    {
      if (ReferenceEquals(session, _session)) SearchStatus.Text = "Ошибка замены: " + exception.Message;
      Ask.LogLib.LoggerUtility.LogException("Ошибка замены в подключённом компоненте", exception);
    }
  }

  /// <summary>Показывает позицию среди найденных совпадений.</summary>
  private void UpdateTargetStatus() => SearchStatus.Text = _session == null || _session.Matches.Count == 0
    ? "0 из 0" : $"{_session.CurrentIndex + 1} из {_session.Matches.Count}";

  /// <summary>Отображает счётчик существующего поиска редактора в потоке интерфейса.</summary>
  /// <param name="progress">Номер и число найденных совпадений.</param>
  private void OnLegacySearchProgressChanged(Ask.Core.Services.EventCore.Events.SearchEvents.SearchProgressChanged progress)
  {
    if (!Dispatcher.CheckAccess()) { Dispatcher.InvokeAsync(() => OnLegacySearchProgressChanged(progress)); return; }
    if (_target != null) return;
    SearchStatus.Visibility = Visibility.Visible;
    SearchStatus.Text = $"{progress.Current} из {progress.Total}";
  }

  /// <summary>Отменяет запросы и освобождает подключение при закрытии окна.</summary>
  private void ReleaseTarget()
  {
    _session?.Dispose();
    _session = null;
    _target = null;
    UpdateTargetAreas();
  }
}
