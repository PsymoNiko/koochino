using Dapper;
using MyShop.Application.Interfaces.Inventory;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Inventory;
using MyShop.Infrastructure.Data;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public WarehouseRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

        public async Task<WarehouseDto> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<WarehouseDto>("koch.spWarehouses_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public async Task<List<WarehouseDto>> GetAllAsync(int? storeId = null, int? companyId = null, bool? isActive = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<WarehouseDto>("koch.spWarehouses_List", new { StoreId = storeId, CompanyId = companyId, IsActive = isActive }, commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<WarehouseDto>();
        }

        public async Task<int> InsertAsync(Warehouse entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", entity.StoreId); p.Add("CompanyId", entity.CompanyId); p.Add("Code", entity.Code); p.Add("Name", entity.Name);
            p.Add("Address", entity.Address); p.Add("Phone", entity.Phone); p.Add("ManagerName", entity.ManagerName);
            p.Add("IsActive", entity.IsActive); p.Add("SortOrder", entity.SortOrder);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spWarehouses_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(Warehouse entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", entity.Id); p.Add("StoreId", entity.StoreId); p.Add("CompanyId", entity.CompanyId); p.Add("Code", entity.Code); p.Add("Name", entity.Name);
            p.Add("Address", entity.Address); p.Add("Phone", entity.Phone); p.Add("ManagerName", entity.ManagerName);
            p.Add("IsActive", entity.IsActive); p.Add("SortOrder", entity.SortOrder);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spWarehouses_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            await ReferenceCount.EnsureUnusedAsync(conn, new { Id = id },
                ("SELECT COUNT(1) FROM koch.InventoryStock WHERE WarehouseId=@Id", "موجودی انبار"));
            var p = new DynamicParameters(); p.Add("Id", id); p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spWarehouses_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
