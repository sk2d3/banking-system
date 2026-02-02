using BankingSystem.Data;
using BankingSystem.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace BankingSystem.Services
{
    class TransactionService
    {
        private readonly DatabaseHelper _dbHelper;

        public TransactionService(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public bool Deposit(int accountId, decimal amount, string description = null)
        {
            if (amount <= 0)
            {
                Console.WriteLine($"Deposit failed: Invalid amount {amount}");
                return false;
            }

            using (var connection = new SQLiteConnection(_dbHelper.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                     
                        using (var updateCommand = new SQLiteCommand(
                            @"UPDATE Accounts 
                            SET Balance = Balance + @amount 
                            WHERE AccountId = @accountId", connection, transaction))
                                {
                            updateCommand.Parameters.AddWithValue("@amount", amount);
                            updateCommand.Parameters.AddWithValue("@accountId", accountId);

                            
                            Console.WriteLine($"Executing: {updateCommand.CommandText}");
                            Console.WriteLine($"Parameters: amount={amount}, accountId={accountId}");

                            int rowsAffected = updateCommand.ExecuteNonQuery();

                            if (rowsAffected == 0)
                            {
                                Console.WriteLine("Deposit failed: Account not found");
                                transaction.Rollback();
                                return false;
                            }
                        }

                        
                        RecordTransaction(connection, transaction, accountId, amount, "Deposit", description);

                        transaction.Commit();
                        Console.WriteLine("Deposit successful");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Deposit failed: {ex.Message}");
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        public bool Withdraw(int accountId, decimal amount, string description = null)
        {
            if (amount <= 0) return false;

            using (var connection = new SQLiteConnection(_dbHelper.ConnectionString))
            {
                connection.Open();
                using (var dbTransaction = connection.BeginTransaction())
                {
                    try
                    {
                        
                        using (var checkCommand = new SQLiteCommand(
                            "SELECT Balance FROM Accounts WHERE AccountId = @accountId",
                            connection, dbTransaction))
                        {
                            checkCommand.Parameters.AddWithValue("@accountId", accountId);
                            var balance = Convert.ToDecimal(checkCommand.ExecuteScalar());

                            if (balance < amount) return false;
                        }

                        
                        using (var updateCommand = new SQLiteCommand(
                            @"UPDATE Accounts 
                            SET Balance = Balance - @amount 
                            WHERE AccountId = @accountId",
                            connection, dbTransaction))
                        {
                            updateCommand.Parameters.AddWithValue("@amount", amount);
                            updateCommand.Parameters.AddWithValue("@accountId", accountId);
                            updateCommand.ExecuteNonQuery();
                        }

                        
                        RecordTransaction(connection, dbTransaction, accountId, amount, "Withdrawal", description);

                        dbTransaction.Commit();
                        return true;
                    }
                    catch
                    {
                        dbTransaction.Rollback();
                        return false;
                    }
                }
            }
        }

        public bool Transfer(int fromAccountId, int toAccountId, decimal amount, string description = null)
        {
            
            if (amount <= 0) return false;
            if (fromAccountId == toAccountId) return false; 

            using (var connection = new SQLiteConnection(_dbHelper.ConnectionString))
            {
                connection.Open();

               
                decimal fromBalance;
                bool toAccountExists;

                
                using (var checkCommand = new SQLiteCommand(
                    "SELECT Balance FROM Accounts WHERE AccountId = @accountId",
                    connection))
                {
                    checkCommand.Parameters.AddWithValue("@accountId", fromAccountId);
                    var result = checkCommand.ExecuteScalar();
                    if (result == null) return false; 
                    fromBalance = Convert.ToDecimal(result);
                    if (fromBalance < amount) return false;
                }

               
                using (var existsCommand = new SQLiteCommand(
                    "SELECT 1 FROM Accounts WHERE AccountId = @accountId",
                    connection))
                {
                    existsCommand.Parameters.AddWithValue("@accountId", toAccountId);
                    toAccountExists = (existsCommand.ExecuteScalar() != null);
                    if (!toAccountExists) return false;
                }

                
                using (var dbTransaction = connection.BeginTransaction())
                {
                    try
                    {
                        
                        using (var withdrawCommand = new SQLiteCommand(
                            @"UPDATE Accounts 
                    SET Balance = Balance - @amount 
                    WHERE AccountId = @accountId",
                            connection, dbTransaction))
                        {
                            withdrawCommand.Parameters.AddWithValue("@amount", amount);
                            withdrawCommand.Parameters.AddWithValue("@accountId", fromAccountId);
                            withdrawCommand.ExecuteNonQuery();
                        }

                    
                        using (var depositCommand = new SQLiteCommand(
                            @"UPDATE Accounts 
                    SET Balance = Balance + @amount 
                    WHERE AccountId = @accountId",
                            connection, dbTransaction))
                        {
                            depositCommand.Parameters.AddWithValue("@amount", amount);
                            depositCommand.Parameters.AddWithValue("@accountId", toAccountId);
                            depositCommand.ExecuteNonQuery();
                        }

                       
                        RecordTransaction(connection, dbTransaction, fromAccountId, amount, "Transfer Out", description);
                        RecordTransaction(connection, dbTransaction, toAccountId, amount, "Transfer In", description);

                        dbTransaction.Commit();
                        return true;
                    }
                    catch
                    {
                        dbTransaction.Rollback();
                        return false;
                    }
                }
            }
        }

        private void RecordTransaction(SQLiteConnection connection, SQLiteTransaction transaction,
            int accountId, decimal amount, string transactionType, string description)
        {
            using (var command = new SQLiteCommand(
                @"INSERT INTO Transactions (AccountId, Amount, TransactionType, Description)
                VALUES (@accountId, @amount, @transactionType, @description)",
                connection, transaction))
            {
                command.Parameters.AddWithValue("@accountId", accountId);
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@transactionType", transactionType);
                command.Parameters.AddWithValue("@description", string.IsNullOrEmpty(description) ? DBNull.Value : (object)description);

                command.ExecuteNonQuery();
            }
        }

        public List<Transaction> GetAccountTransactions(int accountId)
        {
            var transactions = new List<Transaction>();

            using (var connection = new SQLiteConnection(_dbHelper.ConnectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(
                    "SELECT * FROM Transactions WHERE AccountId = @accountId ORDER BY TransactionDate DESC",
                    connection))
                {
                    command.Parameters.AddWithValue("@accountId", accountId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            transactions.Add(new Transaction
                            {
                                TransactionId = Convert.ToInt32(reader["TransactionId"]),
                                AccountId = Convert.ToInt32(reader["AccountId"]),
                                Amount = Convert.ToDecimal(reader["Amount"]),
                                TransactionType = reader["TransactionType"].ToString(),
                                Description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
                                TransactionDate = Convert.ToDateTime(reader["TransactionDate"])
                            });
                        }
                    }
                }
            }

            return transactions;
        }
    }
}