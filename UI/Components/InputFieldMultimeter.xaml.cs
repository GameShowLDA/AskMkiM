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
using Utilities.Events;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для InputField.xaml.
  /// </summary>
  public partial class InputFieldMultimeter : UserControl
  {
    /// <summary>
    /// Первая точка.
    /// </summary>
    public string FirstPoint
    {
      get => FirstTextBox.Text;
      set => FirstTextBox.Text = value;
    }

    /// <summary>
    /// Вторая точка.
    /// </summary>
    public string SecondPoint
    {
      get => SecondTextBox.Text;
      set => SecondTextBox.Text = value;
    }

    /// <summary>
    /// Электрический параметр.
    /// </summary>
    public string ElectricalParameter
    {
      get => ElectricalTextBox.Text;
      set => ElectricalTextBox.Text = value;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="InputFieldMultimeter"/>.
    /// </summary>
    public InputFieldMultimeter()
    {
      InitializeComponent();
      SubscribeToValidationEvents();
    }

    /// <summary>
    /// Подписка на глобальные события валидации.
    /// </summary>
    private void SubscribeToValidationEvents()
    {
      InputValidationEvents.OnInvalidFirstPoint += HighlightFirstTextBox;
      InputValidationEvents.OnInvalidSecondPoint += HighlightSecondTextBox;
      InputValidationEvents.OnInvalidElectricalParameter += HighlightElectricalTextBox;
      InputValidationEvents.OnDuplicatePoints += HighlightBothPoints;
    }

    /// <summary>
    /// Подсветка поля первой точки.
    /// </summary>
    private void HighlightFirstTextBox()
    {
      FirstTextBox.DataError();
    }

    /// <summary>
    /// Подсветка поля второй точки.
    /// </summary>
    private void HighlightSecondTextBox()
    {
      SecondTextBox.DataError();
    }

    /// <summary>
    /// Подсветка поля параметра.
    /// </summary>
    private void HighlightElectricalTextBox()
    {
      ElectricalTextBox.DataError() ;
    }

    /// <summary>
    /// Подсветка обоих точек при совпадении.
    /// </summary>
    private void HighlightBothPoints()
    {
      SecondTextBox.DataError();
    }
  }
}
