using Dapper;
using MyShop.Application.Interfaces.Tickets;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Tickets;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class TicketRepository : ITicketRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public TicketRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<TicketDto> GetByIdAsync(int id, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<TicketDto>(
                "koch.spTickets_GetById",
                new { Id = id, SiteId = siteId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<TicketDto>> GetAllAsync(int siteId, int? departmentId = null, string status = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<TicketDto>(
                "koch.spTickets_List",
                new { SiteId = siteId, DepartmentId = departmentId, Status = status },
                commandType: CommandType.StoredProcedure
            );
            return list?.ToList() ?? new List<TicketDto>();
        }

        public async Task<List<TicketDto>> GetByUserIdAsync(int userId, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<TicketDto>(
                "koch.spTickets_ListByUserId",
                new { UserId = userId, SiteId = siteId },
                commandType: CommandType.StoredProcedure
            );
            return list?.ToList() ?? new List<TicketDto>();
        }

        public async Task<List<TicketDepartmentItemDto>> GetDepartmentsAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<TicketDepartmentItemDto>(
                "koch.spTicketDepartments_List",
                commandType: CommandType.StoredProcedure
            );
            return list?.ToList() ?? new List<TicketDepartmentItemDto>();
        }

        public async Task<List<TicketDepartmentDto>> GetAllDepartmentsAsync(bool? isActive = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<TicketDepartmentDto>(
                "koch.spTicketDepartments_ListAll",
                new { IsActive = isActive },
                commandType: CommandType.StoredProcedure
            );
            return list?.ToList() ?? new List<TicketDepartmentDto>();
        }

        public async Task<TicketDepartmentDto> GetDepartmentByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<TicketDepartmentDto>(
                "koch.spTicketDepartments_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> InsertDepartmentAsync(TicketDepartment entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Name", entity.Name);
            p.Add("Description", entity.Description);
            p.Add("Email", entity.Email);
            p.Add("IsActive", entity.IsActive);
            p.Add("SortOrder", entity.SortOrder);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spTicketDepartments_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateDepartmentAsync(TicketDepartment entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", entity.Id);
            p.Add("Name", entity.Name);
            p.Add("Description", entity.Description);
            p.Add("Email", entity.Email);
            p.Add("IsActive", entity.IsActive);
            p.Add("SortOrder", entity.SortOrder);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spTicketDepartments_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteDepartmentAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spTicketDepartments_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<int> InsertAsync(Ticket entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                var p = new DynamicParameters();
                p.Add("SiteId", entity.SiteId);
                p.Add("DepartmentId", entity.DepartmentId);
                p.Add("TicketNo", entity.TicketNo);
                p.Add("UserId", entity.UserId);
                p.Add("ContactName", entity.ContactName);
                p.Add("ContactEmail", entity.ContactEmail);
                p.Add("ContactPhone", entity.ContactPhone);
                p.Add("Subject", entity.Subject);
                p.Add("Status", entity.Status);
                p.Add("Priority", entity.Priority);
                p.Add("AssignedTo", entity.AssignedTo);
                p.Add("Resolution", entity.Resolution);
                p.Add("OrderId", entity.OrderId);
                p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync("koch.spTickets_Insert", p, commandType: CommandType.StoredProcedure);
                return p.Get<int>("Id");
            }
            catch
            {
                // اگر SP هنوز OrderId ندارد، با کوئری پارامتری درج کن (ستون OrderId باید موجود باشد)
                var id = await conn.QuerySingleAsync<int>(@"
                    INSERT INTO koch.Tickets (
                        SiteId, DepartmentId, TicketNo, UserId, ContactName, ContactEmail, ContactPhone,
                        Subject, Status, Priority, AssignedTo, CreatedAt, Resolution, OrderId
                    )
                    OUTPUT INSERTED.Id
                    VALUES (
                        @SiteId, @DepartmentId, @TicketNo, @UserId, @ContactName, @ContactEmail, @ContactPhone,
                        @Subject, @Status, @Priority, @AssignedTo, SYSDATETIME(), @Resolution, @OrderId
                    )",
                    new
                    {
                        entity.SiteId,
                        entity.DepartmentId,
                        entity.TicketNo,
                        entity.UserId,
                        entity.ContactName,
                        entity.ContactEmail,
                        entity.ContactPhone,
                        entity.Subject,
                        entity.Status,
                        entity.Priority,
                        entity.AssignedTo,
                        entity.Resolution,
                        entity.OrderId
                    });
                return id;
            }
        }

        public async Task<TicketDto> GetOpenByOrderIdAsync(int orderId, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                return await conn.QueryFirstOrDefaultAsync<TicketDto>(
                    "koch.spTickets_GetOpenByOrderId",
                    new { OrderId = orderId, SiteId = siteId },
                    commandType: CommandType.StoredProcedure
                );
            }
            catch
            {
                return await conn.QueryFirstOrDefaultAsync<TicketDto>(@"
                    SELECT TOP 1
                        t.Id, t.SiteId, t.DepartmentId, d.Name AS DepartmentName,
                        t.TicketNo, t.UserId, t.ContactName, t.ContactEmail, t.ContactPhone,
                        t.Subject, t.Status, t.Priority, t.AssignedTo,
                        t.CreatedAt, t.UpdatedAt, t.ClosedAt, t.ClosedBy, t.Resolution,
                        t.OrderId
                    FROM koch.Tickets t
                    LEFT JOIN koch.TicketDepartments d ON t.DepartmentId = d.Id
                    WHERE t.OrderId = @OrderId AND t.SiteId = @SiteId AND t.Status <> N'Closed'
                    ORDER BY t.CreatedAt DESC",
                    new { OrderId = orderId, SiteId = siteId });
            }
        }

        public async Task<bool> UpdateAsync(Ticket entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", entity.Id);
            p.Add("SiteId", entity.SiteId);
            p.Add("DepartmentId", entity.DepartmentId);
            p.Add("TicketNo", entity.TicketNo);
            p.Add("UserId", entity.UserId);
            p.Add("ContactName", entity.ContactName);
            p.Add("ContactEmail", entity.ContactEmail);
            p.Add("ContactPhone", entity.ContactPhone);
            p.Add("Subject", entity.Subject);
            p.Add("Status", entity.Status);
            p.Add("Priority", entity.Priority);
            p.Add("AssignedTo", entity.AssignedTo);
            p.Add("Resolution", entity.Resolution);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spTickets_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("SiteId", siteId);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spTickets_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<int> InsertReplyAsync(TicketReply entity, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("TicketId", entity.TicketId);
            p.Add("SiteId", siteId);
            p.Add("UserId", entity.UserId);
            p.Add("Content", entity.Content);
            p.Add("IsStaffReply", entity.IsStaffReply);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spTicketReplies_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<List<TicketReplyDto>> GetRepliesByTicketIdAsync(int ticketId, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<TicketReplyDto>(
                "koch.spTicketReplies_ListByTicketId",
                new { TicketId = ticketId, SiteId = siteId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<TicketReplyDto>();
        }

        public async Task<int> InsertAttachmentAsync(TicketAttachment entity, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("TicketId", entity.TicketId);
            p.Add("SiteId", siteId);
            p.Add("ReplyId", entity.ReplyId);
            p.Add("FileUrl", entity.FileUrl);
            p.Add("FileName", entity.FileName);
            p.Add("FileSize", entity.FileSize);
            p.Add("UploadedBy", entity.UploadedBy);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spTicketAttachments_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<List<TicketAttachmentDto>> GetAttachmentsByTicketIdAsync(int ticketId, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<TicketAttachmentDto>(
                "koch.spTicketAttachments_ListByTicketId",
                new { TicketId = ticketId, SiteId = siteId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<TicketAttachmentDto>();
        }

        public async Task<TicketStatsDto> GetStatsAsync(int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<TicketStatsDto>(
                "koch.spTickets_GetStats",
                new { SiteId = siteId },
                commandType: CommandType.StoredProcedure)
                ?? new TicketStatsDto();
        }

        public async Task<bool> UpdateStatusAsync(int ticketId, string status, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                var p = new DynamicParameters();
                p.Add("Id", ticketId);
                p.Add("SiteId", siteId);
                p.Add("Status", status);
                p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync("koch.spTickets_UpdateStatus", p, commandType: CommandType.StoredProcedure);
                return p.Get<int>("Result") >= 1;
            }
            catch
            {
                // اگر SP هنوز روی دیتابیس نبود، با کوئری پارامتری به‌روزرسانی کن
                var rows = await conn.ExecuteAsync(@"
                    UPDATE koch.Tickets
                    SET Status = @Status,
                        UpdatedAt = SYSDATETIME(),
                        ClosedAt = CASE WHEN @Status = N'Closed' THEN SYSDATETIME() ELSE ClosedAt END
                    WHERE Id = @Id AND SiteId = @SiteId",
                    new { Id = ticketId, Status = status, SiteId = siteId });
                return rows >= 1;
            }
        }
    }
}
