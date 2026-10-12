using MyShop.Core.Models.Training;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Training
{
    public interface ITrainingCourseService
    {
        Task<TrainingCourseDto> GetByIdAsync(int id);
        Task<List<TrainingCourseDto>> GetAllAsync(int? storeId = null, int? companyId = null);
        Task<int> CreateAsync(CreateTrainingCourseDto dto);
        Task<bool> UpdateAsync(UpdateTrainingCourseDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
