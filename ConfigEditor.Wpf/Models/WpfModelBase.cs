using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConfigEditor.Wpf.Models
{
    public class WpfModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// 屬性變更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
