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
using System.Windows.Shapes;

namespace Message
{
  /// <summary>
  /// Логика взаимодействия для MessageBoxCustom.xaml
  /// </summary>
  public partial class MessageBoxCustom : Window
  {
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    public MessageBoxCustom(MessageBoxButton messageBoxButton, MessageBoxImage image)
    {
      InitializeComponent();
      Loaded += (s, a) => MessageBox_Loaded(messageBoxButton, image);
      TopPanel.PreviewMouseDown += (s, a) =>
      {
        if (a.ChangedButton == MouseButton.Left)
        {
          DragMoveAsync();
        }
      };
    }

    private void MessageBox_Loaded(MessageBoxButton messageBoxButton, MessageBoxImage image)
    {
      switch (image)
      {
        case MessageBoxImage.Error:
          var cross = new Icon.CrossIcon { Size = 50 };
          TopPanel.Background = cross.CircleColor;
          Header.Foreground = cross.IconStrokeColor;
          IconContainer.Children.Add(cross);
          break;

        case MessageBoxImage.Warning:
          var warning = new Icon.WarningIcon { Size = 50 };
          TopPanel.Background = warning.CircleColor;
          Header.Foreground = warning.IconStrokeColor;
          IconContainer.Children.Add(warning);
          break;

        case MessageBoxImage.Information:
          var check = new Icon.CheckIcon { Size = 50 };
          TopPanel.Background = check.CircleColor;
          Header.Foreground = check.IconStrokeColor;
          IconContainer.Children.Add(check);
          break;

        case MessageBoxImage.Question:
          var question = new Icon.QuestionIcon { Size = 50 };
          TopPanel.Background = question.CircleColor;
          Header.Foreground = question.IconStrokeColor;
          IconContainer.Children.Add(question);
          break;
      }

      VisibleButton(messageBoxButton);
    }

    private void VisibleButton(MessageBoxButton messageBoxButton)
    {
      OkButton.Visibility = Visibility.Collapsed;
      CancelButton.Visibility = Visibility.Collapsed;
      YesButton.Visibility = Visibility.Collapsed;
      NoButton.Visibility = Visibility.Collapsed;
      switch (messageBoxButton)
      {
        case MessageBoxButton.OK:
          OkButtonVisible();
          break;
        case MessageBoxButton.OKCancel:
          OkAndCancelButtonsVisible();
          break;
        case MessageBoxButton.YesNo:
          YesNoButtonsVisible();
          break;
        case MessageBoxButton.YesNoCancel:
          YesNoCancelButtonsVisible();
          break;
      }
    }

    /// <summary>
    /// Асинхронно запускает перетаскивание окна пользователем.
    /// </summary>
    private void DragMoveAsync()
    {
      this.DragMove();
    }

    private void OkButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      Result = MessageBoxResult.OK;
      Close();
    }

    private void CancelButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      Result = MessageBoxResult.Cancel;
      Close();
    }

    private void YesButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      Result = MessageBoxResult.Yes;
      Close();
    }

    private void NoButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      Result = MessageBoxResult.No;
      Close();
    }


    public static MessageBoxResult Show(string text = "", string title = "", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
    {
      var window = new MessageBoxCustom(buttons, image)
      {
        Title = title
      };

      var _mainWindow = Application.Current.MainWindow;

      // можно настроить текст и заголовок, если нужно
      window.Header.Text = title;
      window.MessageText.Text = text;

      _mainWindow.Effect = new System.Windows.Media.Effects.BlurEffect();
      window.ShowDialog();
      _mainWindow.Effect = null;
      return window.Result;
    }

    private void OkButtonVisible()
    {
      OkButton.Visibility = Visibility.Visible;
    }
    private void OkAndCancelButtonsVisible()
    {
      OkButton.Visibility = Visibility.Visible;
      CancelButton.Visibility = Visibility.Visible;
    }

    private void YesNoCancelButtonsVisible()
    {
      YesButton.Visibility = Visibility.Visible;
      NoButton.Visibility = Visibility.Visible;
      CancelButton.Visibility = Visibility.Visible;
    }


    private void YesNoButtonsVisible()
    {
      YesButton.Visibility = Visibility.Visible;
      NoButton.Visibility = Visibility.Visible;
    }
  }
}
