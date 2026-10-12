using MyShop.Core.Models.Security;

namespace MyShop.Core.Security
{
    /// <summary>Shared SQL scope predicate for Dapper queries — الگوی مرجع فاز ۷.</summary>
    public static class DataScopeSqlBuilder
    {
        /// <summary>
        /// Returns SQL fragment AND (...) for store-linked tables.
        /// Expects aliases: store table as <paramref name="storeAlias"/> with UserID, SiteId, CompanyId columns.
        /// </summary>
        public static string StoreScopePredicate(string storeAlias, string scopeKindParam = "@ScopeKind",
            string scopeValuesParam = "@ScopeValues", string actorUserIdParam = "@ActorUserId")
        {
            return $@"(
                {scopeKindParam} = N'All'
                OR ({scopeKindParam} = N'Own' AND {storeAlias}.UserID = {actorUserIdParam})
                OR ({scopeKindParam} = N'Site' AND {scopeValuesParam} IS NOT NULL
                    AND {storeAlias}.SiteId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT({scopeValuesParam}, ',')))
                OR ({scopeKindParam} = N'Company' AND {scopeValuesParam} IS NOT NULL
                    AND {storeAlias}.CompanyId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT({scopeValuesParam}, ',')))
            )";
        }

        /// <summary>Scope predicate for entities with SiteId column (Academy, tenant-scoped tables).</summary>
        public static string SiteEntityScopePredicate(string alias, string scopeKindParam = "@ScopeKind",
            string scopeValuesParam = "@ScopeValues")
        {
            return $@"(
                {scopeKindParam} = N'All'
                OR ({scopeKindParam} = N'Site' AND {scopeValuesParam} IS NOT NULL
                    AND {alias}.SiteId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT({scopeValuesParam}, ',')))
            )";
        }

        public static object ScopeParameters(DataScopeQuery scope, int? storeId = null, int? warehouseId = null,
            bool? lowStockOnly = null, string status = null)
        {
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values)
                : null;
            return new
            {
                StoreId = storeId,
                WarehouseId = warehouseId,
                LowStockOnly = lowStockOnly,
                Status = status,
                ScopeKind = scopeKind,
                ScopeValues = scopeValues,
                ActorUserId = scope?.ActorUserId
            };
        }
    }
}
