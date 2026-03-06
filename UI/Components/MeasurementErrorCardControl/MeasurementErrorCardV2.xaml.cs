using Ask.Core.Shared.DTO.Executor.MeasurementError;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using DataBaseConfiguration.Services.MeasurementError;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Ask.LogLib.LoggerUtility;

namespace UI.Components.MeasurementErrorCardControl
{
  /// <summary>
  /// Карточка режима метрологии с диапазонами погрешностей.
  /// Отслеживает изменения, поддерживает сохранение и откат.
  /// </summary>
  public partial class MeasurementErrorCardV2 : UserControl
  {
    private bool _isLoaded = false;
    private MeasurementTypeCommand _pendingType;

    private readonly List<MeasurementErrorRangeEntity> _ranges = new();

    /// <summary>Базовая (сохранённая) модель для отмены изменений.</summary>
    private MeasurementErrorEntity _baseEntity;

    /// <summary>Флаг наличия несохранённых изменений.</summary>
    public bool HasUnsavedChanges { get; private set; }

    public MeasurementTypeCommand TypeCommand
    {
      get => (MeasurementTypeCommand)GetValue(TypeCommandProperty);
      set => SetValue(TypeCommandProperty, value);
    }

    public static readonly DependencyProperty TypeCommandProperty =
        DependencyProperty.Register(
            nameof(TypeCommand),
            typeof(MeasurementTypeCommand),
            typeof(MeasurementErrorCardV2),
            new PropertyMetadata(MeasurementTypeCommand.KC, OnTypeCommandChanged));

