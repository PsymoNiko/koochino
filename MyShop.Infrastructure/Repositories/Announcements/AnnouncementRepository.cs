using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class AnnouncementRepository : IAnnouncementRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AnnouncementRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<AnnouncementDto> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AnnouncementDto>(
                "koch.spAnnouncements_GetById",
                new { AnnouncementId = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<AnnouncementDto>> GetAllAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            return (await conn.QueryAsync<AnnouncementDto>(
                "koch.spAnnouncements_GetAll",
                commandType: CommandType.StoredProcedure
            )).ToList();
        }

        public async Task<List<AnnouncementDto>> GetActiveForUserAsync(string userRole)
        {
            using var conn = _connectionFactory.CreateConnection();
            
            // Map role names to TargetAudience values
            int targetAudience = userRole switch
            {
                "Admin" => 0, // Admin sees all announcements
                "ShopOwner" or "StoreOwner" => 1, // ShopOwner
                "Customer" => 2, // Customer
                _ => 0
            };

            // If Admin, get all active announcements
            if (targetAudience == 0 && userRole == "Admin")
            {
                var all = await GetAllAsync();
                var now = DateTime.Now;
                return all.Where(a => 
                    a.IsActive && 
                    a.StartDateTime <= now && 
                    a.EndDateTime >= now
                ).ToList();
            }

            // Get announcements for the specific role or Both (3)
            return (await conn.QueryAsync<AnnouncementDto>(
                "koch.spAnnouncements_GetActiveForUser",
                new { 
                    UserRole = targetAudience,
                    CurrentDateTime = DateTime.Now
                },
                commandType: CommandType.StoredProcedure
            )).ToList();
        }

        public async Task<int> CreateAsync(Announcement announcement, int createdBy)
        {
            using var conn = _connectionFactory.CreateConnection();
            
            var parameters = new DynamicParameters();
            parameters.Add("Title", announcement.Title);
            parameters.Add("Message", announcement.Message);
            parameters.Add("IsActive", announcement.IsActive);
            parameters.Add("AlertColor", announcement.AlertColor);
            parameters.Add("StartDateTime", announcement.StartDateTime);
            parameters.Add("EndDateTime", announcement.EndDateTime);
            parameters.Add("TargetAudience", (int)announcement.TargetAudience);
            parameters.Add("CreatedBy", createdBy);
            parameters.Add("AnnouncementId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spAnnouncements_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("AnnouncementId");
        }

        public async Task<bool> UpdateAsync(Announcement announcement)
        {
            using var conn = _connectionFactory.CreateConnection();
            
            var parameters = new DynamicParameters();
            parameters.Add("AnnouncementId", announcement.Id);
            parameters.Add("Title", announcement.Title);
            parameters.Add("Message", announcement.Message);
            parameters.Add("IsActive", announcement.IsActive);
            parameters.Add("AlertColor", announcement.AlertColor);
            parameters.Add("StartDateTime", announcement.StartDateTime);
            parameters.Add("EndDateTime", announcement.EndDateTime);
            parameters.Add("TargetAudience", (int)announcement.TargetAudience);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spAnnouncements_Update",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            
            var parameters = new DynamicParameters();
            parameters.Add("AnnouncementId", id);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spAnnouncements_Delete",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<bool> ToggleActiveAsync(int id, bool isActive)
        {
            using var conn = _connectionFactory.CreateConnection();
            
            var parameters = new DynamicParameters();
            parameters.Add("AnnouncementId", id);
            parameters.Add("IsActive", isActive);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spAnnouncements_ToggleActive",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("Result") == 1;
        }
    }
}
