using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp2.Services;



using CommunityToolkit.Mvvm.ComponentModel;

namespace UiDesktopApp2.Models
{
    public partial class BackupItem : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private DateTime _createdDate;

        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private long _size;

        [ObservableProperty]
        private string _status = string.Empty;

        public string SizeFormatted
        {
            get
            {
                string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
                int counter = 0;
                decimal number = Size;

                while (Math.Round(number / 1024) >= 1)
                {
                    number /= 1024;
                    counter++;
                }

                return $"{number:n1} {suffixes[counter]}";
            }
        }
    }
}
