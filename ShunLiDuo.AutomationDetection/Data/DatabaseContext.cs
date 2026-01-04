using System;
using System.Data.SQLite;
using System.IO;

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
                    Role TEXT,
                    Remark TEXT,
                    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdateTime DATETIME
                );";

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

