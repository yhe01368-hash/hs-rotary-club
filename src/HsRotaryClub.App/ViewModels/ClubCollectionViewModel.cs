using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.ViewModels;

public partial class ClubCollectionViewModel : ObservableObject
{
    private readonly RotaryDbContext _db;

    public ObservableCollection<ClubCollection> Collections { get; } = new();

    [ObservableProperty]
    private int _year = DateTime.Today.Year;

    [ObservableProperty]
    private int _month = DateTime.Today.Month;

    public ClubCollectionViewModel(RotaryDbContext db)
    {
        _db = db;
        Reload();
    }

    partial void OnYearChanged(int value) => Reload();
    partial void OnMonthChanged(int value) => Reload();

    [RelayCommand]
    private void Reload()
    {
        Collections.Clear();
        var rows = _db.ClubCollections
            .AsNoTracking()
            .Where(c => c.Year == Year && c.Month == Month)
            .OrderBy(c => c.CollectionDate)
            .ToList();
        foreach (var r in rows) Collections.Add(r);
    }

    [RelayCommand]
    private void Add()
    {
        var c = new ClubCollection
        {
            Year = Year, Month = Month,
            CollectionDate = new DateOnly(Year, Month, 1),
            Category = "會費",
            MemberCode = 0,
        };
        _db.ClubCollections.Add(c);
        _db.SaveChanges();
        Reload();
    }

    [RelayCommand]
    private void Delete()
    {
        var sel = Collections.FirstOrDefault();
        if (sel is null) return;
        _db.ClubCollections.Remove(sel);
        _db.SaveChanges();
        Reload();
    }
}
