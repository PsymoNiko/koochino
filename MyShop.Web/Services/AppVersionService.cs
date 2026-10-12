using Microsoft.AspNetCore.Hosting;
using MyShop.Application.Interfaces.Settings;
using MyShop.Core.Models.Settings;
using MyShop.Infrastructure.Repositories.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class AppVersionService : IAppVersionService
    {
        private readonly AppVersionRepository _repo;
        private readonly string _manifestPath;
        private AppVersionManifest _cached;

        public AppVersionService(AppVersionRepository repo, IWebHostEnvironment env)
        {
            _repo = repo;
            _manifestPath = Path.Combine(env.WebRootPath ?? env.ContentRootPath, "app-version.json");
        }

        public AppVersionManifest GetManifest()
        {
            if (_cached != null) return _cached;
            try
            {
                if (File.Exists(_manifestPath))
                {
                    var json = File.ReadAllText(_manifestPath);
                    _cached = JsonSerializer.Deserialize<AppVersionManifest>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new AppVersionManifest();
                }
                else _cached = new AppVersionManifest();
            }
            catch { _cached = new AppVersionManifest(); }

            if (string.IsNullOrWhiteSpace(_cached.Version)) _cached.Version = "2.1.0";
            if (string.IsNullOrWhiteSpace(_cached.BuildDate))
                _cached.BuildDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return _cached;
        }

        public Task<List<PlatformReleaseNote>> ListReleaseNotesAsync(int take = 30) => _repo.ListAsync(take);
        public Task<int> AddReleaseNoteAsync(PlatformReleaseNote note) => _repo.InsertAsync(note);
        public Task DeleteReleaseNoteAsync(int id) => _repo.DeleteAsync(id);
    }
}
