using MyShop.Core.Models.Reports;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Reports
{
    public interface IReportDefinitionRepository
    {
        Task<ReportDefinitionDto> GetByIdAsync(int id);
        Task<List<ReportDefinitionDto>> GetAllAsync();
        Task<int> InsertAsync(ReportDefinition entity);
        Task<bool> UpdateAsync(ReportDefinition entity);
        Task<bool> DeleteAsync(int id);
    }
}
