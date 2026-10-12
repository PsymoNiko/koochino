using System;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>برای به‌روزرسانی نمایش امتیاز کاربر در Layout بعد از تغییر (مثلاً دریافت امتیاز از ماموریت).</summary>
    public interface IUserPointsRefreshService
    {
        void RegisterForRefresh(Func<Task> handler);
        Task NotifyPointsChangedAsync();
    }
}
