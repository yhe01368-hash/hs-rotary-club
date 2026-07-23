using System;
using System.Globalization;
using System.Windows.Data;

namespace HsRotaryClub.App.Infrastructure;

/// <summary>
/// v0.58: WPF DatePicker.SelectedDate (DateTime?) ↔ EF DateOnly? 雙向 Converter.
/// DatePicker 預期 DateTime,但 domain entity 是 DateOnly.
/// Convert: DateOnly? → DateTime?
/// ConvertBack: DateTime? → DateOnly?
/// </summary>
[ValueConversion(typeof(DateOnly?), typeof(DateTime?))]
public class DateOnlyToDateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateOnly d) return d.ToDateTime(TimeOnly.MinValue);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dt) return DateOnly.FromDateTime(dt);
        return null;
    }
}
