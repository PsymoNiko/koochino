using Dapper;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Commerce;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Commerce
{
    public class CommerceStockReservationRepository : ICommerceStockReservationRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceStockReservationRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task ExpireStaleAsync()
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommerceStockReservations
                SET Status = @Released
                WHERE Status = @Active AND ExpiresAt < SYSDATETIME()",
                new { Released = CommerceConstants.ReservationStatus.Released, Active = CommerceConstants.ReservationStatus.Active });
        }

        public async Task<string> TryReserveLinesAsync(int cartId, IReadOnlyList<OrderItem> items, DateTime expiresAt)
        {
            using var conn = _factory.CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.CommerceStockReservations
                    SET Status = @Released
                    WHERE Status = @Active AND ExpiresAt < SYSDATETIME()",
                    new { Released = CommerceConstants.ReservationStatus.Released, Active = CommerceConstants.ReservationStatus.Active }, tx);

                foreach (var it in items ?? Array.Empty<OrderItem>())
                {
                    if (!it.ProductId.HasValue || it.ProductId.Value <= 0 || it.Quantity <= 0)
                        continue;

                    int onHand;
                    int held;
                    if (it.VariationId.HasValue && it.VariationId.Value > 0)
                    {
                        onHand = await conn.ExecuteScalarAsync<int?>(@"
                            SELECT Stock FROM koch.CommerceProductVariations WITH (UPDLOCK, ROWLOCK)
                            WHERE Id=@Id", new { Id = it.VariationId.Value }, tx) ?? -1;
                        if (onHand < 0)
                        {
                            tx.Rollback();
                            return "تنوع یافت نشد.";
                        }
                        held = await conn.ExecuteScalarAsync<int>(@"
                            SELECT ISNULL(SUM(Quantity),0) FROM koch.CommerceStockReservations
                            WHERE VariationId=@Vid AND Status=@Active AND ExpiresAt >= SYSDATETIME()",
                            new { Vid = it.VariationId.Value, Active = CommerceConstants.ReservationStatus.Active }, tx);
                    }
                    else
                    {
                        onHand = await conn.ExecuteScalarAsync<int?>(@"
                            SELECT Quantity FROM koch.Products WITH (UPDLOCK, ROWLOCK)
                            WHERE Id=@Id", new { Id = it.ProductId.Value }, tx) ?? -1;
                        if (onHand < 0)
                        {
                            tx.Rollback();
                            return "محصول یافت نشد.";
                        }
                        held = await conn.ExecuteScalarAsync<int>(@"
                            SELECT ISNULL(SUM(Quantity),0) FROM koch.CommerceStockReservations
                            WHERE ProductId=@Pid AND VariationId IS NULL AND Status=@Active AND ExpiresAt >= SYSDATETIME()",
                            new { Pid = it.ProductId.Value, Active = CommerceConstants.ReservationStatus.Active }, tx);
                    }

                    if (onHand - held < it.Quantity)
                    {
                        tx.Rollback();
                        return "موجودی کافی نیست (بخشی از موجودی رزرو شده است).";
                    }

                    await conn.ExecuteAsync(@"
                        INSERT INTO koch.CommerceStockReservations
                            (CartId, ProductId, VariationId, Quantity, Status, ExpiresAt)
                        VALUES (@CartId, @ProductId, @VariationId, @Quantity, @Status, @ExpiresAt)",
                        new
                        {
                            CartId = cartId,
                            it.ProductId,
                            VariationId = it.VariationId.HasValue && it.VariationId.Value > 0 ? it.VariationId : null,
                            it.Quantity,
                            Status = CommerceConstants.ReservationStatus.Active,
                            ExpiresAt = expiresAt
                        }, tx);
                }

                tx.Commit();
                return null;
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }

        public async Task BindOrderAsync(int cartId, int orderId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommerceStockReservations
                SET OrderId=@OrderId
                WHERE CartId=@CartId AND Status=@Active AND OrderId IS NULL",
                new { OrderId = orderId, CartId = cartId, Active = CommerceConstants.ReservationStatus.Active });
        }

        public async Task ReleaseByCartAsync(int cartId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommerceStockReservations SET Status=@Released
                WHERE CartId=@CartId AND Status=@Active",
                new { CartId = cartId, Released = CommerceConstants.ReservationStatus.Released, Active = CommerceConstants.ReservationStatus.Active });
        }

        public async Task ReleaseByOrderAsync(int orderId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommerceStockReservations SET Status=@Released
                WHERE OrderId=@OrderId AND Status=@Active",
                new { OrderId = orderId, Released = CommerceConstants.ReservationStatus.Released, Active = CommerceConstants.ReservationStatus.Active });
        }

        public async Task<bool> HasActiveAsync(int orderId)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteScalarAsync<int>(@"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM koch.CommerceStockReservations
                    WHERE OrderId=@OrderId AND Status=@Active AND ExpiresAt >= SYSDATETIME()
                ) THEN 1 ELSE 0 END",
                new { OrderId = orderId, Active = CommerceConstants.ReservationStatus.Active });
            return n == 1;
        }

        public async Task<bool> HasCommittedAsync(int orderId)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteScalarAsync<int>(@"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM koch.CommerceStockReservations
                    WHERE OrderId=@OrderId AND Status=@Committed
                ) THEN 1 ELSE 0 END",
                new { OrderId = orderId, Committed = CommerceConstants.ReservationStatus.Committed });
            return n == 1;
        }

        public async Task MarkCommittedAsync(int orderId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommerceStockReservations SET Status=@Committed
                WHERE OrderId=@OrderId AND Status=@Active",
                new { OrderId = orderId, Committed = CommerceConstants.ReservationStatus.Committed, Active = CommerceConstants.ReservationStatus.Active });
        }
    }
}
