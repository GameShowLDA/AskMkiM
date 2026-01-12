using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для BusSelector.xaml
  /// </summary>
  public partial class BusSelector : UserControl
  {
    #region Видимость элемента управления 

    //public static readonly DependencyProperty IsVisibleProperty =
    //  DependencyProperty.Register(nameof(IsVisible), typeof(bool), typeof(BusSelector),
    //    new PropertyMetadata(true));

    //public bool IsVisible
    //{
    //  get => (bool)GetValue(IsVisibleProperty);
    //  set => SetValue(IsVisibleProperty, value);
    //}

    #endregion

    #region Активный выбор (показывающее поле)

    /// <summary>
    /// Активная группа шин (AB1..AB4).
    /// </summary>
    public SwitchingBusNew ActiveBus { get; private set; } = SwitchingBusNew.AB1;

    /// <summary>
    /// Активный чекбокс индексом.
    /// </summary>
    public int ActiveBusIndex => (int)ActiveBus;

    #endregion

    private bool _internalChange;

    public BusSelector()
    {
      InitializeComponent();

      _internalChange = true;
      SetActive(SwitchingBusNew.AB1);
      _internalChange = false;
    }

    /// <summary>
    /// Включить ровно один чекбокс по выбору.
    /// </summary>
    private void SetActive(SwitchingBusNew bus)
    {
      ActiveBus = bus;

      BusACheckBox.IsChecked = bus == SwitchingBusNew.AB1;
      BusBCheckBox.IsChecked = bus == SwitchingBusNew.AB2;
      BusCCheckBox.IsChecked = bus == SwitchingBusNew.AB3;
      BusDCheckBox.IsChecked = bus == SwitchingBusNew.AB4;
    }

    private SwitchingBusNew NextBus(SwitchingBusNew current) => current switch
    {
      SwitchingBusNew.AB1 => SwitchingBusNew.AB2,
      SwitchingBusNew.AB2 => SwitchingBusNew.AB3,
      SwitchingBusNew.AB3 => SwitchingBusNew.AB4,
      _ => SwitchingBusNew.AB1
    };

    /// <summary>
    /// Пользователь включил какой-то чекбокс и делаем его единственным активным.
    /// </summary>
    private void Switch_Checked(object sender, RoutedEventArgs e)
    {
      if (_internalChange) return;
      if (sender is not CheckBox cb) return;

      _internalChange = true;

      if (cb == BusACheckBox)
        SetActive(SwitchingBusNew.AB1);
      else if (cb == BusBCheckBox)
        SetActive(SwitchingBusNew.AB2);
      else if (cb == BusCCheckBox)
        SetActive(SwitchingBusNew.AB3);
      else if (cb == BusDCheckBox)
        SetActive(SwitchingBusNew.AB4);

      _internalChange = false;
    }

    /// <summary>
    /// Если пользователь нажал на активный чекбокс (пытается выключить),
    /// мы не позволяем остаться "ничего не выбрано":
    /// активный выключается и включается следующий по списку.
    /// </summary>
    private void Switch_Unchecked(object sender, RoutedEventArgs e)
    {
      if (_internalChange) return;
      if (sender is not CheckBox cb) return;

      var wasActive = (cb == BusACheckBox && ActiveBus == SwitchingBusNew.AB1)
                   || (cb == BusBCheckBox && ActiveBus == SwitchingBusNew.AB2)
                   || (cb == BusCCheckBox && ActiveBus == SwitchingBusNew.AB3)
                   || (cb == BusDCheckBox && ActiveBus == SwitchingBusNew.AB4);

      _internalChange = true;

      if (wasActive)
      {
        SetActive(NextBus(ActiveBus));
      }
      else
      {
        SetActive(ActiveBus);
      }

      _internalChange = false;
    }
  }
}
