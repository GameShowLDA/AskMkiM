using System.Windows;
using System.Windows.Controls;
using Mode.Models;
using static Utilities.LoggerUtility;

namespace Mode.Base.SearchDevices
{
  static internal class InputControlSettings
  {
    /// <summary>
    /// Перечисление для различных электрических параметров.
    /// </summary>
    public enum ElectricParameter
    {
      /// <summary>
      /// Сопротивление.
      /// </summary>
      Resistance,

      /// <summary>
      /// Ёмкость.
      /// </summary>
      Capacitance,

      /// <summary>
      /// Сопротивление изоляции.
      /// </summary>
      InsulationResistance,

      None,
    }

    /// <summary>
    /// Словарь, содержащий описания электрических параметров и их единицы измерения.
    /// Ключом является тип электрического параметра, а значением — кортеж, содержащий
    /// описание параметра и его единицу измерения.
    /// </summary>
    static private Dictionary<ElectricParameter, Tuple<string, string>> ElectricParameterDescriptions = new Dictionary<ElectricParameter, Tuple<string, string>>
    {
      { ElectricParameter.Resistance, Tuple.Create("Сопротивление", "Ом") },
      { ElectricParameter.Capacitance, Tuple.Create("Ёмкость", "нФ") },
      { ElectricParameter.InsulationResistance, Tuple.Create("Сопротивление изоляции", "МОм") }
    };

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    static internal StackPanel InitializeSettings(out DataElectricModel measurementDataModel, ElectricParameter electricParameter = ElectricParameter.None)
    {
      DataElectricModel tempModel = null;
      Application.Current.Dispatcher.Invoke(() =>
      {
        tempModel = new DataElectricModel(); 
      });

      measurementDataModel = tempModel;
      
      StackPanel contentStack = CreateContentStack();
      AddMeasurementBorders(measurementDataModel, contentStack);

      if (electricParameter != ElectricParameter.None)
      {
        AddResistanceControls(measurementDataModel, contentStack, electricParameter);
      }

      // Вызов событий
      DataPointEvent(measurementDataModel, electricParameter);
      ResistancePointEvent(measurementDataModel, electricParameter);

      return contentStack;
    }

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    static internal StackPanel InitializeSettings(out TestDataModel measurementDataModel, ElectricParameter electricParameter = ElectricParameter.None)
    {
      measurementDataModel = new TestDataModel();
      StackPanel contentStack = CreateContentStack();
      AddMeasurementBorders(measurementDataModel, contentStack);
      DataPointEvent(measurementDataModel, electricParameter);
      return contentStack;
    }

    /// Создает StackPanel для размещения элементов управления.
    /// Устанавливает отступы и определяет, на какой строке Grid он должен отображаться.
    /// </summary>
    /// <returns>StackPanel для дальнейшего использования.</returns>
    static private StackPanel CreateContentStack()
    {
      try
      {
        LogInformation("Создание панели с элементами управления");

        // Создание и настройка StackPanel
        StackPanel contentStack = new StackPanel
        {
          Margin = new Thickness(10, 20, 10, 20)
        };
        Grid.SetRow(contentStack, 1);

        // Возвращаем StackPanel
        return contentStack;
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при создании панели с элементами управления: {ex}");
        throw;
      }
    }

    /// <summary>
    /// Добавляет элементы управления для первой и второй измерительных точек в переданный StackPanel.
    /// Настраивает стиль и текст для каждого поля ввода.
    /// </summary>
    /// <param name="contentStack">StackPanel, в который добавляются элементы управления.</param>
    static private void AddMeasurementBorders(DataPointModel measurementDataModel, StackPanel contentStack)
    {
      LogInformation("Создание панелей ввода для выбора точек замыкания");

      measurementDataModel.FirstPointBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];
      measurementDataModel.FirstPointData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
      measurementDataModel.FirstPointData.Tag = "Первая точка вида х.х.х";
      measurementDataModel.FirstPointData.Text = "Первая точка вида х.х.х";
      measurementDataModel.FirstPointBorder.Child = measurementDataModel.FirstPointData;

      measurementDataModel.LastPointBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];
      measurementDataModel.LastPointData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
      measurementDataModel.LastPointData.Tag = "Вторая точка вида х.х.х";
      measurementDataModel.LastPointData.Text = "Вторая точка вида х.х.х";
      measurementDataModel.LastPointBorder.Child = measurementDataModel.LastPointData;

