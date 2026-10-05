[Setup]

AppName=АСК-МКИ-М
AppVersion=1.0

DefaultDirName={code:GetDefaultDirName}
DefaultGroupName=АСК-МКИ-М

OutputDir=Output
OutputBaseFilename=Setup_ASKMKIM

Compression=lzma
SolidCompression=yes

PrivilegesRequired=admin

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

DisableFinishedPage=no


[Files]

; ============================================================
; Драйвер CP210x
; ============================================================

Source: "Drivers\*"; DestDir: "{tmp}\Drivers"; Flags: recursesubdirs createallsubdirs


; ============================================================
; WebView2 Runtime
; ============================================================

Source: "WebView2\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"; DestDir: "{tmp}\WebView2"; Flags: ignoreversion


; ============================================================
; Основное приложение
; ============================================================

Source: "D:\AskMkiM\Bin\*"; DestDir: "{app}\Bin"; Flags: ignoreversion recursesubdirs createallsubdirs


[Icons]

Name: "{group}\АСК-МКИ-М"; Filename: "{app}\Bin\AskMkiM.exe"

Name: "{commondesktop}\АСК-МКИ-М"; Filename: "{app}\Bin\AskMkiM.exe"


[Run]

Filename: "{app}\Bin\AskMkiM.exe"; Description: "Запустить АСК-МКИ-М"; Flags: nowait postinstall skipifsilent


[Code]


{ ============================================================ }
{ Выбор диска установки                                       }
{ ============================================================ }

function GetDefaultDirName(Param: String): String;
begin

  if DirExists('D:\') then
    Result := 'D:\AskMkiM'
  else
    Result := 'C:\AskMkiM';

end;


{ ============================================================ }
{ CP210x                                                      }
{ ============================================================ }

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

end;


{ ============================================================ }
{ WebView2                                                    }
{ ============================================================ }

function WebView2Installer(): String;
begin

  Result :=
    ExpandConstant(
      '{tmp}\WebView2\MicrosoftEdgeWebView2RuntimeInstallerX64.exe');

end;


function InstallWebView2(): Boolean;
var
  ResultCode: Integer;
begin

  Result := True;

  if not FileExists(WebView2Installer()) then
  begin

    MsgBox(
      'Не найден установщик Microsoft Edge WebView2 Runtime.',
      mbError,
      MB_OK);

    Result := False;
    Exit;

  end;


  if not Exec(
      WebView2Installer(),
      '/silent /install',
      '',
      SW_HIDE,
      ewWaitUntilTerminated,
      ResultCode)
  then
  begin

    MsgBox(
      'Не удалось запустить установку Microsoft Edge WebView2 Runtime.',
      mbError,
      MB_OK);

    Result := False;
    Exit;

  end;

end;


{ ============================================================ }
{ После установки                                              }
{ ============================================================ }

procedure CurPageChanged(CurPageID: Integer);
begin

  if CurPageID = wpFinished then
  begin

    WizardForm.FinishedHeadingLabel.Caption :=
      'АСК-МКИ-М установлена';

    WizardForm.FinishedLabel.Caption :=
      'Установка успешно завершена.'#13#10#13#10 +
      'Программа АСК-МКИ-М готова к работе.';

    WizardForm.FinishedHeadingLabel.Font.Size := 16;
    WizardForm.FinishedHeadingLabel.Font.Style := [fsBold];

    WizardForm.FinishedLabel.Font.Size := 10;

  end;

end;