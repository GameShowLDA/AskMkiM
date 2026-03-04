using Ask.Core.Services.Usb;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.AdminPanel
{
  /// <summary>
  /// Логика взаимодействия для USBManagementControl.xaml
  /// </summary>
  public partial class USBManagementControl : UserControl
  {
    /// <summary>
    /// Менеджер USB-ключей, отвечающий за создание и управление ключевыми файлами.
    /// </summary>
    private UsbKeyManager usbKeyManager;

    /// <summary>
    /// Конструктор KeyManagementWindow.
    /// </summary>
    /// <remarks>
    /// Инициализирует компоненты окна и загружает доступные USB-накопители в выпадающий список.
    /// </remarks>
    public USBManagementControl()
    {
      InitializeComponent();
      usbKeyManager = new UsbKeyManager();
      LoadUsbDrives();
    }

    /// <summary>
    /// Загружает доступные USB-накопители в выпадающий список.
    /// </summary>
    /// <remarks>
    /// Очищает текущий список элементов и добавляет все доступные USB-накопители в комбобокс.
    /// Если нет доступных USB-накопителей, устанавливается начальный текст "Выберите USB-накопитель".
    /// </remarks>
    private void LoadUsbDrives()
    {
      usbDrivesComboBox.Items.Clear();
      usbDrivesComboBox.Items.Add("Выберите USB-накопитель");
      foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Removable && d.IsReady))
      {
        usbDrivesComboBox.Items.Add(drive.Name);
      }

      usbDrivesComboBox.SelectedIndex = 0;
    }

    /// <summary>
    /// Обработчик события нажатия кнопки "Создать ключ".
    /// </summary>
    /// <param name="sender">Объект, который вызвал событие.</param>
    /// <param name="e">Аргументы события.</param>
    /// <remarks>
    /// Проверяет, выбран ли USB-накопитель. Если выбран, создает ключевой файл на этом устройстве.
    /// В случае успешного создания выводит сообщение об успехе, иначе — предупреждение о необходимости выбора устройства.
    /// </remarks>
    private void CreateKeyButton_Click(object sender, RoutedEventArgs e)
    {
      if (usbDrivesComboBox.SelectedIndex > 0)
      {
        string selectedDrive = usbDrivesComboBox.SelectedItem.ToString();
        var drive = new DriveInfo(selectedDrive);
        usbKeyManager.CreateKeyFile(drive);
        Message.MessageBoxCustom.Show("Ключ успешно создан.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      else
      {
        Message.MessageBoxCustom.Show("Пожалуйста, выберите USB-накопитель.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    /// <summary>
    /// Обработчик события нажатия кнопки "Перезаписать ключ".
    /// </summary>
    /// <param name="sender">Объект, который вызвал событие.</param>
    /// <param name="e">Аргументы события.</param>
    /// <remarks>
    /// Проверяет, выбран ли USB-накопитель. Если выбран, перезаписывает ключевой файл на этом устройстве.
    /// В случае успешной перезаписи выводит сообщение об успехе, иначе — предупреждение о необходимости выбора устройства.
    /// </remarks>
    private void OverwriteKeyButton_Click(object sender, RoutedEventArgs e)
    {
      if (usbDrivesComboBox.SelectedIndex > 0)
      {
        string selectedDrive = usbDrivesComboBox.SelectedItem.ToString();
        var drive = new DriveInfo(selectedDrive);
        usbKeyManager.CreateKeyFile(drive);
        Message.MessageBoxCustom.Show("Ключ успешно перезаписан.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      else
      {
        Message.MessageBoxCustom.Show("Пожалуйста, выберите USB-накопитель.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    /// <summary>
    /// Обработчик события нажатия кнопки "Удалить ключ".
    /// </summary>
    /// <param name="sender">Объект, который вызвал событие.</param>
    /// <param name="e">Аргументы события.</param>
    /// <remarks>
    /// Проверяет, выбран ли USB-накопитель. Если выбран, удаляет ключевой файл с этого устройства.
    /// В случае успешного удаления выводит сообщение об успехе, если файл не найден — предупреждение.
    /// В противном случае — предупреждение о необходимости выбора устройства.
    /// </remarks>
    private void DeleteKeyButton_Click(object sender, RoutedEventArgs e)
    {
      if (usbDrivesComboBox.SelectedIndex > 0)
      {
        string selectedDrive = usbDrivesComboBox.SelectedItem.ToString();
        var drive = new DriveInfo(selectedDrive);
        string filePath = System.IO.Path.Combine(drive.RootDirectory.FullName, "usbkey.dat");
        if (File.Exists(filePath))
        {
          File.Delete(filePath);
          Message.MessageBoxCustom.Show("Ключ успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
          Message.MessageBoxCustom.Show("Ключ не найден на выбранном устройстве.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
      else
      {
        Message.MessageBoxCustom.Show("Пожалуйста, выберите USB-накопитель.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }
  }
}
