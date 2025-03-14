using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mode.ServicesTest.MESH
{
  public partial class MeshControl
  {
    // Допустим, сделаем флаг isPowerOn
    private bool isPowerOn = false;

    /// <summary>
    /// Обработчик изменения выбора устройства в ComboBox.
    /// </summary>
    private async void CmbMeshDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var selectedItem = CmbMeshDevice.SelectedItem as string;
      if (string.IsNullOrEmpty(selectedItem) || selectedItem == "<пусто>")
      {
        // Если уже было инициализировано устройство, сбрасываем
        if (isMeshInitialized)
        {
          InitializeMeshUI();
          //await ShowMessageAsync($"Сброс устройства: {currentDeviceName}");
          await ShowMessageAsync("Устройство отключено");
        }
        isMeshInitialized = false;
        currentDeviceName = string.Empty;

        // Обновляем UI - всё выключено
        await UpdateMeshUI(false, skipLog: true);
      }
      else
      {
        isMeshInitialized = true;
        currentDeviceName = selectedItem;
        // Обновляем UI, включаем кнопку питания
        await UpdateMeshUI(true, skipLog: false);
      }
    }

    /// <summary>
    /// Кнопка "Включение питания" (Toggle-режим)
    /// </summary>
    private async void BtnMeshPower_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      // Переключаем isPowerOn
      isPowerOn = !isPowerOn;

      // Меняем текст кнопки
      BtnMeshPower.Content = isPowerOn ? "ОСТАНОВИТЬ" : "ЗАПУСТИТЬ";

      // Лог
      await ShowMessageAsync(isPowerOn
          ? $"Включение питания ({currentDeviceName})"
          : $"Отключение питания ({currentDeviceName})");

      // Если хотите блокировать ComboBox, пока питание включено,
      // добавьте что-то вроде:
      CmbMeshDevice.IsEnabled = !isPowerOn;

      // Можно ещё раз позвать UpdateMeshUI, если хотим ещё что-то включать/выключать.
      // Пока достаточно логики здесь.
    }

    /// <summary>
    /// Метод для вывода лога (заменяет Helpers.WriteInfo).
    /// </summary>
    private Task ShowMessageAsync(string text)
    {
      protocolTextBox?.ShowMessageAsync($"{text}\n");
      protocolTextBox?.ScrollToEnd();
      return Task.CompletedTask;
    }
  }
}
