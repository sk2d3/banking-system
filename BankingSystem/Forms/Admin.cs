using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using BankingSystem.Data;

namespace BankingSystem.Forms
{
    public partial class Admin : Form
    {
        private readonly DatabaseHelper _dbHelper;
        private string _currentTable = "";
        private DataTable _dataTable = new DataTable();
        private bool _isDirty = false;

        public Admin(DatabaseHelper dbHelper)
        {
            InitializeComponent();
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
            ConfigureDataGridView();
        }

        private void ConfigureDataGridView()
        {
            dataGridViewTables.AutoGenerateColumns = true;
            dataGridViewTables.AllowUserToAddRows = true;
            dataGridViewTables.AllowUserToDeleteRows = true;
            dataGridViewTables.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            dataGridViewTables.CellBeginEdit += (s, e) => _isDirty = true;
            dataGridViewTables.UserDeletingRow += (s, e) => _isDirty = true;
            dataGridViewTables.DataError += DataGridViewTables_DataError;
        }

        private void Admin_Load(object sender, EventArgs e)
        {
            comboBoxTables.SelectedIndex = 0;
            LoadTableData(comboBoxTables.SelectedItem.ToString());
        }

        private void comboBoxTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("You have unsaved changes. Do you want to save before switching tables?",
                    "Unsaved Changes", MessageBoxButtons.YesNoCancel);

