using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Windows.Data;
using System.ComponentModel;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace COTEC_Plugin.PaletteWpf
{
    class PaletteTabViewModel : ObservableObject
    {
        // champs privés
        ICustomTypeDescriptor layer;
        double radius;
        string txtRad;
        bool validRad;

        /// <summary>
        /// Obtient l'objet Command lié au bouton OK.
        /// Le bouton est automatiquement grisé si le prédicat CanExecute retourne false.
        /// </summary>
      

        /// <summary>
        /// Obtient ou définit le calque sélectionné.
        /// </summary>
        public ICustomTypeDescriptor Layer
        {
            get { return layer; }
            set { layer = value; OnPropertyChanged(nameof(Layer)); }
        }

        /// <summary>
        /// Obtient la collection des calques.
        /// </summary>
        public DataItemCollection Layers => AcAp.UIBindings.Collections.Layers;

        /// <summary>
        /// Obtient ou définit la valeur du rayon apparaissant dans la boite de texte.
        /// </summary>
        public string TextRadius
        {
            get { return txtRad; }
            set
            {
                txtRad = value;
                validRad = double.TryParse(value, out radius) && radius > 0.0;
                OnPropertyChanged(nameof(TextRadius));
            }
        }

        /// <summary>
        /// Crée une nouvelle instance de PaletteTabViewModel.
        /// </summary>
        public PaletteTabViewModel()
        {
            TextRadius = "10";
            Layer = Layers.CurrentItem;
            Layers.CollectionChanged += (s, e) => Layer = Layers.CurrentItem;
        }

        /// <summary>
        /// Méthode appelée par DrawCircleCommand. 
        /// Appelle la commande CMD_CIRCLE_WPF avec les options courantes
        /// </summary>
        private void DrawCircle() =>
            AcAp.DocumentManager.MdiActiveDocument?.SendStringToExecute(
                $"CMD_CIRCLE_WPF \"{((INamedValue)Layer).Name}\" {TextRadius} ",
                false, false, false);

        /// <summary>
        /// Méthode appelée par GetRadiusCommand.
        /// </summary>
        private void GetRadius()
        {
            // inviter l'utilisateur à spécifier une distance
            var ed = AcAp.DocumentManager.MdiActiveDocument.Editor;
            var opts = new PromptDistanceOptions("\nSpécifiez le rayon: ");
            opts.AllowNegative = false;
            opts.AllowZero = false;
            var pdr = ed.GetDistance(opts);
            if (pdr.Status == PromptStatus.OK)
                TextRadius = pdr.Value.ToString();
        }
    }
}