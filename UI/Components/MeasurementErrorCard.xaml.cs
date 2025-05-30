using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DataBaseConfiguration.Models.MeasurementError;

namespace UI.Components
{
  /// <summary>
  /// Карточка для настройки погрешностей измерений.
  /// </summary>
  public partial class MeasurementErrorCard : UserControl
  {
    public MeasurementErrorCard()
    {
      InitializeComponent();
      Loaded += MeasurementErrorCard_Loaded;
    }

    private void MeasurementErrorCard_Loaded(object sender, RoutedEventArgs e)
    {
      UpdateDisplay();
    }

    public static readonly DependencyProperty TypeCommandProperty = DependencyProperty.Register(
        nameof(TypeCommand),
        typeof(MeasurementErrorEntity.TypeCommand),
        typeof(MeasurementErrorCard),
        new PropertyMetadata(MeasurementErrorEntity.TypeCommand.KC, OnTypeCommandChanged));

    /// <summary>
    /// Тип команды, определяющий режим метрологии.
    /// </summary>
    public MeasurementErrorEntity.TypeCommand TypeCommand
    {
      get => (MeasurementErrorEntity.TypeCommand)GetValue(TypeCommandProperty);
      set => SetValue(TypeCommandProperty, value);
    }

    private static void OnTypeCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is MeasurementErrorCard card)
      {
        card.UpdateDisplay();
      }
    }

    private void UpdateDisplay()
    {
      var (title, unit) = GetCommandInfo(TypeCommand);
      TitleTextBlock.Text = title;
      UnitTextBlock.Text = unit;
    }

    private static (string DisplayName, string Unit) GetCommandInfo(MeasurementErrorEntity.TypeCommand type)
    {
      var memberInfo = typeof(MeasurementErrorEntity.TypeCommand).GetMember(type.ToString()).FirstOrDefault();
      var attribute = memberInfo?.GetCustomAttributes(typeof(CommandInfoAttribute), false)
          .FirstOrDefault() as CommandInfoAttribute;

      return attribute != null
          ? (attribute.DisplayName, attribute.Unit)
          : (type.ToString(), string.Empty);
    }
  }
}
