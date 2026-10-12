using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IBaleBotFeedbackRepository
    {
        Task<int> AddAsync(long baleUserId, string? username, string? firstName, string? lastName, string choice);
        Task<List<BaleBotFeedbackDto>> GetAllAsync();
    }
}
