using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using AvalonDock.Core;
using AvalonDock.Layout;
using AvalonDock.Serializer.Json;
using AvalonDock.Themes;

namespace Ask.UI.ThirdParty.Docking.Integration.ExecutionWorkspace;

/// <summary>
/// Предоставляет перемещаемую рабочую область исполнителя без зависимости от логики выполнения.
/// </summary>
public partial class ExecutionWorkspaceControl : UserControl
{
    private const string ProtocolId = "runner.protocol";
    private const string ErrorsId = "runner.errors";
    private const string EquipmentId = "runner.equipment";
    private const string TranslationId = "runner.translation";
    private const string SourceId = "runner.source";
    private const string ResultId = "runner.result";
    private static readonly object EmptyPaneContent = new();

    private readonly Dictionary<ExecutionWorkspacePane, object?> _contents = [];
    private readonly Dictionary<ExecutionWorkspacePane, LayoutAnchorable> _panes = [];
    private bool _themeSubscribed;

    public ExecutionWorkspaceControl()
    {
        InitializeComponent();
        IsDarkTheme = IsDark(ThemeSettings.CurrentTheme);
        ApplyTheme(IsDarkTheme);
        ResetLayout();
        Loaded += ExecutionWorkspaceControl_Loaded;
        Unloaded += ExecutionWorkspaceControl_Unloaded;
    }

    public static readonly DependencyProperty IsDarkThemeProperty = DependencyProperty.Register(
        nameof(IsDarkTheme),
        typeof(bool),
        typeof(ExecutionWorkspaceControl),
        new PropertyMetadata(true, OnIsDarkThemeChanged));

    /// <summary>
    /// Признак использования тёмной темы AvalonDock.
    /// </summary>
    public bool IsDarkTheme
    {
        get => (bool)GetValue(IsDarkThemeProperty);
        set => SetValue(IsDarkThemeProperty, value);
    }

    /// <summary>
    /// Устанавливает содержимое указанной панели, не изменяя её положение.
    /// </summary>
    /// <param name="pane">Панель рабочей области.</param>
    /// <param name="content">Отображаемое содержимое.</param>
    public void SetContent(ExecutionWorkspacePane pane, object? content)
    {
        _contents[pane] = content;
        if (_panes.TryGetValue(pane, out var anchorable))
        {
            anchorable.Content = content;
        }
    }

    /// <summary>
    /// Активирует указанную панель.
    /// </summary>
    /// <param name="pane">Панель рабочей области.</param>
    public void ActivatePane(ExecutionWorkspacePane pane)
    {
        if (!_panes.TryGetValue(pane, out var anchorable))
        {
            return;
        }

        if (!anchorable.IsVisible)
        {
            anchorable.Show();
        }

        anchorable.IsActive = true;
        anchorable.IsSelected = true;
    }

    /// <summary>
    /// Изменяет заголовок указанной панели.
    /// </summary>
    public void SetTitle(ExecutionWorkspacePane pane, string title)
    {
        if (_panes.TryGetValue(pane, out var anchorable))
        {
            anchorable.Title = title;
        }
    }

    /// <summary>
    /// Скрывает указанную панель с возможностью последующего восстановления.
    /// </summary>
    public void HidePane(ExecutionWorkspacePane pane)
    {
        if (_panes.TryGetValue(pane, out var anchorable) && anchorable.IsVisible)
        {
            anchorable.Hide();
        }
    }

    /// <summary>
    /// Показывает и при необходимости активирует указанную панель.
    /// </summary>
    public void ShowPane(ExecutionWorkspacePane pane, bool activate = true)
    {
        if (!_panes.TryGetValue(pane, out var anchorable))
        {
            return;
        }

        if (!anchorable.IsVisible)
        {
            anchorable.Show();
        }

        if (activate)
        {
            ActivatePane(pane);
        }
    }

    /// <summary>
    /// Определяет, отображается ли указанная панель.
    /// </summary>
    public bool IsPaneVisible(ExecutionWorkspacePane pane) =>
        _panes.TryGetValue(pane, out var anchorable) && anchorable.IsVisible;

