﻿using ProvisionsDesktop.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProvisionsDesktop
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Window, INotifyPropertyChanged
    {
        private ObservableCollection<Day> _days;

        private readonly User _user;

        public Provision SelectedProvision { get; set; }

        public void NotifyPropertyChanged([CallerMemberName]string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Day> Days
        {
            get
            {
                return _days;
            }
            set
            {
                _days = value;
                NotifyPropertyChanged(nameof(Days));
            }
        }

        public ObservableCollection<Provision> Provisions { get; set; }

        public List<string> Statuses { get; set; }

        public HomePage(User user)
        {
            InitializeComponent();
            _user = user;
            BtnRefreshClick(new object(), new RoutedEventArgs());
        }

        private void GetDays()
        {
            using (SqlConnection connection = new SqlConnection(
                Properties.Settings.Default.connectionString))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.CommandText = "p_get_user_days";

                    SqlParameter dp = command.Parameters.Add("@UserId", SqlDbType.VarChar);
                    dp.Value = _user.Id;

                    dp = command.Parameters.Add("@ProvisionId", SqlDbType.VarChar);
                    dp.Value = SelectedProvision is null ? null : SelectedProvision.Id.ToString();

                    connection.Open();

                    using (SqlDataReader dataReader = command.ExecuteReader())
                    {
                        if (dataReader.HasRows)
                        {
                            int idx_Date = dataReader.GetOrdinal("Date");
                            int idx_ProvisionName = dataReader.GetOrdinal("ProvisionName");
                            int idx_Status = dataReader.GetOrdinal("Status");
                            int idx_Id = dataReader.GetOrdinal("Id");

                            Days = new ObservableCollection<Day>();

                            while (dataReader.Read())
                            {
                                var day = new Day();
                                if (!dataReader.IsDBNull(idx_Date))
                                {
                                    day.Date = dataReader.GetDateTime(idx_Date);
                                }
                                if (!dataReader.IsDBNull(idx_ProvisionName))
                                {
                                    day.ProvisionName = dataReader.GetString(idx_ProvisionName);
                                }
                                if (!dataReader.IsDBNull(idx_Status))
                                {
                                    day.Status = dataReader.GetString(idx_Status);
                                }
                                if (!dataReader.IsDBNull(idx_Id))
                                {
                                    day.Id = dataReader.GetGuid(idx_Id);
                                }
                                Day.Statuses = Statuses;
                                Day.Provisions = Provisions.Select(p => p.Name).ToList();
                                Days.Add(day);
                            }

                            DaysGrid.DataContext = Days;
                        }
                    }
                }
            }
        }

        private void BtnRefreshClick(object sender, RoutedEventArgs e)
        {
            GetStatuses();
            GetProvisions();
            GetDays();
        }

        private async Task UpdateSelectedDay(Day day)
        {
            await Task.Run(() =>
            {
                System.Threading.Thread.Sleep(1000);
                day = _days.Where(d => d.Id.Equals(day.Id)).FirstOrDefault();
                if(day is null)
                {
                    return;
                }

                using (SqlConnection connection = new SqlConnection(
                    Properties.Settings.Default.connectionString))
                {
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.CommandText = "p_save_day_changes";

                        SqlParameter dp = command.Parameters.Add("RETURN_VALUE", SqlDbType.Int);
                        dp.Direction = ParameterDirection.ReturnValue;

                        dp = command.Parameters.Add("@Id", SqlDbType.VarChar);
                        dp.Value = day.Id.ToString() ?? string.Empty;

                        dp = command.Parameters.Add("@Date", SqlDbType.DateTime);
                        dp.Value = day.Date;

                        dp = command.Parameters.Add("@ProvisionName", SqlDbType.VarChar);
                        dp.Value = day.ProvisionName ?? string.Empty;

                        dp = command.Parameters.Add("@Status", SqlDbType.VarChar);
                        dp.Value = day.Status ?? string.Empty;

                        dp = command.Parameters.Add("@UserId", SqlDbType.VarChar);
                        dp.Value = _user.Id;

                        connection.Open();

                        command.ExecuteNonQuery();

                        if ((int)command.Parameters["RETURN_VALUE"].Value != 0)
                        {
                            MessageBox.Show("Coś poszło nie tak... :(",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                }
            });
        }

        private void DaysGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            var day = e.Row.Item as Day;
            UpdateSelectedDay(day);
        }


        public void GetProvisions()
        {
            Provisions = new ObservableCollection<Provision>();

            using (SqlConnection connection = new SqlConnection(
                Properties.Settings.Default.connectionString))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.CommandText = "p_provisions_list";

                    SqlParameter dp = command.Parameters.Add("@UserId", SqlDbType.VarChar);
                    dp.Value = _user.Id;

                    connection.Open();

                    using (SqlDataReader dataReader = command.ExecuteReader())
                    {
                        if (dataReader.HasRows)
                        {
                            int idx_StartDate = dataReader.GetOrdinal("StartDate");
                            int idx_Name = dataReader.GetOrdinal("Name");
                            int idx_Description = dataReader.GetOrdinal("Description");
                            int idx_Id = dataReader.GetOrdinal("Id");

                            while (dataReader.Read())
                            {
                                var provision = new Provision();
                                if (!dataReader.IsDBNull(idx_StartDate))
                                {
                                    provision.StartDate = dataReader.GetDateTime(idx_StartDate);
                                }
                                if (!dataReader.IsDBNull(idx_Name))
                                {
                                    provision.Name = dataReader.GetString(idx_Name);
                                }
                                if (!dataReader.IsDBNull(idx_Description))
                                {
                                    provision.Description = dataReader.GetString(idx_Description);
                                }
                                if (!dataReader.IsDBNull(idx_Id))
                                {
                                    provision.Id = dataReader.GetGuid(idx_Id);
                                }
                                Provisions.Add(provision);
                            }

                            provisionsList.ItemsSource = Provisions;
                        }
                    }
                }
            }
        }

        public void GetStatuses()
        {
            Statuses = new List<string>();

            using (SqlConnection connection = new SqlConnection(
                Properties.Settings.Default.connectionString))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.CommandText = "p_get_statuses";

                    connection.Open();

                    using (SqlDataReader dataReader = command.ExecuteReader())
                    {
                        if (dataReader.HasRows)
                        {
                            int idx_Name = dataReader.GetOrdinal("Name");

                            while (dataReader.Read())
                            {
                                if (!dataReader.IsDBNull(idx_Name))
                                {
                                    Statuses.Add(dataReader.GetString(idx_Name));
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ProvisionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems is null)
            {
                return;
            }

            SelectedProvision = ((sender as ComboBox).SelectedItem as Provision);
            GetDays();
        }
    }


}
