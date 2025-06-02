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
using DataBaseConfiguration.Models.MeasurementError;
using DataBaseConfiguration;
using DataBaseConfiguration.Services.MeasurementError;
using UI.Components.MeasurementErrorCardControl;
using UI.Components.MeasurementErrorCard;

namespace UI.Controls.MeasurementError
{
  /// <summary>
  /// Логика взаимодействия для MeasurementErrorControl.xaml
  /// </summary>
  public partial class MeasurementErrorControl : UserControl
  {

    // Словарь для быстрого доступа к карточкам по TypeCommand
    private Dictionary<MeasurementErrorEntity.TypeCommand, MeasurementErrorCard> _cards;

    public MeasurementErrorControl()
    {
      InitializeComponent();
      Loaded += MeasurementErrorControl_Loaded;

      // Инициализируем словарь
      _cards = new Dictionary<MeasurementErrorEntity.TypeCommand, MeasurementErrorCard>
            {
                { MeasurementErrorEntity.TypeCommand.KC, KcErrorCard },
                { MeasurementErrorEntity.TypeCommand.PR, PrErrorCard },
                { MeasurementErrorEntity.TypeCommand.CI, CiErrorCard },
                { MeasurementErrorEntity.TypeCommand.IE, IeErrorCard }
            };

      // Подписываемся на событие SaveButtonClicked у всех карточек
      foreach (var card in _cards.Values)
      {
        card.SaveButtonClicked += Card_SaveButtonClicked;
      }
    }

    private async void MeasurementErrorControl_Loaded(object sender, RoutedEventArgs e)
    {
      try
      {
        var errors = new MeasurementErrorServices().GetAll();

        foreach (var error in errors)
        {
          switch (error.Type)
          {
            case MeasurementErrorEntity.TypeCommand.KC:
              KcErrorCard.PercentageValue = error.PercentageError;
              KcErrorCard.NumericValue = error.NumericError;
              break;

            case MeasurementErrorEntity.TypeCommand.PR:
              PrErrorCard.PercentageValue = error.PercentageError;
              PrErrorCard.NumericValue = error.NumericError;
              break;

            case MeasurementErrorEntity.TypeCommand.CI:
              CiErrorCard.PercentageValue = error.PercentageError;
              CiErrorCard.NumericValue = error.NumericError;
              break;

            case MeasurementErrorEntity.TypeCommand.IE:
              IeErrorCard.PercentageValue = error.PercentageError;
              IeErrorCard.NumericValue = error.NumericError;
              break;
          }
        }

        Console.WriteLine("✅ Данные MeasurementErrorEntity загружены и применены.");
      }
      catch (Exception ex)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Ошибка при загрузке данных MeasurementErrorEntity: {ex.Message}");
        Console.ResetColor();
      }
    }

    /// <summary>
    /// Универсальный метод обновления карточки по данным.
    /// </summary>
    public void UpdateCard(MeasurementErrorEntity.TypeCommand type, double percentage, double numeric)
    {
      if (_cards.TryGetValue(type, out var card))
      {
        card.PercentageValue = percentage;
        card.NumericValue = numeric;
      }
    }

    /// <summary>
    /// Обработчик сохранения данных из карточек.
    /// </summary>
    private void Card_SaveButtonClicked(object? sender, MeasurementErrorCardEventArgs e)
    {
      try
      {
        var _measurementErrorService = new MeasurementErrorServices();

        var entity = _measurementErrorService.GetAll().FirstOrDefault(x => x.Type == e.TypeCommand);
        if (entity != null)
        {
          entity.PercentageError = e.PercentageValue;
          entity.NumericError = e.NumericValue;

          _measurementErrorService.Update(entity);

          Console.ForegroundColor = ConsoleColor.Green;
          Console.WriteLine($"✅ Данные обновлены для {e.TypeCommand}: {e.PercentageValue}% / {e.NumericValue}");
          Console.ResetColor();

          if (sender is MeasurementErrorCard card)
            card.ShowResult(true);
        }
        else
        {
          if (sender is MeasurementErrorCard card)
            card.ShowResult(false);
        }
      }
      catch (Exception ex)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Ошибка при сохранении данных: {ex.Message}");
        Console.ResetColor();

        if (sender is MeasurementErrorCard card)
          card.ShowResult(false);
      }
    }
  }
}
