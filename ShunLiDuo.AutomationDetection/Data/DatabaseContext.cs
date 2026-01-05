using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace ShunLiDuo.AutomationDetection.Data
{
    public class DatabaseContext : IDisposable
    {
        private SQLiteConnection _connection;
        private readonly string _connectionString;
        private readonly string _databasePath;

        public DatabaseContext()
        {
            // 数据库文件路径
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ShunLiDuo",
                "AutomationDetection"
            );
            
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _databasePath = Path.Combine(appDataPath, "AutomationDetection.db");
            _connectionString = $"Data Source={_databasePath};Version=3;";
        }

        public SQLiteConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = new SQLiteConnection(_connectionString);
                    _connection.Open();
                }
                return _connection;
            }
        }

        public void InitializeDatabase()
        {
            // 创建规则表
            string createRulesTable = @"
                CREATE TABLE IF NOT EXISTS Rules (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RuleNo TEXT NOT NULL,
                    RuleName TEXT NOT NULL,
                    DetectionRooms TEXT,
                    LogisticsBoxNos TEXT,
                    Remark TEXT,
                    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdateTime DATETIME
                );";

            // 创建检测室表
            string createDetectionRoomsTable = @"
                CREATE TABLE IF NOT EXISTS DetectionRooms (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RoomNo TEXT NOT NULL,
                    RoomName TEXT NOT NULL,
                    Remark TEXT,
                    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdateTime DATETIME
                );";

            // 创建物流盒表
            string createLogisticsBoxesTable = @"
                CREATE TABLE IF NOT EXISTS LogisticsBoxes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BoxNo TEXT NOT NULL,
                    BoxName TEXT NOT NULL,
                    Remark TEXT,
                    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdateTime DATETIME
                );";

            // 创建账户表
            string createAccountsTable = @"
                CREATE TABLE IF NOT EXISTS Accounts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AccountNo TEXT NOT NULL,
                    LoginAccount TEXT NOT NULL,
                    Password TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Gender TEXT,
                    Phone TEXT,
                    EmployeeNo TEXT,
                    RoleId INTEGER,
                    Remark TEXT,
                    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdateTime DATETIME,
                    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
                );";
            
            // 如果表已存在但使用旧的 Role 字段，需要迁移数据
            try
            {
                using (var checkCommand = Connection.CreateCommand())
                {
                    checkCommand.CommandText = "PRAGMA table_info(Accounts)";
                    using (var reader = checkCommand.ExecuteReader())
                    {
                        bool hasRoleId = false;
                        bool hasRole = false;
                        while (reader.Read())
                        {
                            string columnName = reader.GetString(1);
                            if (columnName == "RoleId")
                                hasRoleId = true;
                            if (columnName == "Role")
                                hasRole = true;
                        }
                        
                        // 如果表有 Role 字段但没有 RoleId 字段，需要迁移
                        if (hasRole && !hasRoleId)
                        {
                            // 添加 RoleId 字段
                            using (var alterCommand = Connection.CreateCommand())
                            {
                                alterCommand.CommandText = "ALTER TABLE Accounts ADD COLUMN RoleId INTEGER";
                                alterCommand.ExecuteNonQuery();
                            }
                            
                            // 迁移数据：根据角色名称查找角色ID
                            using (var migrateCommand = Connection.CreateCommand())
                            {
                                migrateCommand.CommandText = @"
                                    UPDATE Accounts 
                                    SET RoleId = (SELECT Id FROM Roles WHERE RoleName = Accounts.Role)
                                    WHERE Role IS NOT NULL AND Role != ''";
                                migrateCommand.ExecuteNonQuery();
                            }
                            
                            // 删除旧的 Role 字段（SQLite 不支持直接删除列，需要重建表）
                            // 为了简化，我们保留 Role 字段但不再使用它
                        }
                    }
                }
            }
            catch
            {
                // 忽略错误，继续执行
            }

            // 创建角色表
            string createRolesTable = @"
                CREATE TABLE IF NOT EXISTS Roles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RoleNo TEXT NOT NULL,
                    RoleName TEXT NOT NULL,
                    Remark TEXT,
                    Permissions TEXT,
                    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdateTime DATETIME
                );";

            // 创建权限表
            string createPermissionsTable = @"
                CREATE TABLE IF NOT EXISTS Permissions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Code TEXT NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    ModuleCode TEXT NOT NULL,
                    ModuleName TEXT NOT NULL,
                    SortOrder INTEGER NOT NULL DEFAULT 0,
                    ParentId INTEGER,
                    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdateTime DATETIME,
                    FOREIGN KEY (ParentId) REFERENCES Permissions(Id)
                );";

            // 创建通讯配置表
            string createCommunicationConfigTable = @"
                CREATE TABLE IF NOT EXISTS CommunicationConfig (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IpAddress TEXT NOT NULL,
                    CpuType INTEGER NOT NULL,
                    Rack INTEGER NOT NULL DEFAULT 0,
                    Slot INTEGER NOT NULL DEFAULT 1,
                    AutoConnect INTEGER NOT NULL DEFAULT 0,
                    UpdateTime DATETIME DEFAULT CURRENT_TIMESTAMP
                );";

            using (var command = Connection.CreateCommand())
            {
                command.CommandText = createRulesTable;
                command.ExecuteNonQuery();

                command.CommandText = createDetectionRoomsTable;
                command.ExecuteNonQuery();

                command.CommandText = createLogisticsBoxesTable;
                command.ExecuteNonQuery();

                command.CommandText = createAccountsTable;
                command.ExecuteNonQuery();

                command.CommandText = createRolesTable;
                command.ExecuteNonQuery();

                command.CommandText = createPermissionsTable;
                command.ExecuteNonQuery();

                // 创建通讯配置表
                command.CommandText = createCommunicationConfigTable;
                command.ExecuteNonQuery();
            }

            // 初始化权限数据
            InitializePermissions();
            
            // 初始化默认管理员角色和账号
            InitializeDefaultAdmin();
            
            // 初始化默认检测室
            InitializeDefaultDetectionRooms();
        }

        private void InitializePermissions()
        {
            // 检查是否已有权限数据
            using (var checkCommand = Connection.CreateCommand())
            {
                checkCommand.CommandText = "SELECT COUNT(*) FROM Permissions";
                var count = Convert.ToInt32(checkCommand.ExecuteScalar());
                if (count > 0)
                {
                    return; // 已有数据，不需要初始化
                }
            }

            // 定义权限数据（不包括任务管理）
            var permissions = new[]
            {
                // 调度规则管理模块
                new { Code = "RuleManagement", Name = "调度规则管理", ModuleCode = "RuleManagement", ModuleName = "调度规则管理", SortOrder = 1, ParentId = (int?)null },
                new { Code = "RuleManagement.Add", Name = "新增", ModuleCode = "RuleManagement", ModuleName = "调度规则管理", SortOrder = 1, ParentId = (int?)0 },
                new { Code = "RuleManagement.Edit", Name = "编辑", ModuleCode = "RuleManagement", ModuleName = "调度规则管理", SortOrder = 2, ParentId = (int?)0 },
                new { Code = "RuleManagement.Delete", Name = "删除", ModuleCode = "RuleManagement", ModuleName = "调度规则管理", SortOrder = 3, ParentId = (int?)0 },
                new { Code = "RuleManagement.View", Name = "查看", ModuleCode = "RuleManagement", ModuleName = "调度规则管理", SortOrder = 4, ParentId = (int?)0 },
                
                // 物流盒管理模块
                new { Code = "LogisticsBoxManagement", Name = "物流盒管理", ModuleCode = "LogisticsBoxManagement", ModuleName = "物流盒管理", SortOrder = 2, ParentId = (int?)null },
                new { Code = "LogisticsBoxManagement.Add", Name = "新增", ModuleCode = "LogisticsBoxManagement", ModuleName = "物流盒管理", SortOrder = 1, ParentId = (int?)0 },
                new { Code = "LogisticsBoxManagement.Edit", Name = "编辑", ModuleCode = "LogisticsBoxManagement", ModuleName = "物流盒管理", SortOrder = 2, ParentId = (int?)0 },
                new { Code = "LogisticsBoxManagement.Delete", Name = "删除", ModuleCode = "LogisticsBoxManagement", ModuleName = "物流盒管理", SortOrder = 3, ParentId = (int?)0 },
                new { Code = "LogisticsBoxManagement.View", Name = "查看", ModuleCode = "LogisticsBoxManagement", ModuleName = "物流盒管理", SortOrder = 4, ParentId = (int?)0 },
                
                // 检测室管理模块
                new { Code = "DetectionRoomManagement", Name = "检测室管理", ModuleCode = "DetectionRoomManagement", ModuleName = "检测室管理", SortOrder = 3, ParentId = (int?)null },
                new { Code = "DetectionRoomManagement.Add", Name = "新增", ModuleCode = "DetectionRoomManagement", ModuleName = "检测室管理", SortOrder = 1, ParentId = (int?)0 },
                new { Code = "DetectionRoomManagement.Edit", Name = "编辑", ModuleCode = "DetectionRoomManagement", ModuleName = "检测室管理", SortOrder = 2, ParentId = (int?)0 },
                new { Code = "DetectionRoomManagement.Delete", Name = "删除", ModuleCode = "DetectionRoomManagement", ModuleName = "检测室管理", SortOrder = 3, ParentId = (int?)0 },
                new { Code = "DetectionRoomManagement.View", Name = "查看", ModuleCode = "DetectionRoomManagement", ModuleName = "检测室管理", SortOrder = 4, ParentId = (int?)0 },
                
                // 账户管理模块
                new { Code = "AccountManagement", Name = "账户管理", ModuleCode = "AccountManagement", ModuleName = "账户管理", SortOrder = 4, ParentId = (int?)null },
                new { Code = "AccountManagement.Add", Name = "新增", ModuleCode = "AccountManagement", ModuleName = "账户管理", SortOrder = 1, ParentId = (int?)0 },
                new { Code = "AccountManagement.Edit", Name = "编辑", ModuleCode = "AccountManagement", ModuleName = "账户管理", SortOrder = 2, ParentId = (int?)0 },
                new { Code = "AccountManagement.Delete", Name = "删除", ModuleCode = "AccountManagement", ModuleName = "账户管理", SortOrder = 3, ParentId = (int?)0 },
                new { Code = "AccountManagement.View", Name = "查看", ModuleCode = "AccountManagement", ModuleName = "账户管理", SortOrder = 4, ParentId = (int?)0 },
                
                // 角色管理模块
                new { Code = "RoleManagement", Name = "角色管理", ModuleCode = "RoleManagement", ModuleName = "角色管理", SortOrder = 5, ParentId = (int?)null },
                new { Code = "RoleManagement.Add", Name = "新增", ModuleCode = "RoleManagement", ModuleName = "角色管理", SortOrder = 1, ParentId = (int?)0 },
                new { Code = "RoleManagement.Edit", Name = "编辑", ModuleCode = "RoleManagement", ModuleName = "角色管理", SortOrder = 2, ParentId = (int?)0 },
                new { Code = "RoleManagement.Delete", Name = "删除", ModuleCode = "RoleManagement", ModuleName = "角色管理", SortOrder = 3, ParentId = (int?)0 },
                new { Code = "RoleManagement.View", Name = "查看", ModuleCode = "RoleManagement", ModuleName = "角色管理", SortOrder = 4, ParentId = (int?)0 }
            };

            using (var insertCommand = Connection.CreateCommand())
            {
                insertCommand.CommandText = @"
                    INSERT INTO Permissions (Code, Name, ModuleCode, ModuleName, SortOrder, ParentId, CreateTime, UpdateTime)
                    VALUES (@Code, @Name, @ModuleCode, @ModuleName, @SortOrder, @ParentId, @CreateTime, @UpdateTime)";

                var codeParam = insertCommand.CreateParameter();
                codeParam.ParameterName = "@Code";
                insertCommand.Parameters.Add(codeParam);

                var nameParam = insertCommand.CreateParameter();
                nameParam.ParameterName = "@Name";
                insertCommand.Parameters.Add(nameParam);

                var moduleCodeParam = insertCommand.CreateParameter();
                moduleCodeParam.ParameterName = "@ModuleCode";
                insertCommand.Parameters.Add(moduleCodeParam);

                var moduleNameParam = insertCommand.CreateParameter();
                moduleNameParam.ParameterName = "@ModuleName";
                insertCommand.Parameters.Add(moduleNameParam);

                var sortOrderParam = insertCommand.CreateParameter();
                sortOrderParam.ParameterName = "@SortOrder";
                insertCommand.Parameters.Add(sortOrderParam);

                var parentIdParam = insertCommand.CreateParameter();
                parentIdParam.ParameterName = "@ParentId";
                insertCommand.Parameters.Add(parentIdParam);

                var createTimeParam = insertCommand.CreateParameter();
                createTimeParam.ParameterName = "@CreateTime";
                insertCommand.Parameters.Add(createTimeParam);

                var updateTimeParam = insertCommand.CreateParameter();
                updateTimeParam.ParameterName = "@UpdateTime";
                insertCommand.Parameters.Add(updateTimeParam);

                var now = DateTime.Now;
                var moduleIds = new Dictionary<string, int>();

                foreach (var perm in permissions)
                {
                    if (perm.ParentId == null)
                    {
                        // 插入模块根权限
                        codeParam.Value = perm.Code;
                        nameParam.Value = perm.Name;
                        moduleCodeParam.Value = perm.ModuleCode;
                        moduleNameParam.Value = perm.ModuleName;
                        sortOrderParam.Value = perm.SortOrder;
                        parentIdParam.Value = DBNull.Value;
                        createTimeParam.Value = now;
                        updateTimeParam.Value = now;

                        insertCommand.ExecuteNonQuery();

                        // 获取刚插入的ID
                        using (var getIdCommand = Connection.CreateCommand())
                        {
                            getIdCommand.CommandText = "SELECT last_insert_rowid()";
                            var moduleId = Convert.ToInt32(getIdCommand.ExecuteScalar());
                            moduleIds[perm.ModuleCode] = moduleId;
                        }
                    }
                    else
                    {
                        // 插入子权限
                        codeParam.Value = perm.Code;
                        nameParam.Value = perm.Name;
                        moduleCodeParam.Value = perm.ModuleCode;
                        moduleNameParam.Value = perm.ModuleName;
                        sortOrderParam.Value = perm.SortOrder;
                        parentIdParam.Value = moduleIds[perm.ModuleCode];
                        createTimeParam.Value = now;
                        updateTimeParam.Value = now;

                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private void InitializeDefaultAdmin()
        {
            // 检查是否已有admin账号
            using (var checkAccountCommand = Connection.CreateCommand())
            {
                checkAccountCommand.CommandText = "SELECT COUNT(*) FROM Accounts WHERE LoginAccount = 'admin'";
                var accountCount = Convert.ToInt32(checkAccountCommand.ExecuteScalar());
                if (accountCount > 0)
                {
                    return; // 已有admin账号，不需要初始化
                }
            }

            // 获取所有权限代码
            var allPermissionCodes = new List<string>();
            using (var permCommand = Connection.CreateCommand())
            {
                permCommand.CommandText = "SELECT Code FROM Permissions ORDER BY SortOrder, Id";
                using (var reader = permCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            allPermissionCodes.Add(reader.GetString(0));
                        }
                    }
                }
            }

            var allPermissionsString = string.Join(",", allPermissionCodes);
            var now = DateTime.Now;

            // 检查是否已有管理员角色
            int adminRoleId = 0;
            using (var checkRoleCommand = Connection.CreateCommand())
            {
                checkRoleCommand.CommandText = "SELECT Id FROM Roles WHERE RoleName = '管理员' LIMIT 1";
                var result = checkRoleCommand.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    adminRoleId = Convert.ToInt32(result);
                }
                else
                {
                    // 创建管理员角色
                    using (var roleCommand = Connection.CreateCommand())
                    {
                        roleCommand.CommandText = @"
                            INSERT INTO Roles (RoleNo, RoleName, Remark, Permissions, CreateTime, UpdateTime)
                            VALUES (@RoleNo, @RoleName, @Remark, @Permissions, @CreateTime, @UpdateTime);
                            SELECT last_insert_rowid();";

                        roleCommand.Parameters.AddWithValue("@RoleNo", "ADMIN001");
                        roleCommand.Parameters.AddWithValue("@RoleName", "管理员");
                        roleCommand.Parameters.AddWithValue("@Remark", "系统默认管理员角色，拥有所有权限");
                        roleCommand.Parameters.AddWithValue("@Permissions", allPermissionsString);
                        roleCommand.Parameters.AddWithValue("@CreateTime", now);
                        roleCommand.Parameters.AddWithValue("@UpdateTime", now);

                        adminRoleId = Convert.ToInt32(roleCommand.ExecuteScalar());
                    }
                }
            }

            // 创建admin账号
            using (var accountCommand = Connection.CreateCommand())
            {
                accountCommand.CommandText = @"
                    INSERT INTO Accounts (AccountNo, LoginAccount, Password, Name, Gender, Phone, EmployeeNo, RoleId, Remark, CreateTime, UpdateTime)
                    VALUES (@AccountNo, @LoginAccount, @Password, @Name, @Gender, @Phone, @EmployeeNo, @RoleId, @Remark, @CreateTime, @UpdateTime)";

                accountCommand.Parameters.AddWithValue("@AccountNo", "ADMIN001");
                accountCommand.Parameters.AddWithValue("@LoginAccount", "admin");
                accountCommand.Parameters.AddWithValue("@Password", "123");
                accountCommand.Parameters.AddWithValue("@Name", "管理员");
                accountCommand.Parameters.AddWithValue("@Gender", DBNull.Value);
                accountCommand.Parameters.AddWithValue("@Phone", DBNull.Value);
                accountCommand.Parameters.AddWithValue("@EmployeeNo", "ADMIN001");
                accountCommand.Parameters.AddWithValue("@RoleId", adminRoleId);
                accountCommand.Parameters.AddWithValue("@Remark", "系统默认管理员账号");
                accountCommand.Parameters.AddWithValue("@CreateTime", now);
                accountCommand.Parameters.AddWithValue("@UpdateTime", now);

                accountCommand.ExecuteNonQuery();
            }
        }

        private void InitializeDefaultDetectionRooms()
        {
            // 检查是否已有检测室数据
            using (var checkCommand = Connection.CreateCommand())
            {
                checkCommand.CommandText = "SELECT COUNT(*) FROM DetectionRooms";
                var count = Convert.ToInt32(checkCommand.ExecuteScalar());
                if (count > 0)
                {
                    return; // 已有数据，不需要初始化
                }
            }

            // 创建默认5个检测室
            var now = DateTime.Now;
            using (var insertCommand = Connection.CreateCommand())
            {
                insertCommand.CommandText = @"
                    INSERT INTO DetectionRooms (RoomNo, RoomName, Remark, CreateTime, UpdateTime)
                    VALUES (@RoomNo, @RoomName, @Remark, @CreateTime, @UpdateTime)";

                var roomNoParam = insertCommand.CreateParameter();
                roomNoParam.ParameterName = "@RoomNo";
                insertCommand.Parameters.Add(roomNoParam);

                var roomNameParam = insertCommand.CreateParameter();
                roomNameParam.ParameterName = "@RoomName";
                insertCommand.Parameters.Add(roomNameParam);

                var remarkParam = insertCommand.CreateParameter();
                remarkParam.ParameterName = "@Remark";
                insertCommand.Parameters.Add(remarkParam);

                var createTimeParam = insertCommand.CreateParameter();
                createTimeParam.ParameterName = "@CreateTime";
                insertCommand.Parameters.Add(createTimeParam);

                var updateTimeParam = insertCommand.CreateParameter();
                updateTimeParam.ParameterName = "@UpdateTime";
                insertCommand.Parameters.Add(updateTimeParam);

                // 插入5个默认检测室
                for (int i = 1; i <= 5; i++)
                {
                    roomNoParam.Value = $"ROOM{i:D3}";
                    roomNameParam.Value = $"检测室{i}";
                    remarkParam.Value = $"默认检测室{i}";
                    createTimeParam.Value = now;
                    updateTimeParam.Value = now;

                    insertCommand.ExecuteNonQuery();
                }
            }
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}
