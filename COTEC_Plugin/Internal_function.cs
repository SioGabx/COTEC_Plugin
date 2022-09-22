using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace COTEC_Plugin
{



    public partial class Internal
    {
        #region class_definition
        public class PointClass
        {
            public double X; public double Y; public double Z;
        }

        public class PointCoteClass
        {
            public Point3d point_gp; public Point3d point_cscu; public double cote;
        }

        public class StringIntClass
        {
            public string name; public string value; public string type;
        }

        #endregion


        public void GENENER_Bloc_Pente(double pente, Point3d point, PointCoteClass pointFirst, PointCoteClass pointSecond)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<StringIntClass> stringIntClasses = GeneratePenteBlocSettings(pointFirst, pointSecond);

            Transaction tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                string bloc_name = "_APUd_COTATIONS_Pentes";

                if (!bt.Has(bloc_name))
                {
                    SingleBlockImporting("_APUd_COTATIONS_Pentes");

                }
                if (!bt.Has(bloc_name))
                {
                    ed.WriteMessage("Insertion pente annulée" + "\n");
                    return;
                }
                InsertBloc(bloc_name, point, pente.ToString() + "%", 0, stringIntClasses);
                tr.Commit();
            }
        }

        public void GENENER_Bloc_Cote(double cote, Point3d point)
        {
            DBObjectCollection ents = CoteElementGenerator(0.00, point);
            CreateBlock("_APUd_COTATIONS_Altimetrie", point, cote.ToString("#.00"), ents, GetUSCRotation(true));
        }



        #region Internal_function
        DBObjectCollection CoteElementGenerator(double cote, Point3d point)
        {
            DBObjectCollection ents = new DBObjectCollection();
            AttributeDefinition attribute = new AttributeDefinition
            {
                Position = new Point3d(point.X + 0.15, point.Y + 0.15, 0),
                TextString = cote.ToString("#.00"),
                Tag = "Cote",
                Layer = "0",
                TextStyleId = AddFontStyle("Arial"),
                Height = 0.5,
                ColorIndex = 0,
                Prompt = "Entrez la cote"
            };
            ents.Add(attribute);
            DBPoint pointEntity = new DBPoint(point);
            ents.Add(pointEntity);
            Circle circle = new Circle(point, Vector3d.ZAxis, 0.075)
            {
                Layer = "0",
                ColorIndex = 0
            };
            ents.Add(circle);
            Line linegd = new Line(new Point3d(point.X - 0.10, point.Y, 0), new Point3d(point.X + 0.10, point.Y, 0))
            {
                Layer = "0",
                ColorIndex = 0
            };
            ents.Add(linegd);
            Line linehb = new Line(new Point3d(point.X, point.Y + 0.10, 0), new Point3d(point.X, point.Y - 0.10, 0))
            {
                Layer = "0",
                ColorIndex = 0
            };
            ents.Add(linehb);
            return ents;
        }
        double GetUSCRotation(Boolean in_rad = false)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            Matrix3d ucsCur = ed.CurrentUserCoordinateSystem;
            CoordinateSystem3d cs = ucsCur.CoordinateSystem3d;
            Double ucs_rotAngle = cs.Xaxis.AngleOnPlane(new Plane(Point3d.Origin, Vector3d.ZAxis));
            if (in_rad)
            {
                return ucs_rotAngle;
            }
            double ucs_angle_degres = ucs_rotAngle * 180 / Math.PI;
            return ucs_angle_degres;
        }
        ObjectId AddFontStyle(string font)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            //Editor ed = doc.Editor;
            using (Transaction newTransaction = doc.TransactionManager.StartTransaction())
            {
                BlockTable newBlockTable;
                newBlockTable = newTransaction.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord newBlockTableRecord;
                newBlockTableRecord = (BlockTableRecord)newTransaction.GetObject(newBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                TextStyleTable newTextStyleTable = newTransaction.GetObject(doc.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

                if (!newTextStyleTable.Has(font.ToUpperInvariant()))  //The TextStyle is currently not in the database
                {
                    newTextStyleTable.UpgradeOpen();
                    TextStyleTableRecord newTextStyleTableRecord = new TextStyleTableRecord
                    {
                        FileName = font,
                        Name = font.ToUpperInvariant()
                    };
                    //Autodesk.AutoCAD.GraphicsInterface.FontDescriptor myNewTextStyle = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor("ROMANS", false, false, 0, 0);
                    //newTextStyleTableRecord.Font = myNewTextStyle;
                    newTextStyleTable.Add(newTextStyleTableRecord);
                    newTransaction.AddNewlyCreatedDBObject(newTextStyleTableRecord, true);
                }

                newTransaction.Commit();
                return newTextStyleTable[font];
                //.TextStyleID = newTextStyleTable["ROMANS"];
            }
        }
        List<StringIntClass> GeneratePenteBlocSettings(PointCoteClass pointFirst, PointCoteClass pointSecond)
        {
            //var doc = AcAp.DocumentManager.MdiActiveDocument;
            //var db = doc.Database;
            //Editor ed = doc.Editor;

            double deg_sup_to_normal(double func_anglef)
            {
                double func_anglef_return = func_anglef;
                if (func_anglef_return > 360)
                {
                    func_anglef_return -= 360;
                }
                else if (func_anglef_return < 0)
                {
                    func_anglef_return = 360 - func_anglef;
                }
                return func_anglef_return;
            }

            double angle = 0;
            using (Line acLine = new Line(pointFirst.point_cscu, pointSecond.point_cscu))
            {
                try
                {
                    angle = Vector3d.XAxis.GetAngleTo(acLine.GetFirstDerivative(pointFirst.point_cscu), Vector3d.ZAxis);
                }
                catch (System.Exception)
                {
                    angle = -1;
                }
            }
            double anglef = angle;
            int invf = 0;
            if (pointSecond.cote > pointFirst.cote)
            {
                invf = 1;
            }
            double angle_degres = angle * 180 / Math.PI;

            if (angle_degres > 90 + GetUSCRotation() && angle_degres < 270 + GetUSCRotation())
            {
                if (invf == 0)
                {
                    invf = 1;
                }
                else
                {
                    invf = 0;
                }

                anglef = angle_degres + 180;
                anglef = deg_sup_to_normal(anglef);
                anglef = anglef * Math.PI / 180;
            }

            List<StringIntClass> stringIntClasses = new List<StringIntClass>()
            {
                new StringIntClass(){name ="angle_pente", value = anglef.ToString(), type="double"},
                new StringIntClass(){name ="sens_pente", value = invf.ToString(), type="int"},
            };

            return stringIntClasses;
        }
        private static bool IsOrthModeOn()
        {
            // Check the value of the ORTHOMODE sysvar
            object orth = Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("ORTHOMODE");
            return Convert.ToInt32(orth) > 0;
        }

        private static Point3d GetOrthoPoint(Point3d basePt, Point3d pt)
        {
            // Apply a crude orthographic mode
            double x = pt.X;
            double y = pt.Y;
            Vector3d vec = basePt.GetVectorTo(pt);
            if (Math.Abs(vec.X) >= Math.Abs(vec.Y))
            {
                y = basePt.Y;
            }
            else
            {
                x = basePt.X;
            }
            return new Point3d(x, y, 0.0);
        }

        private static List<Drawable> CreateTransGraphics(DBObjectCollection ents)
        {
            // Create our list of drawables to return
            List<Drawable> drawables = new List<Drawable>();
            foreach (Entity drawable in ents)
            {
                drawable.ColorIndex = 252;
                drawables.Add(drawable);
            }

            // Draw each one initially
            foreach (Drawable d in drawables)
            {
                TransientManager.CurrentTransientManager.AddTransient(d, TransientDrawingMode.DirectShortTerm, 128, new IntegerCollection());
            }
            return drawables;
        }

        private static void ClearTransGraphics(List<Drawable> drawables)
        {
            // Clear the transient graphics for our drawables
            TransientManager.CurrentTransientManager.EraseTransients(
              TransientDrawingMode.DirectShortTerm,
              128, new IntegerCollection()
            );
            // Dispose of them and clear the list
            foreach (Drawable d in drawables)
            {
                d.Dispose();
            }
            drawables.Clear();
        }

        readonly List<ObjectId> HightLightedObject = new List<ObjectId>();
        List<PointCoteClass> GetTwoPoint()
        {
            PointCoteClass pointFirst = AskForPointCote("Selectionnez un premier point");
            if (pointFirst == null)
            {
                // ClearHighlightedObjects();
                return null;
            }
            PointCoteClass pointSecond = AskForPointCote("Selectionnez un second point", new PointClass() { X = pointFirst.point_gp.X, Y = pointFirst.point_gp.Y, Z = 0 });
            if (pointSecond == null)
            {
                ClearHighlightedObjects();
                return null;
            }
            ClearHighlightedObjects();

            if (pointFirst.cote > pointSecond.cote)
            {
                return new List<PointCoteClass> { pointFirst, pointSecond };
            }
            else
            {
                return new List<PointCoteClass> { pointSecond, pointFirst };
            }

        }
        void ClearHighlightedObjects()
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId objId in HightLightedObject)
                {
                    //DBObject obj = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as DBObject;
                    Entity blkRef = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                    blkRef.Unhighlight();
                }
                tr.Commit();
            }
        }


        PointCoteClass AskForPointCote(string message, PointClass pointbaseclass = null)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            MyUserSettings mus = new MyUserSettings();

            PointCoteClass mainreturnPointCote = new PointCoteClass();

            PointCoteClass AskForPoint(PromptPointResult promptPoint_ap)
            {
                PointCoteClass returnPointCote = new PointCoteClass();
                if (promptPoint_ap.Status != PromptStatus.OK) { return null; }
                Point3d point = new Point3d(promptPoint_ap.Value.X, promptPoint_ap.Value.Y, 0);
                DBPoint pointEntity = new DBPoint(point);
                ObjectId TempPoint;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;


                    acBlkTblRec.AppendEntity(pointEntity);
                    tr.AddNewlyCreatedDBObject(pointEntity, true);
                    TempPoint = pointEntity.ObjectId;
                    tr.Commit();
                }
                PromptDoubleOptions pointAltOptions = new PromptDoubleOptions("\n" + "Saississez la côte")
                {
                    AllowNegative = false,
                    AllowNone = false
                };
                PromptDoubleResult promptDoubleResult = ed.GetDouble(pointAltOptions);
                using (Transaction tr2 = db.TransactionManager.StartTransaction())
                {
                    if (TempPoint.IsErased == false)
                    {
                        DBObject obj = TempPoint.GetObject(OpenMode.ForWrite);
                        obj.Erase(true);
                        tr2.Commit();
                    }
                }
                if (promptDoubleResult.Status != PromptStatus.OK) { return null; }
                double cote = promptDoubleResult.Value;
                returnPointCote.cote = cote;
                returnPointCote.point_gp = point;
                returnPointCote.point_cscu = Point3DToCurentSCU(point);
                return returnPointCote;
            }

            PointCoteClass AskForBloc()
            {
                PointCoteClass returnPointCote = new PointCoteClass();
                bool RedoWhile = true;
                do
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        TypedValue[] filList = new TypedValue[1] { new TypedValue((int)DxfCode.Start, "INSERT") };
                        MText empty = new MText();
                        SelectionFilter filter = new SelectionFilter(filList);
                        PromptSelectionOptions opts = new PromptSelectionOptions
                        {
                            MessageForAdding = message + " [POint]",
                            RejectObjectsOnLockedLayers = false
                        };
                        opts.Keywords.Add("POint");
                        opts.KeywordInput += delegate (object sender, SelectionTextInputEventArgs e)
                        {
                            //ed.WriteMessage("\nKeyword entered: {0}", e.Input); 
                            tr.Commit();
                            throw new Autodesk.AutoCAD.Runtime.Exception(Autodesk.AutoCAD.Runtime.ErrorStatus.OK, e.Input);
                        };
                        opts.SingleOnly = true;

                        SelectionSet selSet;
                        do
                        {
                            PromptSelectionResult res = ed.GetSelection(opts, filter);


                            if (res.Status == PromptStatus.Cancel)
                            {
                                tr.Commit();
                                return null;
                            }
                            selSet = res.Value;

                        } while (selSet == null || selSet.Count != 1);
                        ObjectId blkId = selSet.GetObjectIds().FirstOrDefault();
                        BlockReference blkRef = (BlockReference)tr.GetObject(blkId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);
                        blkRef.Highlight();
                        HightLightedObject.Add(blkRef.ObjectId);
                        btr.Dispose();
                        AttributeCollection attCol = blkRef.AttributeCollection;

                        foreach (ObjectId attId in attCol)
                        {
                            AttributeReference attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
                            if (attRef.TextString.Contains("."))
                            {
                                bool isdouble = double.TryParse(attRef.TextString.Trim(), out double cotealti);
                                if (isdouble)
                                {
                                    RedoWhile = false;
                                    returnPointCote.cote = cotealti;
                                }
                                else
                                {
                                    IEnumerable<int> AllIndexesOf(string str, string searchstring)
                                    {
                                        int minIndex = str.IndexOf(searchstring);
                                        while (minIndex != -1)
                                        {
                                            yield return minIndex;
                                            minIndex = str.IndexOf(searchstring, minIndex + searchstring.Length);
                                        }
                                    }
                                    int[] result_point_position = AllIndexesOf(attRef.TextString.Trim(), ".").ToArray();
                                    string number_result_p1 = "";
                                    string number_result_p2 = "";
                                    string txt = attRef.TextString.Trim();
                                    if (!txt.Contains("%"))
                                    {
                                        foreach (int index in result_point_position)
                                        {

                                            int n = index;
                                            while (n > 0 && char.IsDigit(txt[n - 1]))
                                            {
                                                number_result_p1 = txt[n - 1].ToString() + number_result_p1;
                                                n--;
                                            }

                                            n = index;
                                            while (txt.Length > n + 1 && char.IsDigit(txt[n + 1]))
                                            {
                                                number_result_p2 += txt[n + 1].ToString();
                                                n++;
                                            }
                                            
                                            if (!string.IsNullOrEmpty(number_result_p1.Trim()) && number_result_p1.Length > 0)
                                            {
                                                if (!string.IsNullOrEmpty(number_result_p2.Trim()) && number_result_p2.Length > 1)
                                                {
                                                    string numero_trouve = number_result_p1 + "." + number_result_p2;
                                                    bool isdouble2 = double.TryParse(numero_trouve, out double cotealti2);
                                                    if (isdouble2)
                                                    {
                                                        ed.WriteMessage("\nCôte détéctée : " + number_result_p1 + "." + number_result_p2 + "\n");
                                                        RedoWhile = false;
                                                        returnPointCote.cote = cotealti2;
                                                    }

                                                }
                                            }

                                        }
                                    }
                                    else {
                                        ed.WriteMessage("Par mesure de sécurité, les textes contenant des % ne peuvent être converti en côte.");
                                    }

                                }
                            }
                        }

                        Point3d point = new Point3d(blkRef.Position.X, blkRef.Position.Y, 0);
                        returnPointCote.point_gp = point;

                        //TODO : fix with and other functions Point3DToCurentSCU()
                        returnPointCote.point_cscu = point;
                        if (RedoWhile)
                        {
                            blkRef.Unhighlight();
                            ed.WriteMessage("Erreur, ce bloc ne contient aucune côte !");
                        }
                        tr.Commit();
                    }
                } while (RedoWhile);
                //End atribute
                return returnPointCote;
            }

            PromptPointOptions pointOptions = new PromptPointOptions(message + "[Bloc] : ", "Bloc");
            if (pointbaseclass != null)
            {
                pointOptions.UseBasePoint = true;
                //new PointClass() { X = pointFirst.point_gp.X, Y = pointFirst.point_gp.Y, Z = 0 }
                pointOptions.BasePoint = new Point3d(pointbaseclass.X, pointbaseclass.Y, 0);
            }
            bool redoAsk = false;
            do
            {
                redoAsk = false;
                bool isStatusKeyword = false;
                PromptPointResult promptPoint;
                if (mus.point_selection_type == "point")
                {
                    promptPoint = ed.GetPoint(pointOptions);
                    isStatusKeyword = promptPoint.Status == PromptStatus.Keyword;
                    if (isStatusKeyword == false)
                    {
                        mainreturnPointCote = AskForPoint(promptPoint);
                        if (mainreturnPointCote == null) return null;
                    }
                }
                else
                {
                    isStatusKeyword = true;
                }
                if (isStatusKeyword == true)
                {
                    mus.point_selection_type = "block";
                    mus.Save();
                    try
                    {
                        mainreturnPointCote = AskForBloc();
                        if (mainreturnPointCote == null) return null;
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        Autodesk.AutoCAD.Runtime.Exception AutEx = ex as Autodesk.AutoCAD.Runtime.Exception;
                        if (AutEx.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.OK)
                        {
                            mus.point_selection_type = "point";
                            mus.Save();
                            redoAsk = true;
                        }
                    }
                }
            } while (redoAsk);

            return mainreturnPointCote;
        }

        Point3d Point3DToCurentSCU(Point3d point)
        {
            Point3d pointB = point;
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            return pointB.TransformBy(ed.CurrentUserCoordinateSystem);
        }

        ObjectId DrawLine(Point3d start, Point3d end, int ColorIndex = 256)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            ObjectId returnObjectId;
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                // Define the new line
                using (Line acLine = new Line(start, end))
                {
                    //angleR = Vector3d.XAxis.GetAngleTo(acLine.GetFirstDerivative(start), Vector3d.ZAxis);
                    acLine.ColorIndex = ColorIndex;
                    // Add the line to the drawing
                    acBlkTblRec.AppendEntity(acLine);
                    acTrans.AddNewlyCreatedDBObject(acLine, true);
                    returnObjectId = acLine.ObjectId;
                }
                // Commit the changes and dispose of the transaction
                acTrans.Commit();
            }
            return returnObjectId;
        }


        void DrawText(string content, string layer, Point3d position, double textheight = 0.05)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                MText text_element = new MText
                {
                    Contents = content,
                    Layer = layer,
                    Attachment = AttachmentPoint.MiddleLeft,
                    Location = position,
                    TextHeight = textheight,
                    TextStyleId = AddFontStyle("Arial"),
                    ColorIndex = 250
                };
                acBlkTblRec.AppendEntity(text_element);
                tr.AddNewlyCreatedDBObject(text_element, true);
                tr.Commit();
            }
        }

        public void CreateBlock(string bloc_name, Point3d location, string attribut_text, DBObjectCollection ents, double angle = 0)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            Transaction tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                try
                {
                    SymbolUtilityServices.ValidateSymbolName(bloc_name, false);
                }
                catch
                {
                    ed.WriteMessage("\nNom de bloc invalide.");
                }

                if (!bt.Has(bloc_name))
                {
                    //Creation du bloc
                    BlockTableRecord btr = new BlockTableRecord
                    {
                        Name = bloc_name,
                        Origin = location
                    };
                    bt.UpgradeOpen();
                    ObjectId btrId = bt.Add(btr);
                    tr.AddNewlyCreatedDBObject(btr, true);

                    foreach (Entity ent in ents)
                    {
                        btr.AppendEntity(ent);
                        tr.AddNewlyCreatedDBObject(ent, true);
                    }
                }

                InsertBloc(bloc_name, location, attribut_text, angle);
                tr.Commit();
            }
        }


        public void InsertBloc(string bloc_name, Point3d location, string attribut_text, double angle = 0, List<StringIntClass> stringIntClasses = null)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Transaction tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                //insertion du bloc
                BlockTable bt2 = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
                BlockTableRecord blockDef = bt2[bloc_name].GetObject(OpenMode.ForRead) as BlockTableRecord;
                //Also open modelspace - we'll be adding our BlockReference to it
                BlockTableRecord ms = bt2[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite) as BlockTableRecord;
                //Create new BlockReference, and link it to our block definition
                using (BlockReference blockRef = new BlockReference(location, blockDef.ObjectId))
                {
                    //Add the block reference to modelspace
                    blockRef.ColorIndex = 256;
                    blockRef.Rotation = angle;
                    ms.AppendEntity(blockRef);
                    tr.AddNewlyCreatedDBObject(blockRef, true);
                    //Iterate block definition to find all non-constant
                    // AttributeDefinitions
                    foreach (ObjectId id in blockDef)
                    {
                        DBObject obj = id.GetObject(OpenMode.ForRead);
                        AttributeDefinition attDef = obj as AttributeDefinition;
                        if ((attDef != null) && (!attDef.Constant))
                        {
                            using (AttributeReference attRef = new AttributeReference())
                            {
                                attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
                                attRef.TextString = attribut_text;
                                attRef.TextStyleId = AddFontStyle("Arial");
                                //Add the AttributeReference to the BlockReference
                                blockRef.AttributeCollection.AppendAttribute(attRef);
                                tr.AddNewlyCreatedDBObject(attRef, true);
                            }
                        }
                    }

                    var props = blockRef.DynamicBlockReferencePropertyCollection;
                    if (stringIntClasses != null)
                    {
                        foreach (StringIntClass SIclass in stringIntClasses)
                        {
                            foreach (var prop in props.OfType<DynamicBlockReferenceProperty>())
                            {
                                if (prop.PropertyName == SIclass.name)
                                {
                                    //foreach (object t in prop.GetAllowedValues())
                                    //{
                                    //    doc.Editor.WriteMessage(t.ToString());
                                    //}
                                    try
                                    {
                                        switch (SIclass.type)
                                        {
                                            case "bool":
                                                //prop.Value = Convert.ToBoolean(SIclass.value);
                                                break;
                                            case "double":
                                                prop.Value = Convert.ToDouble(SIclass.value);
                                                break;
                                            case "string":
                                                prop.Value = Convert.ToSingle(SIclass.value);
                                                break;
                                            case "int":
                                                prop.Value = (short)Convert.ToInt32(SIclass.value);
                                                break;

                                        }
                                    }
                                    catch (System.Exception ex)
                                    {
                                        doc.Editor.WriteMessage("Erreur : " + ex.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                tr.Commit();
            }
        }


        void ImportSingleBlock(string filepathname)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Database sourceDb = new Database(false, true); //Temporary database to hold data for block we want to import
                try
                {
                    sourceDb.ReadDwgFile(filepathname, System.IO.FileShare.Read, true, ""); //Read the DWG into a side database
                    db.Insert(filepathname, sourceDb, false);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    ed.WriteMessage("\nErreur : " + ex.Message);
                }
                finally
                {
                    sourceDb.Dispose();
                }
                tr.Commit();
            }
        }

        public void SingleBlockImporting(string bloc_name)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            //var ed = doc.Editor;
            /*OpenFileDialog ofd = new OpenFileDialog();
            DialogResult result = ofd.ShowDialog();
            if (result == DialogResult.Cancel) //Ending method on cancel
            {
                return;
            }
            string fileToTryToImport = ofd.FileName;*/
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                string temp_folder = System.IO.Path.GetTempPath();
                string temp_folder_file = System.IO.Path.Combine(temp_folder, bloc_name + "." + "dwg");
                ReadResource(bloc_name, temp_folder_file);
                ImportSingleBlock(temp_folder_file);
                tr.Commit();
            }
        }

        public void ReadResource(string name, string tofile)
        {
            // Determine path
            byte[] ressource_bytes = Properties.Resources.ResourceManager.GetObject(name) as byte[];
            File.WriteAllBytes(tofile, ressource_bytes);

        }


        public double CalcDistanceFromTwoPoint(Point3d first_pointA, Point3d second_pointB)
        {
            double AB_dist_horizontal = Math.Pow(second_pointB.X - first_pointA.X, 2);
            double AB_dist_vertical = Math.Pow(second_pointB.Y - first_pointA.Y, 2);
            double AB_dist_total = Math.Sqrt(AB_dist_horizontal + AB_dist_vertical);
            return AB_dist_total;
        }

        public static Double RoundUpToNearest(Double passednumber, Double roundto)
        {
            // 105.5 up to nearest 1 = 106
            // 105.5 up to nearest 10 = 110
            // 105.5 up to nearest 7 = 112
            // 105.5 up to nearest 100 = 200
            // 105.5 up to nearest 0.2 = 105.6
            // 105.5 up to nearest 0.3 = 105.6

            //if no rounto then just pass original number back
            if (roundto == 0)
            {
                return passednumber;
            }
            else
            {
                return Math.Ceiling(passednumber / roundto) * roundto;
            }
        }

        string CalcCoteFromPente(PointCoteClass pointFirst, Point3d pointSecond, double pourcent_pente, bool force_conversion = false)
        {

            double AB_dist_total;
            if (force_conversion)
            {
                AB_dist_total = CalcDistanceFromTwoPoint(Point3DToCurentSCU(pointFirst.point_gp), Point3DToCurentSCU(pointSecond));
            }
            else
            {
                AB_dist_total = CalcDistanceFromTwoPoint(pointFirst.point_gp, Point3DToCurentSCU(pointSecond));
            }
            
           
            double cote = Math.Abs(pointFirst.cote) + (pourcent_pente * 0.01) * Math.Abs(AB_dist_total);
            return cote.ToString("#.00");
        }






        #endregion
    }
}
