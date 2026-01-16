using DataBaseConfiguration;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.AdminPanel
{
  /// <summary>
  /// Логика взаимодействия для DataBaseView.xaml
  /// </summary>
  public partial class DataBaseView : UserControl
  {
    private readonly DbContext _context;

    public DataBaseView()
    {
      InitializeComponent();
      _context = DataBaseConfig.Context;

      LoadAllTables();
    }

    private void LoadAllTables()
    {
      var dbSetProperties = _context
          .GetType()
          .GetProperties(BindingFlags.Public | BindingFlags.Instance)
          .Where(p => p.PropertyType.IsGenericType &&
                      p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
          .ToList();

      var setMethod = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes);

      foreach (var p in dbSetProperties)
      {
        var entityType = p.PropertyType.GetGenericArguments()[0];

        var generic = setMethod.MakeGenericMethod(entityType);
        var set = generic.Invoke(_context, null);
        var queryable = (IQueryable)set;
        var data = queryable.Cast<object>().ToList();

        var tableView = new DatabaseTableView();
        tableView.SetData(p.Name, data);

        TablesContainer.Children.Add(tableView);
      }
    }
  }
}
