namespace PxCT
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    internal class BindableBase : INotifyPropertyChanged
    {
        #region Delegates & Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetProperty<T>(T value, ref T target, [CallerMemberName] string propertyName = null)
        {
            target = value;
            OnPropertyChanged(propertyName);
        }

        #endregion
    }
}