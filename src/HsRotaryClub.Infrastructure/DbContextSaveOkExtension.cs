using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.Infrastructure;

/// <summary>
/// DbContext.SaveChanges 包 try/catch + 給詳細錯誤字串。
/// 三個 VM 都用 (避免 InvalidOperationException 冒到 WPF 上)。
/// </summary>
public static class DbContextSaveOkExtension
{
    public static bool TrySaveChanges(this DbContext db, out string error)
    {
        try
        {
            db.SaveChanges();
            error = string.Empty;
            return true;
        }
        catch (DbUpdateException ex)
        {
            error = string.IsNullOrEmpty(ex.InnerException?.Message)
                ? ex.Message
                : $"{ex.Message} :: {ex.InnerException.Message}";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
