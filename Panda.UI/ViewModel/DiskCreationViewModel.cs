using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using JetBrains.Annotations;

namespace Panda.UI.ViewModel
{
    public class DiskCreationViewModel : INotifyPropertyChanged
    {
        public String FileName
        {
            get { return _fileName; }
            set
            {
                if (value == _fileName) return;
                _fileName = value;
                OnPropertyChanged();
                Trace.WriteLine("FileName property changed");
            }
        }

        public long? Capacity
        {
            get { return _capacity; }
            set
            {
                if(value == null)
                    throw new PandaException("Invalid capacity.");
                if (value == _capacity) return;
                _capacity = value;
                OnPropertyChanged();
            }
        }

        public DiskCreationViewModel()
        {
            Capacity = 10*1024*1024;
        }

        private string _fileName;
        private long? _capacity;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public static RoutedUICommand Confirm
        {
            get { return _confirm; }
        }

        [NotNull] private static readonly RoutedUICommand _confirm = new RoutedUICommand("Create disk","Confirm",typeof(DiskCreationViewModel));
    }
}