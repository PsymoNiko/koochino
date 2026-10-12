using Dapper;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Messaging;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class UserMessagingLinkRepository : IUserMessagingLinkRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UserMessagingLinkRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> UpsertAsync(UserMessagingLink link)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Provider", link.Provider);
            p.Add("PhoneNumber", link.PhoneNumber);
            p.Add("ChatId", link.ChatId);
            p.Add("ExternalUserId", link.ExternalUserId);
            p.Add("UserId", link.UserId);
            p.Add("Username", link.Username);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spUserMessagingLinks_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<UserMessagingLink> GetByPhoneAsync(string provider, string phoneNumber)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<UserMessagingLink>(
                "koch.spUserMessagingLinks_GetByPhone",
                new { Provider = provider, PhoneNumber = phoneNumber },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<UserMessagingLink> GetByChatAsync(string provider, string chatId)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<UserMessagingLink>(
                "koch.spUserMessagingLinks_GetByChat",
                new { Provider = provider, ChatId = chatId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<UserMessagingLink>> ListAsync(string provider = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<UserMessagingLink>(
                "koch.spUserMessagingLinks_List",
                new { Provider = provider },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<UserMessagingLink>();
        }
    }

    public class MessagingOtpRepository : IMessagingOtpRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MessagingOtpRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> InsertAsync(MessagingOtp otp)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Provider", otp.Provider);
            p.Add("PhoneNumber", otp.PhoneNumber);
            p.Add("Purpose", otp.Purpose);
            p.Add("CodeHash", otp.CodeHash);
            p.Add("ExpiresAt", otp.ExpiresAt);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spMessagingOtps_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<MessagingOtp> GetActiveAsync(string provider, string phoneNumber, string purpose)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<MessagingOtp>(
                "koch.spMessagingOtps_GetActive",
                new { Provider = provider, PhoneNumber = phoneNumber, Purpose = purpose },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> MarkUsedAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spMessagingOtps_MarkUsed", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") >= 1;
        }

        public async Task IncrementAttemptAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync("koch.spMessagingOtps_IncrementAttempt", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> CountRecentAsync(string provider, string phoneNumber, int minutes)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(
                "koch.spMessagingOtps_CountRecent",
                new { Provider = provider, PhoneNumber = phoneNumber, Minutes = minutes },
                commandType: CommandType.StoredProcedure);
        }
    }

    public class MessagingSendLogRepository : IMessagingSendLogRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MessagingSendLogRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> InsertAsync(MessagingSendLog log)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Provider", log.Provider);
            p.Add("PhoneNumber", log.PhoneNumber);
            p.Add("ChatId", log.ChatId);
            p.Add("MessageType", log.MessageType);
            p.Add("MessageText", log.MessageText ?? "");
            p.Add("Success", log.Success);
            p.Add("ErrorMessage", log.ErrorMessage);
            p.Add("LineNumber", log.LineNumber);
            p.Add("ExternalId", log.ExternalId);
            p.Add("TemplateCode", log.TemplateCode);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spMessagingSendLogs_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<List<MessagingSendLog>> ListAsync(string provider = null, int top = 200)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<MessagingSendLog>(
                "koch.spMessagingSendLogs_List",
                new { Provider = provider, Top = top },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<MessagingSendLog>();
        }

        public async Task<List<MessagingSendLog>> SearchAsync(SmsSendLogFilter filter)
        {
            filter ??= new SmsSendLogFilter();
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                var list = await conn.QueryAsync<MessagingSendLog>(
                    "koch.spMessagingSendLogs_Search",
                    new
                    {
                        Provider = filter.Provider,
                        Phone = filter.Phone,
                        MessageType = filter.MessageType,
                        Success = filter.Success,
                        FromDate = filter.FromDate,
                        ToDate = filter.ToDate,
                        Top = filter.Top
                    },
                    commandType: CommandType.StoredProcedure);
                return list?.ToList() ?? new List<MessagingSendLog>();
            }
            catch
            {
                return await ListAsync(filter.Provider, filter.Top);
            }
        }

        public async Task UpdateDeliveryAsync(int id, byte? deliveryState, string deliveryStateText)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                await conn.ExecuteAsync(
                    "koch.spMessagingSendLogs_UpdateDelivery",
                    new { Id = id, DeliveryState = deliveryState, DeliveryStateText = deliveryStateText },
                    commandType: CommandType.StoredProcedure);
            }
            catch { /* ignore if SP missing */ }
        }
    }

    public class BotMenuItemRepository : IBotMenuItemRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public BotMenuItemRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<BotMenuItem>> ListAsync(string provider = null, bool onlyActive = false)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<BotMenuItem>(
                "koch.spBotMenuItems_List",
                new { Provider = provider, OnlyActive = onlyActive ? 1 : 0 },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<BotMenuItem>();
        }

        public async Task<BotMenuItem> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<BotMenuItem>(
                "koch.spBotMenuItems_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertAsync(BotMenuItem item)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", item.Id > 0 ? item.Id : (int?)null);
            p.Add("Provider", item.Provider);
            p.Add("Title", item.Title);
            p.Add("ActionType", item.ActionType);
            p.Add("ActionPayload", item.ActionPayload);
            p.Add("SortOrder", item.SortOrder);
            p.Add("IsActive", item.IsActive);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spBotMenuItems_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync("koch.spBotMenuItems_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        }
    }

    public class OrderNotifyRecipientRepository : IOrderNotifyRecipientRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public OrderNotifyRecipientRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<OrderNotifyRecipient>> ListAsync(string provider = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<OrderNotifyRecipient>(
                "koch.spOrderNotifyRecipients_List",
                new { Provider = provider },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<OrderNotifyRecipient>();
        }

        public async Task<List<string>> ListActivePhonesAsync(string provider = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<string>(
                "koch.spOrderNotifyRecipients_ListActivePhones",
                new { Provider = provider ?? MessagingProviders.Bale },
                commandType: CommandType.StoredProcedure);
            return list?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() ?? new List<string>();
        }

        public async Task<int> AddAsync(string phoneNumber, string provider = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Provider", provider ?? MessagingProviders.Bale);
            p.Add("PhoneNumber", phoneNumber);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spOrderNotifyRecipients_Add", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task SetActiveAsync(int id, bool isActive)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(
                "koch.spOrderNotifyRecipients_SetActive",
                new { Id = id, IsActive = isActive },
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(
                "koch.spOrderNotifyRecipients_Delete",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }
    }
}
