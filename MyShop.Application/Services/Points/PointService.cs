using MyShop.Application.Interfaces;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services
{
    public class PointService : IPointService
    {
        private readonly IPointRepository _repository;
        private readonly ICurrentTenant _tenant;

        public PointService(IPointRepository repository, ICurrentTenant tenant = null)
        {
            _repository = repository;
            _tenant = tenant;
        }

        public async Task<List<PointMission>> GetMissionsAsync(bool activeOnly = false, int? siteId = null)
        {
            return await _repository.GetMissionsAsync(activeOnly, siteId);
        }

        public async Task<PointMission> GetMissionByIdAsync(int id)
        {
            return await _repository.GetMissionByIdAsync(id);
        }

        public async Task<(bool Success, string Message, int MissionId)> CreateMissionAsync(PointMission mission, int adminUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mission.Title))
                    return (false, "عنوان ماموریت الزامی است", 0);

                if (mission.Points <= 0)
                    return (false, "امتیاز باید بیشتر از صفر باشد", 0);
                if (!mission.SiteId.HasValue || mission.SiteId.Value <= 0)
                    return (false, "انتخاب سایت برای ماموریت الزامی است", 0);

                mission.CreatedAt = DateTime.Now;
                mission.CreatedByUserId = adminUserId;

                var missionId = await _repository.CreateMissionAsync(mission);
                return (true, "ماموریت با موفقیت ایجاد شد", missionId);
            }
            catch (Exception ex)
            {
                return (false, $"خطا در ایجاد ماموریت: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string Message)> UpdateMissionAsync(PointMission mission)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mission.Title))
                    return (false, "عنوان ماموریت الزامی است");

                if (mission.Points <= 0)
                    return (false, "امتیاز باید بیشتر از صفر باشد");
                if (!mission.SiteId.HasValue || mission.SiteId.Value <= 0)
                    return (false, "انتخاب سایت برای ماموریت الزامی است");

                mission.UpdatedAt = DateTime.Now;
                var result = await _repository.UpdateMissionAsync(mission);

                return result
                    ? (true, "ماموریت با موفقیت به‌روزرسانی شد")
                    : (false, "خطا در به‌روزرسانی ماموریت");
            }
            catch (Exception ex)
            {
                return (false, $"خطا در به‌روزرسانی ماموریت: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteMissionAsync(int id)
        {
            try
            {
                var result = await _repository.DeleteMissionAsync(id);
                return result
                    ? (true, "ماموریت با موفقیت حذف شد")
                    : (false, "خطا در حذف ماموریت");
            }
            catch (Exception ex)
            {
                return (false, $"خطا در حذف ماموریت: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ToggleMissionStatusAsync(int id, bool isActive)
        {
            try
            {
                var result = await _repository.ToggleMissionStatusAsync(id, isActive);
                return result
                    ? (true, isActive ? "ماموریت فعال شد" : "ماموریت غیرفعال شد")
                    : (false, "خطا در تغییر وضعیت ماموریت");
            }
            catch (Exception ex)
            {
                return (false, $"خطا در تغییر وضعیت: {ex.Message}");
            }
        }

        public async Task<UserPointsSummaryDto> GetUserPointsSummaryAsync(int userId)
        {
            var userPoints = await _repository.GetUserPointsAsync(userId);

            if (userPoints == null)
            {
                return new UserPointsSummaryDto
                {
                    TotalPoints = 0,
                    AvailablePoints = 0,
                    PendingPoints = 0,
                    RejectedPoints = 0
                };
            }

            var transactions = await _repository.GetUserTransactionsAsync(userId, 1000);
            var rejectedPoints = transactions
                .Where(t => t.Status == "Rejected")
                .Sum(t => t.Points);

            return new UserPointsSummaryDto
            {
                TotalPoints = userPoints.TotalPoints,
                AvailablePoints = userPoints.AvailablePoints,
                PendingPoints = userPoints.PendingPoints,
                RejectedPoints = rejectedPoints
            };
        }

        public async Task<List<UserPoints>> GetAllUserPointsAsync(int skip = 0, int take = 100)
        {
            return await _repository.GetAllUserPointsAsync(skip, take);
        }

        public async Task<(bool Success, string Message, int TransactionId)> CreateTransactionAsync(
            int userId,
            string missionType,
            int points,
            string reason,
            int? missionId = null,
            string referenceId = null,
            string referenceType = null)
        {
            try
            {
                if (points <= 0)
                    return (false, "امتیاز باید بیشتر از صفر باشد", 0);

                var transaction = new PointTransaction
                {
                    UserId = userId,
                    MissionId = missionId,
                    MissionType = missionType,
                    Points = points,
                    Status = "Pending",
                    Reason = reason ?? "",
                    ReferenceId = referenceId,
                    ReferenceType = referenceType,
                    CreatedAt = DateTime.Now
                };

                var transactionId = await _repository.CreateTransactionAsync(transaction);
                await UpdateUserPointsAfterTransaction(userId);
                return (true, "درخواست امتیاز ثبت شد و در انتظار تایید است", transactionId);
            }
            catch (Exception ex)
            {
                return (false, $"خطا در ثبت تراکنش: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string Message)> ApproveTransactionAsync(int transactionId, int adminUserId, string notes = null)
        {
            try
            {
                var transaction = await _repository.GetTransactionByIdAsync(transactionId);
                if (transaction == null)
                    return (false, "تراکنش یافت نشد");

                if (transaction.Status != "Pending")
                    return (false, "فقط تراکنش‌های در انتظار تایید قابل تایید هستند");

                var result = await _repository.UpdateTransactionStatusAsync(
                    transactionId,
                    "Approved",
                    adminUserId,
                    notes);

                if (result)
                {
                    await UpdateUserPointsAfterTransaction(transaction.UserId);
                    return (true, "امتیاز تایید شد و به حساب کاربر اضافه شد");
                }
                return (false, "خطا در تایید تراکنش");
            }
            catch (Exception ex)
            {
                return (false, $"خطا در تایید تراکنش: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RejectTransactionAsync(int transactionId, int adminUserId, string notes = null)
        {
            try
            {
                var transaction = await _repository.GetTransactionByIdAsync(transactionId);
                if (transaction == null)
                    return (false, "تراکنش یافت نشد");

                if (transaction.Status != "Pending")
                    return (false, "فقط تراکنش‌های در انتظار تایید قابل رد هستند");

                var result = await _repository.UpdateTransactionStatusAsync(
                    transactionId,
                    "Rejected",
                    adminUserId,
                    notes);

                if (result)
                {
                    await UpdateUserPointsAfterTransaction(transaction.UserId);
                    return (true, "امتیاز رد شد");
                }
                return (false, "خطا در رد تراکنش");
            }
            catch (Exception ex)
            {
                return (false, $"خطا در رد تراکنش: {ex.Message}");
            }
        }

        public async Task<List<PointTransactionDto>> GetUserTransactionsAsync(int userId, int count = 10)
        {
            var transactions = await _repository.GetUserTransactionsAsync(userId, count);
            return transactions.Select(t => MapToDto(t)).ToList();
        }

        public async Task<List<PointTransactionDto>> GetPendingTransactionsAsync(int skip = 0, int take = 50)
        {
            var transactions = await _repository.GetPendingTransactionsAsync(skip, take);
            return transactions.Select(t => MapToDto(t)).ToList();
        }

        public async Task<List<PointTransactionDto>> GetAllTransactionsAsync(int skip = 0, int take = 100, string status = null)
        {
            var transactions = await _repository.GetAllTransactionsAsync(skip, take, status);
            return transactions.Select(t => MapToDto(t)).ToList();
        }

        public async Task<List<PointTransactionDto>> GetLatestTransactionsAsync(int count = 10)
        {
            var transactions = await _repository.GetLatestTransactionsAsync(count);
            return transactions.Select(t => MapToDto(t)).ToList();
        }

        private async Task UpdateUserPointsAfterTransaction(int userId)
        {
            var transactions = await _repository.GetUserTransactionsAsync(userId, 10000);
            var totalPoints = transactions.Sum(t => t.Points);
            var availablePoints = transactions.Where(t => t.Status == "Approved").Sum(t => t.Points);
            var pendingPoints = transactions.Where(t => t.Status == "Pending").Sum(t => t.Points);
            await _repository.UpdateUserPointsAsync(userId, totalPoints, availablePoints, pendingPoints);
        }

        private PointTransactionDto MapToDto(PointTransactionDto dto)
        {
            dto.StatusText = dto.Status switch
            {
                "Pending" => "در انتظار تایید",
                "Approved" => "تایید شده",
                "Rejected" => "رد شده",
                _ => dto.Status
            };
            return dto;
        }

        public async Task<(bool IsEligible, string Message)> CheckMissionEligibilityAsync(int userId, int missionId)
        {
            var mission = await _repository.GetMissionByIdAsync(missionId);
            if (mission == null)
                return (false, "ماموریت یافت نشد");

            if (mission.SiteId.HasValue && _tenant?.HasSite == true && mission.SiteId.Value != _tenant.SiteId)
                return (false, "این ماموریت مربوط به سایت دیگری است");

            if (mission.StartDate.HasValue && DateTime.Now < mission.StartDate.Value)
            {
                return (false, $"ماموریت از تاریخ {PersianDateHelper.ToPersianDateString(mission.StartDate.Value)} شروع می‌شود");
            }

            if (mission.EndDate.HasValue && DateTime.Now > mission.EndDate.Value)
            {
                return (false, $"ماموریت در تاریخ {PersianDateHelper.ToPersianDateString(mission.EndDate.Value)} به پایان رسیده است");
            }

            if (mission.MaxClaimCount > 0)
            {
                var claimCount = await _repository.GetUserClaimCountAsync(userId, missionId);
                if (claimCount >= mission.MaxClaimCount)
                {
                    return (false, $"شما حداکثر تعداد دریافت این ماموریت ({mission.MaxClaimCount} بار) را استفاده کرده‌اید");
                }
            }

            return await _repository.CheckMissionEligibilityAsync(userId, missionId);
        }

        public Task<PointsLoyaltyDashboardDto> GetLoyaltyDashboardAsync() =>
            _repository.GetLoyaltyDashboardAsync();
    }
}
