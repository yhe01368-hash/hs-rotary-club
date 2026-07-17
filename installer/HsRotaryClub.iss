; ============================================================
; 鞊?镼踹??嗉憚蝷曄冗???輻頂蝯???Inno Setup installer script
; v0.6.0 ??framework-dependent, win-x64
; Output: HsRotaryClubSetup-v0.6.exe
; ============================================================
#define MyAppName "鞊?镼踹??嗉憚蝷曄冗???輻頂蝯?
#define MyAppShortName "HsRotaryClub"
#define MyAppVersion "0.6.0"
#define MyAppPublisher "Chia Chang"
#define MyAppUrl "https://github.com/yhe01368-hash/hs-rotary-club"
#define MyAppExeName "HsRotaryClub.App.exe"
#define PublishDir "..\src\HsRotaryClub.App\bin\Release\net8.0-windows\win-x64\publish"
#define OutputBaseDir "bin"

[Setup]
AppId={{B6F3E9A4-1C2D-4F88-9A11-7E2C9B5A0606}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppUrl}
DefaultDirName={autopf}\{#MyAppShortName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#OutputBaseDir}
OutputBaseFilename=HsRotaryClubSetup-v{#MyAppVersion}
Compression=lzma2/ultra
SolidCompression=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
Uninstallable=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
BeveledLabel=鞊?镼踹??嗉憚蝷?
[Files]
[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\HsRotaryClub"

[Files]
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion