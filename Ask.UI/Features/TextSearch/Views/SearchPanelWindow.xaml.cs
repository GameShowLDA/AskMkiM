using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Ask.UI.Features.TextSearch.Views;

/// <summary>Search panel for the editor and components implementing the shared search contract.</summary>
public partial class SearchPanelWindow : Window
{
  private readonly FrameworkElement _anchor;
  private readonly Window _owner;

  public SearchPanelWindow(Window owner, FrameworkElement anchor)
  {
    InitializeComponent();
    Owner = _owner = owner;
    _anchor = anchor;
    _owner.LocationChanged += Owner_LayoutChanged;
    _owner.SizeChanged += Owner_LayoutChanged;
    _owner.StateChanged += Owner_LayoutChanged;
    _anchor.SizeChanged += Owner_LayoutChanged;
    InitializeSearch();
    Closed += (_, _) =>
    {
      ReleaseSearch();
      _owner.LocationChanged -= Owner_LayoutChanged;
      _owner.SizeChanged -= Owner_LayoutChanged;
      _owner.StateChanged -= Owner_LayoutChanged;
      _anchor.SizeChanged -= Owner_LayoutChanged;
    };
  }

  public void ShowPanel()
  {
    bool animate = !IsVisible || _closingAnimation;
    if (animate) PrepareOpening();
    SynchronizeTarget();
    UpdatePosition();
    Show();
    if (animate) AnimateOpening();
    Activate();
    SearchInput.Focus();
    SearchInput.SelectAll();
  }

  private void Owner_LayoutChanged(object? sender, EventArgs e) => UpdatePosition();

  private void UpdatePosition()
  {
    if (PresentationSource.FromVisual(_anchor) == null || _owner.WindowState == WindowState.Minimized)
      return;

    var transform = PresentationSource.FromVisual(_anchor).CompositionTarget.TransformFromDevice;
    var position = transform.Transform(_anchor.PointToScreen(new Point(_anchor.ActualWidth / 2, _anchor.ActualHeight)));
    Width = Math.Max(1, Math.Min(500, _owner.ActualWidth - 24));
    Left = position.X - Width / 2;
    Top = position.Y + 4;
  }

  private void Settings_Click(object sender, RoutedEventArgs e)
  {
    bool expanded = !IsSectionExpanded(SettingsPanel);
    SetSectionExpanded(SettingsPanel, expanded);
    UpdateShellCorners();
    SettingsButton.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty,
      expanded ? "SearchAccentBrush" : "ForegrounfBrushes");
    SettingsButton.SetResourceReference(System.Windows.Controls.Control.BorderBrushProperty,
      expanded ? "SearchAccentBrush" : "SearchPanelOutline");
    System.Windows.Automation.AutomationProperties.SetItemStatus(SettingsButton,
      expanded ? "Настройки открыты" : "Настройки закрыты");
  }

  private void ResetSettings_Click(object sender, RoutedEventArgs e)
  {
    ReplacementInput.Clear();
    MatchCaseOption.IsChecked = false;
    WholeWordOption.IsChecked = false;
    RegexOption.IsChecked = false;
    SelectionOnlyOption.IsChecked = false;
    SearchScope.SelectedIndex = 0;
    ClearSearchResults();
  }

  private void Close_Click(object sender, RoutedEventArgs e) => CloseAnimated();

  private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
  {
    if (e.Key == Key.Escape)
    {
      CloseAnimated();
      e.Handled = true;
    }
    else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
    {
      if (_closingAnimation) ShowPanel();
      SearchInput.Focus();
      SearchInput.SelectAll();
      e.Handled = true;
    }
    else if ((e.Key == Key.Enter || e.Key == Key.F3)
             && (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Shift))
    {
      e.Handled = true;
      _ = RunSearchAsync(Keyboard.Modifiers == ModifierKeys.Shift
        ? Ask.Core.Shared.Interfaces.UiInterfaces.TextSearchAction.Previous
        : Ask.Core.Shared.Interfaces.UiInterfaces.TextSearchAction.Next);
    }
    else if (Keyboard.Modifiers == ModifierKeys.Alt && (e.SystemKey == Key.R || e.SystemKey == Key.A))
    {
      e.Handled = true;
      _ = RunReplaceAsync(e.SystemKey == Key.A);
    }
  }
}
