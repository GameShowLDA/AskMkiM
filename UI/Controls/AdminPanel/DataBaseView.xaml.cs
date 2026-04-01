using DataBaseConfiguration;
using Message;
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
    private DatabaseTableView? _activeTableView;
    private TableItem? _currentTable;
    private bool _suppressSelectionChange;

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
      if (_suppressSelectionChange)
      {
        return;
      }

      if (TablesList.SelectedItem is not TableItem selected)
      {
        TableContent.Content = BuildEmptyState("Выберите таблицу.");
        return;
      }

      if (_activeTableView?.HasPendingChanges == true)
      {
        var result = MessageBoxCustom.Show(
          "Есть несохраненные изменения. Сохранить перед переключением?",
          "База данных",
          MessageBoxButton.YesNoCancel,
          MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
        {
          _suppressSelectionChange = true;
          TablesList.SelectedItem = _currentTable;
          _suppressSelectionChange = false;
          return;
        }

        if (result == MessageBoxResult.Yes && !TrySaveChanges())
        {
          _suppressSelectionChange = true;
          TablesList.SelectedItem = _currentTable;
          _suppressSelectionChange = false;
          return;
        }

        if (result == MessageBoxResult.No)
        {
          _context.ChangeTracker.Clear();
        }
      }

      LoadTable(selected);
    }

    private void LoadTable(TableItem selected)
    {
      _currentTable = selected;
      var data = LoadTableRows(selected.EntityType);
      var tableView = new DatabaseTableView();
      tableView.SetData(selected.Name, data);
      tableView.SetReadOnlyProperties(GetKeyPropertyNames(selected.EntityType));
      tableView.SaveRequested += OnSaveRequested;
      tableView.ResetRequested += OnResetRequested;
      _activeTableView = tableView;
      TableContent.Content = tableView;
    }

    private IList LoadTableRows(Type entityType)
    {
      var generic = _setMethod.MakeGenericMethod(entityType);
      var set = generic.Invoke(_context, null);
      var queryable = (IQueryable)set;
      return queryable.Cast<object>().ToList();
    }

    private IEnumerable<string> GetKeyPropertyNames(Type entityType)
    {
      var modelEntity = _context.Model.FindEntityType(entityType);
      return modelEntity?.FindPrimaryKey()?.Properties.Select(p => p.Name)
             ?? Enumerable.Empty<string>();
    }

    private void OnSaveRequested(object? sender, EventArgs e)
    {
      if (TrySaveChanges())
      {
        _activeTableView?.MarkSaved();
      }
    }

    private void OnResetRequested(object? sender, EventArgs e)
    {
      if (_currentTable == null || _activeTableView == null)
      {
        return;
      }

      _context.ChangeTracker.Clear();
      var data = LoadTableRows(_currentTable.EntityType);
      _activeTableView.ReplaceRows(data);
    }

    private bool TrySaveChanges()
    {
      try
      {
        _context.SaveChanges();
        return true;
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Не удалось сохранить изменения: {ex.Message}",
          "Ошибка сохранения",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
        return false;
      }
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
