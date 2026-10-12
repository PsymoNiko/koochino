using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface ILocationRepository
    {
        Task<List<Day>> GetDaysAsync();
        Task<List<Province>> GetProvincesAsync();
        Task<List<City>> GetCitiesByProvinceIdAsync(int provinceId);
        Task<List<City>> GetAllCitiesAsync();
        Task<List<City>> SearchCitiesAsync(string query, int take = 30);
        Task<City> GetCityByIdAsync(int cityId);

        Task<int> CreateProvinceAsync(string name);
        Task<bool> UpdateProvinceAsync(int id, string name);
        Task<(bool Ok, string Error)> DeleteProvinceAsync(int id);

        Task<int> CreateCityAsync(int provinceId, string name);
        Task<bool> UpdateCityAsync(int id, int provinceId, string name);
        Task<(bool Ok, string Error)> DeleteCityAsync(int id);

        /// <summary>
        /// استان‌ها و شهرهای پیش‌فرض ایران را در صورت نبود اضافه می‌کند (بدون حذف دادهٔ موجود).
        /// </summary>
        Task<(int ProvincesAdded, int CitiesAdded)> EnsureIranLocationsSeededAsync(IReadOnlyDictionary<string, string[]> seed);
    }
}
