using Newtonsoft.Json.Linq;
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
using UI.Components.ArchiveManager.ArchiveFiles.Index;
using UI.Components.ArchiveManager;
using System.IO;
using Path = System.IO.Path;
using static Utilities.LoggerUtility;
using Utilities.Encrypter;
using UI.Components.ArchiveManager.Models;

namespace UI.Components.ArchiveControls
{
  /// <summary>
  /// Логика взаимодействия для TableAllArchivesControl.xaml
  /// </summary>
  public partial class TableAllArchivesControl : UserControl
  {
    public event EventHandler<MouseButtonEventArgs> ArchiveSelected;
    static private bool visibleLeftColumn = true;
    static internal JArray indexData;

    public TableAllArchivesControl()
    {
      InitializeComponent();
      InitializeIndex();
      this.Loaded += async (s, e) => await LoadFilesToDataGridAsync();
    }

    private void InitializeIndex()
    {
      var indexPath = Path.Combine(ArchiveSettings.ArchivePath, ArchiveSettings.IndexName);
      var indexDataDecrypt = string.Empty;
      if (File.Exists(indexPath))
      {
        var indexText = File.ReadAllText(indexPath);
        indexDataDecrypt = FileEncryptionManager.Decrypt(indexText);
        try
        {
          indexData = JArray.Parse(indexDataDecrypt);
        }
        catch (Newtonsoft.Json.JsonReaderException ex)
        {
          indexData = new JArray();
        }
        catch (Exception ex)
        {
          LogError($"Произошла ошибка при чтении индекса: {ex.Message}");
        }
      }
      else
      {
        indexData = new JArray();
      }
    }


    private async Task LoadFilesToDataGridAsync()
    {
      try
      {
        await Task.Run(async () =>
        {
          string folderPath = ArchiveSettings.ArchivePath;
          var data = new List<ApkArchive>();
          if (indexData == null || indexData.Count == 0)
          {
            MessageBox.Show($"У вас еще нет созданных архивов программ контроля.");
          }
          else
          {
            string content = indexData.ToString();
            await IndexEditor.TryGetDataFromIndex(apkFilesDataGrid, data, content);
          }
        });
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при загрузке файлов: {ex.Message}");
      }
    }

    protected virtual void OnFileSelected(MouseButtonEventArgs e)
    {
      ArchiveSelected?.Invoke(this, e);
    }


    private async void MenuButton_PreviewMouseDownAsync(object sender, MouseButtonEventArgs e)
    {
      visibleLeftColumn = !visibleLeftColumn;

      if (!visibleLeftColumn)
      {
        int newWidth = 50;
        while (PanelManagment.Width.Value > newWidth)
        {
          PanelManagment.Width = new GridLength(PanelManagment.Width.Value - 25);
          if (ButtonPanel.Opacity > 0)
          {
            ButtonPanel.Opacity -= 0.1;
          }
          await Task.Delay(1);
        }
        ButtonPanel.Opacity = 0;
      }
      else
      {
        int newWidth = 150;
        while (PanelManagment.Width.Value < newWidth)
        {
          PanelManagment.Width = new System.Windows.GridLength(PanelManagment.Width.Value + 25);
          if (ButtonPanel.Opacity < 1)
          {
            ButtonPanel.Opacity += 0.1;
          }
          await Task.Delay(1);
        }

        PanelManagment.Width = new GridLength(150);
        ButtonPanel.Opacity = 1;
      }
    }

    private void CreateApk_PreviewMouseDownAsync(object sender, MouseButtonEventArgs e)
    {
      this.Effect = new System.Windows.Media.Effects.BlurEffect();
      var createApkWindow = new CreateApkwWindow();
      createApkWindow.DialogClosed += Dialog_Closed;
      createApkWindow.ShowDialog();
      this.Effect = null;
    }

    private async void Dialog_Closed(object sender, EventArgs e)
    {
      await LoadFilesToDataGridAsync();
    }

    private async void apkFilesDataGrid_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      var row = (sender as DataGrid).SelectedItem as ApkArchive;
      if (row != null)
      {
        var archivename = $"{row.ArchiveName}.apkw";
        if (!File.Exists(Path.Combine(ArchiveSettings.ArchivePath, archivename)))
        {
          MessageBox.Show($"Файл {archivename} был удален вне программы", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
          LogError($"Файл {archivename} был удален вне программы");
          var indexEditor = new IndexEditor();
          indexEditor.DeleteDataFromIndex(row.ArchiveName);
          await LoadFilesToDataGridAsync();
          return;
        }
        if (apkFilesDataGrid.SelectedItem is ApkArchive selectedFile)
        {
          OnFileSelected(e);
          apkFilesDataGrid.UnselectAll();
        }
      }
    }
  }
}
