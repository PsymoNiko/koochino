using MyShop.Core.Models.Reports;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Reports
{
    public interface IReportDefinitionService
    {
        Task<ReportDefinitionDto> GetByIdAsync(int id);
        Task<List<ReportDefinitionDto>> GetAllAsync();
        Task<int> CreateAsync(CreateReportDefinitionDto dto);
        Task<bool> UpdateAsync(UpdateReportDefinitionDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
