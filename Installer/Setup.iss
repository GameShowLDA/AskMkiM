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
            'Не найден диск D.'#13#10+
            'Установка невозможна.',
            mbError,
            MB_OK);

        Result := False;
    end;
end;