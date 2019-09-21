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
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.CommandText = "p_login";

                    SqlParameter dp = command.Parameters.Add("@UserName", SqlDbType.VarChar);
                    dp.Value = userName;

                    dp = command.Parameters.Add("@PasswordHash", SqlDbType.VarChar);
                    dp.Value = password;

                    connection.Open();

                    using (SqlDataReader dataReader = command.ExecuteReader())
                    {
                        if(dataReader.HasRows)
                        {
                            int idx_UserName = dataReader.GetOrdinal("UserName");
                            int idx_Id = dataReader.GetOrdinal("Id");
                            int idx_Email = dataReader.GetOrdinal("Email");

                            user = new User();
                            if(dataReader.Read())
                            {
                                if (!dataReader.IsDBNull(idx_UserName))
                                {
                                    user.UserName = dataReader.GetString(idx_UserName);
                                }
                                if (!dataReader.IsDBNull(idx_Email))
                                {
                                    user.Email = dataReader.GetString(idx_Email);
                                }
                                if (!dataReader.IsDBNull(idx_Id))
                                {
                                    user.Id = dataReader.GetString(idx_Id);
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
                    }
                }
            }
            return user;
        }
    }
}
