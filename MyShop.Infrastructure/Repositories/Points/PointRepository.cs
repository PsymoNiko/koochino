using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class PointRepository : IPointRepository
    {
        private readonly IDbConnectionFactory _factory;

        public PointRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        // ========== Point Missions ==========
        public async Task<List<PointMission>> GetMissionsAsync(bool activeOnly = false, int? siteId = null)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<PointMission>(
                "koch.spPointMissions_GetAll",
                new { ActiveOnly = activeOnly, SiteId = siteId },
                commandType: CommandType.StoredProcedure);
            return result.OrderBy(m => m.MissionSort).ToList();
        }

        public async Task<PointMission> GetMissionByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<PointMission>(
                "koch.spPointMissions_GetById",
                new { MissionId = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> CreateMissionAsync(PointMission mission)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("Title", mission.Title);
            parameters.Add("Description", mission.Description);
            parameters.Add("MissionType", mission.MissionType);
            parameters.Add("Points", mission.Points);
            parameters.Add("IsActive", mission.IsActive);
            parameters.Add("CreatedByUserId", mission.CreatedByUserId);
            parameters.Add("MinPurchaseAmount", mission.MinPurchaseAmount);
            parameters.Add("MaxPurchaseAmount", mission.MaxPurchaseAmount);
            parameters.Add("CustomConfig", mission.CustomConfig);
            parameters.Add("RedirectUrl", mission.RedirectUrl);
            parameters.Add("ValidationSP", mission.ValidationSP);
            parameters.Add("MaxClaimCount", mission.MaxClaimCount);
            parameters.Add("StartDate", mission.StartDate);
            parameters.Add("EndDate", mission.EndDate);
            parameters.Add("MissionSort", mission.MissionSort);
            parameters.Add("SiteId", mission.SiteId);
            parameters.Add("MissionId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spPointMissions_Create",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("MissionId");
        }

        public async Task<bool> UpdateMissionAsync(PointMission mission)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("MissionId", mission.Id);
            parameters.Add("Title", mission.Title);
            parameters.Add("Description", mission.Description);
            parameters.Add("MissionType", mission.MissionType);
            parameters.Add("Points", mission.Points);
            parameters.Add("IsActive", mission.IsActive);
            parameters.Add("MinPurchaseAmount", mission.MinPurchaseAmount);
            parameters.Add("MaxPurchaseAmount", mission.MaxPurchaseAmount);
            parameters.Add("CustomConfig", mission.CustomConfig);
            parameters.Add("RedirectUrl", mission.RedirectUrl);
            parameters.Add("ValidationSP", mission.ValidationSP);
            parameters.Add("MaxClaimCount", mission.MaxClaimCount);
            parameters.Add("StartDate", mission.StartDate);
            parameters.Add("EndDate", mission.EndDate);
            parameters.Add("MissionSort", mission.MissionSort);
            parameters.Add("SiteId", mission.SiteId);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spPointMissions_Update",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("Result") > 0;
        }

        public async Task<bool> DeleteMissionAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("MissionId", id);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spPointMissions_Delete",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("Result") > 0;
        }

        public async Task<bool> ToggleMissionStatusAsync(int id, bool isActive)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("MissionId", id);
            parameters.Add("IsActive", isActive);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spPointMissions_ToggleStatus",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("Result") > 0;
        }

        // ========== User Points ==========
        public async Task<UserPoints> GetUserPointsAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<UserPoints>(
                "koch.spUserPoints_GetByUserId",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> UpdateUserPointsAsync(int userId, int totalPoints, int availablePoints, int pendingPoints)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", userId);
            parameters.Add("TotalPoints", totalPoints);
            parameters.Add("AvailablePoints", availablePoints);
            parameters.Add("PendingPoints", pendingPoints);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spUserPoints_Update",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("Result") > 0;
        }

        public async Task<List<UserPoints>> GetAllUserPointsAsync(int skip = 0, int take = 100)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<UserPoints>(
                "koch.spUserPoints_GetAll",
                new { Skip = skip, Take = take },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        // ========== Point Transactions ==========
        public async Task<int> CreateTransactionAsync(PointTransaction transaction)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", transaction.UserId);
            parameters.Add("MissionId", transaction.MissionId);
            parameters.Add("MissionType", transaction.MissionType);
            parameters.Add("Points", transaction.Points);
            parameters.Add("Status", transaction.Status);
            parameters.Add("Reason", transaction.Reason);
            parameters.Add("ReferenceId", transaction.ReferenceId);
            parameters.Add("ReferenceType", transaction.ReferenceType);
            parameters.Add("TransactionId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spPointTransactions_Create",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("TransactionId");
        }

        public async Task<bool> UpdateTransactionStatusAsync(int transactionId, string status, int? reviewedByUserId, string reviewNotes = null)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("TransactionId", transactionId);
            parameters.Add("Status", status);
            parameters.Add("ReviewedByUserId", reviewedByUserId);
            parameters.Add("ReviewNotes", reviewNotes);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spPointTransactions_UpdateStatus",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("Result") > 0;
        }

        public async Task<PointTransaction> GetTransactionByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<PointTransaction>(
                "koch.spPointTransactions_GetById",
                new { TransactionId = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<PointTransactionDto>> GetUserTransactionsAsync(int userId, int count = 10)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<PointTransactionDto>(
                "koch.spPointTransactions_GetByUserId",
                new { UserId = userId, Count = count },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<List<PointTransactionDto>> GetPendingTransactionsAsync(int skip = 0, int take = 50)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<PointTransactionDto>(
                "koch.spPointTransactions_GetPending",
                new { Skip = skip, Take = take },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<List<PointTransactionDto>> GetAllTransactionsAsync(int skip = 0, int take = 100, string status = null)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<PointTransactionDto>(
                "koch.spPointTransactions_GetAll",
                new { Skip = skip, Take = take, Status = status },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<List<PointTransactionDto>> GetLatestTransactionsAsync(int count = 10)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<PointTransactionDto>(
                "koch.spPointTransactions_GetLatest",
                new { Count = count },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        // ========== Check Mission Eligibility ==========
        public async Task<(bool IsEligible, string Message)> CheckMissionEligibilityAsync(int userId, int missionId)
        {
            using var conn = _factory.CreateConnection();
            
            // دریافت اطلاعات ماموریت
            var mission = await GetMissionByIdAsync(missionId);
            if (mission == null)
                return (false, "ماموریت یافت نشد");

            if (string.IsNullOrWhiteSpace(mission.ValidationSP))
                return (false, "ماموریت فاقد Stored Procedure اعتبارسنجی است");

            // فراخوانی Stored Procedure داینامیک (بدون schema → پیش‌فرض koch.)
            var spName = ResolveValidationProcedureName(mission.ValidationSP);
            var parameters = new DynamicParameters();
            parameters.Add("UserId", userId);
            parameters.Add("MissionId", missionId);
            parameters.Add("IsEligible", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            parameters.Add("Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

            try
            {
                await conn.ExecuteAsync(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var isEligible = parameters.Get<bool>("IsEligible");
                var message = parameters.Get<string>("Message")?.Trim() ?? "";
                // اگر پیام خالی، یا فقط ? یا کاراکتر جایگزین (ناشی از encoding اشتباه دیتابیس) بود، پیام پیش‌فرض فارسی بگذار
                if (string.IsNullOrEmpty(message) || IsInvalidOrCorruptedMessage(message))
                    message = isEligible ? "شما واجد شرایط دریافت این امتیاز هستید." : "در حال حاضر واجد شرایط این ماموریت نیستید.";

                return (isEligible, message);
            }
            catch (Exception ex)
            {
                return (false, $"خطا در بررسی شرایط: {ex.Message}");
            }
        }

        /// <summary>
        /// نام SP اعتبارسنجی را به schema پیش‌فرض koch محدود می‌کند
        /// (مثلاً spMission_X → koch.spMission_X).
        /// </summary>
        private static string ResolveValidationProcedureName(string validationSp)
        {
            var name = (validationSp ?? "").Trim().TrimStart('[').TrimEnd(']');
            if (string.IsNullOrWhiteSpace(name))
                return name;

            // [koch].[spX] یا koch.spX
            name = name.Replace("[", "").Replace("]", "");
            if (name.Contains('.'))
                return name;

            return "koch." + name;
        }

        /// <summary>پیام خالی یا خراب (؟ / کاراکتر جایگزین ناشی از encoding اشتباه) را تشخیص می‌دهد.</summary>
        private static bool IsInvalidOrCorruptedMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return true;
            var t = message.Trim();
            if (t.Length == 0) return true;
            // فقط ? یا فقط کاراکتر جایگزین Unicode (U+FFFD) یا ترکیب آن‌ها
            const char replacementChar = '\uFFFD';
            var invalidCount = t.Count(c => c == '?' || c == replacementChar || char.IsControl(c));
            return invalidCount == t.Length || invalidCount >= (t.Length + 1) / 2;
        }

        // ========== Get User Claim Count ==========
        public async Task<int> GetUserClaimCountAsync(int userId, int missionId)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryFirstOrDefaultAsync<int>(
                "koch.spPointTransactions_GetUserClaimCount",
                new { UserId = userId, MissionId = missionId },
                commandType: CommandType.StoredProcedure);
            return result;
        }

        public async Task<PointsLoyaltyDashboardDto> GetLoyaltyDashboardAsync()
        {
            var dto = new PointsLoyaltyDashboardDto();
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var from14 = today.AddDays(-13);

            using var conn = _factory.CreateConnection();

            try
            {
                var totals = await conn.QueryFirstOrDefaultAsync<PointsLoyaltyDashboardDto>(@"
SELECT
    (SELECT COUNT(*) FROM koch.PointMissions) AS MissionsTotal,
    (SELECT COUNT(*) FROM koch.PointMissions WHERE IsActive = 1) AS MissionsActive,
    (SELECT COUNT(*) FROM koch.PointMissions WHERE IsActive = 0) AS MissionsInactive,
    (SELECT COUNT(*) FROM koch.PointMissions WHERE EndDate IS NOT NULL AND EndDate < @Today) AS MissionsExpired,
    (SELECT COUNT(*) FROM koch.PointMissions WHERE StartDate IS NOT NULL AND StartDate > @Today) AS MissionsUpcoming,
    (SELECT COUNT(*) FROM koch.PointTransactions WHERE Status = N'Pending') AS TxPending,
    (SELECT COUNT(*) FROM koch.PointTransactions WHERE Status = N'Approved' AND CreatedAt >= @MonthStart) AS TxApprovedMonth,
    (SELECT COUNT(*) FROM koch.PointTransactions WHERE Status = N'Rejected' AND CreatedAt >= @MonthStart) AS TxRejectedMonth,
    (SELECT COUNT(*) FROM koch.PointTransactions WHERE CreatedAt >= @MonthStart) AS TxTotalMonth,
    (SELECT ISNULL(SUM(CAST(Points AS BIGINT)), 0) FROM koch.PointTransactions WHERE Status = N'Approved' AND CreatedAt >= @MonthStart) AS PointsApprovedMonth,
    (SELECT ISNULL(SUM(CAST(PendingPoints AS BIGINT)), 0) FROM koch.UserPoints) AS PointsPendingTotal,
    (SELECT COUNT(*) FROM koch.UserPoints WHERE AvailablePoints > 0) AS UsersWithPoints,
    (SELECT ISNULL(SUM(CAST(AvailablePoints AS BIGINT)), 0) FROM koch.UserPoints) AS TotalAvailablePoints,
    (SELECT COUNT(*) FROM koch.UserPoints WHERE PendingPoints > 0) AS UsersWithPending
", new { Today = today, MonthStart = monthStart });
                if (totals != null)
                {
                    dto.MissionsTotal = totals.MissionsTotal;
                    dto.MissionsActive = totals.MissionsActive;
                    dto.MissionsInactive = totals.MissionsInactive;
                    dto.MissionsExpired = totals.MissionsExpired;
                    dto.MissionsUpcoming = totals.MissionsUpcoming;
                    dto.TxPending = totals.TxPending;
                    dto.TxApprovedMonth = totals.TxApprovedMonth;
                    dto.TxRejectedMonth = totals.TxRejectedMonth;
                    dto.TxTotalMonth = totals.TxTotalMonth;
                    dto.PointsApprovedMonth = totals.PointsApprovedMonth;
                    dto.PointsPendingTotal = totals.PointsPendingTotal;
                    dto.UsersWithPoints = totals.UsersWithPoints;
                    dto.TotalAvailablePoints = totals.TotalAvailablePoints;
                    dto.UsersWithPending = totals.UsersWithPending;
                }
            }
            catch
            {
                try
                {
                    var simple = await conn.QueryFirstOrDefaultAsync<PointsLoyaltyDashboardDto>(@"
SELECT
    (SELECT COUNT(*) FROM koch.PointMissions) AS MissionsTotal,
    (SELECT COUNT(*) FROM koch.PointMissions WHERE IsActive = 1) AS MissionsActive,
    (SELECT COUNT(*) FROM koch.PointMissions WHERE IsActive = 0) AS MissionsInactive,
    0 AS MissionsExpired,
    0 AS MissionsUpcoming,
    (SELECT COUNT(*) FROM koch.PointTransactions WHERE Status = N'Pending') AS TxPending,
    (SELECT COUNT(*) FROM koch.PointTransactions WHERE Status = N'Approved' AND CreatedAt >= @MonthStart) AS TxApprovedMonth,
    (SELECT COUNT(*) FROM koch.PointTransactions WHERE Status = N'Rejected' AND CreatedAt >= @MonthStart) AS TxRejectedMonth,
    (SELECT COUNT(*) FROM koch.PointTransactions WHERE CreatedAt >= @MonthStart) AS TxTotalMonth,
    (SELECT ISNULL(SUM(CAST(Points AS BIGINT)), 0) FROM koch.PointTransactions WHERE Status = N'Approved' AND CreatedAt >= @MonthStart) AS PointsApprovedMonth,
    (SELECT ISNULL(SUM(CAST(PendingPoints AS BIGINT)), 0) FROM koch.UserPoints) AS PointsPendingTotal,
    (SELECT COUNT(*) FROM koch.UserPoints WHERE AvailablePoints > 0) AS UsersWithPoints,
    (SELECT ISNULL(SUM(CAST(AvailablePoints AS BIGINT)), 0) FROM koch.UserPoints) AS TotalAvailablePoints,
    (SELECT COUNT(*) FROM koch.UserPoints WHERE PendingPoints > 0) AS UsersWithPending
", new { MonthStart = monthStart });
                    if (simple != null) dto = simple;
                }
                catch { return dto; }
            }

            try
            {
                var daily = (await conn.QueryAsync<(DateTime Day, int Approved, int Pending)>(@"
SELECT CAST(CreatedAt AS DATE) AS Day,
       SUM(CASE WHEN Status = N'Approved' THEN 1 ELSE 0 END) AS Approved,
       SUM(CASE WHEN Status = N'Pending' THEN 1 ELSE 0 END) AS Pending
FROM koch.PointTransactions
WHERE CreatedAt >= @From14
GROUP BY CAST(CreatedAt AS DATE)
ORDER BY Day", new { From14 = from14 })).ToList();
                var dailyMap = daily.ToDictionary(x => x.Day.Date, x => x);

                for (var d = from14; d <= today; d = d.AddDays(1))
                {
                    dto.DailyLabels.Add(d.ToString("MM/dd"));
                    if (dailyMap.TryGetValue(d, out var row))
                    {
                        dto.DailyApproved.Add(row.Approved);
                        dto.DailyPending.Add(row.Pending);
                    }
                    else
                    {
                        dto.DailyApproved.Add(0);
                        dto.DailyPending.Add(0);
                    }
                }
            }
            catch { }

            try
            {
                var top = (await conn.QueryAsync<(string Title, int Cnt)>(@"
SELECT TOP 6 ISNULL(m.Title, N'بدون ماموریت') AS Title, COUNT(*) AS Cnt
FROM koch.PointTransactions t
LEFT JOIN koch.PointMissions m ON m.Id = t.MissionId
WHERE t.CreatedAt >= DATEADD(DAY, -29, CAST(GETDATE() AS DATE))
  AND t.Status IN (N'Approved', N'Pending')
GROUP BY ISNULL(m.Title, N'بدون ماموریت')
ORDER BY Cnt DESC")).ToList();
                dto.TopMissionLabels = top.Select(x => x.Title).ToList();
                dto.TopMissionValues = top.Select(x => x.Cnt).ToList();
            }
            catch { }

            return dto;
        }
    }
}
