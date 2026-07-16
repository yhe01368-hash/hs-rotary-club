using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.ViewModels;

public partial class FriendlyClubViewModel : ObservableObject
{
    private readonly RotaryDbContext _db;

    public ObservableCollection<FriendlyClub> Clubs { get; } = new();

    [ObservableProperty]
    private FriendlyClub? _selected;

    public FriendlyClubViewModel(RotaryDbContext db)
    {
        _db = db;
        Reload();
    }

    [RelayCommand]
    private void Reload()
    {
        Clubs.Clear();
        foreach (var c in _db.FriendlyClubs.AsNoTracking().OrderBy(c => c.ClubCode).ToList())
            Clubs.Add(c);
        Selected ??= Clubs.FirstOrDefault();
    }

    [RelayCommand]
    private void Add()
    {
        var c = new FriendlyClub { ClubCode = "NEWC", ClubName = "新社團" };
        _db.FriendlyClubs.Add(c);
        _db.SaveChanges();
        Reload();
        Selected = Clubs.FirstOrDefault(x => x.Id == c.Id);
    }

    [RelayCommand]
    private void Delete()
    {
        if (Selected is null) return;
        _db.FriendlyClubs.Remove(Selected);
        _db.SaveChanges();
        Reload();
    }

    [RelayCommand]
    private void Save()
    {
        _db.SaveChanges();
    }
}
