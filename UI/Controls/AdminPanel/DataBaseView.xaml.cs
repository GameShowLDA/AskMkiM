using DataBaseConfiguration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private readonly MethodInfo _setMethod;
    private readonly List<TableItem> _tables = new();

    public DataBaseView()
    {
      InitializeComponent();
      _context = DataBaseConfig.Context;
      _setMethod = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)
        ?? throw new InvalidOperationException("Не удалось найти метод DbContext.Set().");

      LoadTableList();
      TablesList.ItemsSource = _tables;

      if (_tables.Count > 0)
      {
        TablesList.SelectedIndex = 0;
      }
      else
      {
        TableContent.Content = BuildEmptyState("Таблицы не найдены.");
      }
    }

    private void LoadTableList()
    {
      var dbSetProperties = _context
          .GetType()
          .GetProperties(BindingFlags.Public | BindingFlags.Instance)
          .Where(p => p.PropertyType.IsGenericType &&
                      p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
          .ToList();

      foreach (var p in dbSetProperties)
      {
        var entityType = p.PropertyType.GetGenericArguments()[0];
        _tables.Add(new TableItem(p.Name, entityType));
      }

      _tables.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
    }

    private void TablesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (TablesList.SelectedItem is not TableItem selected)
      {
        TableContent.Content = BuildEmptyState("Выберите таблицу.");
        return;
      }

      var data = LoadTableRows(selected.EntityType);
      var tableView = new DatabaseTableView();
      tableView.SetData(selected.Name, data);
      TableContent.Content = tableView;
    }

    private IList LoadTableRows(Type entityType)
    {
      var generic = _setMethod.MakeGenericMethod(entityType);
      var set = generic.Invoke(_context, null);
      var queryable = (IQueryable)set;
      return queryable.Cast<object>().ToList();
    }

    private static TextBlock BuildEmptyState(string text)
    {
      return new TextBlock
      {
        Text = text,
        Margin = new Thickness(12),
        TextWrapping = TextWrapping.Wrap,
        Style = (Style)Application.Current.TryFindResource("SettingsDescriptionStyle")
      };
    }

    private sealed class TableItem
    {
      public TableItem(string name, Type entityType)
      {
        Name = name;
        EntityType = entityType;
      }

      public string Name { get; }

      public Type EntityType { get; }
    }
  }
}
