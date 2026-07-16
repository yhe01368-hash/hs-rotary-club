using CommunityToolkit.Mvvm.ComponentModel;

namespace HsRotaryClub.App.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    public string Title { get; } = "豐原西南扶輪社社務行政系統";
    public string Subtitle { get; } = "v0.1 (.NET 8 重寫版)";
    public string Note { get; } = "本系統取代舊版 C:\\Program Files (x86)\\Project1 (VB6/JET)。";

    // 7 主模組口號 (v0.1 只做前 3 個, 後 4 個 v0.2 +)
    public string[] Modules { get; } =
    {
        "01. 會員資料管理作業 ✅ v0.1",
        "02. 會內收款管理系統 ✅ v0.1",
        "03. 友社捐款收款作業 ✅ v0.1",
        "04. 社友例會出席管理 ⏳ v0.2",
        "05. 其它收支管理系統 ⏳ v0.2",
        "06. 會計資料管理系統 ⏳ v0.2",
        "07. 各種信件作業系統 ⏳ v0.2",
    };
}
