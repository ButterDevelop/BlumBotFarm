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
                    CREATE TABLE IF NOT EXISTS FeedbackMessages (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TelegramUserId INTEGER NOT NULL,
                        UserFeedbackOriginalMessageId INTEGER NOT NULL,
                        SupportFeedbackMessageId INTEGER NOT NULL,
                        IsReplied INTEGER NOT NULL,
                        SupportReplyMessageId INTEGER
                    );

                    CREATE TABLE IF NOT EXISTS StarsPayments (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL,
                        AmountUsd DECIMAL(10, 2) NOT NULL,
                        AmountStars INTEGER NOT NULL,
                        CreatedDateTime DATETIME NOT NULL,
                        IsCompleted INTEGER NOT NULL,
                        CompletedDateTime DATETIME NOT NULL,
                        FOREIGN KEY(UserId) REFERENCES Users(Id)
                    );

                    CREATE TABLE IF NOT EXISTS DailyRewards (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AccountId INTEGER NOT NULL,
                        CreatedAt DATETIME NOT NULL,
                        FOREIGN KEY(AccountId) REFERENCES Accounts(Id)
                    );

                    CREATE TABLE IF NOT EXISTS WalletPayments (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AmountUsd DECIMAL(10, 2) NOT NULL,
                        AutoConversionCurrency TEXT NOT NULL DEFAULT 'USDT',
                        Description TEXT NOT NULL DEFAULT 'Top up your balance',
                        ReturnUrl TEXT NOT NULL DEFAULT 'https://t.me/autoblumfarmbot',
                        FailReturnUrl TEXT NOT NULL DEFAULT 'https://t.me/wallet',
                        CustomData TEXT NOT NULL DEFAULT '',
                        ExternalId TEXT NOT NULL DEFAULT '',
                        TimeoutSeconds INTEGER NOT NULL DEFAULT 10800,
                        CustomerTelegramId INTEGER NOT NULL,
                        WalletOrderId INTEGER,
                        Status TEXT DEFAULT '',
                        OrderNumber TEXT DEFAULT '',
                        CreatedDateTime DATETIME,
                        ExpirationDateTime DATETIME,
                        CompletedDateTime DATETIME,
                        PayLink TEXT DEFAULT '',
                        DirectPayLink TEXT DEFAULT ''
                    );

                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TelegramUserId INTEGER NOT NULL,
                        FirstName TEXT,
                        LastName TEXT,
                        BalanceUSD REAL NOT NULL,
                        IsBanned INTEGER NOT NULL,
                        LanguageCode TEXT,
                        OwnReferralCode TEXT,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE IF NOT EXISTS Referrals (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        HostUserId INTEGER,
                        DependentUserId INTEGER,
                        FOREIGN KEY(HostUserId) REFERENCES Users(Id),
                        FOREIGN KEY(DependentUserId) REFERENCES Users(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Accounts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER,
                        CustomUsername TEXT,
                        BlumUsername TEXT,
                        Balance REAL,
                        Tickets INTEGER,
                        ReferralsCount INTEGER,
                        ReferralLink TEXT,
                        AccessToken TEXT,
                        RefreshToken TEXT,
                        ProviderToken TEXT,
                        UserAgent TEXT,
                        Proxy TEXT,
                        CountryCode TEXT,
                        ProxySellerListId INTEGER,
                        TimezoneOffset INTEGER,
                        LastStatus TEXT,
                        FOREIGN KEY(UserId) REFERENCES Users(Id)
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
                        ChatId BIGINT,
                        MessageText TEXT,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        IsSilent INTEGER
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
