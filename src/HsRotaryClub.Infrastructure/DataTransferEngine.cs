using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using HsRotaryClub.Domain;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.Infrastructure;

/// <summary>
/// v0.8 — 跨機資料 dump (Export → JSON) 跟 restore (Import ← JSON)。
/// 場景: A 社的「示範扶輪社」資料 dump 給 B 機器用,merge 到 B 機器的本機 db。
///
/// 設計:
/// - 純 managed,沒有 WPF / Win32 依賴,smoke test 可直接測
/// - 用 System.Text.Json + camelCase + JavaScriptEncoder.Unsafe (中文 raw,避免 \uXXXX escape)
/// - Club / Member / ClubCollection / MonthlyReceivableSpec / FriendlyClub / ClubDonation 全 dump
/// - Import 用 dedupe 鍵 upsert:已存在跳過(skip-existing mode)或覆蓋
/// - Transactional rollback: 任一筆失敗整個 import rollback
/// </summary>
public static class DataTransferEngine
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // 不 escape Unicode — 直接寫中文 raw,避免 \uXXXX
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
    };

    public static string ExportToJson(RotaryDbContext db, int? clubId = null, bool includeAllClubs = false)
    {
        var q = db.Clubs.AsNoTracking();
        if (!includeAllClubs)
        {
            q = q.Where(c => c.IsActive);
        }
        if (clubId.HasValue)
        {
            q = q.Where(c => c.Id == clubId.Value);
        }
        var clubs = q.ToList();

        var clubIds = clubs.Select(c => c.Id).ToHashSet();

        var members = db.Members.AsNoTracking().Where(m => clubIds.Contains(m.ClubId)).ToList();
        var collections = db.ClubCollections.AsNoTracking().Where(c => clubIds.Contains(c.ClubId)).ToList();
        var receivables = db.MonthlyReceivableSpecs.AsNoTracking().Where(r => clubIds.Contains(r.ClubId)).ToList();
        var friendlies = db.FriendlyClubs.AsNoTracking().Where(f => clubIds.Contains(f.ClubId)).ToList();

        var friendlyIds = friendlies.Select(f => f.Id).ToHashSet();
        var donations = db.ClubDonations.AsNoTracking().Where(d => friendlyIds.Contains(d.FriendlyClubId)).ToList();

        var payload = new ExportPayload
        {
            SchemaVersion = 1,
            ExportedAt = DateTime.UtcNow,
            Clubs = clubs,
            Members = members,
            Collections = collections,
            Receivables = receivables,
            FriendlyClubs = friendlies,
            Donations = donations,
        };
        return JsonSerializer.Serialize(payload, JsonOpts);
    }

    public static ImportResult ImportFromJson(RotaryDbContext db, string json, bool skipExisting = true)
    {
        var payload = JsonSerializer.Deserialize<ExportPayload>(json, JsonOpts);
        if (payload is null) return new ImportResult(0, 0, 0, 0, 0, "JSON 解析失敗");

        int cIns = 0, cSkip = 0, mIns = 0, mSkip = 0, colIns = 0, colSkip = 0,
            fIns = 0, fSkip = 0, dIns = 0, dSkip = 0, rvIns = 0, rvSkip = 0;

        using var tx = db.Database.BeginTransaction();
        try
        {
            // Clubs — dedupe by Id
            foreach (var c in payload.Clubs)
            {
                var existing = db.Clubs.FirstOrDefault(x => x.Id == c.Id);
                if (existing is not null)
                {
                    if (skipExisting) { cSkip++; continue; }
                    db.Clubs.Remove(existing);
                }
                db.Clubs.Add(c);
                cIns++;
            }
            db.SaveChanges();

            // Members — by (ClubId, Code)
            foreach (var m in payload.Members)
            {
                var existing = db.Members.FirstOrDefault(x => x.ClubId == m.ClubId && x.Code == m.Code);
                if (existing is not null)
                {
                    if (skipExisting) { mSkip++; continue; }
                    db.Members.Remove(existing);
                }
                db.Members.Add(m);
                mIns++;
            }
            db.SaveChanges();

            // Collections — by (ClubId, Year, Month, MemberCode, CollectionDate)
            foreach (var c in payload.Collections)
            {
                var existing = db.ClubCollections.FirstOrDefault(x =>
                    x.ClubId == c.ClubId && x.Year == c.Year && x.Month == c.Month &&
                    x.MemberCode == c.MemberCode && x.CollectionDate == c.CollectionDate);
                if (existing is not null)
                {
                    if (skipExisting) { colSkip++; continue; }
                    db.ClubCollections.Remove(existing);
                }
                db.ClubCollections.Add(c);
                colIns++;
            }
            db.SaveChanges();

            // Receivables — by (ClubId, Year, Month, MemberCode, Item)
            foreach (var r in payload.Receivables)
            {
                var existing = db.MonthlyReceivableSpecs.FirstOrDefault(x =>
                    x.ClubId == r.ClubId && x.Year == r.Year && x.Month == r.Month &&
                    x.MemberCode == r.MemberCode && x.Item == r.Item);
                if (existing is not null)
                {
                    if (skipExisting) { rvSkip++; continue; }
                    db.MonthlyReceivableSpecs.Remove(existing);
                }
                db.MonthlyReceivableSpecs.Add(r);
                rvIns++;
            }
            db.SaveChanges();

            // FriendlyClubs — by (ClubId, ClubCode)
            foreach (var f in payload.FriendlyClubs)
            {
                var existing = db.FriendlyClubs.FirstOrDefault(x =>
                    x.ClubId == f.ClubId && x.ClubCode == f.ClubCode);
                if (existing is not null)
                {
                    if (skipExisting) { fSkip++; continue; }
                    db.FriendlyClubs.Remove(existing);
                }
                db.FriendlyClubs.Add(f);
                fIns++;
            }
            db.SaveChanges();

            // Donations — 用 FriendlyClubName 配對 parent fc
            foreach (var d in payload.Donations)
            {
                var existingFriendly = db.FriendlyClubs.FirstOrDefault(x =>
                    x.Id == d.FriendlyClubId || x.ClubName == d.FriendlyClubName);
                if (existingFriendly is null) { dSkip++; continue; }
                var existing = db.ClubDonations.FirstOrDefault(x =>
                    x.FriendlyClubId == existingFriendly.Id && x.TxDate == d.TxDate &&
                    x.Direction == d.Direction && x.Amount == d.Amount && x.Purpose == d.Purpose);
                if (existing is not null)
                {
                    if (skipExisting) { dSkip++; continue; }
                    db.ClubDonations.Remove(existing);
                }
                d.FriendlyClubId = existingFriendly.Id;
                d.FriendlyClubName = existingFriendly.ClubName;
                db.ClubDonations.Add(d);
                dIns++;
            }
            db.SaveChanges();

            tx.Commit();
            return new ImportResult(
                cIns, mIns, colIns, fIns, dIns,
                $"成功: Clubs +{cIns}, Members +{mIns}, Collections +{colIns}, Friendly +{fIns}, Donations +{dIns}, Receivables +{rvIns}; skip {cSkip + mSkip + colSkip + fSkip + dSkip + rvSkip}");
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}

public record ImportResult(
    int ClubsInserted, int MembersInserted, int CollectionsInserted,
    int FriendlyClubsInserted, int DonationsInserted, string Summary);

public class ExportPayload
{
    public int SchemaVersion { get; set; }
    public DateTime ExportedAt { get; set; }
    public List<Club> Clubs { get; set; } = new();
    public List<Member> Members { get; set; } = new();
    public List<ClubCollection> Collections { get; set; } = new();
    public List<MonthlyReceivableSpec> Receivables { get; set; } = new();
    public List<FriendlyClub> FriendlyClubs { get; set; } = new();
    public List<ClubDonation> Donations { get; set; } = new();
}
