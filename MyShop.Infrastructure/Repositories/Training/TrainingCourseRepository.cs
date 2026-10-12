using Dapper;
using MyShop.Application.Interfaces.Training;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Training;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class TrainingCourseRepository : ITrainingCourseRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public TrainingCourseRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<TrainingCourseDto> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<TrainingCourseDto>(
                "koch.spTrainingCourses_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<TrainingCourseDto>> GetAllAsync(int? storeId = null, int? companyId = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<TrainingCourseDto>(
                "koch.spTrainingCourses_List",
                new { StoreId = storeId, CompanyId = companyId },
                commandType: CommandType.StoredProcedure
            );
            return list?.ToList() ?? new List<TrainingCourseDto>();
        }

        public async Task<int> InsertAsync(TrainingCourse entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", entity.StoreId);
            p.Add("CompanyId", entity.CompanyId);
            p.Add("Code", entity.Code);
            p.Add("Title", entity.Title);
            p.Add("Description", entity.Description);
            p.Add("DurationHours", entity.DurationHours);
            p.Add("StartDate", entity.StartDate);
            p.Add("EndDate", entity.EndDate);
            p.Add("MaxParticipants", entity.MaxParticipants);
            p.Add("Location", entity.Location);
            p.Add("InstructorName", entity.InstructorName);
            p.Add("ImageUrl", entity.ImageUrl);
            p.Add("IsActive", entity.IsActive);
            p.Add("SortOrder", entity.SortOrder);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spTrainingCourses_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(TrainingCourse entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", entity.Id);
            p.Add("StoreId", entity.StoreId);
            p.Add("CompanyId", entity.CompanyId);
            p.Add("Code", entity.Code);
            p.Add("Title", entity.Title);
            p.Add("Description", entity.Description);
            p.Add("DurationHours", entity.DurationHours);
            p.Add("StartDate", entity.StartDate);
            p.Add("EndDate", entity.EndDate);
            p.Add("MaxParticipants", entity.MaxParticipants);
            p.Add("Location", entity.Location);
            p.Add("InstructorName", entity.InstructorName);
            p.Add("ImageUrl", entity.ImageUrl);
            p.Add("IsActive", entity.IsActive);
            p.Add("SortOrder", entity.SortOrder);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spTrainingCourses_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spTrainingCourses_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
