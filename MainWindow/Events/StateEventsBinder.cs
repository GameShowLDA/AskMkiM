using System;
using AppConfiguration.Base;
using AppConfiguration.Execution;
using Utilities.USB;

namespace MainWindowProgram.Events
{
  /// <summary>
  /// Подписывает обработчики для событий состояния приложения.
  /// </summary>
  public class StateEventsBinder
  {
    private readonly USBMonitorService _usbMonitorService;

    public StateEventsBinder(USBMonitorService usbMonitorService)
    {
      _usbMonitorService = usbMonitorService;
    }

    public void Bind()
    {
      EventAggregator.LockedChanged += OnLockedChanged;
      EventAggregator.AdminRightsChanged += OnAdminRightsChanged;
      _usbMonitorService.AdminRightsChanged += OnAdminRightsChangedHandler;
      ExecutionConfig.IdleModeChange += OnIdleModeChange;
    }

    private void OnIdleModeChange(object? sender, bool e) { /* TODO */ }

    private void OnLockedChanged(bool isLocked) { /* TODO */ }
    private void OnAdminRightsChanged(bool isAdmin) { /* TODO */ }
    private void OnAdminRightsChangedHandler(object sender, bool newRights) { /* TODO */ }
  }
}
