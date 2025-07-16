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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Ok;
using UI.Controls.ProtocolNew;
using UI.Controls.TextEditor;
using static Utilities.LoggerUtility;

namespace UI.Controls.Runner
{
  /// <summary>
  /// Логика взаимодействия для RunControl.xaml
  /// </summary>
  public partial class RunControl : UserControl
  {
    bool task = false;
    public RunControl()
    {
      InitializeComponent();
    }


    public void SetLeftEditor(TextEditorUI textEditorUI)
    {
      LogInformation("SetLeftEditor вызван: " + this.GetHashCode());

      if (textEditorUI == null)
        return;

      if (textEditorUI.Parent is Panel oldParent)
      {
        oldParent.Children.Remove(textEditorUI);
      }
      else if (textEditorUI.Parent is ContentControl oldContent)
      {
        oldContent.Content = null;
      }
      else if (textEditorUI.Parent is Decorator decorator)
      {
        decorator.Child = null;
      }

      LeftBox.Children.Clear();
      LeftBox.Children.Add(textEditorUI);
    }

    public void Start(List<BaseCommandModel> models)
    {
      ProtocolUI.MenuButtonVisibility(false);
      var ok = models[0];
      if (ok.Mnemonic != "ОК")
      {
        return;
      }

      // Header.Text = (ok as OkCommandModel).ObjectCode;
      ProtocolUI.SetSettings(this, StartDelegate: StartTest, false);
    }

    private async Task StartTest(CancellationToken cancellationToken)
    {
      int i = 1;
      while (!cancellationToken.IsCancellationRequested)
      {
        await ProtocolUI.ShowMessageAsync(new Utilities.Models.ShowMessageModel($"Тест номер: {i}"));
        i++;
        await Task.Delay(100, cancellationToken); 
      }
    }
  }
}