    /// <summary>
    /// Изменяет видимость плавающих окон рабочей области.
    /// </summary>
    public void SetFloatingWindowsVisible(bool isVisible)
    {
        foreach (var window in WorkspaceDockManager.FloatingWindows.ToArray())
        {
            if (isVisible)
            {
                window.Show();
            }
            else
            {
                window.Hide();
            }
        }
    }

    /// <summary>
    /// Закрывает плавающие окна рабочей области.
    /// </summary>
    public void CloseFloatingWindows()
    {
        foreach (var window in WorkspaceDockManager.FloatingWindows.ToArray())
        {
            window.Close();
        }
    }

    /// <summary>
    /// Возвращает исходное расположение панелей.
    /// </summary>
    public void ResetLayout()
    {
        DetachContentsFromCurrentLayout();
        _panes.Clear();

        var protocol = CreatePane(ExecutionWorkspacePane.Protocol, ProtocolId, "Протокол исполнения");
        var errors = CreatePane(ExecutionWorkspacePane.Errors, ErrorsId, "Ошибки и точки остановки");
        var equipment = CreatePane(ExecutionWorkspacePane.Equipment, EquipmentId, "Состояние оборудования");
        var translation = CreatePane(ExecutionWorkspacePane.Translation, TranslationId, "Транслированная программа");
        var source = CreatePane(ExecutionWorkspacePane.Source, SourceId, "Исходная программа");
        var result = CreatePane(ExecutionWorkspacePane.Result, ResultId, "Итоговый протокол");

        var leftGroup = new LayoutAnchorablePaneGroup { Orientation = Orientation.Vertical, DockWidth = new GridLength(1, GridUnitType.Star) };
        var protocolPane = new LayoutAnchorablePane { DockHeight = new GridLength(3, GridUnitType.Star) };
        protocolPane.Children.Add(protocol);
        var errorsPane = new LayoutAnchorablePane { DockHeight = new GridLength(1, GridUnitType.Star) };
        errorsPane.Children.Add(errors);
        leftGroup.Children.Add(protocolPane);
        leftGroup.Children.Add(errorsPane);

        var rightPane = new LayoutAnchorablePane { DockWidth = new GridLength(1, GridUnitType.Star) };
        rightPane.Children.Add(equipment);
        rightPane.Children.Add(translation);
        rightPane.Children.Add(source);
        rightPane.Children.Add(result);

        var rootPanel = new LayoutPanel { Orientation = Orientation.Horizontal };
        rootPanel.Children.Add(leftGroup);
        rootPanel.Children.Add(rightPane);
        WorkspaceDockManager.Layout = new LayoutRoot { RootPanel = rootPanel };

        protocol.IsActive = true;
        equipment.IsSelected = true;
    }

