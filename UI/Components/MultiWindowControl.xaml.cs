using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
  /// Логика взаимодействия для MultiWindowControl.xaml
  /// </summary>
  public partial class MultiWindowControl : UserControl
  {
    public MultiWindowControl()
    {
      InitializeComponent();
    }
    /// <summary>
    /// Добавляет новый MultiEditorControl в контейнер.
    /// </summary>
    public void OpenFileInEditor(string filePath)
    {
      if (EditorsContainer == null) return; // Предотвращает ошибку

      var newEditor = new MultiEditorControl();
      newEditor.OpenFile(filePath);

      EditorsContainer.Children.Add(newEditor);
    }


    /// <summary>
    /// Удаляет конкретный MultiEditorControl.
    /// </summary>
    public void CloseEditor(MultiEditorControl editor)
    {
      EditorsContainer.Children.Remove(editor);
    }

    /// <summary>
    /// Обрабатывает перемещение разделителя GridSplitter для изменения размера области результатов поиска.
    /// </summary>
    private void GridSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
      if (SearchResultsRow == null) return; // Предотвращает исключение

      double newHeight = SearchResultsRow.Height.Value - e.VerticalChange;

      if (newHeight > 50) // Минимальная высота области результатов поиска
      {
        SearchResultsRow.Height = new GridLength(newHeight);
      }
    }
  }
}