    private static void OnTypeCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is MeasurementErrorCardV2 card && e.NewValue is MeasurementTypeCommand newType)
      {
        if (card._isLoaded)
        {
          LogInformation($"[UI] MeasurementErrorCardV2.LoadDataForType вызван из OnTypeCommandChanged для {newType}");
          _ = card.LoadDataForType(newType);
        }
        else
        {
          card._pendingType = newType;
        }
      }
    }

    public MeasurementErrorCardV2()
    {
      InitializeComponent();
      Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
      _isLoaded = true;
      var type = _pendingType != default ? _pendingType : TypeCommand;

      LogInformation($"[UI] MeasurementErrorCardV2.OnLoaded → загрузка для {type}");
      await LoadDataForType(type);

      SuccessIcon.PreviewMouseDown += SuccessIcon_PreviewMouseDown;
      ErrorIcon.PreviewMouseDown += ErrorIcon_PreviewMouseDown;
    }

    /// <summary>
    /// Загружает данные диапазонов из БД для указанного типа команды.
    /// </summary>
    private async Task LoadDataForType(MeasurementTypeCommand type)
    {
      try
      {
        TitleText.Text = $"Команда {type}";

        var service = new MeasurementErrorServices();
        _baseEntity = service
            .GetAllWithRanges<MeasurementErrorEntity>()
            .FirstOrDefault(x => x.Type == type);

        if (_baseEntity == null)
        {
          MessageBox.Show($"Для команды {type} нет данных в БД.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        _ranges.Clear();
        // создаём копию диапазонов, чтобы не менять EF-трекинг
        _ranges.AddRange(_baseEntity.Ranges.Select(r => new MeasurementErrorRangeEntity
        {
          Id = r.Id,
          MinValue = r.MinValue,
          MaxValue = r.MaxValue,
          NumericError = r.NumericError,
          PercentageError = r.PercentageError,
          MeasurementErrorEntityId = r.MeasurementErrorEntityId
        }));

        DisplayRanges(type);
        HasUnsavedChanges = false;
        UpdateIcons();
      }
      catch (Exception ex)
      {
        LogException(ex, $"Ошибка при загрузке диапазонов для {type}");
        MessageBox.Show($"Ошибка при загрузке диапазонов: {ex.Message}");
      }
    }

    /// <summary>
    /// Отображает диапазоны на экране.
    /// </summary>
    private void DisplayRanges(MeasurementTypeCommand type)
    {
      if (RangesContainer == null)
      {
        LogWarning($"[UI] DisplayRanges вызван до инициализации визуальных элементов для {type}");
        return;
      }

      RangesContainer.Children.Clear();

      string unit = type switch
      {
        MeasurementTypeCommand.IE => "пФ",
        MeasurementTypeCommand.KC => "Ом",
        MeasurementTypeCommand.SI => "Ом",
        MeasurementTypeCommand.PR => "Ом",
        _ => ""
      };

      foreach (var range in _ranges)
      {
        var view = new MeasurementErrorRangeView { Unit = unit };
        view.SetRange(range);

        // Подписываемся на изменение любого поля диапазона
        view.ValueChanged += (s, e) => OnRangeChanged();

        RangesContainer.Children.Add(view);
      }

      LogInformation($"[UI] Отображено {_ranges.Count} диапазонов для {type}");
    }

    /// <summary>
    /// Обработка изменения диапазона — проверяет, есть ли отличия от сохранённой модели.
    /// </summary>
    private void OnRangeChanged()
    {
      if (_baseEntity == null)
        return;

      // Пересобираем текущие значения с UI
      foreach (var child in RangesContainer.Children)
      {
        if (child is MeasurementErrorRangeView rangeView)
          rangeView.ApplyChanges();
      }

      // Проверяем: отличается ли хотя бы один диапазон
      HasUnsavedChanges = !RangesEqual(_ranges, _baseEntity.Ranges);

      UpdateIcons();
    }

    /// <summary>
    /// Сравнение текущих диапазонов с базовой моделью.
    /// </summary>
    private static bool RangesEqual(List<MeasurementErrorRangeEntity> current, List<MeasurementErrorRangeEntity> original)
    {
      if (current.Count != original.Count)
        return false;

      for (int i = 0; i < current.Count; i++)
      {
        var a = current[i];
        var b = original[i];

        if (a.MinValue != b.MinValue ||
            a.MaxValue != b.MaxValue ||
            a.NumericError != b.NumericError ||
            a.PercentageError != b.PercentageError)
          return false;
      }

      return true;
    }

    /// <summary>
    /// Обновляет отображение иконок «✔ / ✖».
    /// </summary>
    private void UpdateIcons()
    {
      if (SuccessIcon == null || ErrorIcon == null)
        return;

      if (HasUnsavedChanges)
      {
        SuccessIcon.Visibility = Visibility.Visible;
        ErrorIcon.Visibility = Visibility.Visible;
      }
      else
      {
        SuccessIcon.Visibility = Visibility.Collapsed;
        ErrorIcon.Visibility = Visibility.Collapsed;
      }
    }

    /// <summary>
    /// Сохраняет изменения диапазонов в БД.
    /// </summary>
    private async void SuccessIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e) => await SaveDataAsync();

    private async Task SaveDataAsync()
    {
      try
      {
        foreach (var child in RangesContainer.Children)
        {
          if (child is MeasurementErrorRangeView rangeView)
            rangeView.ApplyChanges();
        }

        var updatedEntity = new MeasurementErrorEntity(TypeCommand)
        {
          Id = _baseEntity.Id,
          Ranges = _ranges
        };

        var service = new MeasurementErrorServices();
        await Task.Run(() => service.Update(updatedEntity));

        _baseEntity = updatedEntity;
        HasUnsavedChanges = false;
        UpdateIcons();

        MessageBox.Show("Изменения успешно сохранены.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        LogException(ex, "Ошибка при сохранении диапазонов");
        MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Откатывает все изменения к сохранённой версии.
    /// </summary>
    private void ErrorIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (_baseEntity == null)
        return;

      _ranges.Clear();
      _ranges.AddRange(_baseEntity.Ranges.Select(r => new MeasurementErrorRangeEntity
      {
        Id = r.Id,
        MinValue = r.MinValue,
        MaxValue = r.MaxValue,
        NumericError = r.NumericError,
        PercentageError = r.PercentageError,
        MeasurementErrorEntityId = r.MeasurementErrorEntityId
      }));

      DisplayRanges(TypeCommand);
      HasUnsavedChanges = false;
      UpdateIcons();
    }
  }
}
