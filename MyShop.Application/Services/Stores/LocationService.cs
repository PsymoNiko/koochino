using MyShop.Application.Interfaces;
using MyShop.Application.Services.Stores;
using MyShop.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services
{
    public class LocationService : ILocationService
    {
        private readonly ILocationRepository _repo;

        public LocationService(ILocationRepository repo)
        {
            _repo = repo;
        }

        public Task<List<Day>> GetDaysAsync() => _repo.GetDaysAsync();
        public Task<List<Province>> GetProvincesAsync() => _repo.GetProvincesAsync();
        public Task<List<City>> GetCitiesByProvinceIdAsync(int provinceId) => _repo.GetCitiesByProvinceIdAsync(provinceId);
        public Task<List<City>> GetAllCitiesAsync() => _repo.GetAllCitiesAsync();
        public Task<List<City>> SearchCitiesAsync(string query, int take = 30) => _repo.SearchCitiesAsync(query, take);
        public Task<City> GetCityByIdAsync(int cityId) => _repo.GetCityByIdAsync(cityId);

        public Task<int> CreateProvinceAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("نام استان الزامی است.");
            return _repo.CreateProvinceAsync(name);
        }

        public Task<bool> UpdateProvinceAsync(int id, string name)
        {
            if (id <= 0) throw new ArgumentException("شناسه استان نامعتبر است.");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("نام استان الزامی است.");
            return _repo.UpdateProvinceAsync(id, name);
        }

        public Task<(bool Ok, string Error)> DeleteProvinceAsync(int id) => _repo.DeleteProvinceAsync(id);

        public Task<int> CreateCityAsync(int provinceId, string name)
        {
            if (provinceId <= 0) throw new ArgumentException("استان را انتخاب کنید.");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("نام شهر الزامی است.");
            return _repo.CreateCityAsync(provinceId, name);
        }

        public Task<bool> UpdateCityAsync(int id, int provinceId, string name)
        {
            if (id <= 0) throw new ArgumentException("شناسه شهر نامعتبر است.");
            if (provinceId <= 0) throw new ArgumentException("استان را انتخاب کنید.");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("نام شهر الزامی است.");
            return _repo.UpdateCityAsync(id, provinceId, name);
        }

        public Task<(bool Ok, string Error)> DeleteCityAsync(int id) => _repo.DeleteCityAsync(id);

        public Task<(int ProvincesAdded, int CitiesAdded)> EnsureIranLocationsSeededAsync()
            => _repo.EnsureIranLocationsSeededAsync(IranLocationSeedData.ProvincesWithCities);
    }
}
