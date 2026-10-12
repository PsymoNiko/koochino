using System;

namespace MyShop.Core.Academy
{
    /// <summary>
    /// ظرفیت هر تاریخ Daily روی Schedule است، نه ظرفیت ثبت‌نام دوره.
    /// NULL یعنی نامحدود. وقتی Approved پر است درخواست جدید Waitlisted می‌شود، نه Pending بی‌نهایت.
    /// </summary>
    public static class AcademyDailyCapacity
    {
        public static bool IsLimited(int? capacity) => capacity is > 0;

        public static int? Effective(int? scheduleCapacity, int? exceptionCapacity) =>
            exceptionCapacity is > 0 ? exceptionCapacity : scheduleCapacity;

        public static bool IsFull(int? capacity, int approvedCount) =>
            IsLimited(capacity) && approvedCount >= capacity.Value;
    }
}
