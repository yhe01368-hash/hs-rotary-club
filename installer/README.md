# hs-rotary-club installer (Inno Setup 6.x)

**輸出**: `bin/HsRotaryClubSetup-v0.6.exe` (單檔 self-extracting)

## 環境需求

1. **Inno Setup 6.x** 已裝
   ```powershell
   winget install --id=jrsoftware.InnoSetup
   ```
   預設路徑:`C:\Program Files (x86)\Inno Setup 6\ISCC.exe`

2. **.NET 8 SDK** 已裝 (build 階段用)

## Build (PowerShell 5.x cp950 環境)

> ⚠️ **逐行不可合併**;路徑不可裸打;前綴不能脫落。

```powershell
cd C:\Users\Admin\hs-rotary-club\installer
```

```powershell
powershell -ExecutionPolicy Bypass -File .\build-installer.ps1
```

> 如果 ISCC.exe 找不到,改用:
> ```powershell
> & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" HsRotaryClub.iss
> ```

## 輸出

```
hs-rotary-club\installer\bin\
└── HsRotaryClubSetup-v0.6.exe     ← 單檔安裝檔(雙擊 next→next→install)
```

裝完會:
- 桌面 + 開始功能表 加捷徑「豐原西南扶輪社社務行政系統」
- `C:\Program Files\HsRotaryClub\HsRotaryClub.App.exe` (主程式)
- 勾選才啟動主程式(不勾不啟動)
- 卸載透過 控制台 > 新增/移除程式

## 資料位置

| 物件 | 路徑 |
|---|---|
| SQLite db (含 seed) | `%LocalAppData%\HsRotaryClub\rotary.db` |
| 卸載時清掉的舊 data | `%AppData%\HsRotaryClub\` |

> v0.6 framework-dependent — 需機器安裝 .NET 8 Desktop Runtime (`winget install Microsoft.DotNet.DesktopRuntime.8`)
