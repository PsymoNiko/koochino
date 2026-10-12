using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;

namespace MyShop.Infrastructure.Repository
{
    public class CommentRepository : ICommentRepository
    {
        private readonly IDbConnectionFactory _factory;

        public CommentRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<long> InsertAsync(Comment comment)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("ProductId", comment.ProductId);
            p.Add("OrderId", comment.OrderId);
            p.Add("StoreId", comment.StoreId);
            p.Add("UserId", comment.UserId);
            p.Add("ParentId", comment.ParentId);
            p.Add("Text", comment.Text);
            p.Add("Rate", comment.Rate);
            p.Add("ReplierType", comment.ReplierType);
            p.Add("StatusView", comment.StatusView);
            p.Add("CommentId", dbType: DbType.Int64, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spComments_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<long>("CommentId");
        }

        public async Task<CommentDto> GetByIdAsync(long commentId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommentDto>(
                "koch.spComments_GetById",
                new { CommentId = commentId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<CommentDto>> GetTreeFlatByStoreAsync(long storeId)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<CommentDto>(
                "koch.spComments_GetTreeByStore",
                new { StoreId = storeId },
                commandType: CommandType.StoredProcedure);
            return list.AsList();
        }

        public async Task<List<CommentDto>> GetTreeFlatByOrderAsync(long orderId)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<CommentDto>(
                "koch.spComments_GetTreeByOrder",
                new { OrderId = orderId },
                commandType: CommandType.StoredProcedure);
            return list.AsList();
        }

        public async Task<List<CommentDto>> GetTreeFlatByProductAsync(long productId)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<CommentDto>(
                "koch.spComments_GetTreeByProduct",
                new { ProductId = productId },
                commandType: CommandType.StoredProcedure);
            return list.AsList();
        }

        public async Task<bool> UpdateStatusAsync(long commentId, byte statusView, int? moderatorUserId = null, string? moderationNotes = null)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("CommentId", commentId);
            p.Add("StatusView", statusView);
            p.Add("ModeratorUserId", moderatorUserId);
            p.Add("ModerationNotes", moderationNotes ?? "");
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spComments_UpdateStatus", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> UpdateContentAsync(long commentId, string text)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("CommentId", commentId);
            p.Add("Text", text);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spComments_UpdateContent", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> HardDeleteAsync(long commentId)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("CommentId", commentId);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spComments_HardDelete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> SoftDeleteAsync(long commentId)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("CommentId", commentId);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spComments_SoftDelete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<List<CommentListItemDto>> GetListForAdminAsync(byte filterType, string searchText)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<CommentListItemDto>(
                "koch.spComments_GetListForAdmin",
                new { FilterType = filterType, SearchText = searchText ?? "" },
                commandType: CommandType.StoredProcedure);
            return list.AsList();
        }

        public async Task<List<CommentDto>> GetThreadFlatByCommentIdAsync(long commentId)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<CommentDto>(
                "koch.spComments_GetThreadByCommentId",
                new { CommentId = commentId },
                commandType: CommandType.StoredProcedure);
            return list.AsList();
        }

        public async Task<CommentCountResultDto?> GetCountByStoreAsync(int storeId)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", storeId);
            p.Add("TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("AvgRate", dbType: DbType.Double, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spComments_GetCountByStore", p, commandType: CommandType.StoredProcedure);
            var total = p.Get<int>("TotalCount");
            var avg = p.Get<double?>("AvgRate");
            return new CommentCountResultDto { TotalCount = total, AvgRate = avg };
        }

        public async Task<CommentCountResultDto?> GetCountByProductAsync(long productId)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("ProductId", productId);
            p.Add("TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("AvgRate", dbType: DbType.Double, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spComments_GetCountByProduct", p, commandType: CommandType.StoredProcedure);
            var total = p.Get<int>("TotalCount");
            var avg = p.Get<double?>("AvgRate");
            return new CommentCountResultDto { TotalCount = total, AvgRate = avg };
        }

        public async Task<long?> GetCommentIdByOrderAndUserAsync(int orderId, int userId)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("OrderId", orderId);
            p.Add("UserId", userId);
            p.Add("CommentId", dbType: DbType.Int64, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spComments_GetCommentIdByOrderAndUser", p, commandType: CommandType.StoredProcedure);
            return p.Get<long?>("CommentId");
        }
    }
}
