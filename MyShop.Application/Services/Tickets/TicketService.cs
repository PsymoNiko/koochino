using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Security;
using MyShop.Application.Interfaces.Tickets;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Tickets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Tickets
{
    public class TicketService : ITicketService
    {
        public const string OrderReviewDepartmentName = "بررسی سفارش";

        private readonly ITicketRepository _repo;
        private readonly IOrderService _orderService;
        private readonly IRequestAuditContext _audit;
        private readonly ICurrentTenant _tenant;

        public TicketService(ITicketRepository repo, IOrderService orderService, IRequestAuditContext audit, ICurrentTenant tenant)
        {
            _repo = repo;
            _orderService = orderService;
            _audit = audit;
            _tenant = tenant;
        }

        public Task<TicketDto> GetByIdAsync(int id)
        {
            var siteId = CurrentSiteId();
            return siteId.HasValue ? _repo.GetByIdAsync(id, siteId.Value) : Task.FromResult<TicketDto>(null);
        }
        public Task<List<TicketDto>> GetAllAsync(int? departmentId = null, string status = null)
        {
            var siteId = CurrentSiteId();
            return siteId.HasValue ? _repo.GetAllAsync(siteId.Value, departmentId, status) : Task.FromResult(new List<TicketDto>());
        }
        public Task<List<TicketDto>> GetByUserIdAsync(int userId)
        {
            var siteId = CurrentSiteId();
            return siteId.HasValue ? _repo.GetByUserIdAsync(userId, siteId.Value) : Task.FromResult(new List<TicketDto>());
        }
        public Task<List<TicketDepartmentItemDto>> GetDepartmentsAsync() => _repo.GetDepartmentsAsync();
        public Task<List<TicketDepartmentDto>> GetAllDepartmentsAsync(bool? isActive = null) => _repo.GetAllDepartmentsAsync(isActive);
        public Task<TicketDepartmentDto> GetDepartmentByIdAsync(int id) => _repo.GetDepartmentByIdAsync(id);
        public Task<TicketStatsDto> GetStatsAsync()
        {
            var siteId = CurrentSiteId();
            return siteId.HasValue ? _repo.GetStatsAsync(siteId.Value) : Task.FromResult(new TicketStatsDto());
        }
        public Task<TicketDto> GetOpenByOrderIdAsync(int orderId)
        {
            var siteId = CurrentSiteId();
            return siteId.HasValue ? _repo.GetOpenByOrderIdAsync(orderId, siteId.Value) : Task.FromResult<TicketDto>(null);
        }

        public async Task<TicketDetailDto> GetDetailAsync(int ticketId)
        {
            var siteId = CurrentSiteId();
            if (!siteId.HasValue) return null;
            var ticket = await _repo.GetByIdAsync(ticketId, siteId.Value);
            if (ticket == null) return null;
            if (!CanReadTicket(ticket)) return null;

            var replies = await _repo.GetRepliesByTicketIdAsync(ticketId, siteId.Value) ?? new List<TicketReplyDto>();
            var attachments = await _repo.GetAttachmentsByTicketIdAsync(ticketId, siteId.Value) ?? new List<TicketAttachmentDto>();

            foreach (var reply in replies)
                reply.Attachments = attachments.Where(a => a.ReplyId == reply.Id).ToList();

            return new TicketDetailDto
            {
                Ticket = ticket,
                Replies = replies,
                Attachments = attachments.Where(a => a.ReplyId == null).ToList()
            };
        }

        public async Task<int> CreateDepartmentAsync(CreateTicketDepartmentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Name))
                return 0;

            var entity = new TicketDepartment
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                Email = dto.Email,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                CreatedAt = DateTime.UtcNow
            };
            return await _repo.InsertDepartmentAsync(entity);
        }

        public async Task<bool> UpdateDepartmentAsync(UpdateTicketDepartmentDto dto)
        {
            var existing = await _repo.GetDepartmentByIdAsync(dto.Id);
            if (existing == null || string.IsNullOrWhiteSpace(dto.Name))
                return false;

            return await _repo.UpdateDepartmentAsync(new TicketDepartment
            {
                Id = dto.Id,
                Name = dto.Name.Trim(),
                Description = dto.Description,
                Email = dto.Email,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder
            });
        }

        public Task<bool> DeleteDepartmentAsync(int id) => _repo.DeleteDepartmentAsync(id);

        public async Task<int> CreateAsync(CreateTicketDto dto)
        {
            var siteId = CurrentSiteId();
            if (!siteId.HasValue) return 0;
            var ticketNo = string.IsNullOrWhiteSpace(dto.TicketNo)
                ? $"TKT-{DateTime.Now:yyyyMMddHHmmss}"
                : dto.TicketNo.Trim();

            var message = dto.Resolution;
            var entity = new Ticket
            {
                SiteId = siteId.Value,
                DepartmentId = dto.DepartmentId,
                TicketNo = ticketNo,
                UserId = dto.UserId,
                ContactName = dto.ContactName,
                ContactEmail = dto.ContactEmail,
                ContactPhone = dto.ContactPhone,
                Subject = dto.Subject ?? "",
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Open" : dto.Status,
                Priority = dto.Priority,
                AssignedTo = dto.AssignedTo,
                Resolution = null,
                OrderId = dto.OrderId,
                CreatedAt = DateTime.UtcNow
            };

            var ticketId = await _repo.InsertAsync(entity);
            if (ticketId <= 0) return 0;

            if (!string.IsNullOrWhiteSpace(message))
            {
                await _repo.InsertReplyAsync(new TicketReply
                {
                    TicketId = ticketId,
                    UserId = dto.UserId,
                    Content = message.Trim(),
                    IsStaffReply = false,
                    CreatedAt = DateTime.UtcNow
                }, siteId.Value);
            }

            return ticketId;
        }

        public async Task<OrderDisputeResult> CreateOrderDisputeAsync(CreateTicketDto dto)
        {
            if (dto == null || !dto.OrderId.HasValue || dto.OrderId <= 0)
                return Fail("سفارش مشخص نشده است.");

            if (!dto.UserId.HasValue || dto.UserId <= 0)
                return Fail("کاربر نامعتبر است.");

            if (string.IsNullOrWhiteSpace(dto.Resolution))
                return Fail("متن درخواست بررسی را وارد کنید.");

            var order = await _orderService.GetByIdAsync(dto.OrderId.Value);
            if (order == null)
                return Fail("سفارش یافت نشد.");

            if (order.UserId != dto.UserId.Value)
                return Fail("این سفارش متعلق به شما نیست.");

            if (order.Status != 2)
                return Fail("فقط برای سفارش‌های ردشده می‌توان درخواست بررسی ثبت کرد.");

            var siteId = CurrentSiteId();
            if (!siteId.HasValue) return Fail("سایت جاری مشخص نیست.");
            var existing = await _repo.GetOpenByOrderIdAsync(order.Id, siteId.Value);
            if (existing != null)
            {
                return new OrderDisputeResult
                {
                    Success = false,
                    TicketId = existing.Id,
                    Message = "برای این سفارش قبلاً درخواست بررسی باز ثبت شده است."
                };
            }

            var departments = await _repo.GetDepartmentsAsync() ?? new List<TicketDepartmentItemDto>();
            var dept = departments.FirstOrDefault(d =>
                string.Equals(d.Name, OrderReviewDepartmentName, StringComparison.OrdinalIgnoreCase));

            if (dept == null)
            {
                var deptId = await _repo.InsertDepartmentAsync(new TicketDepartment
                {
                    Name = OrderReviewDepartmentName,
                    Description = "اعتراض مشتری به سفارش‌های ردشده توسط فروشنده",
                    IsActive = true,
                    SortOrder = 10,
                    CreatedAt = DateTime.UtcNow
                });
                if (deptId <= 0)
                    return Fail("دپارتمان بررسی سفارش در سیستم تعریف نشده است.");
                dto.DepartmentId = deptId;
            }
            else
            {
                dto.DepartmentId = dept.Id;
            }

            var orderLabel = string.IsNullOrWhiteSpace(order.OrderNumber) ? $"#{order.Id}" : order.OrderNumber;
            dto.Subject = string.IsNullOrWhiteSpace(dto.Subject)
                ? $"درخواست بررسی سفارش {orderLabel}"
                : dto.Subject.Trim();
            dto.Status = "Open";
            if (string.IsNullOrWhiteSpace(dto.Priority))
                dto.Priority = "زیاد";
            dto.OrderId = order.Id;

            var ticketId = await CreateAsync(dto);
            if (ticketId <= 0)
                return Fail("خطا در ثبت درخواست بررسی.");

            return new OrderDisputeResult
            {
                Success = true,
                TicketId = ticketId,
                Message = "درخواست بررسی با موفقیت ثبت شد."
            };
        }

        private static OrderDisputeResult Fail(string message) =>
            new OrderDisputeResult { Success = false, TicketId = 0, Message = message };

        public async Task<bool> UpdateAsync(UpdateTicketDto dto)
        {
            var siteId = CurrentSiteId();
            if (!siteId.HasValue) return false;
            var existing = await _repo.GetByIdAsync(dto.Id, siteId.Value);
            if (existing == null) return false;
            return await _repo.UpdateAsync(new Ticket
            {
                Id = dto.Id,
                SiteId = siteId.Value,
                DepartmentId = dto.DepartmentId,
                TicketNo = dto.TicketNo,
                UserId = dto.UserId,
                ContactName = dto.ContactName,
                ContactEmail = dto.ContactEmail,
                ContactPhone = dto.ContactPhone,
                Subject = dto.Subject ?? "",
                Status = dto.Status ?? "Open",
                Priority = dto.Priority,
                AssignedTo = dto.AssignedTo,
                Resolution = dto.Resolution,
                OrderId = existing.OrderId,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public Task<bool> DeleteAsync(int id)
        {
            var siteId = CurrentSiteId();
            return siteId.HasValue ? _repo.DeleteAsync(id, siteId.Value) : Task.FromResult(false);
        }

        public async Task<int> AddReplyAsync(CreateTicketReplyDto dto)
        {
            if (dto == null || dto.TicketId <= 0 || string.IsNullOrWhiteSpace(dto.Content))
                return 0;

            var siteId = CurrentSiteId();
            if (!siteId.HasValue) return 0;
            var ticket = await _repo.GetByIdAsync(dto.TicketId, siteId.Value);
            if (ticket == null) return 0;
            if (!CanWriteReply(ticket, dto.IsStaffReply, dto.UserId))
                return 0;

            if (string.Equals(ticket.Status, "Closed", StringComparison.OrdinalIgnoreCase))
                return 0;

            var replyId = await _repo.InsertReplyAsync(new TicketReply
            {
                TicketId = dto.TicketId,
                UserId = dto.UserId,
                Content = dto.Content.Trim(),
                IsStaffReply = dto.IsStaffReply,
                CreatedAt = DateTime.UtcNow
            }, siteId.Value);

            if (replyId <= 0) return 0;

            if (dto.IsStaffReply)
            {
                var newStatus = dto.CloseTicket ? "Closed" : "Answered";
                await _repo.UpdateStatusAsync(dto.TicketId, newStatus, siteId.Value);
            }
            else
            {
                await _repo.UpdateStatusAsync(dto.TicketId, "Open", siteId.Value);
            }

            return replyId;
        }

        public async Task<int> AddAttachmentAsync(int ticketId, int? replyId, string fileUrl, string fileName, int? fileSize, int? uploadedBy)
        {
            if (ticketId <= 0 || string.IsNullOrWhiteSpace(fileUrl))
                return 0;
            var siteId = CurrentSiteId();
            if (!siteId.HasValue) return 0;
            var ticket = await _repo.GetByIdAsync(ticketId, siteId.Value);
            if (ticket == null || !CanReadTicket(ticket))
                return 0;

            return await _repo.InsertAttachmentAsync(new TicketAttachment
            {
                TicketId = ticketId,
                ReplyId = replyId,
                FileUrl = fileUrl,
                FileName = fileName,
                FileSize = fileSize,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow
            }, siteId.Value);
        }

        public async Task<bool> CloseTicketAsync(int ticketId, int? userId = null)
        {
            var siteId = CurrentSiteId();
            if (!siteId.HasValue) return false;
            var ticket = await _repo.GetByIdAsync(ticketId, siteId.Value);
            if (ticket == null) return false;

            if (userId.HasValue)
            {
                if (!ticket.UserId.HasValue || ticket.UserId.Value != userId.Value)
                    return false;
            }

            if (string.Equals(ticket.Status, "Closed", StringComparison.OrdinalIgnoreCase))
                return true;

            return await _repo.UpdateStatusAsync(ticketId, "Closed", siteId.Value);
        }

        bool CanReadTicket(TicketDto ticket)
        {
            if (ticket == null) return false;
            if (_audit == null) return true;
            if (_audit.IsAdmin) return true;
            return _audit.UserId.HasValue && ticket.UserId.HasValue && ticket.UserId.Value == _audit.UserId.Value;
        }

        bool CanWriteReply(TicketDto ticket, bool isStaff, int? dtoUserId)
        {
            if (_audit == null) return true;
            if (isStaff)
                return _audit.IsAdmin;
            if (!_audit.UserId.HasValue || !ticket.UserId.HasValue)
                return false;
            if (ticket.UserId.Value != _audit.UserId.Value)
                return false;
            if (dtoUserId.HasValue && dtoUserId.Value != _audit.UserId.Value)
                return false;
            return true;
        }

        int? CurrentSiteId() => SiteScope.CurrentSiteId(_tenant);
    }
}
