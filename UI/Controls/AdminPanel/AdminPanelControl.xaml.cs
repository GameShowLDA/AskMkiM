using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.AdminPanel
{
  /// <summary>
  /// Предоставляет навигацию по административным и сервисным инструментам.
  /// </summary>
  public partial class AdminPanelControl : UserControl
  {
    private ServiceUtilitiesControl? serviceUtilitiesControl;
    private DataBaseView? dataBaseView;
    private CheckResistanceControl? checkResistanceControl;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AdminPanelControl"/>.
    /// </summary>
    public AdminPanelControl()
    {
      InitializeComponent();
      ServiceUtilitiesNavigation.IsChecked = true;
    }

    /// <summary>
    /// Отображает сервисные утилиты.
    /// </summary>
    /// <param name="sender">Выбранный элемент навигации.</param>
    /// <param name="e">Данные события выбора.</param>
    private void ServiceUtilitiesNavigation_Checked(object sender, RoutedEventArgs e)
    {
      RightContentPresenter.Content = serviceUtilitiesControl ??= new ServiceUtilitiesControl();
    }

    /// <summary>
    /// Отображает инструменты работы с базой данных.
    /// </summary>
    /// <param name="sender">Выбранный элемент навигации.</param>
    /// <param name="e">Данные события выбора.</param>
    private void DatabaseNavigation_Checked(object sender, RoutedEventArgs e)
    {
      RightContentPresenter.Content = dataBaseView ??= new DataBaseView();
    }

    /// <summary>
    /// Отображает настройку сопротивления МКР.
    /// </summary>
    /// <param name="sender">Выбранный элемент навигации.</param>
    /// <param name="e">Данные события выбора.</param>
    private void ResistanceNavigation_Checked(object sender, RoutedEventArgs e)
    {
      RightContentPresenter.Content = checkResistanceControl ??= new CheckResistanceControl();
    }
  }
}
