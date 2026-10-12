using MyShop.Core.Models.Tickets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Tickets
{
    public interface ITicketService
    {
        Task<TicketDto> GetByIdAsync(int id);
        Task<List<TicketDto>> GetAllAsync(int? departmentId = null, string status = null);
        Task<List<TicketDto>> GetByUserIdAsync(int userId);
        Task<TicketDetailDto> GetDetailAsync(int ticketId);
        Task<List<TicketDepartmentItemDto>> GetDepartmentsAsync();
        Task<List<TicketDepartmentDto>> GetAllDepartmentsAsync(bool? isActive = null);
        Task<TicketDepartmentDto> GetDepartmentByIdAsync(int id);
        Task<int> CreateDepartmentAsync(CreateTicketDepartmentDto dto);
        Task<bool> UpdateDepartmentAsync(UpdateTicketDepartmentDto dto);
        Task<bool> DeleteDepartmentAsync(int id);
        Task<int> CreateAsync(CreateTicketDto dto);
        Task<bool> UpdateAsync(UpdateTicketDto dto);
        Task<bool> DeleteAsync(int id);
        Task<int> AddReplyAsync(CreateTicketReplyDto dto);
        Task<int> AddAttachmentAsync(int ticketId, int? replyId, string fileUrl, string fileName, int? fileSize, int? uploadedBy);
        Task<bool> CloseTicketAsync(int ticketId, int? userId = null);
        Task<TicketStatsDto> GetStatsAsync();
        Task<TicketDto> GetOpenByOrderIdAsync(int orderId);
        Task<OrderDisputeResult> CreateOrderDisputeAsync(CreateTicketDto dto);
    }
}
