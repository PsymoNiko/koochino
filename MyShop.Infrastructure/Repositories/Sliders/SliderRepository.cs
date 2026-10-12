using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class SliderRepository : ISliderRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SliderRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<SliderDto>> ListAsync(string? placeKey = null, bool? isActive = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<SliderDto>(
                "koch.spSliders_List",
                new { PlaceKey = placeKey, IsActive = isActive },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<SliderDto>();
        }

        public async Task<SliderDto?> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<SliderDto>(
                "koch.spSliders_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertAsync(SliderDto dto)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Name", dto.Name);
            p.Add("PlaceKey", dto.PlaceKey);
            p.Add("Description", dto.Description);
            p.Add("IsActive", dto.IsActive);
            p.Add("SortOrder", dto.SortOrder);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spSliders_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(SliderDto dto)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id);
            p.Add("Name", dto.Name);
            p.Add("PlaceKey", dto.PlaceKey);
            p.Add("Description", dto.Description);
            p.Add("IsActive", dto.IsActive);
            p.Add("SortOrder", dto.SortOrder);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spSliders_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spSliders_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<List<SliderSlideDto>> GetSlidesBySliderIdAsync(int sliderId, bool? isActive = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<SliderSlideDto>(
                "koch.spSliderSlides_ListBySlider",
                new { SliderId = sliderId, IsActive = isActive },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<SliderSlideDto>();
        }

        public async Task<List<SliderSlideDto>> GetActiveSlidesByPlaceKeyAsync(string placeKey)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<SliderSlideDto>(
                "koch.spSliderSlides_GetActiveForPlace",
                new { PlaceKey = placeKey },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<SliderSlideDto>();
        }

        public async Task<SliderSlideDto?> GetSlideByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<SliderSlideDto>(
                "koch.spSliderSlides_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertSlideAsync(SliderSlideDto dto)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("SliderId", dto.SliderId);
            p.Add("Title", dto.Title);
            p.Add("Subtitle", dto.Subtitle);
            p.Add("Description", dto.Description);
            p.Add("ImageUrl", dto.ImageUrl);
            p.Add("ImageAltText", dto.ImageAltText);
            p.Add("LinkUrl", dto.LinkUrl);
            p.Add("LinkTarget", dto.LinkTarget ?? "_self");
            p.Add("ButtonText", dto.ButtonText);
            p.Add("SortOrder", dto.SortOrder);
            p.Add("IsActive", dto.IsActive);
            p.Add("StartDate", dto.StartDate);
            p.Add("EndDate", dto.EndDate);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spSliderSlides_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateSlideAsync(SliderSlideDto dto)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id);
            p.Add("SliderId", dto.SliderId);
            p.Add("Title", dto.Title);
            p.Add("Subtitle", dto.Subtitle);
            p.Add("Description", dto.Description);
            p.Add("ImageUrl", dto.ImageUrl);
            p.Add("ImageAltText", dto.ImageAltText);
            p.Add("LinkUrl", dto.LinkUrl);
            p.Add("LinkTarget", dto.LinkTarget ?? "_self");
            p.Add("ButtonText", dto.ButtonText);
            p.Add("SortOrder", dto.SortOrder);
            p.Add("IsActive", dto.IsActive);
            p.Add("StartDate", dto.StartDate);
            p.Add("EndDate", dto.EndDate);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spSliderSlides_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteSlideAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spSliderSlides_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
