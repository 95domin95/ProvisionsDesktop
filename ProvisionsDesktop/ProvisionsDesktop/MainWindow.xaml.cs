using ProvisionsDesktop.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProvisionsDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const int SaltSize = 16; // 128 bit 
        private const int KeySize = 32; // 256 bit
        private const int Iterations = 1000;
        public MainWindow()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName]string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private void LoginClick(object sender, RoutedEventArgs e)
        {
            var userName = loginTextBox.Text;
            var password = loginPasswordBox.Password;

            var user = Login(userName, password);

            if (user is null)
            {
                MessageBox.Show(
                    "Hasło lub login niepoprawne",
                    "Nie udało się zalogować",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                new HomePage(user).Show();
                this.Close();
            }
        }

        private User Login(string userName, string password)
        {
            User user = null;

            using (SqlConnection connection = new SqlConnection(
                Properties.Settings.Default.connectionString))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = @"SELECT
                                                [Id],
                                                [Email],
                                                [PasswordHash]
                                            FROM [User]
                                            WHERE [Email] = @UserName";

                    SqlParameter dp = command.Parameters.Add("@UserName", SqlDbType.VarChar);
                    dp.Value = userName;

                    connection.Open();

                    using (SqlDataReader dataReader = command.ExecuteReader())
                    {
                        string passwordHash = null;
                        if(dataReader.HasRows)
                        {
                            int idx_Id = dataReader.GetOrdinal("Id");
                            int idx_Email = dataReader.GetOrdinal("Email");
                            int idx_PasswordHash = dataReader.GetOrdinal("PasswordHash");

                            user = new User();
                            if(dataReader.Read())
                            {
                                if (!dataReader.IsDBNull(idx_Email))
                                {
                                    user.Email = dataReader.GetString(idx_Email);
                                }
                                if (!dataReader.IsDBNull(idx_Id))
                                {
                                    user.Id = dataReader.GetInt32(idx_Id);
                                }
                                if (!dataReader.IsDBNull(idx_PasswordHash))
                                {
                                    passwordHash = dataReader.GetString(idx_PasswordHash);
                                }
                            }
                            if (dataReader.Read())
                            {
                                MessageBox.Show("Znaleziono więcej niż 1 użytkownika o tej samej nazwie",
                                    "Błąd",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                                return null;
                            }
                        }
                        if (string.IsNullOrEmpty(passwordHash) || !Check(passwordHash, password))
                        {
                            return null;
                        }
                    }
                }
            }
            return user;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key.Equals(Key.Enter))
            {
                LoginClick(new object(), new RoutedEventArgs());
            }
        }

        private bool Check(string hash, string password)
        {
            var parts = hash.Split('.');

            if (parts.Length != 3)
            {
                throw new FormatException("Unexpected hash format. " +
                  "Should be formatted as `{iterations}.{salt}.{hash}`");
            }

            var iterations = Convert.ToInt32(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var key = Convert.FromBase64String(parts[2]);

            using (var algorithm = new Rfc2898DeriveBytes(
              password,
              salt,
              iterations,
              HashAlgorithmName.SHA256))
            {
                var keyToCheck = algorithm.GetBytes(KeySize);

                var verified = keyToCheck.SequenceEqual(key);

                return verified;
            }
        }
    }
}
