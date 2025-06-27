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

namespace UI.Controls.Message
{
  /// <summary>
  /// Логика взаимодействия для MessageBox.xaml
  /// </summary>
  public partial class MessageBox : Window
  {
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;
    public enum Status
    {
      Error,
      Warning,
      Information,
      Question,
    }
    public MessageBox(Status status, MessageBoxButton messageBoxButton = MessageBoxButton.OK)
    {
      InitializeComponent();
      Loaded += (s, a) => MessageBox_Loaded(status, messageBoxButton);
      TopPanel.PreviewMouseDown += (s, a) =>
      {
        if (a.ChangedButton == MouseButton.Left)
        {
          DragMoveAsync();
        }
      };
    }

    private void MessageBox_Loaded(Status status, MessageBoxButton messageBoxButton)
    {
      switch (status)
      {
        case Status.Error:
          var cross = new Icon.CrossIcon { Size = 50 };
          TopPanel.Background = cross.CircleColor;
          Header.Foreground = cross.IconStrokeColor;
          IconContainer.Children.Add(cross);
          break;

        case Status.Warning:
          var warning = new Icon.WarningIcon { Size = 50 };
          TopPanel.Background = warning.CircleColor;
          Header.Foreground = warning.IconStrokeColor;
          IconContainer.Children.Add(warning);
          break;

        case Status.Information:
          var check = new Icon.CheckIcon { Size = 50 };
          TopPanel.Background = check.CircleColor;
          Header.Foreground = check.IconStrokeColor;
          IconContainer.Children.Add(check);
          break;

        case Status.Question:
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

    public static MessageBoxResult Show(Status status, string text = "", string title = "",  MessageBoxButton buttons = MessageBoxButton.OK)
    {
      var window = new MessageBox(status, buttons)
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
  }
}
