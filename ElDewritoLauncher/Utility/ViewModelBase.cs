using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EDLauncher.Utility
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void SetAndNotify<T>(ref T member, T newValue, [CallerMemberName] string propertyName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(member, newValue))
            {
                member = newValue;
                RaisePropertyChanged(propertyName);
            }
        }

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
