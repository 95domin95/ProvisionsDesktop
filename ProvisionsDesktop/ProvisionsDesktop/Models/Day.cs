using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProvisionsDesktop.Models
{
    public class Day: INotifyPropertyChanged
    {
        public void NotifyPropertyChanged([CallerMemberName]string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        private Guid _id;
        private DateTime _date = DateTime.Now;
        private string _status;
        private string _provisionName;
        public Guid Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                NotifyPropertyChanged(nameof(Id));
            }
        }

        public DateTime Date
        {
            get
            {
                return _date;
            }
            set
            {
                _date = value;
                NotifyPropertyChanged(nameof(Date));
            }
        }
        public string ProvisionName
        {
            get
            {
                return _provisionName;
            }
            set
            {
                _provisionName = value;
                NotifyPropertyChanged(nameof(ProvisionName));
            }
        }
        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                NotifyPropertyChanged(nameof(Status));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
