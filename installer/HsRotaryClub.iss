; ============================================================
; 豐原西南扶輪社社務行政系統 — Inno Setup installer script
; v0.6.2 — ASCII AppName + 中文自訂 [Messages]
; Output: HsRotaryClubSetup-v0.6.0.exe
; ============================================================
#define MyAppName "HsRotaryClub"
#define MyAppShortName "HsRotaryClub"
#define MyAppVersion "0.6.0"
#define MyAppPublisher "Chia Chang"
#define MyAppUrl "https://github.com/yhe01368-hash/hs-rotary-club"
#define MyAppExeName "HsRotaryClub.App.exe"
#define PublishDir "..\src\HsRotaryClub.App/bin/Release/net8.0-windows/win-x64/publish"
#define OutputBaseDir "bin"

[Setup]
AppId={{B6F3E9A4-1C2D-4F88-9A11-7E2C9B5A0606}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppUrl}
DefaultDirName={autopf}\{#MyAppShortName}
DefaultGroupName={#MyAppShortName}
DisableProgramGroupPage=yes
OutputDir={#OutputBaseDir}
OutputBaseFilename=HsRotaryClubSetup-v{#MyAppVersion}
Compression=lzma2/ultra
SolidCompression=yes
; 絕對路徑給 Inno compiler (PS relative 路徑偶爾踩 file resolver)
SetupIconFile=C:\Users\Admin\hs-rotary-club\installer\HsRotaryClub.ico
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
Uninstallable=yes
UninstallDisplayIcon={app}\HsRotaryClub.App.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[LangOptions]
LanguageID=1028
LanguageCodePage=950

[Messages]
; 中文自訂訊息 (Default.isl 沒有繁中 .isl)
; 注意:這些是 Default.isl 內建的 CustomMessage key 名稱
WelcomeLabel2=本安裝程式會安裝 [name/ver] 到您的電腦。%n%n請按「下一步」繼續。
FinishedLabel=安裝程式已經將 [name] 安裝到您的電腦。應用程式可以透過已建立的捷徑啟動。
FinishedHeadingLabel=完成安裝 [name]
ClickFinish=按「完成」結束安裝。
LaunchLabel=啟動 [name]
SetupWindowTitle=安裝 - [name]
UninstallAppTitle=解除安裝 - [name]

[Files]
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\e_sqlite3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#PublishDir}\*.json"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppShortName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall HsRotaryClub"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "啟動豐原西南扶輪社社務行政系統"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Data"
