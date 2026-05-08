using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.EventInterfaces;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Metadata.Enums.HotkeysEnums;

namespace Ask.Core.Services.EventCore.Events
{
  /// <summary>
  /// �������, ��������� � ����������� ���������� � ����������� �������� ����������.
  /// </summary>
  /// <remarks>
  /// ��� ������� ������������ ��� ������������ ��������� ��������� ����������, ����� ���
  /// ��������� ���������� ������, ������� � �������������� ����� � ������ ��������� ���������.
  /// </remarks>
  public static class ExecutionEvents
  {
    /// <summary>
    /// �������, ������������ ��� ��������� ��������� ���������� ������ ���������� ���������.
    /// </summary>
    /// <remarks>
    /// ������� ���������� ��� ���������������� ���������� � ���, ��� ����� ���������� ���������� ��� ������� ��� ��������.
    /// </remarks>
    public class StepByStepModeChanged : IEvent
    {
      /// <summary>
      /// ����������, ����������� �� ��������� �����.
      /// </summary>
      public bool IsEnabled { get; }

      /// <summary>
      /// �������������� ����� ��������� ������� ��������� ��������� ���������� ������.
      /// </summary>
      /// <param name="isEnabled">
      /// <see langword="true"/> � ���� ��������� ����� �������;  
      /// <see langword="false"/> � ���� ����� ��������.
      /// </param>
      public StepByStepModeChanged(bool isEnabled)
      {
        IsEnabled = isEnabled;
      }
    }

    public class ActiveDeviceChanged : IEvent
    {
      public List<IAttachableDevice> Devices { get; }
      public ActiveDeviceChanged(List<IAttachableDevice> devices)
      {
        Devices = devices;
      }
    }
    public class DeviceStatusUpdate : IEvent { }

    /// <summary>
    /// ������� ������� ������ ���������� �����������.
    /// </summary>
    public class ControlButtonPressed : IEvent
    {
      /// <summary>
      /// ����� ������ ���� ������.
      /// </summary>
      public ExecutionControlButton Button { get; }

      public ControlButtonPressed(ExecutionControlButton button)
      {
        Button = button;
      }
    }

    /// <summary>
    /// ������� ������� ������� F4 � ��������� ������,
    /// �������������� �� ����� ��������.
    /// </summary>
    public class BreakpointF4Pressed : IEvent
    {
      /// <summary>
      /// �������� �������, �� ������� ������ ������� F4.
      /// </summary>
      public IExecutionCommandInfo CommandInfo { get; }

      public BreakpointF4Pressed(IExecutionCommandInfo commandInfo)
      {
        CommandInfo = commandInfo;
      }
    }
  }
}
