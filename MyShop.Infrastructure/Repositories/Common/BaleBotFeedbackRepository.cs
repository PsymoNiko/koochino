using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class BaleBotFeedbackRepository : IBaleBotFeedbackRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public BaleBotFeedbackRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> AddAsync(long baleUserId, string? username, string? firstName, string? lastName, string choice)
        {
            using var conn = _connectionFactory.CreateConnection();
            var id = await conn.ExecuteScalarAsync<int>(
                "koch.spBaleBotFeedback_Insert",
                new { BaleUserId = baleUserId, Username = username, FirstName = firstName, LastName = lastName, Choice = choice },
                commandType: CommandType.StoredProcedure
            );
            return id;
        }

        public async Task<List<BaleBotFeedbackDto>> GetAllAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<BaleBotFeedbackDto>(
                "koch.spBaleBotFeedback_GetAll",
                commandType: CommandType.StoredProcedure
            );
            return list.ToList();
        }
    }
}
