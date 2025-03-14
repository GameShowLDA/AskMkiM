using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.ServicesTest.MINT
{
  public partial class MintControl
  {
    /// <summary>
    /// Сбрасывает состояние к начальному (неинициализированному).
    /// </summary>
    private void InitializeMintUI()
    {
      isDeviceInitialized = false;
      currentDeviceName = string.Empty;

      // ComboBox
      CmbMintDevice.IsEnabled = true;
      CmbMintDevice.SelectedItem = "<пусто>";

      // Кнопка Сброс
      BtnMintReset.Content = "Сброс устройства";
      BtnMintReset.IsEnabled = false;

      // Слайдеры
      SliderMintPinVoltage.Value = 0;
      SliderMintPinVoltage.IsEnabled = false;
      TextMintPinVoltage.Text = "0";
      TextMintPinVoltage.IsEnabled = false;

      SliderMintPitAmperage.Value = 0;
      SliderMintPitAmperage.IsEnabled = false;
      TextMintPitAmperage.Text = "0";
      TextMintPitAmperage.IsEnabled = false;

      // Радиокнопки и заземление
      RbMintK.IsChecked = false;
      RbMintK.IsEnabled = false;
      RbMintKplus.IsChecked = false;
      RbMintKplus.IsEnabled = false;
      BtnMintGround.Content = "Заземлить шину";
      BtnMintGround.IsEnabled = false;
      BtnMintGround.Tag = false;
      btnMintGroundStatus = false;

      // Источник питания
      RbMintPower12v.IsChecked = false;
      RbMintPower48v.IsChecked = false;
      RbMintPower12v.IsEnabled = false;
      RbMintPower48v.IsEnabled = false;

      // Кнопки ПИН / ПИТ
      BtnMintPin.Content = "Подключение ПИН";
      BtnMintPin.IsEnabled = false;
      isMintPinConnected = false;

      BtnMintPit.Content = "Подключение ПИТ";
      BtnMintPit.IsEnabled = false;
      isMintPitConnected = false;
    }

    /// <summary>
    /// Сбрасывает параметры устройства (не отключая его совсем).
    /// </summary>
    private async void ResetMintParameters()
    {
      // Возвращаемся к начальному, но устройство уже выбрано (isDeviceInitialized=true).
      SliderMintPinVoltage.Value = 0;
      SliderMintPinVoltage.IsEnabled = true;
      TextMintPinVoltage.Text = "0";
      TextMintPinVoltage.IsEnabled = true;

      SliderMintPitAmperage.Value = 0;
      SliderMintPitAmperage.IsEnabled = true;
      TextMintPitAmperage.Text = "0";
      TextMintPitAmperage.IsEnabled = true;

      RbMintK.IsChecked = false;
      RbMintKplus.IsChecked = false;
      RbMintK.IsEnabled = true;
      RbMintKplus.IsEnabled = true;

      BtnMintGround.Content = "Заземлить шину";
      BtnMintGround.IsEnabled = false;
      BtnMintGround.Tag = false;
      btnMintGroundStatus = false;

      RbMintPower12v.IsChecked = false;
      RbMintPower48v.IsChecked = false;
      RbMintPower12v.IsEnabled = true;
      RbMintPower48v.IsEnabled = true;

      BtnMintPin.Content = "Подключение ПИН";
      BtnMintPin.IsEnabled = false;
      isMintPinConnected = false;

      BtnMintPit.Content = "Подключение ПИТ";
      BtnMintPit.IsEnabled = false;
      isMintPitConnected = false;

      // Разрешаем выбор устройства
      CmbMintDevice.IsEnabled = true;

      // Логируем сброс
      if (!string.IsNullOrEmpty(currentDeviceName))
        await ShowMessageAsync($"Сброс {currentDeviceName}");
      else
        await ShowMessageAsync("Сброс устройства (не выбрано имя)");
    }

    /// <summary>
    /// Аналог UpdateUkshUI, главный метод для включения/выключения элементов и логирования.
    /// </summary>
    /// <param name="enable">Если true — устройство инициализировано (и не заблокировано модулями)</param>
    /// <param name="skipLog">Если true — не выводим лог.</param>
    public async Task UpdateMintUI(bool enable, bool skipLog)
    {
      // enable = устройство выбрано
      bool isPowerChosen = (RbMintPower12v.IsChecked ?? false) || (RbMintPower48v.IsChecked ?? false);
      bool lockNeeded = isMintPinConnected || isMintPitConnected;

      // 1) Основной флаг
      isDeviceInitialized = enable;

      // 2) Кнопка "Сброс устройства"
      BtnMintReset.IsEnabled = enable && !lockNeeded;

      // 3) Слайдеры, текстбоксы (блокируем, если уже подключены ПИН/ПИТ)
      SliderMintPinVoltage.IsEnabled = enable && !lockNeeded;
      TextMintPinVoltage.IsEnabled = enable && !lockNeeded;
      SliderMintPitAmperage.IsEnabled = enable && !lockNeeded;
      TextMintPitAmperage.IsEnabled = enable && !lockNeeded;

      // 4) Радиокнопки k/k+
      RbMintK.IsEnabled = enable && !lockNeeded && !btnMintGroundStatus;
      RbMintKplus.IsEnabled = enable && !lockNeeded && !btnMintGroundStatus;
      bool kChosen = (RbMintK.IsChecked ?? false) || (RbMintKplus.IsChecked ?? false);
      BtnMintGround.IsEnabled = enable && !lockNeeded && kChosen && !btnMintGroundStatus;

      // 5) Источник питания
      RbMintPower12v.IsEnabled = enable && !lockNeeded;
      RbMintPower48v.IsEnabled = enable && !lockNeeded;

      // 6) Кнопки ПИН/ПИТ
      //   Доступны, если (устройство выбрано) + (источник питания выбран, или уже подключены)
      bool canUsePinPit = enable && (isPowerChosen || isMintPinConnected || isMintPitConnected);
      BtnMintPin.IsEnabled = canUsePinPit;
      BtnMintPit.IsEnabled = canUsePinPit;

      // 7) ComboBox
      //   Если устройство ещё не выбрано (enable=false) => !enable = true => включён.
      //   Если устройство выбрано, но модули не подключены => !lockNeeded = true => включён.
      //   Иначе (модули подключены) => false => отключён.
      CmbMintDevice.IsEnabled = !lockNeeded || !enable;

      // 8) Логирование
      if (!skipLog)
      {
        if (enable)
        {
          await ShowMessageAsync($"Инициализация {currentDeviceName}");
        }
        else
        {
          if (!string.IsNullOrEmpty(currentDeviceName))
            await ShowMessageAsync($"Отключение {currentDeviceName}");
        }
      }
    }


  }
}
