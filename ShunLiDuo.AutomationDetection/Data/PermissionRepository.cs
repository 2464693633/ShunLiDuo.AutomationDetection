using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly DatabaseContext _context;

        public PermissionRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<PermissionModel>> GetAllPermissionsAsync()
        {
            return await Task.Run(() =>
            {
                var permissions = new List<PermissionModel>();
                string sql = "SELECT Id, Code, Name, ModuleCode, ModuleName, SortOrder, ParentId FROM Permissions ORDER BY SortOrder, Id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        permissions.Add(new PermissionModel
                        {
                            Id = reader.GetInt32(0),
                            Code = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            ModuleCode = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            ModuleName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            SortOrder = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                            ParentId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6)
                        });
                    }
                }
                return permissions;
            });
        }

        public async Task<List<PermissionModel>> GetModulePermissionsAsync(string moduleCode)
        {
            return await Task.Run(() =>
            {
                var permissions = new List<PermissionModel>();
                string sql = "SELECT Id, Code, Name, ModuleCode, ModuleName, SortOrder, ParentId FROM Permissions WHERE ModuleCode = @ModuleCode ORDER BY SortOrder, Id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@ModuleCode", moduleCode ?? string.Empty);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            permissions.Add(new PermissionModel
                            {
                                Id = reader.GetInt32(0),
                                Code = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                ModuleCode = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                ModuleName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                SortOrder = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                                ParentId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6)
                            });
                        }
                    }
                }
                return permissions;
            });
        }
    }
}

