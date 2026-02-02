using System;
using System.Data.SQLite;

namespace BankingSystem.Data
{
    public class DatabaseHelper : IDisposable
    {
        private readonly string _connectionString;
        private SQLiteConnection _sharedConnection;
        public string ConnectionString => _connectionString;

        public DatabaseHelper(string dbPath = "BankingSystem.db")
        {
            
            _connectionString = $"Data Source={dbPath};Version=3;Journal Mode=WAL;Pooling=True;Foreign Keys=True;";

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            
            using (var connection = CreateConnection())
            {
               
                ExecuteNonQuery(connection, "PRAGMA foreign_keys = ON;");

                ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS Customers (
                    CustomerId INTEGER PRIMARY KEY AUTOINCREMENT,
                    FirstName TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    Email TEXT NOT NULL UNIQUE,
                    Phone TEXT,
                    DateCreated TEXT DEFAULT CURRENT_TIMESTAMP
                )");

                ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS Accounts (
                    AccountId INTEGER PRIMARY KEY AUTOINCREMENT,
                    CustomerId INTEGER NOT NULL,
                    AccountType TEXT NOT NULL,
                    Balance DECIMAL(18,2) DEFAULT 0.00,
                    StandingBalance DECIMAL(18,2) DEFAULT 0.00,
                    DateOpened TEXT DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId) ON DELETE CASCADE
                )");

                ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS Transactions (
                    TransactionId INTEGER PRIMARY KEY AUTOINCREMENT,
                    AccountId INTEGER NOT NULL,
                    Amount DECIMAL(18,2) NOT NULL,
                    TransactionType TEXT NOT NULL,
                    Description TEXT,
                    TransactionDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (AccountId) REFERENCES Accounts(AccountId) ON DELETE CASCADE
                )");

                ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS Users (
                    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                    CustomerId INTEGER,
                    Username TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId) ON DELETE SET NULL
                )");
            }
        }

        public SQLiteConnection CreateConnection()
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private void ExecuteNonQuery(SQLiteConnection connection, string commandText)
        {
            using (var command = new SQLiteCommand(commandText, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        public SQLiteConnection GetSharedConnection()
        {
            if (_sharedConnection == null)
            {
                _sharedConnection = CreateConnection();
            }
            return _sharedConnection;
        }

        public void Dispose()
        {
            _sharedConnection?.Dispose();
            SQLiteConnection.ClearAllPools();
        }
    }
}