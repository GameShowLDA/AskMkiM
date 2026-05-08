using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;

namespace Ask.Core.Services.App
{
  /// <summary>
  /// �������� ��������� ���������� ������.
  /// </summary>
  public enum StepModeActivationSource
  {
    Unknown = 0,
    ManualOrConfig = 1,
    Breakpoint = 2
  }

  /// <summary>
  /// ����������� �������� ��� ���������� �������� ���������� ���������� ��������� (F10/F11)
  /// � ���������� ������� ������.
  /// ������������ ��� ����������� ��������� "��� ������" (F11), "��� ������ �����" (F10)
  /// � ������������ �������� ��������� ��� ���������� ��������� ��������.
  /// </summary>
  public static class StepControlManager
  {
    /// <summary>
    /// ����, �����������, ������� �� ��������� �����.
    /// ���� <c>true</c>, ���������� ��������� ����� ��������������� �� ������ ����.
    /// </summary>
    public static bool StepMode => _stepMode;

    /// <summary>
    /// ���������� ���� ��� �������� ��������� ���������� ������.
    /// </summary>
    private static bool _stepMode;

    /// <summary>
    /// ���������� ���� ������� ������ ���������� ������ (��������, ����� ������ �� ����).
    /// </summary>
    private static bool _stepBypassRequested;

    /// <summary>
    /// �������� ��������� �������� ���������� ������.
    /// </summary>
    private static StepModeActivationSource _activationSource = StepModeActivationSource.Unknown;

    /// <summary>
    /// �������� �������, ������� ������������ ��������� ����� ����� ����� ��������.
    /// </summary>
    private static IExecutionCommandInfo? _breakpointCommandInfo;

    /// <summary>
    /// ���������� <c>true</c>, ���� ����� ���������� ���������� ������ ���������
    /// ���������� ���������� ��� ���������.
    /// </summary>
    public static bool StepBypassRequested => _stepBypassRequested;

    /// <summary>
    /// ����, ������������ ��� ���������� ����������.
    /// <para><c>true</c> � ��� ������ (F11).</para>
    /// <para><c>false</c> � ��� ������ ����� (F10).</para>
    /// </summary>
    public static bool IsStepInto { get; set; } = false;

    /// <summary>
    /// ����, �����������, ��� ���������� ��������� ������ ���������� ����� ������.
    /// ������������ ��� ���������� ��������� ������ F10/F11.
    /// </summary>
    public static bool InsideBlock { get; private set; } = false;

    /// <summary>
    /// ���� F10 �� ��������� ������� ��� �������� ��������.
    /// ���� <c>true</c>, �������� ���� ������������ �� ������� ���������
    /// ��������� ��������� ������� ���������� ��.
    /// </summary>
    public static bool StepOverUntilNextControlCommand { get; private set; }

    /// <summary>
    /// �������� ��������� �������� ���������� ������.
    /// </summary>
    public static StepModeActivationSource ActivationSource => _activationSource;

    /// <summary>
    /// �������, ��-�� ������� ��� ����������� ��������� ����� ����� ����������.
    /// </summary>
    public static IExecutionCommandInfo? BreakpointCommandInfo => _breakpointCommandInfo;

    /// <summary>
    /// ���������� <c>true</c>, ���� ��������� ����� ������� � ��� ������� ������ �� �����������.
    /// </summary>
    public static bool IsBreakpointStepModeActive =>
      _stepMode &&
      _activationSource == StepModeActivationSource.Breakpoint &&
      _breakpointCommandInfo != null;

    /// <summary>
    /// ������������� ��������� ����� �� ��������� ���� ������.
    /// ������ ���������� ����� ������������ ��������� (<c>ShowMessageAsync</c>).
    /// </summary>
    public static void EnterBlock()
    {
      InsideBlock = true;
    }

    /// <summary>
    /// ������������� ��������� ��������� ���������� ������ ����� ������.
    /// ������ ������������ ��� ������ �� ����� �������.
    /// </summary>
    public static void ExitBlock() => InsideBlock = false;

    /// <summary>
    /// ��������� ������ ����� ���� ��������� ���������� ������.
    /// ������ ���������� ����� ���������� ���������� ���������.
    /// </summary>
    public static void Reset()
    {
      IsStepInto = false;
      InsideBlock = false;
      StepOverUntilNextControlCommand = false;
      _activationSource = StepModeActivationSource.Unknown;
      _breakpointCommandInfo = null;
    }

