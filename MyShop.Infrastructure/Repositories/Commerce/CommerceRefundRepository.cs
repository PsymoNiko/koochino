using Dapper;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Commerce;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Commerce
{
    public class CommerceRefundRepository : ICommerceRefundRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceRefundRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceRefund>> ListByOrderAsync(int orderId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceRefund>(
                "SELECT * FROM koch.CommerceRefunds WHERE OrderId=@OrderId ORDER BY Id",
                new { OrderId = orderId });
            return rows.ToList();
        }

        public async Task<CommerceRefund> GetAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceRefund>(
                "SELECT * FROM koch.CommerceRefunds WHERE Id=@Id", new { Id = id });
        }

        public async Task<int> InsertAsync(CommerceRefund m)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommerceRefunds (OrderId, PaymentId, Amount, Reason, Status, CreatedByUserId)
                OUTPUT INSERTED.Id
                VALUES (@OrderId, @PaymentId, @Amount, @Reason, @Status, @CreatedByUserId)", m);
        }

        public async Task UpdateStatusAsync(int id, byte status)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommerceRefunds
                SET Status=@Status, CompletedAt=CASE WHEN @Status=1 THEN SYSDATETIME() ELSE CompletedAt END
                WHERE Id=@Id", new { Id = id, Status = status });
        }

        public async Task<CommercePayment> GetLatestPaymentAsync(int orderId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommercePayment>(
                "SELECT TOP 1 * FROM koch.CommercePayments WHERE OrderId=@OrderId ORDER BY Id DESC",
                new { OrderId = orderId });
        }
    }
}
