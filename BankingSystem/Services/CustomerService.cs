//using BankingSystem.Data;
//using BankingSystem.Models;
//using System;
//using System.Collections.Generic;
//using System.Data.SQLite;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace BankingSystem.Services
//{
//    class CustomerService
//    {
//        private readonly DatabaseHelper _dbHelper;

//        public CustomerService(DatabaseHelper dbHelper)
//        {
//            _dbHelper = dbHelper;
//        }

//        public int CreateCustomer(string firstName, string lastName, string email, string phone)
//        {
//            var connection = new SqliteConnection(_dbHelper.ConnectionString);
//            connection.Open();

//            var command = connection.CreateCommand();
//            command.CommandText = @"
//            INSERT INTO Customers (FirstName, LastName, Email, Phone)
//            VALUES (@firstName, @lastName, @email, @phone);
//            SELECT last_insert_rowid();";

//            command.Parameters.AddWithValue("@firstName", firstName);
//            command.Parameters.AddWithValue("@lastName", lastName);
//            command.Parameters.AddWithValue("@email", email);
//            command.Parameters.AddWithValue("@phone", phone);

//            return Convert.ToInt32(command.ExecuteScalar());
//        }

//        public bool UpdateCustomer(int customerId, string firstName, string lastName, string email, string phone)
//        {
//            var connection = new SqliteConnection(_dbHelper.ConnectionString);
//            connection.Open();

//            var command = connection.CreateCommand();
//            command.CommandText = @"
//            UPDATE Customers 
//            SET FirstName = @firstName,
//                LastName = @lastName,
//                Email = @email,
//                Phone = @phone
//            WHERE CustomerId = @customerId";

//            command.Parameters.AddWithValue("@firstName", firstName);
//            command.Parameters.AddWithValue("@lastName", lastName);
//            command.Parameters.AddWithValue("@email", email);
//            command.Parameters.AddWithValue("@phone", phone);
//            command.Parameters.AddWithValue("@customerId", customerId);

//            return command.ExecuteNonQuery() > 0;
//        }

//        public bool DeleteCustomer(int customerId)
//        {
//            var connection = new SqliteConnection(_dbHelper.ConnectionString);
//            connection.Open();

//            var command = connection.CreateCommand();
//            command.CommandText = "DELETE FROM Customers WHERE CustomerId = @customerId";
//            command.Parameters.AddWithValue("@customerId", customerId);

//            return command.ExecuteNonQuery() > 0;
//        }

//        public Customer GetCustomer(int customerId)
//        {
//            var connection = new SqliteConnection(_dbHelper.ConnectionString);
//            connection.Open();

//            var command = connection.CreateCommand();
//            command.CommandText = "SELECT * FROM Customers WHERE CustomerId = @customerId";
//            command.Parameters.AddWithValue("@customerId", customerId);

//            var reader = command.ExecuteReader();
//            if (reader.Read())
//            {
//                return new Customer
//                {
//                    CustomerId = reader.GetInt32(0),
//                    FirstName = reader.GetString(1),
//                    LastName = reader.GetString(2),
//                    Email = reader.GetString(3),
//                    Phone = reader.IsDBNull(4) ? null : reader.GetString(4),
//                    DateCreated = reader.GetDateTime(5)
//                };
//            }

//            return null;
//        }

//        public List<Customer> GetAllCustomers()
//        {
//            var customers = new List<Customer>();

//            var connection = new SqliteConnection(_dbHelper.ConnectionString);
//            connection.Open();

//            var command = connection.CreateCommand();
//            command.CommandText = "SELECT * FROM Customers ORDER BY LastName, FirstName";

//            var reader = command.ExecuteReader();
//            while (reader.Read())
//            {
//                customers.Add(new Customer
//                {
//                    CustomerId = reader.GetInt32(0),
//                    FirstName = reader.GetString(1),
//                    LastName = reader.GetString(2),
//                    Email = reader.GetString(3),
//                    Phone = reader.IsDBNull(4) ? null : reader.GetString(4),
//                    DateCreated = reader.GetDateTime(5)
//                });
//            }

//            return customers;
//        }
//    }
//}
