using Message;
using Message.Icon;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ask.UI.UnitTests.Controls;

public sealed class MessageBoxThemeTests
{
  [Theory]
  [InlineData("dark", "#212A36", "#F0EBF5", "#7453A6", "#4E3D61", "#4D5B70")]
  [InlineData("light", "#F2EDF5", "#2C2532", "#704C9B", "#665073", "#B7AFBD")]
  public void QuestionDialogUsesCurrentThemePalette(
    string theme,
    string cardColor,
    string textColor,
    string accentColor,
    string headerColor,
    string borderColor) => RunInSta(() =>
  {
    var window = new MessageBoxCustom(MessageBoxButton.YesNo, MessageBoxImage.Question);
    window.Resources.MergedDictionaries.Add(new ResourceDictionary
    {
      Source = new Uri($"/UI;component/Resources/Theme/{theme}.xaml", UriKind.Relative),
    });

    window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

    var card = Assert.IsType<Border>(window.FindName("BorderWin"));
    var headerPanel = Assert.IsType<Border>(window.FindName("TopPanel"));
    var message = Assert.IsType<TextBlock>(window.FindName("MessageText"));
    var iconContainer = Assert.IsType<StackPanel>(window.FindName("IconContainer"));
    var question = Assert.IsType<QuestionIcon>(Assert.Single(iconContainer.Children));

    Assert.Equal(ToColor(cardColor), Assert.IsType<SolidColorBrush>(card.Background).Color);
    Assert.Equal(ToColor(borderColor), Assert.IsType<SolidColorBrush>(card.BorderBrush).Color);
    Assert.Equal(ToColor(headerColor), Assert.IsType<SolidColorBrush>(headerPanel.Background).Color);
    Assert.Equal(ToColor(textColor), Assert.IsType<SolidColorBrush>(message.Foreground).Color);
    Assert.Equal(ToColor(accentColor), Assert.IsType<SolidColorBrush>(question.CircleColor).Color);
  });

  private static Color ToColor(string value) =>
    (Color)ColorConverter.ConvertFromString(value);

  private static void RunInSta(Action action)
  {
    Exception? exception = null;
    var thread = new Thread(() =>
    {
      try
      {
        action();
      }
      catch (Exception error)
      {
        exception = error;
      }
    }) { IsBackground = true };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
    if (exception != null)
    {
      throw new InvalidOperationException("Проверка темы диалога завершилась ошибкой.", exception);
    }
  }
}
