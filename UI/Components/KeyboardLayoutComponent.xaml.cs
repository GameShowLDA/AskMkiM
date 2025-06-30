using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
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