                if (result == DialogResult.Yes)
                {
                    SaveChanges();
                }
                else if (result == DialogResult.Cancel)
                {
                    comboBoxTables.SelectedItem = _currentTable;
                    return;
                }
            }
            LoadTableData(comboBoxTables.SelectedItem.ToString());
        }

        private void LoadTableData(string tableName)
        {
            try
            {
                using (var connection = _dbHelper.CreateConnection())
                {
                    _currentTable = tableName;
                    string query = $"SELECT * FROM {tableName}";

                    using (var adapter = new SQLiteDataAdapter(query, connection))
                    {
                        var commandBuilder = new SQLiteCommandBuilder(adapter);
                        adapter.InsertCommand = commandBuilder.GetInsertCommand();
                        adapter.UpdateCommand = commandBuilder.GetUpdateCommand();
                        adapter.DeleteCommand = commandBuilder.GetDeleteCommand();

                        _dataTable = new DataTable();  
                        adapter.Fill(_dataTable);

    
                        switch (tableName)
                        {
                            case "Accounts":
                                _dataTable.PrimaryKey = new[] { _dataTable.Columns["AccountId"] };
                                break;
                            case "Customers":
                                _dataTable.PrimaryKey = new[] { _dataTable.Columns["CustomerId"] };
                                break;
                            case "Transactions":
                                _dataTable.PrimaryKey = new[] { _dataTable.Columns["TransactionId"] };
                                break;
                            case "Users":
                                _dataTable.PrimaryKey = new[] { _dataTable.Columns["UserId"] };
                                break;
                        }

                        dataGridViewTables.DataSource = _dataTable;
                        _isDirty = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading table: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            SaveChanges();
        }

        private void SaveChanges()
        {
            try
            {
                using (var connection = _dbHelper.CreateConnection())
                {
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string query = $"SELECT * FROM {_currentTable} WHERE 1=0"; 

                            using (var adapter = new SQLiteDataAdapter(query, connection))
                            {
                                var commandBuilder = new SQLiteCommandBuilder(adapter)
                                {
                                    ConflictOption = ConflictOption.OverwriteChanges
                                };

                                
                                adapter.InsertCommand = commandBuilder.GetInsertCommand();
                                adapter.UpdateCommand = commandBuilder.GetUpdateCommand();
                                adapter.DeleteCommand = commandBuilder.GetDeleteCommand();

                                
                                DataTable changes = _dataTable.GetChanges();

                                if (changes != null)
                                {
                                    int rowsAffected = adapter.Update(changes);
                                    transaction.Commit();

                                    MessageBox.Show($"{rowsAffected} records updated successfully.", "Success",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                                    _dataTable.AcceptChanges();
                                    _isDirty = false;
                                }
                                else
                                {
                                    MessageBox.Show("No changes to save.", "Information",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw; 
                        }
                    }
                }
            }
            catch (SQLiteException sqlEx)
            {
                MessageBox.Show($"Database error: {sqlEx.Message}\nError Code: {sqlEx.ErrorCode}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}\n\n{ex.InnerException?.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("You have unsaved changes. Refresh anyway?",
                    "Unsaved Changes", MessageBoxButtons.YesNo);

                if (result != DialogResult.Yes) return;
            }
            LoadTableData(_currentTable);
        }

        private void DataGridViewTables_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show($"Data error: {e.Exception.Message}", "Input Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            e.ThrowException = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("You have unsaved changes. Do you want to save before exiting?",
                    "Unsaved Changes", MessageBoxButtons.YesNoCancel);

                if (result == DialogResult.Yes)
                {
                    SaveChanges();
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnFormClosing(e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var dbHelper = new DatabaseHelper();
            LoginForm loginForm = new LoginForm(dbHelper);
            loginForm.Show();
            this.Close();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            
            Form createUserAccountForm = new Form()
            {
                Text = "Create New User & Account",
                Width = 450,
                Height = 500,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label lblEmailStatus = new Label() { Left = 410, Top = 110, Width = 30, Text = "❌" };
            Label lblPhoneStatus = new Label() { Left = 410, Top = 140, Width = 30, Text = "❌" };
            Label lblPasswordStatus = new Label() { Left = 410, Top = 200, Width = 30, Text = "❌" };

            Label lblUserInfo = new Label() { Text = "User Information", Left = 20, Top = 20, Width = 400, Font = new Font(Font, FontStyle.Bold) };

            Label lblFirstName = new Label() { Text = "First Name:", Left = 20, Top = 50, Width = 100 };
            TextBox txtFirstName = new TextBox() { Left = 150, Top = 50, Width = 250 };

            Label lblLastName = new Label() { Text = "Last Name:", Left = 20, Top = 80, Width = 100 };
            TextBox txtLastName = new TextBox() { Left = 150, Top = 80, Width = 250 };

            Label lblEmail = new Label() { Text = "Email:", Left = 20, Top = 110, Width = 100 };
            TextBox txtEmail = new TextBox() { Left = 150, Top = 110, Width = 250 };

            Label lblPhone = new Label() { Text = "Phone:", Left = 20, Top = 140, Width = 100 };
            MaskedTextBox mtxtPhone = new MaskedTextBox()
            {
                Left = 150,
                Top = 140,
                Width = 250,
                Mask = "00000000000",
                PromptChar = ' ',
                Text = "09",
                HidePromptOnLeave = true
            };

            Label lblUsername = new Label() { Text = "Username:", Left = 20, Top = 170, Width = 100 };
            TextBox txtUsername = new TextBox() { Left = 150, Top = 170, Width = 250 };

            Label lblPassword = new Label() { Text = "Password:", Left = 20, Top = 200, Width = 100 };
            TextBox txtPassword = new TextBox() { Left = 150, Top = 200, Width = 250, PasswordChar = '*' };

            
            Label lblAccountInfo = new Label() { Text = "Account Information", Left = 20, Top = 240, Width = 400, Font = new Font(Font, FontStyle.Bold) };

            Label lblAccountType = new Label() { Text = "Account Type:", Left = 20, Top = 270, Width = 100 };
            ComboBox cmbAccountType = new ComboBox() { Left = 150, Top = 270, Width = 250 };
            cmbAccountType.Items.AddRange(new string[] { "Savings", "Checking" });

            Label lblInitialDeposit = new Label() { Text = "Initial Deposit:", Left = 20, Top = 300, Width = 100 };
            NumericUpDown numInitialDeposit = new NumericUpDown() { Left = 150, Top = 300, Width = 250, Minimum = 0, Maximum = 1000000, DecimalPlaces = 2 };

            Label lblStandingBalance = new Label() { Text = "Set standing balance:", Left = 20, Top = 330, Width = 100 };
            ComboBox cmbStandingBalance = new ComboBox() { Left = 150, Top = 330, Width = 250 };
            cmbStandingBalance.Items.AddRange(new string[] { "0", "5000" });

            Button btnCreate = new Button() { Text = "Create", Left = 150, Top = 370, Width = 100, DialogResult = DialogResult.OK };
            Button btnCancel = new Button() { Text = "Cancel", Left = 270, Top = 370, Width = 100, DialogResult = DialogResult.Cancel };

            
            createUserAccountForm.Controls.AddRange(new Control[] {
                lblUserInfo,
                lblFirstName, txtFirstName,
                lblLastName, txtLastName,
                lblEmail, txtEmail,
                lblPhone, mtxtPhone,
                lblUsername, txtUsername,
                lblPassword, txtPassword,
                lblAccountInfo,
                lblAccountType, cmbAccountType,
                lblInitialDeposit, numInitialDeposit,
                btnCreate, btnCancel,
                lblEmailStatus, lblPhoneStatus, lblPasswordStatus,
                lblStandingBalance, cmbStandingBalance
            });

            mtxtPhone.Enter += (s, eventArgs) =>
            {
                mtxtPhone.SelectionStart = 2;
            };


            mtxtPhone.KeyDown += (s, keyEventArgs) =>
            {
                if (mtxtPhone.SelectionStart < 2)
                {
                    keyEventArgs.SuppressKeyPress = true;
                }
            };
            {
                mtxtPhone.SelectionStart = 2;
            };


            txtEmail.TextChanged += (s, emailEventArgs) =>
            {
                lblEmailStatus.Text = (txtEmail.Text.Contains("@") && txtEmail.Text.Contains(".")) ? "✅" : "❌";
            };

            mtxtPhone.TextChanged += (s, phoneEventArgs) =>
            {
                lblPhoneStatus.Text = mtxtPhone.MaskCompleted ? "✅" : "❌";
            };

            txtPassword.TextChanged += (s, passwordEventArgs) =>
            {
                lblPasswordStatus.Text = txtPassword.Text.Length >= 8 ? "✅" : "❌";
            };


            if (createUserAccountForm.ShowDialog() == DialogResult.OK)
            {
                
                if (string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                    string.IsNullOrWhiteSpace(txtLastName.Text) ||
                    string.IsNullOrWhiteSpace(txtEmail.Text) ||
                    string.IsNullOrWhiteSpace(txtUsername.Text) ||
                    string.IsNullOrWhiteSpace(txtPassword.Text) ||
                    string.IsNullOrWhiteSpace(cmbAccountType.Text))
                {
                    MessageBox.Show("Please fill in all required fields", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                
                if (!txtEmail.Text.Contains("@") || !txtEmail.Text.Contains("."))
                {
                    MessageBox.Show("Please enter a valid email address", "Invalid Email", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                
                if (!mtxtPhone.MaskCompleted)
                {
                    MessageBox.Show("Phone number must be exactly 11 digits", "Invalid Phone", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                
                if (txtPassword.Text.Length < 8)
                {
                    MessageBox.Show("Password must be at least 8 characters long", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    using (var connection = new SQLiteConnection(_dbHelper.ConnectionString))
                    {
                        connection.Open();
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                
                                using (var emailCheckCommand = new SQLiteCommand(
                                    "SELECT COUNT(*) FROM Customers WHERE Email = @email",
                                    connection, transaction))
                                {
                                    emailCheckCommand.Parameters.AddWithValue("@email", txtEmail.Text);
                                    int emailCount = Convert.ToInt32(emailCheckCommand.ExecuteScalar());
                                    if (emailCount > 0)
                                    {
                                        MessageBox.Show("This email is already registered", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        return;
                                    }
                                }

                                
                                using (var userCheckCommand = new SQLiteCommand(
                                    "SELECT COUNT(*) FROM Users WHERE Username = @username",
                                    connection, transaction))
                                {
                                    userCheckCommand.Parameters.AddWithValue("@username", txtUsername.Text);
                                    int userCount = Convert.ToInt32(userCheckCommand.ExecuteScalar());
                                    if (userCount > 0)
                                    {
                                        MessageBox.Show("This username is already taken", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        return;
                                    }
                                }

                                
                                int newCustomerId;
                                using (var customerCommand = new SQLiteCommand(
                                    @"INSERT INTO Customers (FirstName, LastName, Email, Phone) 
                              VALUES (@firstName, @lastName, @email, @phone);
                              SELECT last_insert_rowid();",
                                    connection, transaction))
                                {
                                    customerCommand.Parameters.AddWithValue("@firstName", txtFirstName.Text);
                                    customerCommand.Parameters.AddWithValue("@lastName", txtLastName.Text);
                                    customerCommand.Parameters.AddWithValue("@email", txtEmail.Text);
                                    customerCommand.Parameters.AddWithValue("@phone", mtxtPhone.Text); 
                                    newCustomerId = Convert.ToInt32(customerCommand.ExecuteScalar());
                                }

                                
                                using (var userCommand = new SQLiteCommand(
                                    @"INSERT INTO Users (CustomerId, Username, Password, Role) 
                              VALUES (@customerId, @username, @password, 'Customer')",
                                    connection, transaction))
                                {
                                    string hashedPassword = txtPassword.Text;

                                    userCommand.Parameters.AddWithValue("@customerId", newCustomerId);
                                    userCommand.Parameters.AddWithValue("@username", txtUsername.Text);
                                    userCommand.Parameters.AddWithValue("@password", hashedPassword);
                                    userCommand.ExecuteNonQuery();
                                }

                                
                                using (var accountCommand = new SQLiteCommand(
                                    @"INSERT INTO Accounts (CustomerId, AccountType, Balance, StandingBalance) 
                              VALUES (@customerId, @accountType, @balance, @standingBalance)",
                                    connection, transaction))
                                {
                                    if (!int.TryParse(cmbStandingBalance.Text, out int standingBalance))
                                    {
                                
                                        standingBalance = 0; 
                                    }
                                    accountCommand.Parameters.AddWithValue("@customerId", newCustomerId);
                                    accountCommand.Parameters.AddWithValue("@accountType", cmbAccountType.Text);
                                    accountCommand.Parameters.AddWithValue("@balance", numInitialDeposit.Value);
                                    accountCommand.Parameters.AddWithValue("@standingBalance", standingBalance);
                                    accountCommand.ExecuteNonQuery();
                                }

                                transaction.Commit();
                                MessageBox.Show("User and account created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                MessageBox.Show("Error creating user and account: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}