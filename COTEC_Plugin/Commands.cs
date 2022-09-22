using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;


using AcRx = Autodesk.AutoCAD.Runtime;
using AcDb = Autodesk.AutoCAD.DatabaseServices;
using AcGe = Autodesk.AutoCAD.Geometry;
using AcEd = Autodesk.AutoCAD.EditorInput;


[assembly: CommandClass(typeof(COTEC_Plugin.PaletteWpf.Commands))]

namespace COTEC_Plugin.PaletteWpf
{
    #region commandes
    public class Commands
    {
        [CommandMethod("CC_cotei")]
        [CommandMethod("CCI")]
        public void CC_cotei()
        {
            Internal internal_var = new Internal();
            internal_var.INTER_Command_CCI();
        }

        [CommandMethod("CC_pente")]
        [CommandMethod("CCP")]
        public void CC_pente()
        {
            Internal internal_var = new Internal();
            internal_var.INTER_Command_CCP();
        }

        [CommandMethod("CC_deniv")]
        [CommandMethod("CCD")]
        public void CC_deniv()
        {
            Internal internal_var = new Internal();
            internal_var.INTER_Command_CCD();
        }

        [CommandMethod("CCD2")]
        public void CC_deniv2()
        {
            Internal internal_var = new Internal();
            internal_var.INTER_Command_CCD2();
        }

        [CommandMethod("DumpEntityInfo")]
        public void DumpEntityInfo_Method()
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //TODO: add your code below.

                ed.WriteMessage("DumpEntityInfo ran.\n");

                PromptEntityResult selRes = ed.GetEntity("Pick an entity:");
                if (selRes.Status == PromptStatus.OK)
                {
                    Entity ent = tr.GetObject(selRes.ObjectId, OpenMode.ForRead) as Entity;
                    Dump(ent);
                }

                tr.Commit();
            }
        }

        void Dump(DBObject obj)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            string msg = string.Format("Properties of the {0} with handle {1}:\n", obj.GetType().Name, obj.Handle);

            System.Reflection.PropertyInfo[] piArr = obj.GetType().GetProperties();
            foreach (System.Reflection.PropertyInfo pi in piArr)
            {
                object value = null;
                try
                {
                    value = pi.GetValue(obj, null);
                }
                catch (System.Exception ex)
                {
                    if (ex.InnerException is Autodesk.AutoCAD.Runtime.Exception &&
                    (ex.InnerException as Autodesk.AutoCAD.Runtime.Exception).ErrorStatus == ErrorStatus.NotApplicable)
                        continue;
                    else
                        throw;
                }

                msg += string.Format("\t{0}: {1}\n", pi.Name, value);
            }

            ed.WriteMessage("\n" + msg);
        }



        [CommandMethod("debug_random_point")]
        public void DBG_Random_Point()
        {
            //Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Random _random = new Random();

            // Generates a random number within a range.
            int RandomNumber(int min, int max)
            {
                return _random.Next(min, max);
            }

            for (int i = 0; i < 50; i++)
            {
                double x = RandomNumber(-50, 50);
                double y = RandomNumber(-50, 50);
                double alti = RandomNumber(100, 200) + RandomNumber(0, 99) * 0.01;
                Point3d point = new Point3d(x, y, 0);
                Internal internal_var = new Internal();
                internal_var.GENENER_Bloc_Cote(alti, point);
            }

        }

        CustomPaletteSet palette = null;
        [CommandMethod("CMD_PALETTE_WPF")]
        public void ShowPaletteSetWpf()
        {
            
            if (palette == null)
                palette = new CustomPaletteSet();
            palette.Visible = true;
        }
        #endregion














    }

}
