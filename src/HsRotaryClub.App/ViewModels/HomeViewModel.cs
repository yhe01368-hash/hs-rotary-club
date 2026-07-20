using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HsRotaryClub.App.Infrastructure;

namespace HsRotaryClub.App.ViewModels;

/// <summary>
/// HomePage 主要顯示內容:
///   - Title: 當前社團名稱
///   - Subtitle: 版本資訊
///   - Note: 取代舊系統說明
///   - Modules: 7 大主模組完成狀態
/// v0.28: 改為動態 CurrentClubName (支援不同扶輪社).
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "HsRotaryClub 社務行政系統";

    [ObservableProperty]
    private string _subtitle = "v0.28 (.NET 8 重寫版)";

    public string Note { get; } = "取代舊版「C:\\Program Files (x86)\\Project1 (VB6/JET)」管理系統";

    public string[] Modules { get; } =
    {
        "01. 會員資料管理作業    ✓v0.2+",
        "02. 會內收款管理系統    ✓v0.4+",
        "03. 友社捐款收款作業    ✓v0.5+",
        "04. 例會出席管理        ✓v0.10+",
        "05. 其它收支管理系統    ✓v0.12+",
        "06. 會計月報表管理系統  ✓v0.12+",
        "07. 各種信件作業系統    ✓v0.12+",
    };

    public HomeViewModel(CurrentClubContext currentClubCtx)
    {
        UpdateTitle(currentClubCtx.CurrentClubName);
        currentClubCtx.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CurrentClubContext.CurrentClubName))
                UpdateTitle(currentClubCtx.CurrentClubName);
        };
    }

    private void UpdateTitle(string clubName)
    {
        Title = string.IsNullOrWhiteSpace(clubName)
            ? "HsRotaryClub 社務行政系統"
            : $"{clubName} 社務行政系統";
    }
}
