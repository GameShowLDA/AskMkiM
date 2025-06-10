// MkrContent.xaml.cs
using Mode.Models;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mode.ServicesTest.MKR
{
  public partial class MkrContent : UserControl
  {
    /// <summary>
    /// Ссылка на родительский контрол MkrControl.
    /// Эту ссылку мы установим из MkrControl.xaml.cs сразу после InitializeComponent().
    /// </summary>
    public MkrControl ParentControl { get; set; }

    public MkrContent()
    {
      InitializeComponent();
      RbOff.IsChecked = true;
      RbOff.IsHitTestVisible = false;
    }

    /// <summary>
    /// Сработает, когда пользователь сменит устройство в ComboBox.
    /// Вместо того, чтобы здесь самому искать device в списке и подключать/инициализировать,
    /// просто «делегируем» всю работу родителю.
    /// </summary>
    private async void SerialNumComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (ParentControl == null) return;

      // Получаем строку вида "шасси.номер"
      var selectedString = SerialNumComboBox.SelectedItem as string;

      // Вызываем асинхронный метод у родителя, 
      // он уже внутри себя вызовет ProtocolSelfCheckControl и все остальное.
      await ParentControl.HandleSerialSelectionAsync(selectedString);
    }

    /// <summary>
    /// Кнопка «Сброс устройства» нажата.
    /// Делегируем сброс родителю.
    /// </summary>
    private async void BtnMkrReset_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (ParentControl == null) return;
      await ParentControl.HandleResetDeviceAsync();
    }

    /// <summary>
    /// При клике на RadioButton (выбор группы шин).
    /// Родитель сам разберётся, какую шину подключать/отключать.
    /// </summary>
    private async void RadioButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (ParentControl == null) return;
      if (sender is RadioButton rb)
      {
        // здесь просто передаём имя радиокнопки, 
        // или можно передать сам RadioButton, если хотите.
        await ParentControl.HandleBusRadioClickAsync(rb);
      }
    }

    /// <summary>
    /// Текст в поиске изменился — делегируем родителю,
    /// чтобы он подправил pointsView.Filter. 
    /// </summary>
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (ParentControl == null) return;
      string searchText = SearchBox.Text.Trim();
      ParentControl.HandleSearchTextChanged(searchText);
    }

    /// <summary>
    /// Нажали на кнопку-точку. 
    /// Делегируем родителю, чтобы он уже сделал ConnectRelay/DisconnectRelay. 
    /// В MkrPointModel у нас есть флаги A/B, и у родителя доступ к currentDevice.
    /// </summary>
    private void Point_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (ParentControl == null) return;

      if (sender is Button btn && btn.DataContext is MkrPointModel point)
      {
        ParentControl.HandlePointClick(point);
      }
    }

    /// <summary>
    /// Когда мышь ушла с контекстного меню, 
    /// в родительском контроле уже будет logic, какая конкретно точка подключается к каким шинам.
    /// </summary>
    private async void ContextMenu_MouseLeave(object sender, MouseEventArgs e)
    {
      if (ParentControl == null) return;

      // Просто передаем сам ContextMenu, 
      // а родитель внутри разберёт, какая кнопка (PlacementTarget) и какой MkrPointModel.
      await ParentControl.HandleContextMenuMouseLeaveAsync(sender as ContextMenu);
    }
  }
}