    /// <summary>
    /// ��������� ��������� ����� � F10 �� ������ ��������� ������� ��.
    /// </summary>
    public static void RequestStepOverUntilNextControlCommand()
    {
      IsStepInto = false;
      StepOverUntilNextControlCommand = true;
    }

    /// <summary>
    /// ��������� ����� � F11 ��� ������ � ���������� ����� F10-������.
    /// </summary>
    public static void SetStepIntoMode()
    {
      IsStepInto = true;
      StepOverUntilNextControlCommand = false;
    }

    /// <summary>
    /// ���������� ���� �������� ��������� ������� ��� F10.
    /// </summary>
    public static void CompleteStepOverUntilNextControlCommand()
    {
      StepOverUntilNextControlCommand = false;
    }

    /// <summary>
    /// �������������� ��������� ���������� ������ ��� ������ ����������.
    /// ��������� ��������� �� <c>ExecutionConfig</c> � ������������� �� �������
    /// <c>StepByStepModeChanged</c> ��� ������������� ��������� ���������.
    /// </summary>
    public static async Task InitializeAsync()
    {
      _stepMode = ExecutionConfig.GetIsStepByStepModeEnabled();
      if (_stepMode)
      {
        EnableStepMode(true);
      }
      else
      {
        DisableStepMode();
      }
    }

    /// <summary>
    /// �������� ��������� ����� ��� ������/����������������.
    /// </summary>
    /// <param name="isStepInto">
    /// <c>true</c> � ��� ������ (F11),
    /// <c>false</c> � ��� ������ ����� (F10).
    /// </param>
    public static void EnableStepMode(bool isStepInto)
    {
      EnableStepModeCore(isStepInto, StepModeActivationSource.ManualOrConfig);
    }

    /// <summary>
    /// �������� ��������� ����� �� ����� �������� � ��������� �������� �������.
    /// ���� ����� ��� ������� �� �� �����������, �������� �� ��������.
    /// </summary>
    /// <param name="commandInfo">�������� �������, �� ������� �������� ����������.</param>
    /// <param name="isStepInto">
    /// <c>true</c> � ��� ������ (F11),
    /// <c>false</c> � ��� ������ ����� (F10).
    /// </param>
    public static void EnableStepModeByBreakpoint(IExecutionCommandInfo commandInfo, bool isStepInto = true)
    {
      if (!_stepMode)
      {
        EnableStepModeCore(isStepInto, StepModeActivationSource.Breakpoint, commandInfo);
        return;
      }

      IsStepInto = isStepInto;

      if (_activationSource == StepModeActivationSource.Breakpoint)
      {
        _breakpointCommandInfo = commandInfo;
      }
    }

    /// <summary>
    /// �������� ��������� ����� � ��������� ��������� ���������.
    /// </summary>
    /// <param name="isStepInto">
    /// <c>true</c> � ��� ������ (F11),
    /// <c>false</c> � ��� ������ ����� (F10).
    /// </param>
    /// <param name="activationSource">�������� ��������� ���������� ������.</param>
    /// <param name="breakpointCommandInfo">
    /// �������� ������� ��� ��������� <see cref="StepModeActivationSource.Breakpoint"/>.
    /// </param>
    private static void EnableStepModeCore(
      bool isStepInto,
      StepModeActivationSource activationSource,
      IExecutionCommandInfo? breakpointCommandInfo = null)
    {
      _stepMode = true;
      IsStepInto = isStepInto;
      StepOverUntilNextControlCommand = !isStepInto;
      _stepBypassRequested = false;
      _activationSource = activationSource;
      _breakpointCommandInfo = activationSource == StepModeActivationSource.Breakpoint
        ? breakpointCommandInfo
        : null;

      ExecutionEventAdapter.RaiseStepByStepModeChanged(true);
    }

    /// <summary>
    /// ��������� ��������� ����� � ������������� ���� ������
    /// ��� ����������� ���������� ��� ���������.
    /// </summary>
    public static void DisableStepMode()
    {
      _stepMode = false;
      _stepBypassRequested = true;
      StepOverUntilNextControlCommand = false;
      _activationSource = StepModeActivationSource.Unknown;
      _breakpointCommandInfo = null;
      ExecutionEventAdapter.RaiseStepByStepModeChanged(false);
    }

    /// <summary>
    /// ����������� �����������, ����������� �������������
    /// ��������� ���������� ������ ��� ������ ��������� � ������.
    /// </summary>
    static StepControlManager()
    {
      InitializeAsync().ConfigureAwait(true);
    }
  }
}
