; Inno Setup script. Driven by build-windows-installer.ps1, which passes every /D define below.
#ifndef AppIsCli
  #define AppIsCli 0
#endif
#ifndef AppName
  #error AppName must be defined
#endif

[Setup]
AppId={{{#AppGuid}}
AppName={#AppTitle}
AppVersion={#AppVersion}
AppPublisher=Majorsilence
AppPublisherURL=https://github.com/majorsilence/Reporting
DefaultDirName={autopf}\Majorsilence Reporting\{#AppName}
DefaultGroupName=Majorsilence Reporting
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AppName}.exe
OutputDir={#OutDir}
OutputBaseFilename={#AppName}-{#AppVersion}-{#Rid}-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequiredOverridesAllowed=dialog
#if Rid == "win-arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

#if AppIsCli == 1
; Command line tool: no shortcuts, offer to put it on PATH instead.
[Tasks]
Name: "addtopath"; Description: "Add RdlCmd to the system PATH"

[Registry]
Root: HKA; Subkey: "{code:EnvKey}"; ValueType: expandsz; ValueName: "Path"; \
  ValueData: "{olddata};{app}"; Tasks: addtopath; Check: NeedsPath(ExpandConstant('{app}')); Flags: preservestringtype

[Setup]
ChangesEnvironment=yes

[Code]
function EnvKey(Param: string): string;
begin
  if IsAdminInstallMode then
    Result := 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
  else
    Result := 'Environment';
end;

function NeedsPath(Dir: string): Boolean;
var
  Paths, Key: string;
  Root: Integer;
begin
  Key := EnvKey('');
  if IsAdminInstallMode then Root := HKEY_LOCAL_MACHINE else Root := HKEY_CURRENT_USER;
  if not RegQueryStringValue(Root, Key, 'Path', Paths) then Paths := '';
  Result := Pos(';' + Uppercase(Dir) + ';', ';' + Uppercase(Paths) + ';') = 0;
end;
#else
[Icons]
Name: "{group}\{#AppTitle}"; Filename: "{app}\{#AppName}.exe"
Name: "{autodesktop}\{#AppTitle}"; Filename: "{app}\{#AppName}.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Run]
Filename: "{app}\{#AppName}.exe"; Description: "Launch {#AppTitle}"; Flags: nowait postinstall skipifsilent
#endif
