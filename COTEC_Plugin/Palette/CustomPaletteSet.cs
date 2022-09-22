using System;

using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Windows;

namespace COTEC_Plugin.PaletteWpf
{
    internal class CustomPaletteSet : PaletteSet
    {
        // champ statique
        static bool wasVisible;

        /// <summary>
        /// Crée une nouvelle instance de CustomPaletteSet
        /// </summary>
        public CustomPaletteSet()
            : base("Palette WPF", "CMD_PALETTE_WPF", new Guid("{42425FEE-B3FD-4776-8090-DB857E9F7A0E}"))
        {
            Style =
                PaletteSetStyles.ShowAutoHideButton |
                PaletteSetStyles.ShowCloseButton |
                PaletteSetStyles.ShowPropertiesMenu;
            MinimumSize = new System.Drawing.Size(250, 150);
            AddVisual("Cercle", new PaletteTabView());

            // masquage automatique de la palette quand aucune instance de Document
            // n'est active (no document state)
            var docs = Application.DocumentManager;
            docs.DocumentBecameCurrent += (s, e) =>
                Visible = e.Document == null ? false : wasVisible;
            docs.DocumentCreated += (s, e) =>
                Visible = wasVisible;
            docs.DocumentToBeDeactivated += (s, e) =>
                wasVisible = Visible;
            docs.DocumentToBeDestroyed += (s, e) =>
            {
                wasVisible = Visible;
                if (docs.Count == 1)
                    Visible = false;
            };
        }
    }
}

