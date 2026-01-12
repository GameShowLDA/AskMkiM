using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram.Engine
{
  /// <summary>
  /// Отвечает за настройку визуального интерфейса и базовые GUI-события.
  /// </summary>
  internal static class GuiInitializer
  {
    /// <summary>
    /// Применяет базовую инициализацию GUI.
    /// </summary>
    /// <param name="adminPanel">UI-элемент панели администратора.</param>
    /// <param name="infoBlock">Информационный текстовый блок.</param>
    /// <param name="bindings">Коллекция биндингов команды окна.</param>
    /// <param name="command">Команда, которую нужно привязать.</param>
    /// <param name="handler">Метод-обработчик команды.</param>
    public static void Apply(this MainWindow window)
    {
      MainWindow._infoBlock = window.InfoBlock;
      window.Admin.Visibility = Visibility.Collapsed;
      window.CommandBindings.Add(new CommandBinding(ActivateMenuItemCommand, ExecuteActivateMenuItem));
      LogInformation("Главное окно инициализировано.");
    }

    /// <summary>
    /// Команда для активации пункта меню.
    /// </summary>
    public static readonly RoutedUICommand ActivateMenuItemCommand = new RoutedUICommand("ActivateMenuItem", "ActivateMenuItemCommand", typeof(MainWindow));

    /// <summary>
    /// Выполняет команду активации пункта меню при срабатывании команды.
    /// Генерирует событие клика для пункта меню.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы выполненной команды.</param>
    private static void ExecuteActivateMenuItem(object sender, ExecutedRoutedEventArgs e)
    {
      if (e.Parameter is MenuItem menuItem)
      {
        menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        LogInformation($"Menu item '{menuItem.Header}' activated with Enter key.");
      }
    }
  }
}
