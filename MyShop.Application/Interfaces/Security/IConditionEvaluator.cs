using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Security
{
    public sealed class PermissionEvaluationContext
    {
        public int UserId { get; set; }
        public string PermissionCode { get; set; }
        public int? SiteId { get; set; }
        public string ConditionJson { get; set; }
    }

    /// <summary>قلاب ABAC — اگر ثبت نشده باشد، Grantهای شرطی Deny می‌شوند (فاز ۱۰).</summary>
    public interface IConditionEvaluator
    {
        bool IsRegistered { get; }
        Task<bool> EvaluateAsync(PermissionEvaluationContext context, CancellationToken ct = default);
    }
}