    /// <summary>
    /// Сохраняет расположение панелей в JSON-файл.
    /// </summary>
    /// <param name="filePath">Путь к файлу раскладки.</param>
    public void SaveLayout(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = fullPath + ".tmp";
        try
        {
            new JsonLayoutSerializer(WorkspaceDockManager).Serialize(temporaryPath);
            File.Move(temporaryPath, fullPath, true);
            FloatingWindowPlacementStore.Save(
                FloatingWindowPlacementStore.GetCompanionPath(fullPath),
                WorkspaceDockManager.FloatingWindows);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <summary>
    /// Восстанавливает расположение панелей из JSON-файла.
    /// </summary>
    /// <param name="filePath">Путь к файлу раскладки.</param>
    /// <returns><see langword="true"/>, если раскладка восстановлена.</returns>
    public bool TryRestoreLayout(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        var contentsById = _panes.Values.ToDictionary(item => item.ContentId, item => item.Content);
        DetachContentsFromCurrentLayout();

        try
        {
            var serializer = new JsonLayoutSerializer(WorkspaceDockManager);
            serializer.LayoutSerializationCallback += (_, args) =>
            {
                if (args.Model.ContentId is not null && contentsById.TryGetValue(args.Model.ContentId, out var content))
                {
                    // AvalonDock removes an unresolved pane during fixup. Some runner panes are
                    // intentionally empty until their content is produced, but their saved place
                    // must survive restoration.
                    args.Content = content ?? EmptyPaneContent;
                }
            };
            serializer.Deserialize(filePath);
            IndexRestoredPanes();
            var owner = Window.GetWindow(this);
            FloatingWindowPlacementStore.Restore(
                FloatingWindowPlacementStore.GetCompanionPath(filePath),
                WorkspaceDockManager.Layout,
                WorkspaceDockManager.FloatingWindows,
                owner is null ? nint.Zero : new WindowInteropHelper(owner).Handle);
            return HasAllRequiredPanes();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.WriteLine(exception);
            ResetLayout();
            return false;
        }
    }

    private static void OnIsDarkThemeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var control = (ExecutionWorkspaceControl)dependencyObject;
        control.ApplyTheme((bool)args.NewValue);
    }

    private void ApplyTheme(bool isDarkTheme) =>
        WorkspaceDockManager.Theme = isDarkTheme ? new ArcDarkTheme() : new ArcLightTheme();

    private void ExecutionWorkspaceControl_Loaded(object sender, RoutedEventArgs e)
    {
        IsDarkTheme = IsDark(ThemeSettings.CurrentTheme);
        if (_themeSubscribed)
        {
            return;
        }

        ThemeSettings.ThemeChanged += ThemeSettings_ThemeChanged;
        _themeSubscribed = true;
    }

    private void ExecutionWorkspaceControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (!_themeSubscribed)
        {
            return;
        }

        ThemeSettings.ThemeChanged -= ThemeSettings_ThemeChanged;
        _themeSubscribed = false;
    }

    private void ThemeSettings_ThemeChanged(ThemeMode theme) =>
        Dispatcher.BeginInvoke(() => IsDarkTheme = IsDark(theme));

    private static bool IsDark(ThemeMode theme) => theme is ThemeMode.Dark or ThemeMode.DarkCustom;

    private LayoutAnchorable CreatePane(ExecutionWorkspacePane pane, string contentId, string title)
    {
        var anchorable = new LayoutAnchorable
        {
            ContentId = contentId,
            Title = title,
            Content = _contents.GetValueOrDefault(pane),
            CanClose = false,
            CanHide = false,
            CanFloat = true,
            CanDockAsTabbedDocument = true,
        };
        _panes[pane] = anchorable;
        return anchorable;
    }

    private void DetachContentsFromCurrentLayout()
    {
        if (WorkspaceDockManager.Layout is null)
        {
            return;
        }

        foreach (var anchorable in WorkspaceDockManager.Layout.Descendents().OfType<LayoutAnchorable>())
        {
            anchorable.Content = null;
        }
    }

    private void IndexRestoredPanes()
    {
        _panes.Clear();
        foreach (var anchorable in WorkspaceDockManager.Layout.Descendents().OfType<LayoutAnchorable>())
        {
            if (TryGetPane(anchorable.ContentId, out var pane))
            {
                _panes[pane] = anchorable;
                _contents[pane] = ReferenceEquals(anchorable.Content, EmptyPaneContent)
                    ? null
                    : anchorable.Content;
                if (ReferenceEquals(anchorable.Content, EmptyPaneContent))
                    anchorable.Content = null;
            }
        }
    }

    private bool HasAllRequiredPanes()
    {
        if (_panes.Count == Enum.GetValues<ExecutionWorkspacePane>().Length)
        {
            return true;
        }

        ResetLayout();
        return false;
    }

    private static bool TryGetPane(string? contentId, out ExecutionWorkspacePane pane)
    {
        pane = contentId switch
        {
            ProtocolId => ExecutionWorkspacePane.Protocol,
            ErrorsId => ExecutionWorkspacePane.Errors,
            EquipmentId => ExecutionWorkspacePane.Equipment,
            TranslationId => ExecutionWorkspacePane.Translation,
            SourceId => ExecutionWorkspacePane.Source,
            ResultId => ExecutionWorkspacePane.Result,
            _ => default,
        };
        return contentId is ProtocolId or ErrorsId or EquipmentId or TranslationId or SourceId or ResultId;
    }
}
