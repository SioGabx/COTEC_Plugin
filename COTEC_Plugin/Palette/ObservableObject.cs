using System.ComponentModel;

namespace COTEC_Plugin.PaletteWpf
{
    /// <summary>
    /// Fournit un type qui implémente INotifyPropertyChanged
    /// </summary>
    class ObservableObject : INotifyPropertyChanged
    {
        /// <summary>
        /// Evénement déclenché lorsqu'une propriété change.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Méthode appelée dans le 'setter' des propriétés dont on veut notifier le changement.
        /// </summary>
        /// <param name="propertyName">Nom de la propriété.</param>
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
