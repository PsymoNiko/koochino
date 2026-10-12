using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Data
{
    public class LocationRepository : ILocationRepository
    {
        private readonly IDbConnectionFactory _factory;

        public LocationRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<List<Day>> GetDaysAsync()
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<Day>("SELECT [Id], [Name], [SortOrder] FROM [koch].[Days] ORDER BY [SortOrder]");
            return list.AsList();
        }

        public async Task<List<Province>> GetProvincesAsync()
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<Province>("SELECT [Id], [Name] FROM [koch].[Provinces] ORDER BY [Name]");
            return list.AsList();
        }

        public async Task<List<City>> GetCitiesByProvinceIdAsync(int provinceId)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<City>(
                @"SELECT c.[Id], c.[ProvinceId], c.[Name], p.[Name] AS ProvinceName
                  FROM [koch].[Cities] c
                  INNER JOIN [koch].[Provinces] p ON p.[Id] = c.[ProvinceId]
                  WHERE c.[ProvinceId] = @ProvinceId
                  ORDER BY c.[Name]",
                new { ProvinceId = provinceId });
            return list.AsList();
        }

        public async Task<List<City>> GetAllCitiesAsync()
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<City>(
                @"SELECT c.[Id], c.[ProvinceId], c.[Name], p.[Name] AS ProvinceName
                  FROM [koch].[Cities] c
                  INNER JOIN [koch].[Provinces] p ON p.[Id] = c.[ProvinceId]
                  ORDER BY c.[Name]");
            return list.AsList();
        }

        public async Task<List<City>> SearchCitiesAsync(string query, int take = 30)
        {
            using var conn = _factory.CreateConnection();
            var q = (query ?? "").Trim();
            if (string.IsNullOrEmpty(q))
            {
                var top = await conn.QueryAsync<City>(
                    @"SELECT TOP (@Take) c.[Id], c.[ProvinceId], c.[Name], p.[Name] AS ProvinceName
                      FROM [koch].[Cities] c
                      INNER JOIN [koch].[Provinces] p ON p.[Id] = c.[ProvinceId]
                      ORDER BY c.[Name]",
                    new { Take = take <= 0 ? 30 : take });
                return top.AsList();
            }

            var list = await conn.QueryAsync<City>(
                @"SELECT TOP (@Take) c.[Id], c.[ProvinceId], c.[Name], p.[Name] AS ProvinceName
                  FROM [koch].[Cities] c
                  INNER JOIN [koch].[Provinces] p ON p.[Id] = c.[ProvinceId]
                  WHERE c.[Name] LIKE @Pattern OR p.[Name] LIKE @Pattern
                  ORDER BY
                    CASE WHEN c.[Name] LIKE @Starts THEN 0 ELSE 1 END,
                    c.[Name]",
                new { Take = take <= 0 ? 30 : take, Pattern = "%" + q + "%", Starts = q + "%" });
            return list.AsList();
        }

        public async Task<City> GetCityByIdAsync(int cityId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<City>(
                @"SELECT c.[Id], c.[ProvinceId], c.[Name], p.[Name] AS ProvinceName
                  FROM [koch].[Cities] c
                  LEFT JOIN [koch].[Provinces] p ON p.[Id] = c.[ProvinceId]
                  WHERE c.[Id] = @Id",
                new { Id = cityId });
        }

        public async Task<int> CreateProvinceAsync(string name)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(
                @"INSERT INTO [koch].[Provinces] ([Name]) OUTPUT INSERTED.[Id] VALUES (@Name)",
                new { Name = name.Trim() });
        }

        public async Task<bool> UpdateProvinceAsync(int id, string name)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteAsync(
                "UPDATE [koch].[Provinces] SET [Name] = @Name WHERE [Id] = @Id",
                new { Id = id, Name = name.Trim() });
            return n > 0;
        }

        public async Task<(bool Ok, string Error)> DeleteProvinceAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var cityCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM [koch].[Cities] WHERE [ProvinceId] = @Id", new { Id = id });
            if (cityCount > 0)
                return (false, $"این استان {cityCount} شهر دارد. ابتدا شهرها را حذف کنید.");

            var n = await conn.ExecuteAsync("DELETE FROM [koch].[Provinces] WHERE [Id] = @Id", new { Id = id });
            return n > 0 ? (true, null) : (false, "استان یافت نشد.");
        }

        public async Task<int> CreateCityAsync(int provinceId, string name)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(
                @"INSERT INTO [koch].[Cities] ([ProvinceId], [Name]) OUTPUT INSERTED.[Id]
                  VALUES (@ProvinceId, @Name)",
                new { ProvinceId = provinceId, Name = name.Trim() });
        }

        public async Task<bool> UpdateCityAsync(int id, int provinceId, string name)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteAsync(
                "UPDATE [koch].[Cities] SET [ProvinceId] = @ProvinceId, [Name] = @Name WHERE [Id] = @Id",
                new { Id = id, ProvinceId = provinceId, Name = name.Trim() });
            return n > 0;
        }

        public async Task<(bool Ok, string Error)> DeleteCityAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var storeCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM [koch].[Stores] WHERE [StoreCity] = @Id", new { Id = id });
            if (storeCount > 0)
                return (false, $"این شهر به {storeCount} فروشگاه متصل است و قابل حذف نیست.");

            var n = await conn.ExecuteAsync("DELETE FROM [koch].[Cities] WHERE [Id] = @Id", new { Id = id });
            return n > 0 ? (true, null) : (false, "شهر یافت نشد.");
        }

        public async Task<(int ProvincesAdded, int CitiesAdded)> EnsureIranLocationsSeededAsync(
            IReadOnlyDictionary<string, string[]> seed)
        {
            if (seed == null || seed.Count == 0) return (0, 0);

            using var conn = _factory.CreateConnection();
            var existingProvinces = (await conn.QueryAsync<(int Id, string Name)>(
                "SELECT [Id], [Name] FROM [koch].[Provinces]")).AsList();
            var provinceByName = existingProvinces
                .GroupBy(p => p.Name?.Trim() ?? "", System.StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First().Id, System.StringComparer.Ordinal);

            var existingCities = (await conn.QueryAsync<(int ProvinceId, string Name)>(
                "SELECT [ProvinceId], [Name] FROM [koch].[Cities]")).AsList();
            var cityKeys = new HashSet<string>(
                existingCities.Select(c => CityKey(c.ProvinceId, c.Name)),
                System.StringComparer.Ordinal);

            int provincesAdded = 0, citiesAdded = 0;

            foreach (var kv in seed)
            {
                var provinceName = (kv.Key ?? "").Trim();
                if (string.IsNullOrEmpty(provinceName)) continue;

                if (!provinceByName.TryGetValue(provinceName, out var provinceId))
                {
                    provinceId = await conn.ExecuteScalarAsync<int>(
                        @"INSERT INTO [koch].[Provinces] ([Name]) OUTPUT INSERTED.[Id] VALUES (@Name)",
                        new { Name = provinceName });
                    provinceByName[provinceName] = provinceId;
                    provincesAdded++;
                }

                foreach (var rawCity in kv.Value ?? System.Array.Empty<string>())
                {
                    var cityName = (rawCity ?? "").Trim();
                    if (string.IsNullOrEmpty(cityName)) continue;
                    var key = CityKey(provinceId, cityName);
                    if (cityKeys.Contains(key)) continue;

                    await conn.ExecuteAsync(
                        @"INSERT INTO [koch].[Cities] ([ProvinceId], [Name]) VALUES (@ProvinceId, @Name)",
                        new { ProvinceId = provinceId, Name = cityName });
                    cityKeys.Add(key);
                    citiesAdded++;
                }
            }

            return (provincesAdded, citiesAdded);
        }

        private static string CityKey(int provinceId, string name)
            => provinceId + "|" + (name ?? "").Trim();
    }
}
