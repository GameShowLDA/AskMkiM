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

Filename: "{app}\Bin\MainWindowProgram.exe"; Flags: nowait postinstall skipifsilent


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


function InstallDriver(): Boolean;
var
  ResultCode: Integer;
begin

  Result := True;
  
  if not FileExists(ExpandConstant('{tmp}\Drivers\SETUP.EXE')) then
begin

    MsgBox(
      'Не найден установщик драйвера GPT-9000.',
      mbError,
      MB_OK);

    Result := False;
    Exit;

end;
  if MsgBox(
      'Для работы программы требуется драйвер GPT-9000.'#13#10#13#10 +
      'Установить драйвер сейчас?',
      mbConfirmation,
      MB_YESNO) = IDNO then
  begin
    Exit;
  end;

  if not Exec(
    ExpandConstant('{tmp}\Drivers\SETUP.EXE'),
    '',
    '',
    SW_SHOW,
    ewWaitUntilTerminated,
    ResultCode
  )
  then
  begin

    MsgBox(
      'Не удалось запустить установку драйвера GPT-9000.',
      mbError,
      MB_OK);

    Result := False;
    Exit;
  end;

  case ResultCode of

    0:
      begin
        MsgBox(
          'Драйвер GPT-9000 успешно установлен.',
          mbInformation,
          MB_OK);

        Result := True;
      end;

    3010:
      begin
        MsgBox(
          'Драйвер установлен.'#13#10 +
          'Для завершения установки потребуется перезагрузка компьютера.',
          mbInformation,
          MB_OK);

        Result := True;
      end;

  else

    begin

      MsgBox(
        'Ошибка установки драйвера GPT-9000.'#13#10 +
        'Код ошибки: ' + IntToStr(ResultCode),
        mbError,
        MB_OK);

      Result := False;
    end;

  end;
end;


procedure CurStepChanged(CurStep: TSetupStep);
begin

  if CurStep = ssPostInstall then
  begin

    if not InstallDriver() then
      Abort;

  end;

end;