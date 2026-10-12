using MyShop.Core.Models.Training;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Training
{
    public interface ITrainingCourseRepository
    {
        Task<TrainingCourseDto> GetByIdAsync(int id);
        Task<List<TrainingCourseDto>> GetAllAsync(int? storeId = null, int? companyId = null);
        Task<int> InsertAsync(TrainingCourse entity);
        Task<bool> UpdateAsync(TrainingCourse entity);
        Task<bool> DeleteAsync(int id);
    }
}
