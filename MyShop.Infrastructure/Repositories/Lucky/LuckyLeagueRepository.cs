using Dapper;
using MyShop.Application.Interfaces.Lucky;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Lucky;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Lucky
{
    public class LuckyLeagueRepository : ILuckyLeagueRepository
    {
        private readonly IDbConnectionFactory _factory;

        public LuckyLeagueRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<List<LuckyLeague>> GetAllLeaguesAsync(bool activeOnly = false)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<LuckyLeague>(
                "koch.spLuckyLeague_GetAll",
                new { ActiveOnly = activeOnly },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<LuckyLeague> GetLeagueByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<LuckyLeague>(
                "koch.spLuckyLeague_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> CreateLeagueAsync(LuckyLeague league)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("SiteId", league.SiteId);
            parameters.Add("Name", league.Name);
            parameters.Add("ThemeKey", league.ThemeKey);
            parameters.Add("MinPoints", league.MinPoints);
            parameters.Add("MaxPoints", league.MaxPoints);
            parameters.Add("PointsPerBox", league.PointsPerBox);
            parameters.Add("TotalBoxesPerRound", league.TotalBoxesPerRound);
            parameters.Add("LosingBoxesPerRound", league.LosingBoxesPerRound);
            parameters.Add("SortOrder", league.SortOrder);
            parameters.Add("IsActive", league.IsActive);
            parameters.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("koch.spLuckyLeague_Create", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("Id");
        }

        public async Task<bool> UpdateLeagueAsync(LuckyLeague league)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("Id", league.Id);
            parameters.Add("Name", league.Name);
            parameters.Add("ThemeKey", league.ThemeKey);
            parameters.Add("MinPoints", league.MinPoints);
            parameters.Add("MaxPoints", league.MaxPoints);
            parameters.Add("PointsPerBox", league.PointsPerBox);
            parameters.Add("TotalBoxesPerRound", league.TotalBoxesPerRound);
            parameters.Add("LosingBoxesPerRound", league.LosingBoxesPerRound);
            parameters.Add("SortOrder", league.SortOrder);
            parameters.Add("IsActive", league.IsActive);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("koch.spLuckyLeague_Update", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("Result") > 0;
        }

        public async Task<bool> ToggleLeagueStatusAsync(int id, bool isActive)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("Id", id);
            parameters.Add("IsActive", isActive);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("koch.spLuckyLeague_ToggleStatus", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("Result") > 0;
        }

        public async Task<List<LuckyLeagueViewDto>> GetEligibleLeaguesForUserAsync(int userId, int availablePoints, int? siteId)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<LuckyLeagueViewDto>(
                "koch.spLuckyLeague_GetEligibleForUser",
                new { UserId = userId, AvailablePoints = availablePoints, SiteId = siteId },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<List<LuckyPrize>> GetPrizesByLeagueIdAsync(int leagueId, bool includeInactive = false)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<LuckyPrize>(
                "koch.spLuckyPrize_GetByLeagueId",
                new { LeagueId = leagueId, IncludeInactive = includeInactive },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<LuckyPrize> GetPrizeByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<LuckyPrize>(
                "koch.spLuckyPrize_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<LuckyPrize>> GetPendingPrizeQueueAsync()
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<LuckyPrize>(
                "koch.spLuckyPrize_GetPendingQueue",
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<List<LuckyPrize>> GetPrizesByCreatorAsync(int createdByUserId)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<LuckyPrize>(
                "koch.spLuckyPrize_GetByCreator",
                new { CreatedByUserId = createdByUserId },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<int> CreatePrizeAsync(LuckyPrize prize)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("LeagueId", prize.LeagueId);
            parameters.Add("Title", prize.Title);
            parameters.Add("PrizeType", prize.PrizeType);
            parameters.Add("DiscountType", prize.DiscountType);
            parameters.Add("DiscountValue", prize.DiscountValue);
            parameters.Add("MinBuy", prize.MinBuy);
            parameters.Add("ExpireDays", prize.ExpireDays);
            parameters.Add("PointsValue", prize.PointsValue);
            parameters.Add("Weight", prize.Weight);
            parameters.Add("Stock", prize.Stock);
            parameters.Add("IconKey", prize.IconKey);
            parameters.Add("Scope", prize.Scope);
            parameters.Add("StoreId", prize.StoreId);
            parameters.Add("CreatedByUserId", prize.CreatedByUserId);
            parameters.Add("CreatedByRole", prize.CreatedByRole);
            parameters.Add("Status", prize.Status);
            parameters.Add("IsActive", prize.IsActive);
            parameters.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("koch.spLuckyPrize_Create", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("Id");
        }

        public async Task<bool> UpdatePrizeAsync(LuckyPrize prize)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("Id", prize.Id);
            parameters.Add("Title", prize.Title);
            parameters.Add("DiscountType", prize.DiscountType);
            parameters.Add("DiscountValue", prize.DiscountValue);
            parameters.Add("MinBuy", prize.MinBuy);
            parameters.Add("ExpireDays", prize.ExpireDays);
            parameters.Add("PointsValue", prize.PointsValue);
            parameters.Add("Weight", prize.Weight);
            parameters.Add("Stock", prize.Stock);
            parameters.Add("IconKey", prize.IconKey);
            parameters.Add("IsActive", prize.IsActive);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("koch.spLuckyPrize_Update", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("Result") > 0;
        }

        public async Task<int> DeletePrizeAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("Id", id);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("koch.spLuckyPrize_Delete", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("Result");
        }

        public async Task<bool> TogglePrizeStatusAsync(int id, bool isActive)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("Id", id);
            parameters.Add("IsActive", isActive);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("koch.spLuckyPrize_ToggleStatus", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("Result") > 0;
        }

        public async Task<bool> ApprovePrizeAsync(int id, bool approve, int approvedByUserId, string adminNote)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("Id", id);
            parameters.Add("Approve", approve);
            parameters.Add("ApprovedByUserId", approvedByUserId);
            parameters.Add("AdminNote", adminNote);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("koch.spLuckyPrize_Approve", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("Result") > 0;
        }

        public async Task<List<LuckyBox>> GetActiveRoundBoxesAsync(int userId, int leagueId)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<LuckyBox>(
                "koch.spLuckyBoxRound_GetActiveForUserLeague",
                new { UserId = userId, LeagueId = leagueId },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<int> CreateRoundAsync(int userId, int leagueId)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", userId);
            parameters.Add("LeagueId", leagueId);
            parameters.Add("RoundId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("koch.spLuckyBoxRound_Create", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("RoundId");
        }

        public async Task<(int Total, int Opened)> CountRoundOpenAsync(int roundId)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("RoundId", roundId);
            parameters.Add("Total", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("Opened", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("koch.spLuckyBoxRound_CountOpen", parameters, commandType: CommandType.StoredProcedure);
            return (parameters.Get<int>("Total"), parameters.Get<int>("Opened"));
        }

        public async Task<(int Result, bool IsLosing, string PrizeType, string PrizeTitle, int? PointsCredited, int? PrizeId, int PointsSpent)>
            OpenBoxAsync(int boxId, int userId, string paymentMethod)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("BoxId", boxId);
            parameters.Add("UserId", userId);
            parameters.Add("PaymentMethod", paymentMethod);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("IsLosing", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            parameters.Add("PrizeType", dbType: DbType.String, size: 20, direction: ParameterDirection.Output);
            parameters.Add("PrizeTitle", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);
            parameters.Add("PointsCredited", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("PrizeId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("PointsSpentOut", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("koch.spLuckyBox_Open", parameters, commandType: CommandType.StoredProcedure);

            return (
                parameters.Get<int>("Result"),
                parameters.Get<bool?>("IsLosing") ?? false,
                parameters.Get<string>("PrizeType"),
                parameters.Get<string>("PrizeTitle"),
                parameters.Get<int?>("PointsCredited"),
                parameters.Get<int?>("PrizeId"),
                parameters.Get<int?>("PointsSpentOut") ?? 0);
        }

        public async Task SetBoxResultCouponAsync(int boxId, int coponId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "koch.spLuckyBox_SetResultCoupon",
                new { BoxId = boxId, CoponId = coponId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task RefundBoxCostAsync(int boxId, int userId, int leagueId, string paymentMethod, int pointsSpent)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "koch.spLuckyBox_RefundCost",
                new { BoxId = boxId, UserId = userId, LeagueId = leagueId, PaymentMethod = paymentMethod, PointsSpent = pointsSpent },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<(int Result, int NewChanceBalance)> ConvertPointsToChanceAsync(int userId, int leagueId)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", userId);
            parameters.Add("LeagueId", leagueId);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("NewChanceBalance", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("koch.spLuckyChance_ConvertFromPoints", parameters, commandType: CommandType.StoredProcedure);
            return (parameters.Get<int>("Result"), parameters.Get<int>("NewChanceBalance"));
        }

        public Task<List<LuckyBoxOpenReportRow>> GetBoxOpenReportAsync(
            int? leagueId, string outcomeFilter, DateTime? fromUtc, DateTime? toUtc, string search, int skip, int take)
        {
            return QueryBoxOpenReportAsync(leagueId, outcomeFilter, fromUtc, toUtc, search, skip, take, statsOnly: false);
        }

        public async Task<LuckyBoxOpenReportStats> GetBoxOpenReportStatsAsync(
            int? leagueId, string outcomeFilter, DateTime? fromUtc, DateTime? toUtc, string search)
        {
            using var conn = _factory.CreateConnection();
            try
            {
                var row = await conn.QueryFirstOrDefaultAsync<LuckyBoxOpenReportStats>(
                    "koch.spLuckyBox_GetOpenReportStats",
                    new { LeagueId = leagueId, OutcomeFilter = NormalizeOutcome(outcomeFilter), FromUtc = fromUtc, ToUtc = toUtc, Search = NormalizeSearch(search) },
                    commandType: CommandType.StoredProcedure);
                return row ?? new LuckyBoxOpenReportStats();
            }
            catch
            {
                var list = await QueryBoxOpenReportAsync(leagueId, outcomeFilter, fromUtc, toUtc, search, 0, int.MaxValue, statsOnly: true);
                return new LuckyBoxOpenReportStats
                {
                    TotalOpened = list.Count,
                    WinnersCount = list.Count(x => !x.IsLosing),
                    LosersCount = list.Count(x => x.IsLosing)
                };
            }
        }

        async Task<List<LuckyBoxOpenReportRow>> QueryBoxOpenReportAsync(
            int? leagueId, string outcomeFilter, DateTime? fromUtc, DateTime? toUtc, string search, int skip, int take, bool statsOnly)
        {
            using var conn = _factory.CreateConnection();
            try
            {
                if (!statsOnly)
                {
                    var rows = await conn.QueryAsync<LuckyBoxOpenReportRow>(
                        "koch.spLuckyBox_GetOpenReport",
                        new
                        {
                            LeagueId = leagueId,
                            OutcomeFilter = NormalizeOutcome(outcomeFilter),
                            FromUtc = fromUtc,
                            ToUtc = toUtc,
                            Search = NormalizeSearch(search),
                            Skip = skip,
                            Take = take
                        },
                        commandType: CommandType.StoredProcedure);
                    return rows.ToList();
                }
            }
            catch { /* fallback below */ }

            var filter = NormalizeOutcome(outcomeFilter);
            var sql = @"
                SELECT b.Id AS BoxId, r.UserId, u.Username, u.PhoneNumber,
                       l.Id AS LeagueId, l.Name AS LeagueName, l.ThemeKey,
                       b.OpenedAt, b.IsLosing, b.PaymentMethod, b.PointsSpent,
                       p.PrizeType, p.Title AS PrizeTitle, b.ResultPointsCredited,
                       c.CoponCode AS CouponCode
                FROM koch.LuckyBox b
                INNER JOIN koch.LuckyBoxRound r ON r.Id = b.RoundId
                INNER JOIN koch.LuckyLeague l ON l.Id = r.LeagueId
                INNER JOIN koch.Users u ON u.Id = r.UserId
                LEFT JOIN koch.LuckyPrize p ON p.Id = b.PrizeId
                LEFT JOIN koch.Copon c ON c.CoponID = b.ResultCouponId
                WHERE b.IsOpened = 1
                  AND (@LeagueId IS NULL OR l.Id = @LeagueId)
                  AND (@FromUtc IS NULL OR b.OpenedAt >= @FromUtc)
                  AND (@ToUtc IS NULL OR b.OpenedAt < @ToUtc)
                  AND (
                        @OutcomeFilter = N'All'
                        OR (@OutcomeFilter = N'Winners' AND b.IsLosing = 0)
                        OR (@OutcomeFilter = N'Losers' AND b.IsLosing = 1)
                  )
                  AND (
                        @Search IS NULL
                        OR u.Username LIKE N'%' + @Search + N'%'
                        OR u.PhoneNumber LIKE N'%' + @Search + N'%'
                        OR p.Title LIKE N'%' + @Search + N'%'
                        OR c.CoponCode LIKE N'%' + @Search + N'%'
                  )
                ORDER BY b.OpenedAt DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

            var result = await conn.QueryAsync<LuckyBoxOpenReportRow>(sql, new
            {
                LeagueId = leagueId,
                OutcomeFilter = filter,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Search = NormalizeSearch(search),
                Skip = skip,
                Take = statsOnly ? int.MaxValue : take
            });
            return result.ToList();
        }

        static string NormalizeOutcome(string outcomeFilter)
        {
            if (string.Equals(outcomeFilter, "Winners", StringComparison.OrdinalIgnoreCase)) return "Winners";
            if (string.Equals(outcomeFilter, "Losers", StringComparison.OrdinalIgnoreCase)) return "Losers";
            return "All";
        }

        static string NormalizeSearch(string search)
        {
            var s = search?.Trim();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
    }
}
