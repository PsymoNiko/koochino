using System;
using System.Collections.Generic;
using System.Linq;
using MyShop.Core.Models.Security;

namespace MyShop.Application.Security
{
    public static class PermissionTreeBuilder
    {
        public static UserPermissionTreeResult Build(
            int userId,
            string username,
            IEnumerable<PermissionTreeCatalogRow> catalog,
            ISet<string> allowedCodes)
        {
            var rows = catalog?.ToList() ?? new List<PermissionTreeCatalogRow>();
            var nodes = new List<PermissionTreeNode>();
            var total = 0;
            var allowed = 0;

            foreach (var moduleGroup in rows.GroupBy(r => r.ModuleId).OrderBy(g => g.First().ModuleSort).ThenBy(g => g.First().ModuleName))
            {
                var first = moduleGroup.First();
                var moduleCode = first.ModuleCode;
                var moduleLabel = SecurityCatalogLabels.Module(moduleCode, first.ModuleName);
                var moduleKey = "m:" + moduleCode;

                var resourceGroups = moduleGroup
                    .GroupBy(r => r.ResourceId)
                    .OrderBy(g => g.First().ResourceDepth)
                    .ThenBy(g => g.First().ResourceName)
                    .ToList();

                var moduleChildren = new List<PermissionTreeNode>();
                foreach (var resourceGroup in resourceGroups)
                {
                    var rf = resourceGroup.First();
                    var resourceLabel = SecurityCatalogLabels.Resource(rf.ResourceCode, moduleCode, rf.ResourceName);
                    var collapseResource = resourceGroups.Count == 1
                        && string.Equals(rf.ResourceCode, moduleCode, StringComparison.OrdinalIgnoreCase);

                    var actionNodes = resourceGroup
                        .OrderBy(r => r.ActionSort)
                        .Select(r =>
                        {
                            var has = allowedCodes.Contains(r.PermissionCode);
                            total++;
                            if (has) allowed++;
                            return new PermissionTreeNode
                            {
                                Key = "p:" + r.PermissionId,
                                Label = SecurityCatalogLabels.Action(r.ActionCode, r.ActionName),
                                Subtitle = r.PermissionCode,
                                IsLeaf = true,
                                HasAccess = has,
                                AllowedCount = has ? 1 : 0,
                                TotalCount = 1
                            };
                        })
                        .ToList();

                    if (collapseResource)
                    {
                        moduleChildren.AddRange(actionNodes);
                        continue;
                    }

                    var resourceNode = new PermissionTreeNode
                    {
                        Key = "r:" + rf.ResourceId,
                        Label = resourceLabel,
                        Subtitle = rf.ResourceCode,
                        IsLeaf = false,
                        Children = actionNodes
                    };
                    ApplyCounts(resourceNode);
                    moduleChildren.Add(resourceNode);
                }

                var moduleNode = new PermissionTreeNode
                {
                    Key = moduleKey,
                    Label = moduleLabel,
                    Subtitle = moduleCode,
                    IsLeaf = false,
                    Children = moduleChildren
                };
                ApplyCounts(moduleNode);
                nodes.Add(moduleNode);
            }

            return new UserPermissionTreeResult
            {
                UserId = userId,
                Username = username,
                Nodes = nodes,
                TotalCount = total,
                AllowedCount = allowed
            };
        }

        static void ApplyCounts(PermissionTreeNode node)
        {
            if (node.IsLeaf) return;
            node.AllowedCount = node.Children.Sum(c => c.AllowedCount);
            node.TotalCount = node.Children.Sum(c => c.TotalCount);
            node.HasAccess = node.TotalCount > 0 && node.AllowedCount == node.TotalCount;
        }
    }
}
