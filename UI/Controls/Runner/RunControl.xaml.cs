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
using Utilities;
using static Utilities.LoggerUtility;

namespace UI.Controls.Runner
{
  /// <summary>
  /// Логика взаимодействия для RunControl.xaml
  /// </summary>
  public partial class RunControl : UserControl
  {
    List<BaseCommandModel> ControlProgram = null;
    private ProtocolUI ProtocolUI { get; set; }
    bool task = false;
    public RunControl()
    {
      InitializeComponent();
      ProtocolUI = new ProtocolUI(true);
      MainContent.Content = ProtocolUI;
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
      ControlProgram = models;

      var ok = models[0];
      if (ok.Mnemonic != "ОК")
      {
        return;
      }
      
      ProtocolUI.Header = (ok as OkCommandModel).ObjectCode;
      ProtocolUI.SetSettings(this, StartDelegate: StartTest, false);
    }

    private async Task StartTest(CancellationToken cancellationToken)
    {
      int j = 1;
      while (!cancellationToken.IsCancellationRequested)
      {
        await ProtocolUI.ShowMessageAsync(new Utilities.Models.ShowMessageModel($"Проверка блока {j}"), IsBlockStart: true);

        for (int i = 1; i <= 100; i++)
        {
          await UserActionHelper.RunWithUserRepeatAsync(async () =>
          {

            var type = Utilities.Models.ShowMessageModel.MessageType.Success;
            if (i % 100 == 0)
            {
              type = Utilities.Models.ShowMessageModel.MessageType.Error;
            }

            await ProtocolUI.ShowMessageAsync(new Utilities.Models.ShowMessageModel($"Тест номер", message: i.ToString(), type: type), skipPause: true);
            await Task.Delay(100, cancellationToken);

            return type != Utilities.Models.ShowMessageModel.MessageType.Error;

          }, ProtocolUI);
        }

        j++;
      }
    }
  }
}
