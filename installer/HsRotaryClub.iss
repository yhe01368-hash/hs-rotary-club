; ============================================================
; ??????????????????????????????Inno Setup installer script
; v0.6.0 ??framework-dependent, win-x64
; Output: HsRotaryClubSetup-v0.6.exe
; ============================================================
#define MyAppName "????????????????????????????
#define MyAppShortName "HsRotaryClub"
#define MyAppVersion "0.6.0"
#define MyAppPublisher "Chia Chang"
#define MyAppUrl "https://github.com/yhe01368-hash/hs-rotary-club"
#define MyAppExeName "HsRotaryClub.App.exe"
#define PublishDir "..\src\HsRotaryClub.App/bin/Release/net8.0-windows/win-x64/publish"
#define OutputBaseDir "bin"

[Setup]
AppId={{B6F3E9A4-1C2D-4F88-9A11-7E2C9B5A0606}}
AppName=HsRotaryClub {#MyAppVersion}
AppVersion={#MyAppVersion}
AppVerName=HsRotaryClub {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppUrl}
DefaultDirName={autopf}\{#MyAppShortName}
DefaultGroupName=HsRotaryClub
DisableProgramGroupPage=yes
OutputDir={#OutputBaseDir}
OutputBaseFilename=HsRotaryClubSetup-v{#MyAppVersion}
Compression=lzma2/ultra
SolidCompression=yes
SetupIconFile=HsRotaryClub.ico
Uninstallable=yes
UninstallDisplayIcon={app}\HsRotaryClub.App.exe

[LangOptions]
LanguageID=1028
LanguageCodePage=950



[Messages]
BeveledLabel=HsRotaryClub v0.6
[Files]
[Icons]
Name: "{group}\HsRotaryClub"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall HsRotaryClub"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
[Files]
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\e_sqlite3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#PublishDir}\*.json"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist