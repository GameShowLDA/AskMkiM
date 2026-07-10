[Setup]
AppName=АСК-МКИ-М
AppVersion=1.0

DefaultDirName=D:\AskMkiM
DefaultGroupName=АСК-МКИ-М

OutputDir=Output
OutputBaseFilename=Setup_ASKMKIM

Compression=lzma
SolidCompression=yes

PrivilegesRequired=admin


[Files]

Source: "Drivers\*"; DestDir: "{tmp}\Drivers"; Flags: recursesubdirs createallsubdirs

Source: "D:\NewGit\AskMkiM\MainWindow\Bin\MainWindowProgram\win-x64\publish\*"; DestDir: "{app}\Bin"; Flags: ignoreversion recursesubdirs createallsubdirs


[Icons]

Name: "{group}\АСКМKIM"; Filename: "{app}\Bin\MainWindowProgram.exe"

Name: "{commondesktop}\АСКМKIM"; Filename: "{app}\Bin\MainWindowProgram.exe"


[Run]

Filename: "{app}\Bin\MainWindowProgram.exe"; Description: "Запустить АСК-МКИ-М"; Flags: nowait postinstall skipifsilent


[Code]

function InitializeSetup(): Boolean;
begin
  Result := True;

  if not DirExists('D:\') then
  begin
    MsgBox(
      'Не найден диск D.'#13#10 +
      'Установка невозможна.',
      mbError,
      MB_OK);

    Result := False;
  end;
end;


function DriverFile(): String;
begin

  if IsWin64 then
    Result := ExpandConstant('{tmp}\Drivers\CP210xVCPInstaller_x64.exe')
  else
    Result := ExpandConstant('{tmp}\Drivers\CP210xVCPInstaller_x86.exe');

end;


function InstallDriver(): Boolean;
var
  ResultCode: Integer;
begin

  Result := True;

  if not FileExists(DriverFile()) then
  begin

    MsgBox(
      'Не найден установщик драйвера CP210x.',
      mbError,
      MB_OK);

    Result := False;
    Exit;

  end;


  if MsgBox(
      'Для работы программы требуется драйвер USB CP210x.'#13#10#13#10 +
      'Установить драйвер сейчас?',
      mbConfirmation,
      MB_YESNO) = IDNO then
  begin

    Exit;

  end;


  if not Exec(

      DriverFile(),

      '',

      '',

      SW_SHOW,

      ewWaitUntilTerminated,

      ResultCode)

  then
  begin

    MsgBox(
      'Не удалось запустить установку драйвера CP210x.',
      mbError,
      MB_OK);

    Result := False;
    Exit;

  end;


  MsgBox(
    'Установка драйвера CP210x завершена.',
    mbInformation,
    MB_OK);

  Result := True;


end;


procedure CurStepChanged(CurStep: TSetupStep);
begin

  if CurStep = ssPostInstall then
  begin

    if not InstallDriver() then
      Abort;

  end;

end;