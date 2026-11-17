using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ask.UI.ThirdParty.Docking.Integration.ExecutionWorkspace;

namespace ExecutionWorkspacePrototype;

public partial class MainWindow : Window
{
    private readonly string _layoutPath = Path.Combine(Path.GetTempPath(), "AskMkiM.ExecutionWorkspace.prototype.json");

    public MainWindow()
    {
        InitializeComponent();
        Workspace.SetContent(ExecutionWorkspacePane.Protocol, CreateText("Протокол исполнения", 80));
        Workspace.SetContent(ExecutionWorkspacePane.Errors, CreateText("Таблица ошибок и точек остановки", 12));
        Workspace.SetContent(ExecutionWorkspacePane.Equipment, CreateText("Состояние оборудования", 20));
        Workspace.SetContent(ExecutionWorkspacePane.Translation, CreateText("Транслированная программа", 45));
        Workspace.SetContent(ExecutionWorkspacePane.Source, CreateText("Исходная программа", 45));
        Workspace.SetContent(ExecutionWorkspacePane.Result, CreateText("Итоговый протокол", 45));
    }

    private static TextBox CreateText(string title, int lines)
    {
        return new TextBox
        {
            Text = string.Join(Environment.NewLine, Enumerable.Range(1, lines).Select(index => $"{index,3}  {title}: тестовая строка {index}")),
            IsReadOnly = title != "Исходная программа",
            AcceptsReturn = true,
            AcceptsTab = true,
            FontFamily = new FontFamily("Cascadia Mono"),
            FontSize = 14,
            Background = new SolidColorBrush(Color.FromRgb(23, 30, 40)),
            Foreground = new SolidColorBrush(Color.FromRgb(230, 236, 245)),
            BorderThickness = new Thickness(0),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
    }

    private void DarkThemeClick(object sender, RoutedEventArgs e) => Workspace.IsDarkTheme = true;

    private void LightThemeClick(object sender, RoutedEventArgs e) => Workspace.IsDarkTheme = false;

    private void SaveLayoutClick(object sender, RoutedEventArgs e) => Workspace.SaveLayout(_layoutPath);

    private void RestoreLayoutClick(object sender, RoutedEventArgs e) => Workspace.TryRestoreLayout(_layoutPath);

    private void ResetLayoutClick(object sender, RoutedEventArgs e) => Workspace.ResetLayout();
}
