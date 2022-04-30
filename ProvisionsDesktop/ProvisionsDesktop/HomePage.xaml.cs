using ProvisionsDesktop.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
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
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = @"SELECT
	                                            d.[Date],
	                                            s.[Name] AS 'Status',
	                                            d.[Id],
	                                            o.[Name] AS 'ProvisionName',
	                                            d.[Description]
                                            FROM Day AS d
                                            INNER JOIN Objective AS o
	                                            ON d.ObjectiveId = o.Id AND o.UserId = @UserId AND ObjectiveId = CASE WHEN @ProvisionId = 0 THEN ObjectiveId ELSE @ProvisionId END
                                            INNER JOIN [Status] AS s
	                                            ON d.StatusId = s.Id";

                    SqlParameter dp = command.Parameters.Add("@UserId", SqlDbType.Int);
                    dp.Value = _user.Id;

                    dp = command.Parameters.Add("@ProvisionId", SqlDbType.VarChar);
                    dp.Value = SelectedProvision is null ? 0 : SelectedProvision.Id;

                    connection.Open();

                    using (SqlDataReader dataReader = command.ExecuteReader())
                    {
                        if (dataReader.HasRows)
                        {
                            int idx_Date = dataReader.GetOrdinal("Date");
                            int idx_ProvisionName = dataReader.GetOrdinal("ProvisionName");
                            int idx_Status = dataReader.GetOrdinal("Status");
                            int idx_Id = dataReader.GetOrdinal("Id");
                            int idx_Description = dataReader.GetOrdinal("Description");

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
                                    day.Id = dataReader.GetInt32(idx_Id);
                                }
                                if (!dataReader.IsDBNull(idx_Description))
                                {
                                    day.Description = dataReader.GetString(idx_Description);
                                }
                                Day.Statuses = Statuses;
                                Day.Provisions = Provisions.Select(p => p.Name).ToList();
                                Days.Add(day);
                            }

                            DaysGrid.DataContext = Days;
                        }
                        else
                        {
                            Day.Statuses = Statuses;
                            Day.Provisions = Provisions.Select(p => p.Name).ToList();
                            Days = new ObservableCollection<Day>();
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
                try
                {
                    System.Threading.Thread.Sleep(1000);
                    day = _days.Where(d => d.ProvisionName.Equals(day.ProvisionName) &&
                        d.Status.Equals(day.Status) && d.Date.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"))
                        .Equals(day.Date.ToString("d", CultureInfo.CreateSpecificCulture("de-DE")))).FirstOrDefault();

                    if (day is null)
                    {
                        return;
                    }

                    using (SqlConnection connection = new SqlConnection(
                        Properties.Settings.Default.connectionString))
                    {
                        using (SqlCommand command = connection.CreateCommand())
                        {
                            command.CommandType = System.Data.CommandType.Text;
                            command.CommandText = @"DECLARE @StatusId AS INT
                                                    DECLARE @ObjectiveId AS INT
                                                    SELECT TOP 1 @ObjectiveId = Id FROM Objective WHERE [Name] = @ObjectiveName AND UserId = @UserId
                                                    SELECT TOP 1 @StatusId = Id FROM [Status] WHERE [Name] = @Status


                                                    IF @Id = 0 BEGIN
	                                                    INSERT INTO [Day]
	                                                    (
		                                                    [Date],
		                                                    [StatusId],
                                                            [Description],
		                                                    [ObjectiveId]
	                                                    )
	                                                    VALUES
	                                                    (
		                                                    CASE WHEN @Date IS NULL THEN GETDATE() ELSE @Date END,
		                                                    CASE WHEN @StatusId IS NULL THEN 1 ELSE @StatusId END,
		                                                    @Description,
		                                                    @ObjectiveId
	                                                    )
                                                    END ELSE BEGIN
	                                                    UPDATE [Day]
	                                                    SET 
		                                                    [Description] = CASE WHEN @Description IS NULL THEN [Description] ELSE @Description END,
		                                                    [Date] = CASE WHEN @Date IS NULL THEN [Date] ELSE @Date END,
		                                                    [ObjectiveId] = @ObjectiveId,
		                                                    [StatusId] = CASE WHEN @StatusId IS NULL THEN [StatusId] ELSE @StatusId END
                                                        WHERE Id = @Id
                                                    END";

                            SqlParameter dp = command.Parameters.Add("RETURN_VALUE", SqlDbType.Int);
                            dp.Direction = ParameterDirection.ReturnValue;

                            dp = command.Parameters.Add("@Id", SqlDbType.Int);
                            dp.Value = day.Id;

                            dp = command.Parameters.Add("@Date", SqlDbType.DateTime);
                            dp.Value = day.Date;

                            dp = command.Parameters.Add("@ObjectiveName", SqlDbType.VarChar);
                            dp.Value = day.ProvisionName ?? string.Empty;

                            dp = command.Parameters.Add("@Status", SqlDbType.VarChar);
                            dp.Value = day.Status ?? string.Empty;

                            dp = command.Parameters.Add("@Description", SqlDbType.VarChar);
                            dp.Value = day.Description ?? string.Empty;

                            dp = command.Parameters.Add("@UserId", SqlDbType.Int);
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
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Wystąpił nieznany bład... :(",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
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
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = @"SELECT
                                                o.Name,
	                                            o.Id
                                            FROM Objective AS o
                                            WHERE UserId = @UserId";

                    SqlParameter dp = command.Parameters.Add("@UserId", SqlDbType.Int);
                    dp.Value = _user.Id;

                    connection.Open();

                    using (SqlDataReader dataReader = command.ExecuteReader())
                    {
                        if (dataReader.HasRows)
                        {
                            int idx_Name = dataReader.GetOrdinal("Name");
                            int idx_Id = dataReader.GetOrdinal("Id");

                            while (dataReader.Read())
                            {
                                var provision = new Provision();
                                if (!dataReader.IsDBNull(idx_Name))
                                {
                                    provision.Name = dataReader.GetString(idx_Name);
                                }
                                if (!dataReader.IsDBNull(idx_Id))
                                {
                                    provision.Id = dataReader.GetInt32(idx_Id);
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
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = @"SELECT
                                                [Name]
                                            FROM[Status]";

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

        private void AddNew_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(newProvisionTbx.Text) || newProvisionTbx.Text.Length < 3)
            {
                MessageBox.Show("Nazwa postanowienia niepoprawna",
                    "Nazwa niepoprawna",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            using (SqlConnection connection = new SqlConnection(
                Properties.Settings.Default.connectionString))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = @"INSERT INTO Objective
                                            (
	                                            [Name],
	                                            [UserId],
	                                            [TypeId]
                                            )
                                            VALUES
                                            (
	                                            @Name,
	                                            @UserId,
	                                            1
                                            )";

                    SqlParameter dp = command.Parameters.Add("@Name", SqlDbType.VarChar);
                    dp.Value = newProvisionTbx.Text;

                    dp = command.Parameters.Add("@Id", SqlDbType.VarChar);
                    dp.Value = _user.Id;

                    connection.Open();

                    command.ExecuteNonQuery();

                    BtnRefreshClick(new object(), new RoutedEventArgs());

                    MessageBox.Show(string.Format("Dodano nowe postanowienie: {0}.", newProvisionTbx.Text),
                        "Dodano postanowienie",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    newProvisionTbx.Text = string.Empty;
                }
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            var selectedProvision = ((provisionsList.SelectedItem as Provision) ?? new Provision()).Name;

            if (string.IsNullOrWhiteSpace(selectedProvision))
            {
                return;
            }

            var result = MessageBox.Show(string.Format("Czy napewno chcesz usunąć postanowienie: {0}", selectedProvision),
                "Powierdź usunięcie postanowienia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            switch(result)
            {
                case MessageBoxResult.No:
                    return;
            }

            using (SqlConnection connection = new SqlConnection(
                Properties.Settings.Default.connectionString))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "DELETE FROM Objective WHERE UserId = @UserId AND [Name] = @Name";

                    SqlParameter dp = command.Parameters.Add("@Name", SqlDbType.VarChar);
                    dp.Value = selectedProvision;

                    connection.Open();

                    command.ExecuteNonQuery();

                    BtnRefreshClick(new object(), new RoutedEventArgs());

                    MessageBox.Show(string.Format("Usunięto postanowienie: {0}.", selectedProvision),
                        "Usunięto postanowienie",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    provisionsList.SelectedItem = null;
                }
            }
        }
    }


}
