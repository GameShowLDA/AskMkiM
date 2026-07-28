using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Features.ServiceTools.Gpt
{
  /// <summary>
  /// Логика взаимодействия для GPTController.xaml.
  /// </summary>
  public partial class GPTController : UserControl
  {
    private readonly Dictionary<string, UserControl> modeControls = new();
    private readonly GptDeviceContext deviceContext = new();
    private RadioButton? currentModeButton;
    private bool isSwitchingMode;

    /// <summary>
    /// Пробойная установка текущей вкладки управления.
    /// </summary>
    internal Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.IBreakdownTester? Device
    {
      set => deviceContext.Device = value;
    }

    /// <summary>
    /// Контроллер для управления режимами GPT.
    /// </summary>
    public GPTController()
    {
      InitializeComponent();
      DataContext = this;
      AcwModeButton.IsChecked = true;
    }

    /// <summary>
    /// Получает или устанавливает выбранный контент режима.
    /// </summary>
    public object SelectedModeContent { get; set; }

    /// <summary>
    /// Обрабатывает событие выбора режима.
    /// В зависимости от выбранного режима загружает соответствующий элемент управления в контейнер.
    /// </summary>
    /// <param name="sender">Источник события, обычно радио кнопка.</param>
    /// <param name="e">Данные события.</param>
    private async void Mode_Checked(object sender, RoutedEventArgs e)
    {
      if (isSwitchingMode || sender is not RadioButton radioButton)
      {
        return;
      }

      var mode = radioButton.Tag as string;
      if (string.IsNullOrWhiteSpace(mode))
      {
        return;
      }

      isSwitchingMode = true;
      try
      {
        var modeControl = GetOrCreateModeControl(mode);
        var activeMode = modeControls.Values
          .OfType<IGptModeControl>()
          .FirstOrDefault(control => control.IsModeActive);

        if (activeMode != null
            && modeControl is IGptModeControl targetMode
            && activeMode != targetMode)
        {
          if (!await targetMode.ActivateModeAsync())
          {
            RestorePreviousModeSelection();
            return;
          }

          activeMode.DeactivateMode();
        }
        else if (activeMode != null && modeControl is not IGptModeControl)
        {
          activeMode.DeactivateMode();
        }

        ShowModeControl(modeControl);
        currentModeButton = radioButton;
      }
      catch (Exception ex)
      {
        GptUiOperation.ReportError("переключение вкладки режима", ex);
        RestorePreviousModeSelection();
      }
      finally
      {
        isSwitchingMode = false;
      }
    }

    /// <summary>
    /// Возвращает сохранённый контрол режима или создаёт его при первом обращении.
    /// </summary>
    /// <param name="mode">Идентификатор режима.</param>
    /// <returns>Контрол выбранного режима.</returns>
    private UserControl GetOrCreateModeControl(string mode)
    {
      if (modeControls.TryGetValue(mode, out var modeControl))
      {
        return modeControl;
      }

      modeControl = mode switch
      {
        "Mode1" => new Modes.AcwMode(deviceContext),
        "Mode2" => new Modes.DcwMode(deviceContext),
        "Mode3" => new Modes.IrMode(deviceContext),
        "Mode4" => new Modes.SettingsGPT(deviceContext),
        _ => throw new InvalidOperationException($"Неизвестный режим GPT: {mode}.")
      };
      modeControls.Add(mode, modeControl);
      return modeControl;
    }

    /// <summary>
    /// Отображает контрол выбранного режима.
    /// </summary>
    /// <param name="modeControl">Контрол режима.</param>
    private void ShowModeControl(UserControl modeControl)
    {
      ModeContent.Children.Clear();
      ModeContent.Children.Add(modeControl);
    }

    /// <summary>
    /// Возвращает выбор на предыдущую вкладку после неудачного переключения оборудования.
    /// </summary>
    private void RestorePreviousModeSelection()
    {
      if (currentModeButton != null)
      {
        currentModeButton.IsChecked = true;
      }
    }
  }
}
