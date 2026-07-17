using System.Windows;
using System.Windows.Controls;

namespace HsRotaryClub.App.Controls;

/// <summary>
/// 共用速查列 — 上方 🔍 + TextBox + Filter DP。
/// Filter 走 DependencyProperty 給上層 ViewModel 雙向綁。
/// </summary>
public partial class QuickFilterBar : UserControl
{
    public static readonly DependencyProperty FilterProperty =
        DependencyProperty.Register(
            nameof(Filter),
            typeof(string),
            typeof(QuickFilterBar),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string Filter
    {
        get => (string)GetValue(FilterProperty);
        set => SetValue(FilterProperty, value);
    }

    public QuickFilterBar()
    {
        InitializeComponent();
    }
}
