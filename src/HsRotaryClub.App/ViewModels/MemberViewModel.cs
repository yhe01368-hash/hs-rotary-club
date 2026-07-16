using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.ViewModels;

public partial class MemberViewModel : ObservableObject
{
    private readonly RotaryDbContext _db;

    public ObservableCollection<Member> Members { get; } = new();

    [ObservableProperty]
    private Member? _selected;

    [ObservableProperty]
    private string _filter = string.Empty;

    public MemberViewModel(RotaryDbContext db)
    {
        _db = db;
        Reload();
    }

    partial void OnFilterChanged(string value) => Reload();

    [RelayCommand]
    private void Reload()
    {
        Members.Clear();
        var q = _db.Members.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(Filter))
        {
            q = q.Where(m => m.Name.Contains(Filter) || m.EnglishName!.Contains(Filter));
        }
        foreach (var m in q.OrderBy(m => m.Code).ToList())
        {
            Members.Add(m);
        }
        Selected ??= Members.FirstOrDefault();
    }

    [RelayCommand]
    private void Add()
    {
        var m = new Member { Code = NextCode(), Name = "新社員" };
        _db.Members.Add(m);
        _db.SaveChanges();
        Reload();
        Selected = Members.FirstOrDefault(x => x.Id == m.Id);
    }

    [RelayCommand]
    private void Delete()
    {
        if (Selected is null) return;
        Selected.IsDeleted = true;
        _db.SaveChanges();
        Reload();
    }

    [RelayCommand]
    private void Save()
    {
        if (Selected is null) return;
        _db.SaveChanges();
    }

    private int NextCode()
    {
        var max = _db.Members.Max(m => (int?)m.Code) ?? 0;
        return max + 1;
    }
}