      contentStack.Children.Add(measurementDataModel.FirstPointBorder);
      contentStack.Children.Add(measurementDataModel.LastPointBorder);
    }

    /// <summary>
    /// Добавляет элементы управления для ввода электрического параметра' и единицы измерения в переданный StackPanel.
    /// Настраивает сетку для правильного расположения полей ввода и текста.
    /// </summary>
    /// <param name="contentStack">StackPanel, в который добавляются элементы управления.</param>
    static private void AddResistanceControls(DataElectricModel measurementDataModel, StackPanel contentStack, ElectricParameter electricParametr)
    {
      LogInformation($"Создание панели ввода значения электрического параметра в {electricParametr}");
      measurementDataModel.ElectricParameterBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];
      ElectricParameterDescriptions.TryGetValue(electricParametr, out Tuple<string, string> value);

      Grid resistanceGrid = new Grid();
      var resistanceColumn1 = new ColumnDefinition
      {
        Width = new GridLength(1, GridUnitType.Star)
      };
      var resistanceColumn2 = new ColumnDefinition
      {
        Width = new GridLength(1, GridUnitType.Auto)
      };

      resistanceGrid.ColumnDefinitions.Add(resistanceColumn1);
      resistanceGrid.ColumnDefinitions.Add(resistanceColumn2);

      measurementDataModel.ElectricParameterData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
      measurementDataModel.ElectricParameterData.Tag = $"{value.Item1} в {value.Item2}";
      measurementDataModel.ElectricParameterData.Text = $"{value.Item1}";

      var resistanceUnitTextBox = new TextBox
      {
        Style = (Style)Application.Current.Resources["MetrologyTextBox"],
        Text = $",{value.Item2}",
        IsReadOnly = true,
        IsTabStop = false,
      };

      if (measurementDataModel.ElectricParameterBorder.Parent is Panel parent)
      {
        parent.Children.Remove(measurementDataModel.ElectricParameterBorder);
      }

      measurementDataModel.ElectricParameterBorder.Child = resistanceGrid;

      Grid.SetColumn(resistanceUnitTextBox, 1);
      resistanceGrid.Children.Add(measurementDataModel.ElectricParameterData);
      resistanceGrid.Children.Add(resistanceUnitTextBox);

      contentStack.Children.Add(measurementDataModel.ElectricParameterBorder);
    }

    /// <summary>
    /// Настраивает события для введённых данных.
    /// </summary>
    static private void DataPointEvent(DataPointModel measurementDataModel, ElectricParameter electricParameter = ElectricParameter.None)
    {
      FirstPointEvent(measurementDataModel);
      SecondPointEvent(measurementDataModel);
    }

    /// <summary>
    /// Настраивает события для первой точки.
    /// </summary>
    static private void FirstPointEvent(DataPointModel measurementDataModel)
    {
      DefaultGotAndLostEvent(measurementDataModel.FirstPointData, "Первая точка вида х.х.х");
      TextInputEvent(measurementDataModel.FirstPointData);
    }

    /// <summary>
    /// Настраивает события для второй точки.
    /// </summary>
    static private void SecondPointEvent(DataPointModel measurementDataModel)
    {
      DefaultGotAndLostEvent(measurementDataModel.LastPointData, "Вторая точка вида х.х.х");
      TextInputEvent(measurementDataModel.LastPointData);
    }
    /// <summary>
    /// Настраивает события для ввода электрического параметра.
    /// </summary>
    static private void ResistancePointEvent(DataElectricModel measurementDataModel, ElectricParameter electricParameter)
    {
      ElectricParameterDescriptions.TryGetValue(electricParameter, out Tuple<string, string> value);
      DefaultGotAndLostEvent(measurementDataModel.ElectricParameterData, value.Item1);
      TextInputEvent(measurementDataModel.ElectricParameterData);
    }

    private static bool IsTextAllowed(string text)
    {
      if (string.IsNullOrEmpty(text)) return false;

      return double.TryParse(text, out _) || text == "." || text == ",";
    }

    /// <summary>
    /// Настраивает события GotFocus и LostFocus для TextBox.
    /// </summary>
    /// <param name="border">Граница для настройки.</param>
    /// <param name="textBox">TextBox для настройки.</param>
    /// <param name="defaultText">Текст по умолчанию для TextBox.</param>
    static public void DefaultGotAndLostEvent(TextBox textBox, string defaultText)
    {
      textBox.GotFocus += (sender, e) =>
      {
        if (textBox.Text == defaultText)
        {
          textBox.Text = string.Empty;
        }

      };

      textBox.LostFocus += (sender, e) =>
      {
        if (string.IsNullOrEmpty(textBox.Text))
        {
          textBox.Text = defaultText;
        }
      };
    }

    static public void TextInputEvent(TextBox textBox)
    {
      textBox.PreviewTextInput += (sender, e) =>
      {
        if (e.Text == ",")
        {
          e.Handled = true;
          var textBox1 = sender as TextBox;
          int caretIndex = textBox1.CaretIndex;
          textBox1.Text = textBox1.Text.Insert(caretIndex, ".");
          textBox1.CaretIndex = caretIndex + 1;
        }
        else if (!IsTextAllowed(e.Text))
        {
          e.Handled = true;
        }
      };
    }
  }
}
