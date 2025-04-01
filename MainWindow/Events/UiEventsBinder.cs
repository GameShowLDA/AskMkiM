using System;
using System.Windows;
using AppConfiguration.Base;
using ICSharpCode.AvalonEdit;

namespace MainWindowProgram.Events
{
  /// <summary>
  /// Подписывает обработчики для событий пользовательского интерфейса (UI).
  /// </summary>
  public class UiEventsBinder
  {
    private readonly MainWindow _mainWindow;

    public UiEventsBinder(MainWindow mainWindow)
    {
      _mainWindow = mainWindow;
    }

    public void Bind()
    {
      EventAggregator.TextEditorActive += OnTextEditorActive;
      EventAggregator.TextEditorClosing += OnTextEditorClosing;
      EventAggregator.SearchWindowClosing += OnSearchWindowClosing;
      EventAggregator.SearchWindowAtivated += OnSearchWindowActivated;
      EventAggregator.RequestShowProgress += OnRequestShowProgress;
      EventAggregator.RequestCloseProgress += OnRequestCloseProgress;
    }

    private void OnTextEditorActive(bool isActive)
    {
      _mainWindow.IsTextEditorActive = isActive;
      Visibility visibility = isActive ? Visibility.Visible : Visibility.Collapsed;

      _mainWindow.fileActionsSeparator.Visibility = visibility;
      _mainWindow.saveMenuItem.Visibility = visibility;
      _mainWindow.saveAsMenuItem.Visibility = visibility;
      _mainWindow.printMenuItem.Visibility = visibility;
      _mainWindow.searchMenuItem.Visibility = visibility;
      _mainWindow.compareMenuItem.Visibility = visibility;
    }
    private void OnTextEditorClosing(bool isActive, string name)
    {
      if (isActive)
      {
        OnTextEditorActive(false);
      }
    }
    private void OnSearchWindowClosing(bool closing) { /* TODO */ }
    private void OnSearchWindowActivated(bool activated) { /* TODO */ }
    private void OnRequestShowProgress() { /* TODO */ }
    private void OnRequestCloseProgress() { /* TODO */ }
  }
}
