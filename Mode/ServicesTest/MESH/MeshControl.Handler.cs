using System.Windows.Controls;
using System.Windows.Input;

namespace Mode.ServicesTest.MESH
{
  /// <summary>
  /// Логика взаимодействия для MeshControl.xaml.
  /// Контрол предназначен для управления устройством MESH, включая выбор устройства и переключение питания.
  /// </summary>
  public partial class MeshControl : UserControl
  {
    /// <summary>
    /// Флаг, указывающий, что питание устройства включено.
    /// </summary>
    private bool isPowerOn = false;

    /// <summary>
    /// Обрабатывает изменение выбранного элемента в ComboBox для выбора устройства MESH.
    /// Если выбран пустой элемент ("<пусто>") или пустая строка, происходит сброс параметров устройства,
    /// обновление пользовательского интерфейса и вывод сообщения о том, что устройство отключено.
    /// В противном случае устройство инициализируется и обновляется UI для включения возможности управления.
    /// </summary>
    /// <param name="sender">Источник события, ожидается ComboBox с выбором устройства.</param>
    /// <param name="e">Аргументы события изменения выбора.</param>
    private async void CmbMeshDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var selectedItem = CmbMeshDevice.SelectedItem as string;
      if (string.IsNullOrEmpty(selectedItem) || selectedItem == "<пусто>")
      {
        // Если устройство ранее было инициализировано, выполняется его сброс.
        if (isMeshInitialized)
        {
          InitializeMeshUI();
          await ShowMessageAsync("Устройство отключено");
        }
        isMeshInitialized = false;
        currentDeviceName = string.Empty;

        // Обновляем пользовательский интерфейс: все элементы управления переводятся в состояние "выключено".
        await UpdateMeshUI(false, skipLog: true);
      }
      else
      {
        isMeshInitialized = true;
        currentDeviceName = selectedItem;

        // Обновляем пользовательский интерфейс: включаем необходимые элементы управления, включая кнопку питания.
        await UpdateMeshUI(true, skipLog: false);
      }
    }

    /// <summary>
    /// Обрабатывает нажатие на кнопку "Включение питания" в режиме Toggle.
    /// Переключает состояние питания, обновляет текст кнопки, блокирует или разблокирует выбор устройства
    /// и выводит соответствующее сообщение в лог.
    /// </summary>
    /// <param name="sender">Источник события, ожидается Button для управления питанием.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void BtnMeshPower_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      // Переключаем состояние питания.
      isPowerOn = !isPowerOn;

      // Обновляем текст кнопки в зависимости от состояния питания.
      BtnMeshPower.Content = isPowerOn ? "ОСТАНОВИТЬ" : "ЗАПУСТИТЬ";

      // Выводим сообщение о включении или отключении питания.
      await ShowMessageAsync(isPowerOn
          ? $"Включение питания ({currentDeviceName})"
          : $"Отключение питания ({currentDeviceName})");

      // Блокируем ComboBox выбора устройства, если питание включено.
      CmbMeshDevice.IsEnabled = !isPowerOn;

      // При необходимости можно дополнительно обновить пользовательский интерфейс.
    }

    /// <summary>
    /// Асинхронно выводит заданное сообщение в лог посредством элемента protocolTextBox.
    /// </summary>
    /// <param name="text">Текст сообщения, которое требуется вывести в лог.</param>
    /// <returns>Задача, представляющая завершение асинхронной операции.</returns>
    private Task ShowMessageAsync(string text)
    {
      protocolTextBox?.ShowMessageAsync($"{text}\n");
      protocolTextBox?.ScrollToEnd();
      return Task.CompletedTask;
    }
  }
}