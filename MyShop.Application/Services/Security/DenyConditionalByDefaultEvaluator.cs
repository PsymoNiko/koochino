using MyShop.Application.Interfaces.Security;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Security
{
    /// <summary>پیش‌فرض فاز ۱۰: بدون Evaluator واقعی، شرط‌ها رد می‌شوند.</summary>
    public sealed class DenyConditionalByDefaultEvaluator : IConditionEvaluator
    {
        public bool IsRegistered => false;

        public Task<bool> EvaluateAsync(PermissionEvaluationContext context, CancellationToken ct = default)
            => Task.FromResult(false);
    }
}
