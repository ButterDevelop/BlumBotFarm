using BlumBotFarm.Core;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace BlumBotFarm.Database
{
    public static class Database
    {
        private static readonly string ConnectionString = AppConfig.DatabaseSettings.ConnectionString ?? "Data Source=blumbotfarmDefault.db";

        public static void Initialize()
        {
            using (IDbConnection db = new SqliteConnection(ConnectionString))
            {
                db.Execute(@"
                    CREATE TABLE IF NOT EXISTS Accounts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT,
                        Balance REAL,
                        Tickets INTEGER,
                        AccessToken TEXT,
                        RefreshToken TEXT,
                        ProviderToken TEXT,
                        UserAgent TEXT,
                        Proxy TEXT,
                        TimezoneOffset INTEGER
                    );

                    CREATE TABLE IF NOT EXISTS Tasks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AccountId INTEGER,
                        TaskType TEXT,
                        MinScheduleSeconds INTEGER,
                        MaxScheduleSeconds INTEGER,
                        NextRunTime DATETIME,
                        FOREIGN KEY(AccountId) REFERENCES Accounts(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Messages (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ChatId TEXT,
                        MessageText TEXT,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE IF NOT EXISTS Earnings (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AccountId INTEGER,
                        Total REAL,
                        Created DATETIME,
                        Action TEXT,
                        FOREIGN KEY(AccountId) REFERENCES Accounts(Id)
                    );
                ");
            }
        }

        public static IDbConnection GetConnection()
        {
            return new SqliteConnection(ConnectionString);
        }
    }
}
