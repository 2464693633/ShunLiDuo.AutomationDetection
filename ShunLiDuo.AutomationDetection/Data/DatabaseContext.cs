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
            // 数据库文件路径 - 创建在项目目录（bin\Debug 或 bin\Release）
            string projectPath = AppDomain.CurrentDomain.BaseDirectory;
            
            if (!Directory.Exists(projectPath))
            {
                Directory.CreateDirectory(projectPath);
            }

            _databasePath = Path.Combine(projectPath, "AutomationDetection.db");
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
                    ScannerPortName TEXT,
                    ScannerBaudRate INTEGER DEFAULT 9600,
                    ScannerDataBits INTEGER DEFAULT 8,
                    ScannerStopBits INTEGER DEFAULT 1,
                    ScannerParity TEXT DEFAULT 'None',
                    ScannerIsEnabled INTEGER DEFAULT 0,
                    Cylinder1Name TEXT,
                    Cylinder1ExtendAddress TEXT,
                    Cylinder1RetractAddress TEXT,
                    Cylinder1ExtendFeedbackAddress TEXT,
                    Cylinder1RetractFeedbackAddress TEXT,
                    Cylinder1DataType TEXT,
                    Cylinder2Name TEXT,
                    Cylinder2ExtendAddress TEXT,
                    Cylinder2RetractAddress TEXT,
                    Cylinder2ExtendFeedbackAddress TEXT,
                    Cylinder2RetractFeedbackAddress TEXT,
                    Cylinder2DataType TEXT,
                    SensorName TEXT,
                    SensorAddress TEXT,
                    SensorDataType TEXT,
                    PushCylinderRetractTimeout INTEGER DEFAULT 5000,
                    PushCylinderExtendTimeout INTEGER DEFAULT 10000,
                    BlockingCylinderRetractTimeout INTEGER DEFAULT 5000,
                    BlockingCylinderExtendTimeout INTEGER DEFAULT 5000,
                    SensorDetectTimeout INTEGER DEFAULT 15000,
                    PassageDelayTime INTEGER DEFAULT 5000,
                    SensorConfirmDelayTime INTEGER DEFAULT 3000,
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

            // 迁移检测室表，添加串口配置字段
            try
            {
                using (var checkCommand = Connection.CreateCommand())
                {
                    checkCommand.CommandText = "PRAGMA table_info(DetectionRooms)";
                    using (var reader = checkCommand.ExecuteReader())
                    {
                        var columns = new List<string>();
                        while (reader.Read())
                        {
                            columns.Add(reader.GetString(1));
                        }
                        
                        // 如果表存在但没有串口配置字段，则添加
                        if (columns.Count > 0)
                        {
                            if (!columns.Contains("ScannerPortName"))
                            {
                                using (var alterCommand = Connection.CreateCommand())
                                {
                                    alterCommand.CommandText = "ALTER TABLE DetectionRooms ADD COLUMN ScannerPortName TEXT";
                                    alterCommand.ExecuteNonQuery();
                                }
                            }
                            if (!columns.Contains("ScannerBaudRate"))
                            {
                                using (var alterCommand = Connection.CreateCommand())
                                {
                                    alterCommand.CommandText = "ALTER TABLE DetectionRooms ADD COLUMN ScannerBaudRate INTEGER DEFAULT 9600";
                                    alterCommand.ExecuteNonQuery();
                                }
                            }
                            if (!columns.Contains("ScannerDataBits"))
                            {
                                using (var alterCommand = Connection.CreateCommand())
                                {
                                    alterCommand.CommandText = "ALTER TABLE DetectionRooms ADD COLUMN ScannerDataBits INTEGER DEFAULT 8";
                                    alterCommand.ExecuteNonQuery();
                                }
                            }
                            if (!columns.Contains("ScannerStopBits"))
                            {
                                using (var alterCommand = Connection.CreateCommand())
                                {
                                    alterCommand.CommandText = "ALTER TABLE DetectionRooms ADD COLUMN ScannerStopBits INTEGER DEFAULT 1";
                                    alterCommand.ExecuteNonQuery();
                                }
                            }
                            if (!columns.Contains("ScannerParity"))
                            {
                                using (var alterCommand = Connection.CreateCommand())
                                {
                                    alterCommand.CommandText = "ALTER TABLE DetectionRooms ADD COLUMN ScannerParity TEXT DEFAULT 'None'";
                                    alterCommand.ExecuteNonQuery();
                                }
                            }
                            if (!columns.Contains("ScannerIsEnabled"))
                            {
                                using (var alterCommand = Connection.CreateCommand())
                                {
                                    alterCommand.CommandText = "ALTER TABLE DetectionRooms ADD COLUMN ScannerIsEnabled INTEGER DEFAULT 0";
                                    alterCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // 忽略错误，继续执行
            }

            // 检查DetectionRooms表是否需要添加PLC配置字段，并进行数据迁移
            try
            {
                using (var checkCommand = Connection.CreateCommand())
                {
                    // 检查DetectionRooms表是否已有PLC配置字段
                    checkCommand.CommandText = "PRAGMA table_info(DetectionRooms)";
                    using (var reader = checkCommand.ExecuteReader())
                    {
                        var columns = new List<string>();
                        while (reader.Read())
                        {
                            columns.Add(reader.GetString(1));
                        }
                        
                        // 需要添加的PLC配置字段
                        var plcColumns = new Dictionary<string, string>
                        {
                            { "Cylinder1ExtendAddress", "TEXT" },
                            { "Cylinder1RetractAddress", "TEXT" },
                            { "Cylinder1ExtendFeedbackAddress", "TEXT" },
                            { "Cylinder1RetractFeedbackAddress", "TEXT" },
                            { "Cylinder1DataType", "TEXT" },
                            { "Cylinder2ExtendAddress", "TEXT" },
                            { "Cylinder2RetractAddress", "TEXT" },
                            { "Cylinder2ExtendFeedbackAddress", "TEXT" },
                            { "Cylinder2RetractFeedbackAddress", "TEXT" },
                            { "Cylinder2DataType", "TEXT" },
                            { "SensorAddress", "TEXT" },
                            { "SensorDataType", "TEXT" },
                            { "PushCylinderRetractTimeout", "INTEGER DEFAULT 5000" },
                            { "PushCylinderExtendTimeout", "INTEGER DEFAULT 10000" },
                            { "BlockingCylinderRetractTimeout", "INTEGER DEFAULT 5000" },
                            { "BlockingCylinderExtendTimeout", "INTEGER DEFAULT 5000" },
                            { "SensorDetectTimeout", "INTEGER DEFAULT 15000" },
                            { "PassageDelayTime", "INTEGER DEFAULT 5000" },
                            { "SensorConfirmDelayTime", "INTEGER DEFAULT 3000" },
                            { "EnableBlockingCylinderRetractFeedback", "INTEGER DEFAULT 1" }
                        };
                        
                        bool needMigration = false;
                        // 添加缺失的字段
                        foreach (var column in plcColumns)
                        {
                            if (!columns.Contains(column.Key))
                            {
                                using (var alterCommand = Connection.CreateCommand())
                                {
                                    alterCommand.CommandText = $"ALTER TABLE DetectionRooms ADD COLUMN {column.Key} {column.Value}";
                                    alterCommand.ExecuteNonQuery();
                                    needMigration = true;
                                }
                            }
                        }
                        
                        // 如果添加了新字段，尝试从PlcMonitorConfig表迁移数据
                        if (needMigration)
                        {
                            // 检查PlcMonitorConfig表是否存在
                            checkCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='PlcMonitorConfig'";
                            var plcTableExists = checkCommand.ExecuteScalar() != null;
                            
                            if (plcTableExists)
                            {
                                // 迁移PlcMonitorConfig表的数据到DetectionRooms表
                                using (var migrateCommand = Connection.CreateCommand())
                                {
                                    migrateCommand.CommandText = @"
                                        UPDATE DetectionRooms 
                                        SET Cylinder1ExtendAddress = (SELECT Cylinder1ExtendAddress FROM PlcMonitorConfig WHERE PlcMonitorConfig.RoomId = DetectionRooms.Id),
                                            Cylinder1RetractAddress = (SELECT Cylinder1RetractAddress FROM PlcMonitorConfig WHERE PlcMonitorConfig.RoomId = DetectionRooms.Id),
                                            Cylinder1ExtendFeedbackAddress = (SELECT Cylinder1ExtendFeedbackAddress FROM PlcMonitorConfig WHERE PlcMonitorConfig.RoomId = DetectionRooms.Id),
                                            Cylinder1RetractFeedbackAddress = (SELECT Cylinder1RetractFeedbackAddress FROM PlcMonitorConfig WHERE PlcMonitorConfig.RoomId = DetectionRooms.Id),
                                            Cylinder1DataType = (SELECT Cylinder1DataType FROM PlcMonitorConfig WHERE PlcMonitorConfig.RoomId = DetectionRooms.Id),
                                            Cylinder2ExtendAddress = (SELECT Cylinder2ExtendAddress FROM PlcMonitorConfig WHERE PlcMonitorConfig.RoomId = DetectionRooms.Id),
                                            Cylinder2RetractAddress = (SELECT Cylinder2RetractAddress FROM PlcMonitorConfig WHERE PlcMonitorConfig.RoomId = DetectionRooms.Id),
                                            Cylinder2ExtendFeedbackAddress = (SELECT Cylinder2ExtendFeedbackAddress FROM PlcMonitorConfig WHERE PlcMonitorConfig.RoomId = DetectionRooms.Id),
                                            Cylinder2RetractFeedbackAddress = (SELECT Cylinder2RetractFeedbackAddress FROM PlcMonitorConfig WHERE PlcMonitorConfig.RoomId = DetectionRooms.Id),
                                            Cylinder2DataType = (SELECT Cylinder2DataType FROM PlcMonitorConfig WHERE PlcMonitorConfig.RoomId = DetectionRooms.Id),
                                            SensorAddress = (SELECT SensorAddress FROM PlcMonitorConfig WHERE PlcMonitorConfig.RoomId = DetectionRooms.Id),
                                            SensorDataType = (SELECT SensorDataType FROM PlcMonitorConfig WHERE PlcMonitorConfig.RoomId = DetectionRooms.Id)
                                        WHERE EXISTS (SELECT 1 FROM PlcMonitorConfig WHERE PlcMonitorConfig.RoomId = DetectionRooms.Id)";
                                    migrateCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[数据库迁移] 迁移PLC配置字段时出错: {ex.Message}");
                // 继续执行，不阻止程序启动
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

            // 创建PLC监控配置表
            string createPlcMonitorConfigTable = @"
                CREATE TABLE IF NOT EXISTS PlcMonitorConfig (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RoomId INTEGER NOT NULL,
                    Cylinder1Name TEXT,
                    Cylinder1ExtendAddress TEXT,
                    Cylinder1RetractAddress TEXT,
                    Cylinder1ExtendFeedbackAddress TEXT,
                    Cylinder1RetractFeedbackAddress TEXT,
                    Cylinder1DataType TEXT,
                    Cylinder2Name TEXT,
                    Cylinder2ExtendAddress TEXT,
                    Cylinder2RetractAddress TEXT,
                    Cylinder2ExtendFeedbackAddress TEXT,
                    Cylinder2RetractFeedbackAddress TEXT,
                    Cylinder2DataType TEXT,
                    SensorName TEXT,
                    SensorAddress TEXT,
                    SensorDataType TEXT,
                    Remark TEXT,
                    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdateTime DATETIME,
                    FOREIGN KEY (RoomId) REFERENCES DetectionRooms(Id)
                );";

            // 创建检测日志表
            string createDetectionLogsTable = @"
                CREATE TABLE IF NOT EXISTS DetectionLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    LogisticsBoxCode TEXT NOT NULL,
                    WorkOrderNo TEXT,
                    RoomId INTEGER,
                    RoomName TEXT,
                    Status TEXT NOT NULL,
                    StartTime DATETIME,
                    EndTime DATETIME,
                    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                    Remark TEXT,
                    FOREIGN KEY (RoomId) REFERENCES DetectionRooms(Id)
                );";

            // 创建报警记录表
            string createAlarmRecordsTable = @"
                CREATE TABLE IF NOT EXISTS AlarmRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AlarmCode TEXT NOT NULL UNIQUE,
                    AlarmTitle TEXT NOT NULL,
                    AlarmMessage TEXT,
                    RoomId INTEGER,
                    RoomName TEXT,
                    DeviceName TEXT,
                    Status TEXT DEFAULT '未处理',
                    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                    HandleTime DATETIME,
                    Handler TEXT,
                    Remark TEXT,
                    FOREIGN KEY (RoomId) REFERENCES DetectionRooms(Id)
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

                // 创建PLC监控配置表
                command.CommandText = createPlcMonitorConfigTable;
                command.ExecuteNonQuery();

                // 创建检测日志表
                command.CommandText = createDetectionLogsTable;
                command.ExecuteNonQuery();

                // 创建报警记录表
                command.CommandText = createAlarmRecordsTable;
                command.ExecuteNonQuery();
                
                // 检查并添加 AlarmType 字段（如果不存在）
                try
                {
                    // 先检查字段是否存在
                    command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('AlarmRecords') WHERE name='AlarmType';";
                    var fieldExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                    
                    if (!fieldExists)
                    {
                        command.CommandText = "ALTER TABLE AlarmRecords ADD COLUMN AlarmType TEXT DEFAULT '系统报警';";
                        command.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine("[数据库] 已添加 AlarmType 字段到 AlarmRecords 表");
                    }
                    
                    // 更新现有记录，为 NULL 的 AlarmType 设置默认值
                    command.CommandText = "UPDATE AlarmRecords SET AlarmType = '系统报警' WHERE AlarmType IS NULL;";
                    int updatedRows = command.ExecuteNonQuery();
                    if (updatedRows > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[数据库] 已更新 {updatedRows} 条报警记录的 AlarmType 字段");
                    }
                }
                catch (SQLiteException ex)
                {
                    // 如果字段已存在或其他错误，记录日志但继续执行
                    System.Diagnostics.Debug.WriteLine($"[数据库] 处理 AlarmType 字段时出错: {ex.Message}");
                }
                
                // 检查并添加通讯配置表的新字段（如果不存在）
                try
                {
                    // 需要添加的扫描枪配置字段
                    var commColumns = new Dictionary<string, string>
                    {
                        { "LoadingScannerPort", "TEXT" },
                        { "LoadingScannerBaudRate", "INTEGER DEFAULT 9600" },
                        { "LoadingScannerDataBits", "INTEGER DEFAULT 8" },
                        { "LoadingScannerStopBits", "INTEGER DEFAULT 1" },
                        { "LoadingScannerParity", "TEXT DEFAULT 'None'" },
                        { "LoadingScannerIsEnabled", "INTEGER DEFAULT 0" },
                        { "UnloadingScannerPort", "TEXT" },
                        { "UnloadingScannerBaudRate", "INTEGER DEFAULT 9600" },
                        { "UnloadingScannerDataBits", "INTEGER DEFAULT 8" },
                        { "UnloadingScannerStopBits", "INTEGER DEFAULT 1" },
                        { "UnloadingScannerParity", "TEXT DEFAULT 'None'" },
                        { "UnloadingScannerIsEnabled", "INTEGER DEFAULT 0" }
                    };

                    using (var checkCommand = Connection.CreateCommand())
                    {
                        checkCommand.CommandText = "PRAGMA table_info(CommunicationConfig)";
                        using (var reader = checkCommand.ExecuteReader())
                        {
                            var existingColumns = new List<string>();
                            while (reader.Read())
                            {
                                existingColumns.Add(reader.GetString(1));
                            }
                            
                            foreach (var column in commColumns)
                            {
                                if (!existingColumns.Contains(column.Key))
                                {
                                    using (var alterCommand = Connection.CreateCommand())
                                    {
                                        alterCommand.CommandText = $"ALTER TABLE CommunicationConfig ADD COLUMN {column.Key} {column.Value}";
                                        alterCommand.ExecuteNonQuery();
                                        System.Diagnostics.Debug.WriteLine($"[数据库] 已添加 {column.Key} 字段到 CommunicationConfig 表");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[数据库迁移] 迁移通讯配置字段时出错: {ex.Message}");
                }
                
                // 检查并添加 AlarmLevel 字段（如果不存在）
                try
                {
                    // 先检查字段是否存在
                    command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('AlarmRecords') WHERE name='AlarmLevel';";
                    var fieldExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                    
                    if (!fieldExists)
                    {
                        command.CommandText = "ALTER TABLE AlarmRecords ADD COLUMN AlarmLevel TEXT DEFAULT '警告';";
                        command.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine("[数据库] 已添加 AlarmLevel 字段到 AlarmRecords 表");
                    }
                    
                    // 更新现有记录，为 NULL 的 AlarmLevel 设置默认值
                    command.CommandText = "UPDATE AlarmRecords SET AlarmLevel = '警告' WHERE AlarmLevel IS NULL;";
                    int updatedRows = command.ExecuteNonQuery();
                    if (updatedRows > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[数据库] 已更新 {updatedRows} 条报警记录的 AlarmLevel 字段");
                    }
                }
                catch (SQLiteException ex)
                {
                    // 如果字段已存在或其他错误，记录日志但继续执行
                    System.Diagnostics.Debug.WriteLine($"[数据库] 处理 AlarmLevel 字段时出错: {ex.Message}");
                }
                
                // 检查并添加 WorkOrderNo 字段到 DetectionLogs 表（如果不存在）
                try
                {
                    command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('DetectionLogs') WHERE name='WorkOrderNo';";
                    var fieldExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                    
                    if (!fieldExists)
                    {
                        command.CommandText = "ALTER TABLE DetectionLogs ADD COLUMN WorkOrderNo TEXT;";
                        command.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine("[数据库] 已添加 WorkOrderNo 字段到 DetectionLogs 表");
                    }
                }
                catch (SQLiteException ex)
                {
                    // 如果字段已存在或其他错误，记录日志但继续执行
                    System.Diagnostics.Debug.WriteLine($"[数据库] 处理 WorkOrderNo 字段时出错: {ex.Message}");
                }

                // 检查并添加 InspectorName 字段到 DetectionLogs 表（如果不存在）
                try
                {
                    command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('DetectionLogs') WHERE name='InspectorName';";
                    var fieldExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                    
                    if (!fieldExists)
                    {
                        command.CommandText = "ALTER TABLE DetectionLogs ADD COLUMN InspectorName TEXT;";
                        command.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine("[数据库] 已添加 InspectorName 字段到 DetectionLogs 表");
                    }
                }
                catch (SQLiteException ex)
                {
                    // 如果字段已存在或其他错误，记录日志但继续执行
                    System.Diagnostics.Debug.WriteLine($"[数据库] 处理 InspectorName 字段时出错: {ex.Message}");
                }
                
                // [新增] 检查并添加气缸参数字段到 CommunicationConfig 表
                try
                {
                    // 需要添加的气缸参数字段
                    var cylinderColumns = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "LoadingCylinderExtendDelay", "INTEGER DEFAULT 3000" },
                        { "LoadingCylinderRetractDelay", "INTEGER DEFAULT 2000" },
                        { "LoadingCylinderInterlockDelay", "INTEGER DEFAULT 50" },
                        { "LoadingCylinderCooldown", "INTEGER DEFAULT 500" },
                        { "LoadingCylinderLoopInterval", "INTEGER DEFAULT 50" },
                        { "UnloadingCylinderExtendDelay", "INTEGER DEFAULT 3000" },
                        { "UnloadingCylinderRetractDelay", "INTEGER DEFAULT 2000" },
                        { "UnloadingCylinderInterlockDelay", "INTEGER DEFAULT 50" },
                        { "UnloadingCylinderCooldown", "INTEGER DEFAULT 500" },
                        { "UnloadingCylinderLoopInterval", "INTEGER DEFAULT 50" }
                    };
                    
                    // 检查表是否存在
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='CommunicationConfig';";
                    var tableExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                    
                    if (tableExists)
                    {
                        // 添加缺失的字段
                        foreach (var column in cylinderColumns)
                        {
                            command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('CommunicationConfig') WHERE name='{column.Key}';";
                            var fieldExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                            
                            if (!fieldExists)
                            {
                                command.CommandText = $"ALTER TABLE CommunicationConfig ADD COLUMN {column.Key} {column.Value};";
                                command.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"[数据库] 已添加 {column.Key} 字段到 CommunicationConfig 表");
                            }
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    // 如果出错，记录日志但继续执行
                    System.Diagnostics.Debug.WriteLine($"[数据库] 处理 CommunicationConfig 气缸参数字段时出错: {ex.Message}");
                }
                
                // [新增] 检查并添加Mode字段到 CommunicationConfig 表
                try
                {
                    // 检查表是否存在
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='CommunicationConfig';";
                    var tableExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                    
                    if (tableExists)
                    {
                        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('CommunicationConfig') WHERE name='Mode';";
                        var fieldExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                        
                        if (!fieldExists)
                        {
                            command.CommandText = "ALTER TABLE CommunicationConfig ADD COLUMN Mode INTEGER DEFAULT 0;";
                            command.ExecuteNonQuery();
                            System.Diagnostics.Debug.WriteLine("[数据库] 已添加 Mode 字段到 CommunicationConfig 表");
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    // 如果出错，记录日志但继续执行
                    System.Diagnostics.Debug.WriteLine($"[数据库] 处理 CommunicationConfig Mode 字段时出错: {ex.Message}");
                }
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
            var now = DateTime.Now;

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

            // 检查是否已有管理员角色，如果没有则创建
            int adminRoleId = 0;
            using (var checkRoleCommand = Connection.CreateCommand())
            {
                checkRoleCommand.CommandText = "SELECT Id FROM Roles WHERE RoleName = '管理员' LIMIT 1";
                var result = checkRoleCommand.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    adminRoleId = Convert.ToInt32(result);
                    // 如果管理员角色已存在，更新其权限为所有权限
                    using (var updateRoleCommand = Connection.CreateCommand())
                    {
                        updateRoleCommand.CommandText = @"
                            UPDATE Roles 
                            SET Permissions = @Permissions, UpdateTime = @UpdateTime 
                            WHERE Id = @Id";
                        updateRoleCommand.Parameters.AddWithValue("@Permissions", allPermissionsString);
                        updateRoleCommand.Parameters.AddWithValue("@UpdateTime", now);
                        updateRoleCommand.Parameters.AddWithValue("@Id", adminRoleId);
                        updateRoleCommand.ExecuteNonQuery();
                    }
                }
                else
                {
                    // 创建管理员角色（即使没有权限数据也要创建）
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

            // 检查是否已有admin账号，如果没有则创建
            using (var checkAccountCommand = Connection.CreateCommand())
            {
                checkAccountCommand.CommandText = "SELECT COUNT(*) FROM Accounts WHERE LoginAccount = 'admin'";
                var accountCount = Convert.ToInt32(checkAccountCommand.ExecuteScalar());
                if (accountCount == 0)
                {
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
                else
                {
                    // 如果admin账号已存在，确保其角色ID指向管理员角色
                    using (var updateAccountCommand = Connection.CreateCommand())
                    {
                        updateAccountCommand.CommandText = @"
                            UPDATE Accounts 
                            SET RoleId = @RoleId, UpdateTime = @UpdateTime 
                            WHERE LoginAccount = 'admin'";
                        updateAccountCommand.Parameters.AddWithValue("@RoleId", adminRoleId);
                        updateAccountCommand.Parameters.AddWithValue("@UpdateTime", now);
                        updateAccountCommand.ExecuteNonQuery();
                    }
                }
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
