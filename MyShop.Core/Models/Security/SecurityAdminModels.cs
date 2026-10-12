using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Security
{
    public static class PermissionTriState
    {
        public const byte Inherited = 0;
        public const byte Allow = 1;
        public const byte Deny = 2;
    }

    public sealed class SecurityRoleRow
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
        public int ChildRoleCount { get; set; }
    }

    public sealed class RolePermissionMatrixRow
    {
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; }
        public string ModuleCode { get; set; }
        public string ModuleName { get; set; }
        public string ResourceCode { get; set; }
        public string ResourceName { get; set; }
        public string ActionCode { get; set; }
        public string ActionName { get; set; }
        /// <summary>0=Inherited, 1=Allow, 2=Deny</summary>
        public byte TriState { get; set; }
        public bool IsMandatory { get; set; }
        public int? RolePermissionId { get; set; }
        public string ScopeCode { get; set; }
        public string ScopeValue { get; set; }
    }

    public sealed class RoleHierarchyEdge
    {
        public int ParentRoleId { get; set; }
        public string ParentName { get; set; }
        public int ChildRoleId { get; set; }
        public string ChildName { get; set; }
    }

    public sealed class UserPermissionOverrideRow
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; }
        public byte Effect { get; set; }
        public string Reason { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive => ExpiresAt == null || ExpiresAt > DateTime.UtcNow;
    }

    public sealed class PermissionHolderRow
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Source { get; set; }
        public string RoleName { get; set; }
    }

    public sealed class ActiveOverrideReportRow
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PermissionCode { get; set; }
        public byte Effect { get; set; }
        public string Reason { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public sealed class UnscopedGrantReportRow
    {
        public int RolePermissionId { get; set; }
        public string RoleName { get; set; }
        public string PermissionCode { get; set; }
    }

    public sealed class PermissionCatalogRow
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string ModuleCode { get; set; }
        public string ActionCode { get; set; }
        public string ModuleName { get; set; }
        public string ActionName { get; set; }
    }

    public sealed class DataScopeOptionRow
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool RequiresValue { get; set; }
    }

    public sealed class RolePermissionScopeRow
    {
        public int RolePermissionId { get; set; }
        public int PermissionId { get; set; }
        public string ScopeCode { get; set; } = "All";
        public string ScopeValue { get; set; }
    }

    public sealed class PermissionTreeCatalogRow
    {
        public int ModuleId { get; set; }
        public string ModuleCode { get; set; }
        public string ModuleName { get; set; }
        public int ModuleSort { get; set; }
        public int ResourceId { get; set; }
        public string ResourceCode { get; set; }
        public string ResourceName { get; set; }
        public int? ParentResourceId { get; set; }
        public int ResourceDepth { get; set; }
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; }
        public string ActionCode { get; set; }
        public string ActionName { get; set; }
        public int ActionSort { get; set; }
    }

    public sealed class PermissionTreeNode
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Subtitle { get; set; }
        public bool IsLeaf { get; set; }
        public bool HasAccess { get; set; }
        public int AllowedCount { get; set; }
        public int TotalCount { get; set; }
        public List<PermissionTreeNode> Children { get; set; } = new();
    }

    public sealed class UserPermissionTreeResult
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public int AllowedCount { get; set; }
        public int TotalCount { get; set; }
        public List<PermissionTreeNode> Nodes { get; set; } = new();
    }
}
