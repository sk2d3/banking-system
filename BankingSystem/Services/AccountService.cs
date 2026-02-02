using BankingSystem.Data;
using BankingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

//No use na
namespace BankingSystem.Services
{
    class AccountService
    {
        private readonly DatabaseHelper _dbHelper;

        public AccountService(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public int CreateAccount(int customerId, string accountType)
        {
            using (var connection = new SQLiteConnection(_dbHelper.ConnectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(@"
                INSERT INTO Accounts (CustomerId, AccountType)
                VALUES (@customerId, @accountType);
                SELECT last_insert_rowid();", connection))
                {
                    command.Parameters.AddWithValue("@customerId", customerId);
                    command.Parameters.AddWithValue("@accountType", accountType);

                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }


        public bool DeleteAccount(int accountId)
        {
            using (var connection = new SQLiteConnection(_dbHelper.ConnectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand("DELETE FROM Accounts WHERE AccountId = @accountId", connection))
                {
                    command.Parameters.AddWithValue("@accountId", accountId);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public Account GetAccount(int accountId)
        {
            using (var connection = new SQLiteConnection(_dbHelper.ConnectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand("SELECT * FROM Accounts WHERE AccountId = @accountId", connection))
                {
                    command.Parameters.AddWithValue("@accountId", accountId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Account
                            {
                                AccountId = reader.GetInt32(0),
                                CustomerId = reader.GetInt32(1),
                                AccountType = reader.GetString(2),
                                Balance = Convert.ToDecimal(reader.GetValue(3)),
                                DateOpened = DateTime.Parse(reader.GetString(4))
                            };
                        }
                    }
                }
            }

            return null;
        }

        public List<Account> GetCustomerAccounts(int customerId)
        {
            var accounts = new List<Account>();

            using (var connection = new SQLiteConnection(_dbHelper.ConnectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand("SELECT * FROM Accounts WHERE CustomerId = @customerId", connection))
                {
                    command.Parameters.AddWithValue("@customerId", customerId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            accounts.Add(new Account
                            {
                                AccountId = reader.GetInt32(0),
                                CustomerId = reader.GetInt32(1),
                                AccountType = reader.GetString(2),
                                Balance = Convert.ToDecimal(reader.GetValue(3)),
                                DateOpened = DateTime.Parse(reader.GetString(4))
                            });
                        }
                    }
                }
            }

            return accounts;
        }
    }
}
