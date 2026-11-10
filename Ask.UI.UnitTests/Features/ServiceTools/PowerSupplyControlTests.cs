using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.PowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using Ask.Device.Runtime.Device;
using Ask.UI.Features.ServiceTools.PowerSupply;
using Ask.UI.Shared.Controls;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Ask.UI.UnitTests.Features.ServiceTools;

[Collection(nameof(WpfApplicationCollection))]
public sealed class PowerSupplyControlTests(WpfApplicationFixture wpf)
{
  [Fact]
  public Task RootPanel_ControlsConfiguredSource_AndSharedConfigurationEditsLan()
    => wpf.RunAsync(VerifyAsync, TimeSpan.FromSeconds(30));

  private static async Task VerifyAsync()
  {
    var previousRole = RoleAuthorizationConfig.CurrentRole;
    string previousName = RoleAuthorizationConfig.CurrentRoleDisplayName;
    bool previousIdle = ExecutionConfig.GetIsIdleModeEnabled();
    var resources = new ResourceDictionary { Source = new Uri("/UI;component/Style.xaml", UriKind.Relative) };
    Application.Current.Resources.MergedDictionaries.Add(resources);
    PowerSupplyControl? panel = null;
    try
    {
      ExecutionConfig.SetIdleMode(true);
      RoleAuthorizationConfig.Clear();
      int loads = 0;
      IPowerSupply source = new B5108 { Id = 1, Number = 1, ConnectionDetails = "192.0.2.17" };
      var sources = new List<IPowerSupply> { source };
      panel = new PowerSupplyControl(_ =>
      {
        loads++;
        return Task.FromResult<IReadOnlyList<IPowerSupply>>(sources);
      });
      panel.Width = 920;
      panel.Height = 1100;
      panel.Measure(new Size(920, 1100));
      panel.Arrange(new Rect(0, 0, 920, 1100));
      panel.UpdateLayout();
      panel.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
      Assert.Equal(0, loads);
      Assert.False(((StackPanel)panel.FindName("OperationPanel")).IsEnabled);
      RoleAuthorizationConfig.SetCurrentRole(RoleType.Root, "root");
      panel.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
      await WaitAsync(() => loads == 1);
      Assert.Same(source, ((ComboBox)panel.FindName("SourceComboBox")).SelectedItem);
      var voltage = (NumericComboBox)panel.FindName("VoltageInput");
      var current = (NumericComboBox)panel.FindName("CurrentInput");
      Assert.Equal(0.1, voltage.Increment);
      Assert.Equal(1, current.Increment);
      Assert.Equal(5000, current.Maximum);
      await ClickAsync(panel, "Подключить");
      voltage.Value = 0.1;
      current.Value = 1;
      await ClickAsync(panel, "Установить U");
      await ClickAsync(panel, "Установить I");
      Assert.Equal(0.1m, await source.VoltageManager.GetVoltageAsync());
      Assert.Equal(0.001m, await source.CurrentManager.GetCurrentAsync());
      await ClickAsync(panel, "Выход: включить");
      Assert.True(await source.OutputManager.GetOutputStateAsync());
      await ClickAsync(panel, "Выход: выключить");
      Assert.False(await source.OutputManager.GetOutputStateAsync());
      await ClickAsync(panel, "Отключить связь");
      Assert.Null(panel.FindName("AddressInput"));
      Assert.Null(panel.FindName("PortInput"));
      VerifyConfigurationForm();
      foreach (string theme in new[] { "light", "dark" })
      {
        var themeResources = new ResourceDictionary
          { Source = new Uri($"/UI;component/Resources/Theme/{theme}.xaml", UriKind.Relative) };
        panel.Resources.MergedDictionaries.Add(themeResources);
        try
        {
          panel.Background = (Brush)panel.FindResource("SettingsMainBackgroundSolidColorBrush");
          await Dispatcher.Yield(DispatcherPriority.Render);
          panel.Width = 920;
          panel.Height = 1100;
          panel.Measure(new Size(920, 1100));
          panel.Arrange(new Rect(0, 0, 920, 1100));
          panel.UpdateLayout();
          Assert.True(voltage.ActualWidth > 0);
          string? directory = Environment.GetEnvironmentVariable("ASK_UI_ARTIFACT_DIR");
          if (directory != null)
          {
            Directory.CreateDirectory(directory);
            var bitmap = new RenderTargetBitmap(920, 1100, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(panel);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(Path.Combine(directory, "power-supply-" + theme + ".png"));
            encoder.Save(file);
          }
        }
        finally { panel.Resources.MergedDictionaries.Remove(themeResources); }
      }
    }
    finally
    {
      panel?.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
      Application.Current.Resources.MergedDictionaries.Remove(resources);
      ExecutionConfig.SetIdleMode(previousIdle);
      if (previousRole.HasValue) RoleAuthorizationConfig.SetCurrentRole(previousRole.Value, previousName);
      else RoleAuthorizationConfig.Clear();
    }
  }

  private static void VerifyConfigurationForm()
  {
    var tester = new ManagerChassis { Number = 2 };
    var window = new global::UI.Controls.Settings.DeviceConfig.PowerSupply.PowerSupplyWindow();
    window.SetSettings(tester);
    var form = window.DetachSettingsControl();
    var modelBox = (ComboBox)form.FindName("DeviceModelSelectionBox");
    Assert.Contains("Б5-108", modelBox.Items.Cast<string>());
    modelBox.SelectedItem = "Б5-108";
    var connectionBox = (ComboBox)form.FindName("ConnectionTypeSelectionBox");
    connectionBox.SelectedItem = connectionBox.Items.Cast<ComboBoxItem>().Single(item => Equals(item.Content, "IP"));
    ((TextBox)form.FindName("DeviceNumberTextBox")).Text = "17";
    Assert.Equal(192, form.IpPart1Value);
    Assert.Equal(168, form.IpPart2Value);
    Assert.Equal(2, form.IpPart3Value);
    Assert.Equal(17, form.IpPart4Value);
    Assert.Equal(80, form.GetPowerSupplyPort());
    var instance = Assert.IsType<B5108>(form.CreateSelectedDeviceInstance());
    var dto = new global::UI.Controls.Settings.DeviceConfig.Base.DeviceSettingsProcessorBase()
      .ProcessDevice<PowerSupplyDto>(instance, form);
    Assert.Equal(2, dto.NumberChassis);
    Assert.Equal("192.168.2.17", dto.ConnectionDetails);
    Assert.Equal(17, dto.Number);
    window.Close();
    var editing = new global::UI.Controls.Settings.DeviceConfig.PowerSupply.PowerSupplyWindow();
    dto.Id = 9;
    dto.Port = 5080;
    editing.SetSettings(tester, dto);
    var editingForm = editing.DetachSettingsControl();
    Assert.Equal(5080, editingForm.GetPowerSupplyPort());
    Assert.Equal(17, editingForm.IpPart4Value);
    Assert.Equal(17, editingForm.NumberDevice);
    CaptureConfiguration(editingForm, "power-supply-settings.png", 470, 1000);
    editing.Close();
    var manager = new global::UI.Controls.Settings.DeviceConfig.DeviceManager.DeviceManagerControl();
    manager.AddDevice(dto);
    var list = (global::UI.Controls.Settings.DeviceConfig.Controls.DeviceListControl)manager.FindName("PowerSupplyControl");
    Assert.Equal("Источник питания", list.DeviceTitle);
    Assert.Single(list.Devices);
    CaptureConfiguration(manager, "power-supply-device-list.png", 1100, 700);
    manager.SetEditingEnabled(false);
    Assert.False(list.IsEditingEnabled);
  }

  private static void CaptureConfiguration(FrameworkElement control, string name, int width, int height)
  {
    string? directory = Environment.GetEnvironmentVariable("ASK_UI_ARTIFACT_DIR");
    if (directory == null) return;
    control.Resources.MergedDictionaries.Add(new ResourceDictionary
      { Source = new Uri("/UI;component/Resources/Theme/dark.xaml", UriKind.Relative) });
    control.Width = width;
    control.Height = height;
    control.Measure(new Size(width, height));
    control.Arrange(new Rect(0, 0, width, height));
    control.UpdateLayout();
    var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
    bitmap.Render(control);
    Directory.CreateDirectory(directory);
    using var file = File.Create(Path.Combine(directory, name));
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(bitmap));
    encoder.Save(file);
  }

  private static async Task ClickAsync(PowerSupplyControl panel, string title)
  {
    var button = Descendants(panel).OfType<Button>().Single(b => Equals(b.Content, title));
    Assert.True(button.IsEnabled);
    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    await WaitAsync(() => !((Button)panel.FindName("CancelButton")).IsEnabled);
  }

  private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
  {
    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
    {
      var child = VisualTreeHelper.GetChild(parent, i);
      yield return child;
      foreach (var descendant in Descendants(child)) yield return descendant;
    }
    if (parent is ContentControl { Content: DependencyObject content }
        && VisualTreeHelper.GetChildrenCount(parent) == 0)
    {
      yield return content;
      foreach (var descendant in Descendants(content)) yield return descendant;
    }
  }

  private static async Task WaitAsync(Func<bool> condition)
  {
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    while (!condition()) await Task.Delay(10, timeout.Token);
  }
}
