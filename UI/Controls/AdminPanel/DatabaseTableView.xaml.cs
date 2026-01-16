using System.Collections;
using System.Windows.Controls;

namespace UI.Controls.AdminPanel
{
  /// <summary>
  /// Логика взаимодействия для DatabaseTableView.xaml
  /// </summary>
  public partial class DatabaseTableView : UserControl
  {
    public DatabaseTableView()
    {
      InitializeComponent();
    }

    public void SetData(string tableName, IList rows)
    {
      TableTitle.Text = tableName;
      Grid.ItemsSource = rows;
    }
  }
}
