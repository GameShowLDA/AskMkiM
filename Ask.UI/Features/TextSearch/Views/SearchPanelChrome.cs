using System.Windows;

namespace Ask.UI.Features.TextSearch.Views;

/// <summary>Shape shared by the surface, bevel and shadows of search buttons.</summary>
public static class SearchPanelChrome
{
  public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.RegisterAttached(
    "CornerRadius", typeof(CornerRadius), typeof(SearchPanelChrome), new PropertyMetadata(new CornerRadius(100)));

  public static CornerRadius GetCornerRadius(DependencyObject element) => (CornerRadius)element.GetValue(CornerRadiusProperty);

  public static void SetCornerRadius(DependencyObject element, CornerRadius value) => element.SetValue(CornerRadiusProperty, value);
}
