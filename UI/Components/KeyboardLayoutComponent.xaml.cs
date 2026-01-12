using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для KeyboardLayoutComponent.xaml
  /// </summary>
  public partial class KeyboardLayoutComponent : UserControl
  {
    public KeyboardLayoutComponent()
    {
      InitializeComponent();
      UpdateLayoutDisplay();

      InputLanguageManager.Current.InputLanguageChanged += (s, e) =>
      {
        UpdateLayoutDisplay();
      };
      this.MouseLeftButtonUp += (s, e) => SwitchToNextInputLanguage();
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
