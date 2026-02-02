using BankingSystem.Data;
using BankingSystem.Models;
using System;
using System.Data.SQLite;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace BankingSystem.Services
{
    class AuthService
    {
        private readonly DatabaseHelper _dbHelper;

        public AuthService(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public (bool success, string role, Customer customer) Login(string username, string password)
        {
            using (var connection = new SQLiteConnection(_dbHelper.ConnectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand("SELECT * FROM Users WHERE Username = @username", connection))
                {
                    command.Parameters.AddWithValue("@username", username);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())   
                        {
                            var storedPassword = reader["Password"].ToString();
                            var role = reader["Role"].ToString();
                            var customerId = reader["CustomerId"].ToString();

                            if (VerifyPassword(password, storedPassword))
                            {
                                using (var custCommand = new SQLiteCommand("SELECT * FROM Customers WHERE CustomerId = @CustomerId", connection))
                               
                                {
                                    custCommand.Parameters.AddWithValue("@CustomerId", customerId);
                                    
                                    using (var custReader = custCommand.ExecuteReader())
           
                                    {
                                        if (custReader.Read())
                                        {
                                            var customer = new Customer
                                            {
                                                CustomerId = Convert.ToInt32(custReader["CustomerId"]),
                                                FirstName = custReader["FirstName"].ToString(),
                                                LastName = custReader["LastName"].ToString(),
                                                Email = custReader["Email"].ToString(),
                                                Phone = custReader["Phone"].ToString(),
                                                DateCreated = Convert.ToDateTime(custReader["DateCreated"])
                                            };


                                            return (true, role, customer);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return (false, null, null);
        }


        private bool VerifyPassword(string password, string storedPassword)
        {
            return password == storedPassword;
        }
    }
}
