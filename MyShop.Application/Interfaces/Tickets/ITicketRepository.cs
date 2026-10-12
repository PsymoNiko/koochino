using MyShop.Core.Models.Tickets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Tickets
{
    public interface ITicketRepository
    {
        Task<TicketDto> GetByIdAsync(int id, int siteId);
        Task<List<TicketDto>> GetAllAsync(int siteId, int? departmentId = null, string status = null);
        Task<List<TicketDto>> GetByUserIdAsync(int userId, int siteId);
        Task<List<TicketDepartmentItemDto>> GetDepartmentsAsync();
        Task<List<TicketDepartmentDto>> GetAllDepartmentsAsync(bool? isActive = null);
        Task<TicketDepartmentDto> GetDepartmentByIdAsync(int id);
        Task<int> InsertDepartmentAsync(TicketDepartment entity);
        Task<bool> UpdateDepartmentAsync(TicketDepartment entity);
        Task<bool> DeleteDepartmentAsync(int id);
        Task<int> InsertAsync(Ticket entity);
        Task<bool> UpdateAsync(Ticket entity);
        Task<bool> DeleteAsync(int id, int siteId);

        Task<int> InsertReplyAsync(TicketReply entity, int siteId);
        Task<List<TicketReplyDto>> GetRepliesByTicketIdAsync(int ticketId, int siteId);
        Task<int> InsertAttachmentAsync(TicketAttachment entity, int siteId);
        Task<List<TicketAttachmentDto>> GetAttachmentsByTicketIdAsync(int ticketId, int siteId);
        Task<TicketStatsDto> GetStatsAsync(int siteId);
        Task<bool> UpdateStatusAsync(int ticketId, string status, int siteId);
        Task<TicketDto> GetOpenByOrderIdAsync(int orderId, int siteId);
    }
}
