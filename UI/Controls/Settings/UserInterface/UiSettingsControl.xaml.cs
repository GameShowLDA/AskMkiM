using AppConfiguration.Execution;
using AppConfiguration.Parameter;
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
using UI.Localization;
using static AppConfiguration.Parameter.ParameterConfig;


namespace UI.Controls.Settings.UserInterface
{
  /// <summary>
  /// Логика взаимодействия для UiSettingsControl.xaml
  /// </summary>
  public partial class UiSettingsControl : UserControl
  {
    ParameterModel _baseParameterModel { get; set; }
    private record LangOption(string Key, string Title);

    /// <summary>
    /// Глобальный флаг наличия несохранённых изменений в разделе.
    /// <para>True — есть отличия от сохранённой модели; False — всё совпадает.</para>
    /// </summary>
    public bool HasUnsavedChanges { get; private set; }

    public UiSettingsControl()
    {
      InitializeComponent();
      Loaded += UiSettingsControl_Loaded;

    }

    /// <summary>
    /// Клик по галочке «сохранить»: сохраняет текущую модель,
    /// перечитывает базу и скрывает индикаторы изменений.
    /// </summary>
    private async void Success_PreviewMouseDown(object sender, MouseButtonEventArgs e) => await SaveData();

    /// <summary>
    /// Клик по кресту «отменить»: откатывает значения к сохранённой модели
    /// и скрывает индикаторы изменений.
    /// </summary>
    private void Error_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      DefalultData();

      Error.Visibility = Visibility.Collapsed;
      Success.Visibility = Visibility.Collapsed;
      HasUnsavedChanges = false;
    }

    public async Task SaveData()
    {
      await SaveProtocolModel(GetModel());
      _baseParameterModel = await GetParameterModel();

      Error.Visibility = Visibility.Collapsed;
      Success.Visibility = Visibility.Collapsed;
      HasUnsavedChanges = false;
    }

    private void ValueChanged(object? sender, object? e)
    {
      if (!ProtocolEquals(_baseParameterModel, GetModel()))
      {
        Error.Visibility = Visibility.Visible;
        Success.Visibility = Visibility.Visible;
        HasUnsavedChanges = true;
      }
      else
      {
        Error.Visibility = Visibility.Collapsed;
        Success.Visibility = Visibility.Collapsed;
        HasUnsavedChanges = false;
      }
    }

    private async void UiSettingsControl_Loaded(object sender, RoutedEventArgs e)
    {
      _baseParameterModel = await GetParameterModel();
      
      LanguageSelect.ValueChanged += ValueChanged;
      
      Success.PreviewMouseDown += Success_PreviewMouseDown;
      Error.PreviewMouseDown += Error_PreviewMouseDown;
      
      Error.Visibility = Visibility.Collapsed;
      Success.Visibility = Visibility.Collapsed;
      HasUnsavedChanges = false;

      // 1) Собираем доступные языки из ресурсов
      var cultures = LocalizationService.GetAvailableCultures();

      var options = cultures
        .Select(c => new LangOption(
            Key: c.Name,                          // "ru" / "en" / "ru-RU"
            Title: LocalizationService.GetDisplayName(c))) // "Русский (Россия)" и т.п.
        .ToList();

      // 2) Кладём в карточку выбора
      LanguageSelect.ItemsSource = options;

      // 3) Текущее значение + дефолт
      var current = LanguageSettings.CurrentLanguage;   // "ru" или "en"
      LanguageSelect.DefaultValue = current;           // подставится, если пусто
      LanguageSelect.SelectedValue = current;          // отобразим как выбранный

      // 4) Реакция на смену значения
      // LanguageSelect.ValueChanged += async (_, val) =>
      // {
      //   var lang = val as string;
      //   if (string.IsNullOrWhiteSpace(lang)) return;
      // 
      //   await LanguageSettings.SetLanguageAsync(lang);
      // };
    }

    /// <summary>
    /// Заполняет элементы UI значениями из базовой (сохранённой) модели.
    /// </summary>
    private void DefalultData()
    {
      var current = LanguageSettings.CurrentLanguage;   
      LanguageSelect.DefaultValue = current;          
      LanguageSelect.SelectedValue = current;         
    }

    /// <summary>
    /// Формирует модель протокола из текущих значений элементов UI.
    /// </summary>
    private ParameterModel GetModel()
    {
      var code =
          LanguageSelect.SelectedValue as string          
          ?? LanguageSelect.SelectedItem?.ToString()      
          ?? LanguageSettings.CurrentLanguage;            

      return new ParameterModel
      {
        Language = code
      };
    }

    /// <summary>
    /// Сравнивает две модели протокола по всем флагам.
    /// </summary>
    private static bool ProtocolEquals(ParameterModel a, ParameterModel b) =>
      a.Language == b.Language;
  }
}
