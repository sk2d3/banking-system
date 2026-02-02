using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using BankingSystem.Forms;
using BankingSystem.Data;
using BankingSystem.Models;
using System.Data.SQLite;
using BankingSystem.Services;

namespace dashboard
{
    public partial class Dashboard : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
            (
                int nLeft,
                int nTop,
                int nRight,
                int nBottom,
                int nWidthEllipse,
                int nHeightEllipse
            );

        private Customer _customer;
        private readonly DatabaseHelper _dbHelper;
        private TransactionService _transactionService;
        private int _currentAccountId;
        public Dashboard(Customer customer)
        {
            this.MaximizeBox = false;
            InitializeComponent();
            _customer = customer;
            _dbHelper = new DatabaseHelper();
            _transactionService = new TransactionService(_dbHelper);
            dataGridView1.CellFormatting += dataGridView1_CellFormatting;



        }


        private void btnreq_Click(object sender, EventArgs e)
        {
            btntransfer.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btntransfer.Width, btntransfer.Height, 30, 30));
        }


        private void label14_Click_1(object sender, EventArgs e)
        {
            var dbHelper = new DatabaseHelper();
            LoginForm loginForm = new LoginForm(dbHelper);
            loginForm.Show();
            this.Close();
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }
        private void Dashboard_Load(object sender, EventArgs e)
        {
            panel2.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, panel2.Width, panel2.Height, 30, 30));
            panel3.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, panel3.Width, panel3.Height, 30, 30));
            panel4.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, panel4.Width, panel4.Height, 30, 30));
            panel6.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, panel6.Width, panel6.Height, 30, 30));
            panel5.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, panel5.Width, panel5.Height, 30, 30));
            if (_customer != null)
            {
                
                label11.Text = _customer.FirstName;
                label4.Text = _customer.LastName;
                label13.Text = _customer.Email;
                label22.Text = _customer.Phone;
                label23.Text = _customer.DateCreated.ToString();

                
                string query = "SELECT AccountId, AccountType, Balance, DateOpened FROM Accounts WHERE CustomerId = @CustomerId";

                using (var connection = new SQLiteConnection(_dbHelper.ConnectionString))
                using (var command = new SQLiteCommand(query, connection))
                {
                    connection.Open();
                    command.Parameters.AddWithValue("@CustomerId", _customer.CustomerId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _currentAccountId = (int)Convert.ToInt64(reader["AccountId"]);
                            label7.Text = $"₱{Convert.ToDecimal(reader["Balance"]):0.00}";
                            label24.Text = reader["AccountId"].ToString();

                           
                            LoadTransactionHistory();
                        }
                        else
                        {
                            label7.Text = "₱0.00";
                            label24.Text = "N/A";
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Customer is null.");
            }
        }

        private void button2_Click(object sender, EventArgs e) // Deposit
        {

            if (decimal.TryParse(textBox2.Text, out decimal amount))
            {
                string query = "SELECT AccountId, AccountType, Balance, DateOpened FROM Accounts WHERE CustomerId = @CustomerId";
                using (var connection = new SQLiteConnection(_dbHelper.ConnectionString))
                using (var command = new SQLiteCommand(query, connection))
                {
                    connection.Open();
                    command.Parameters.AddWithValue("@CustomerId", _customer.CustomerId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {

                            bool success = _transactionService.Deposit(_currentAccountId, amount, "ATM Deposit");

                            if (success)
                            {
                                MessageBox.Show($"Successfully deposited ₱{amount:0.00}", "Deposit Complete",
                                             MessageBoxButtons.OK, MessageBoxIcon.Information);
                                textBox2.Clear();
                                RefreshAccountBalance();
                                LoadTransactionHistory();
                            }
                            else
                            {
                                MessageBox.Show("Deposit failed. Please try again.", "Error",
                                             MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }

                }

            }
        }

        private void button1_Click(object sender, EventArgs e) 
        {
            if (decimal.TryParse(textBox2.Text, out decimal amount))
            {
                bool success = _transactionService.Withdraw(_currentAccountId, amount, "ATM Withdrawal");

                if (success)
                {
                    MessageBox.Show($"Successfully withdrew ₱{amount:0.00}", "Withdrawal Complete",
                                 MessageBoxButtons.OK, MessageBoxIcon.Information);
                    textBox2.Clear();
                    RefreshAccountBalance(); 
                    LoadTransactionHistory();
                }
                else
                {
                    MessageBox.Show("Withdrawal failed. Insufficient funds or invalid amount.", "Error",
                                 MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid amount", "Invalid Input",
                             MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void btntransfer_Click(object sender, EventArgs e)
        {
     
            if (!int.TryParse(textBox1.Text, out int targetAccountId) ||
                !decimal.TryParse(textBox3.Text, out decimal amount))
            {
                MessageBox.Show("Please enter valid account number and amount",
                              "Invalid Input",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (targetAccountId == _currentAccountId)
            {
                MessageBox.Show("Can't transfer to own account",
                              "Invalid Operation",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            bool success = _transactionService.Transfer(
                _currentAccountId,
                targetAccountId,
                amount,
                $"Transfer to account {targetAccountId}");

            if (success)
            {
                MessageBox.Show($"Successfully transferred ₱{amount:0.00} to account {targetAccountId}",
                              "Transfer Complete",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                textBox1.Clear();
                textBox3.Clear();
                RefreshAccountBalance();
                LoadTransactionHistory();
            }
            else
            {
                MessageBox.Show("Transfer failed. Insufficient funds or invalid accounts.",
                              "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshAccountBalance()
        {
            using (var connection = new SQLiteConnection(_dbHelper.ConnectionString))
            {
                connection.Open();
               
                string sql = "SELECT Balance FROM Accounts WHERE AccountId = @accountId";
                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@accountId", _currentAccountId);
                    var result = command.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        decimal balance = Convert.ToDecimal(result);
                        label7.Text = balance.ToString("C", new System.Globalization.CultureInfo("en-PH")); 

                        // Optional: Change color based on balance
                        //label7.ForeColor = balance >= 0 ? Color.Green : Color.Red;
                    }
                    else
                    {
                        //label7.Text = "N/A";
                        label7.Text = _currentAccountId.ToString();
                        label7.ForeColor = Color.Black;
                    }
                }
            }
        }
        private void LoadTransactionHistory()
        {
            try
            {
                
                var transactions = _transactionService.GetAccountTransactions(_currentAccountId);

                
                dataGridView1.AutoGenerateColumns = false; 
                dataGridView1.DataSource = transactions;

                
                dataGridView1.Columns.Clear();

                
                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn()
                {
                    DataPropertyName = "TransactionDate",
                    HeaderText = "Date",
                    Name = "colDate",
                    Width = 150,
                    DefaultCellStyle = new DataGridViewCellStyle()
                    {
                        Format = "g" 
                    }
                });

                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn()
                {
                    DataPropertyName = "TransactionType",
                    HeaderText = "Type",
                    Name = "colType",
                    Width = 100
                });

                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn()
                {
                    DataPropertyName = "Amount",
                    HeaderText = "Amount",
                    Name = "colAmount",
                    Width = 120,
                    DefaultCellStyle = new DataGridViewCellStyle()
                    {
                        Format = "\"₱\"#,##0.00", 
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        NullValue = "₱0.00"
                    }
                });

                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn()
                {
                    DataPropertyName = "Description",
                    HeaderText = "Description",
                    Name = "colDescription",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                });

                
                dataGridView1.EnableHeadersVisualStyles = false;
                dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.Navy;
                dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);
                dataGridView1.RowHeadersVisible = false;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading transaction history: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
        
            if (System.Text.RegularExpressions.Regex.IsMatch(textBox2.Text, "[^0-9.]"))
            {
                textBox2.Text = textBox2.Text.Remove(textBox2.Text.Length - 1);
                textBox2.SelectionStart = textBox2.Text.Length;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "colAmount" && e.Value != null)
            {
                var row = dataGridView1.Rows[e.RowIndex];
                if (row.Cells["colType"].Value?.ToString() == "Withdrawal")
                {
                    e.CellStyle.ForeColor = Color.Red;
                    
                }
                else
                {
                    e.CellStyle.ForeColor = Color.Green;
                }
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
        private void NumericTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
           
            if (char.IsControl(e.KeyChar))
                return;

            TextBox txt = sender as TextBox;

            if (char.IsDigit(e.KeyChar))
                return;

           
            //if (e.KeyChar == '.' && !txt.Text.Contains("."))
            //    return;

            e.Handled = true;
        }


    }
}
