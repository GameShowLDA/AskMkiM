using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для KeyboardLayoutComponent.xaml
  /// </summary>
  public partial class KeyboardLayoutComponent : UserControl
  {
    private readonly DispatcherTimer _keyboardStateTimer;

    public KeyboardLayoutComponent()
    {
      InitializeComponent();
      UpdateLayoutDisplay();

      _keyboardStateTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(250),
      };
      _keyboardStateTimer.Tick += (_, _) => UpdateCapsLockIndicator();
      Loaded += KeyboardLayoutComponent_Loaded;
      Unloaded += KeyboardLayoutComponent_Unloaded;
    }

    public new System.Windows.Media.Brush Foreground
    {
      get
      {
        return LayoutText.Foreground;
      }
      set
      {
        LayoutText.Foreground = value;
      }
    }

    private void KeyboardLayoutComponent_Loaded(object sender, RoutedEventArgs e)
    {
      UpdateLayoutDisplay();
      UpdateCapsLockIndicator();
      InputLanguageManager.Current.InputLanguageChanged += InputLanguageChanged;
      _keyboardStateTimer.Start();
    }

    private void KeyboardLayoutComponent_Unloaded(object sender, RoutedEventArgs e)
    {
      _keyboardStateTimer.Stop();
      InputLanguageManager.Current.InputLanguageChanged -= InputLanguageChanged;
    }

    private void InputLanguageChanged(object sender, InputLanguageEventArgs e)
    {
      UpdateLayoutDisplay();
    }

    private void UpdateCapsLockIndicator()
    {
      CapsLockBadge.Visibility = Keyboard.IsKeyToggled(Key.CapsLock)
        ? Visibility.Visible
        : Visibility.Collapsed;
    }

    private void LayoutButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
      SwitchToNextInputLanguage();
    }

    private void UpdateLayoutDisplay()
    {
      var culture = InputLanguageManager.Current.CurrentInputLanguage;
      LayoutText.Text = culture.TwoLetterISOLanguageName.ToUpper();
    }

    private void SwitchToNextInputLanguage()
    {
      var languages = InputLanguageManager.Current.AvailableInputLanguages;
      var current = InputLanguageManager.Current.CurrentInputLanguage;

      // Поиск следующего языка
      bool foundCurrent = false;
      foreach (CultureInfo lang in languages)
      {
        if (foundCurrent)
        {
          ActivateLanguage(lang);
          return;
        }

        if (lang.Equals(current))
        {
          foundCurrent = true;
        }
      }

      // Если текущий был последним — вернуться к первому
      foreach (CultureInfo lang in languages)
      {
        ActivateLanguage(lang);
        break;
      }
    }

    private void ActivateLanguage(CultureInfo culture)
    {
      var hkl = LoadKeyboardLayout(culture.KeyboardLayoutId.ToString("X8"), KLF_ACTIVATE);
      ActivateKeyboardLayout(hkl, 0);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

    [DllImport("user32.dll")]
    private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint Flags);

    private const uint KLF_ACTIVATE = 0x00000001;
  }
}
