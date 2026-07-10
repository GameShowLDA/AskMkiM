using Ask.UI.Features.RoleManagement.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Features.RoleManagement.Views
{
  public partial class RolePasswordManagementControl : UserControl
  {
    private readonly RolePasswordManagementViewModel _viewModel = new();
    private bool _isClearingPasswordFields;

    public RolePasswordManagementControl()
    {
      InitializeComponent();
      DataContext = _viewModel;

      Loaded += RolePasswordManagementControl_Loaded;
      _viewModel.PasswordChangedSuccessfully += ViewModel_PasswordChangedSuccessfully;
    }

    private async void RolePasswordManagementControl_Loaded(object sender, RoutedEventArgs e)
    {
      await _viewModel.LoadAsync();
    }

    private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      ClearPasswordBoxes();
      _viewModel.ClearPasswordFields();
    }

    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
      if (_isClearingPasswordFields)
      {
        return;
      }

      _viewModel.NewPassword = NewPasswordBox.Password;
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
      if (_isClearingPasswordFields)
      {
        return;
      }

      _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
    }

    private void ViewModel_PasswordChangedSuccessfully(object? sender, EventArgs e)
    {
      ClearPasswordBoxes();
    }

    private void ClearPasswordBoxes()
    {
      _isClearingPasswordFields = true;
      NewPasswordBox.Clear();
      ConfirmPasswordBox.Clear();
      _isClearingPasswordFields = false;
    }
  }
}
