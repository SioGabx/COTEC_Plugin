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
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace COTEC_Plugin
{
    public partial class Internal
    {
        public void INTER_Command_CCI()
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            //var db = doc.Database;
            var ed = doc.Editor;
            Database db = doc.Database;

            //string curentLayer = (string)AcAp.GetSystemVariable("clayer");
            while (true)
            {
                List<PointCoteClass> Getpoints = GetTwoPoint();
                if (Getpoints == null) { return; }
                PointCoteClass pointFirst = Getpoints[0];
                PointCoteClass pointSecond = Getpoints[1];
                ObjectId Line = DrawLine(pointFirst.point_cscu, pointSecond.point_cscu, 252);
                Boolean redoWhile = false;
                do
                {

                    Transaction tr = db.TransactionManager.StartTransaction();
                    using (tr)
                    {
                        ObjectIdCollection ids = new ObjectIdCollection();
                        DBObjectCollection ents = CoteElementGenerator(0.00, new Point3d(0, 0, 0));
                        foreach (Entity ent in ents)
                        {
                            Matrix3d usc_rot = Matrix3d.Rotation(GetUSCRotation(true), Vector3d.ZAxis, new Point3d(0, 0, 0));
                            ent.TransformBy(usc_rot);
                        }
                        Point3d basePt = new Point3d(0, 0, 0);
                        Point3d curPt = basePt;
                        PointMonitorEventHandler handler = null;
                        List<Drawable> drawables = CreateTransGraphics(ents);
                        handler = delegate (object sender, PointMonitorEventArgs e)
                    {
                        //Point3d pt = e.Context.RawPoint;
                        Point3d pt = e.Context.ComputedPoint;
                        /* if (IsOrthModeOn())
                         {
                             pt = GetOrthoPoint(basePt, pt);
                         }*/
                        // Update our graphics and the current point
                        UpdateTransGraphics(drawables, curPt, pt);
                        curPt = pt;
                    };

                        List<double> PenteA = CalcCoteAndPente(new Point3d(0, 0, 0));

                        ed.WriteMessage("Pente : " + PenteA[1].ToString() + "%\n");


                        ed.PointMonitor += handler;
                        //get location intermediaire
                        PromptPointOptions pointOptions;
                        if (redoWhile)
                        {
                            pointOptions = new PromptPointOptions("Indiquer les emplacements des points côte");
                        }
                        else
                        {
                            pointOptions = new PromptPointOptions("Indiquer l'emplacement du point de la côte" + "[Multiple] : ", "Multiple");
                        }

                        PromptPointResult IpromptPoint = ed.GetPoint(pointOptions);
                        ClearTransGraphics(drawables);
                        if (IpromptPoint.Status == PromptStatus.OK)
                        {
                            if (handler != null) { ed.PointMonitor -= handler; }

                            Point3d pointIntermediaire = Point3DToCurentSCU(IpromptPoint.Value);
                            List<double> CoteAndPente = CalcCoteAndPente(pointIntermediaire);
                            double I_cote = CoteAndPente[0];
                            double pente = CoteAndPente[1];

                            ed.WriteMessage("Pente : " + pente + "%\n");



                            // DrawText(I_cote.ToString("#.00"), curentLayer, pointIntermediaire, 0.2);
                            GENENER_Bloc_Cote(I_cote, pointIntermediaire);
                            /*
                            PromptKeywordOptions put_pente = new PromptKeywordOptions("Ajouter pente ? [Oui/Non]");
                            put_pente.Keywords.Add("Oui");
                            put_pente.Keywords.Add("Non");
                            PromptResult resu = ed.GetKeywords(put_pente);
                            if (resu.StringResult == "Oui") { 
                            double min(double a, double b) { return Math.Min(a, b); }
                            double max(double a, double b) { return Math.Max(a, b); }
                            Point3d point_Pente_AI = new Point3d(
                                min(pointIntermediaire.X, pointFirst.point_cscu.X) + (max(pointIntermediaire.X, pointFirst.point_cscu.X) - min(pointIntermediaire.X, pointFirst.point_cscu.X)) / 2,
                                min(pointIntermediaire.Y, pointFirst.point_cscu.Y) + (max(pointIntermediaire.Y, pointFirst.point_cscu.Y) - min(pointIntermediaire.Y, pointFirst.point_cscu.Y)) / 2, 
                                0
                                );
                            Point3d point_Pente_IB = new Point3d(
                               min(pointIntermediaire.X, pointSecond.point_cscu.X) + (max(pointIntermediaire.X, pointSecond.point_cscu.X) - min(pointIntermediaire.X, pointSecond.point_cscu.X)) / 2,
                               min(pointIntermediaire.Y, pointSecond.point_cscu.Y) + (max(pointIntermediaire.Y, pointSecond.point_cscu.Y) - min(pointIntermediaire.Y, pointSecond.point_cscu.Y)) / 2,
                               0
                               );
                            Generer_pente(pente, point_Pente_AI, pointFirst, pointSecond);
                            Generer_pente(pente, point_Pente_IB, pointFirst, pointSecond);
                            }*/
                            tr.Commit();
                        }
                        else
                        {
                            if (IpromptPoint.Status == PromptStatus.Keyword)
                            {
                                redoWhile = true;
                            }
                            else
                            {
                                tr.Commit();
                                break;
                            }
                        }
                    }

                } while (redoWhile);

                Transaction tr_finish = db.TransactionManager.StartTransaction();
                using (tr_finish)
                {
                    DBObject obj = Line.GetObject(OpenMode.ForWrite);
                    obj.Erase(true);
                    tr_finish.Commit();
                }
                List<double> CalcCoteAndPente(Point3d pointIntermediaire)
                {
                    //calcul distance :
                    double AI_dist_horizontal = Math.Pow(pointIntermediaire.X - pointFirst.point_cscu.X, 2);
                    double AI_dist_vertical = Math.Pow(pointIntermediaire.Y - pointFirst.point_cscu.Y, 2);
                    double AI_dist_total = Math.Sqrt(AI_dist_horizontal + AI_dist_vertical);

                    double IB_dist_horizontal = Math.Pow(pointSecond.point_cscu.X - pointIntermediaire.X, 2);
                    double IB_dist_vertical = Math.Pow(pointSecond.point_cscu.Y - pointIntermediaire.Y, 2);
                    double IB_dist_total = Math.Sqrt(IB_dist_horizontal + IB_dist_vertical);

                    double AB_dist_horizontal = Math.Pow(pointSecond.point_cscu.X - pointFirst.point_cscu.X, 2);
                    double AB_dist_vertical = Math.Pow(pointSecond.point_cscu.Y - pointFirst.point_cscu.Y, 2);
                    double AB_dist_total = Math.Sqrt(AB_dist_horizontal + AB_dist_vertical);

                    double AIB_dist_total = AI_dist_total + IB_dist_total;
                    //ed.WriteMessage("Distance : " + Math.Round(AIB_dist_total, 2) + "\n");

                    double AI_pourcent = AI_dist_total / AIB_dist_total;
                    double AB_cote_dif = Math.Abs(pointFirst.cote - pointSecond.cote);
                    double I_dif_to_add_sus = AB_cote_dif * AI_pourcent;
                    double I_cote = pointFirst.cote;

                    double pente = Math.Round((AB_cote_dif / AB_dist_total) * 100.00, 2);
                    if (pointFirst.cote > pointSecond.cote)
                    {
                        I_cote -= I_dif_to_add_sus;
                    }
                    else
                    {
                        I_cote += I_dif_to_add_sus;
                    }
                    I_cote = Math.Round(I_cote, 2);
                    return new List<double>() { I_cote, pente };
                }


                void UpdateTransGraphics(List<Drawable> drawables, Point3d curPt, Point3d moveToPt)
                {
                    // Displace each of our drawables
                    Matrix3d mat = Matrix3d.Displacement(curPt.GetVectorTo(moveToPt));
                    // Update their graphics
                    foreach (Drawable d in drawables)
                    {
                        Entity e = d as Entity;
                        if (e is AttributeDefinition attElement)
                        {
                            string cote = CalcCoteAndPente(moveToPt)[0].ToString("#.00");
                            attElement.TextString = cote;
                            attElement.Tag = cote;
                            e = attElement as Entity;
                            //    DBObject obj = e.ObjectId.GetObject(OpenMode.ForRead);
                            //    AttributeDefinition attDef = obj as AttributeDefinition;
                            //    attDef.TextString = "patate";
                        }
                        e.TransformBy(mat);
                        TransientManager.CurrentTransientManager.UpdateTransient(d, new IntegerCollection());
                    }
                }

            }
        }



        public void INTER_Command_CCP()
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            ObjectId Line = ObjectId.Null;
            void ClearTempLine(ObjectId objectId)
            {
                if (Line != ObjectId.Null && Line.IsErased == false)
                {
                    Transaction tr_finish = db.TransactionManager.StartTransaction();
                    using (tr_finish)
                    {
                        DBObject obj = Line.GetObject(OpenMode.ForWrite);
                        obj.Erase(true);
                        tr_finish.Commit();
                    }

                }
            }
            void UpdateTransGraphics(List<Drawable> drawables, Point3d curPt, Point3d moveToPt)
            {
                Matrix3d mat = Matrix3d.Displacement(curPt.GetVectorTo(moveToPt));
                foreach (Drawable d in drawables)
                {
                    Entity e = d as Entity;
                    e.TransformBy(mat);
                    TransientManager.CurrentTransientManager.UpdateTransient(d, new IntegerCollection());
                }
            }

            while (true)
            {
                ClearTempLine(Line);
                List<PointCoteClass> Getpoints = GetTwoPoint();
                if (Getpoints == null)
                {

                    break;
                }

                PointCoteClass pointFirst = Getpoints[0];
                PointCoteClass pointSecond = Getpoints[1];
                Line = DrawLine(pointFirst.point_cscu, pointSecond.point_cscu, 252);
                List<StringIntClass> stringIntClasses = GeneratePenteBlocSettings(pointFirst, pointSecond);
                if (stringIntClasses[0].value != "-1")
                {
                    DBObjectCollection ents = new DBObjectCollection();
                    //Point3d basePt = new Point3d(0, 0, 0);
                    //Point3d curPt = basePt;
                    Point3d curPt = new Point3d(0, 0, 0);
                    PointMonitorEventHandler handler = null;
                    Transaction tr = db.TransactionManager.StartTransaction();
                    using (tr)
                    {
                        //double AB_dist_horizontal = Math.Pow(pointSecond.point_cscu.X - pointFirst.point_cscu.X, 2);
                        //double AB_dist_vertical = Math.Pow(pointSecond.point_cscu.Y - pointFirst.point_cscu.Y, 2);
                        double AB_dist_total = CalcDistanceFromTwoPoint(pointFirst.point_cscu, pointSecond.point_cscu);//Math.Sqrt(AB_dist_horizontal + AB_dist_vertical);


                        double AB_cote_dif = Math.Abs(pointFirst.cote - pointSecond.cote);
                        double pente = Math.Round((AB_cote_dif / AB_dist_total) * 100.00, 2);
                        ed.WriteMessage("Pente : " + pente + "%\n");
                        #region generate_fake_pente_element_preview
                        double angle_pente = Convert.ToDouble(stringIntClasses[0].value);
                        Matrix3d blockRotate = Matrix3d.Rotation(angle_pente, Vector3d.ZAxis, new Point3d(0, 0, 0));
                        Autodesk.AutoCAD.DatabaseServices.Polyline poly = new Autodesk.AutoCAD.DatabaseServices.Polyline(3);
                        if (stringIntClasses[1].value == "0")
                        {
                            poly.AddVertexAt(0, new Point2d(-0.5, 0), 0, 0.035, 0.035);
                            poly.AddVertexAt(0, new Point2d(0.2105, 0), 0, 0.035, 0.035);
                            poly.AddVertexAt(0, new Point2d(0.5, 0), 0, 0.0, 0.2);
                        }
                        else
                        {
                            poly.AddVertexAt(0, new Point2d(0.5, 0), 0, 0.035, 0.035);
                            poly.AddVertexAt(0, new Point2d(-0.2105, 0), 0, 0.035, 0.035);
                            poly.AddVertexAt(0, new Point2d(-0.5, 0), 0, 0.0, 0.2);
                        }
                        poly.TransformBy(blockRotate);
                        ents.Add(poly);
                        MText text_element = new MText
                        {
                            Contents = RoundUpToNearest(pente, 0.05).ToString() + "%",
                            Layer = "0",
                            Attachment = AttachmentPoint.BottomCenter,
                            Location = new Point3d(0.001, 0.135, 0),
                            TextHeight = 0.35,
                            ColorIndex = 250
                        };
                        text_element.TransformBy(blockRotate);
                        ents.Add(text_element);
                        /*
                        foreach (Entity ent in ents)
                        {
                            Matrix3d usc_rot = Matrix3d.Rotation(get_ucs_rot(true), Vector3d.ZAxis, new Point3d(0, 0, 0));
                            ent.TransformBy(usc_rot);
                        }
                        */
                        #endregion
                        List<Drawable> drawables = CreateTransGraphics(ents);
                        handler = delegate (object sender, PointMonitorEventArgs e)
                        {

                            //Point3d pt = e.Context.RawPoint;
                            Point3d pt = e.Context.ComputedPoint;
                            /* if (IsOrthModeOn())
                             {
                                 pt = GetOrthoPoint(basePt, pt);
                             }*/
                            UpdateTransGraphics(drawables, curPt, pt);
                            curPt = pt;
                        };

                        ed.PointMonitor += handler;

                        PromptPointResult IpromptPoint = ed.GetPoint("Indiquer l'emplacement du bloc pente à ajouter");
                        ClearTransGraphics(drawables);
                        if (handler != null) { ed.PointMonitor -= handler; }
                        tr.Commit();
                        if (IpromptPoint.Status == PromptStatus.OK)
                        {
                            Point3d point = Point3DToCurentSCU(IpromptPoint.Value);
                            GENENER_Bloc_Pente(RoundUpToNearest(pente, 0.5), point, pointFirst, pointSecond);
                        }
                        else
                        {
                            //ed.WriteMessage("Pente : " + pente + "%\n");

                        }
                        ClearTempLine(Line);

                    }
                }
            }

        }



        public void INTER_Command_CCD()
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            //PromptDistanceOptions d = new PromptDistanceOptions("Distance");
            //d.DefaultValue = 2;
            //d.UseDefaultValue = true;
            //d.AllowZero = true;
            //d.BasePoint = new Point3d(100, 100, 0);
            //d.UseBasePoint = true;
            //d.AllowArbitraryInput = true;
            //ed.GetDistance("e");
            while (true)
            {
                bool askForPourcent = true;
                MyUserSettings mus = new MyUserSettings();
                double pourcent = 0.00;

                PromptDoubleOptions doubleOptions = new PromptDoubleOptions("Inquiquez un pourcentage de pente (chiffre négatif pour descendre)");
                doubleOptions.AllowNegative = true;
                doubleOptions.UseDefaultValue = true;
                doubleOptions.DefaultValue = Convert.ToDouble(mus.default_pourcentage_CCD);
                var start_res_p = ed.GetDouble(doubleOptions);
                if (start_res_p.Status == PromptStatus.OK)
                {
                    pourcent = start_res_p.Value;
                    mus.default_pourcentage_CCD = pourcent.ToString("#.00");
                    mus.Save();
                    askForPourcent = false;
                }
                else
                {
                    return;  // user cancels the process
                }


                /* while (true)
                 {*/
                PointCoteClass pointCoteClass_fp = AskForPointCote("\nSelectionner la côte de base");

               
                if (pointCoteClass_fp == null || pointCoteClass_fp.point_gp == null)
                {
                    break;
                }

                //GENENER_Bloc_Cote(Convert.ToDouble(0), Point3DToCurentSCU(pointCoteClass_fp.point_cscu));
                //GENENER_Bloc_Cote(Convert.ToDouble(1), Point3DToCurentSCU(pointCoteClass_fp.point_gp));
                //GENENER_Bloc_Cote(Convert.ToDouble(2), pointCoteClass_fp.point_gp);
                //GENENER_Bloc_Cote(Convert.ToDouble(3), pointCoteClass_fp.point_cscu);

                //Point3d point_gp = pointCoteClass_fp.point_gp;
                //Point3d point_cscu = pointCoteClass_fp.point_cscu;


                //ed.WriteMessage("\n" + pointCoteClass_fp.point_gp.ToString());
                //PromptPointOptions pointOptionsA = new PromptPointOptions("\nA");
                //pointOptionsA.UseBasePoint = true;
                //pointOptionsA.BasePoint = Point3DToCurentSCU(point_cscu);
                //var resA = ed.GetPoint(pointOptionsA);
                //PromptPointOptions pointOptionsB = new PromptPointOptions("\nB");
                //pointOptionsB.UseBasePoint = true;
                //pointOptionsB.BasePoint = Point3DToCurentSCU(point_gp);
                //var resB = ed.GetPoint(pointOptionsB);
                //PromptPointOptions pointOptionsC = new PromptPointOptions("\nC");
                //pointOptionsC.UseBasePoint = true;
                //pointOptionsC.BasePoint = point_gp;
                //var resC = ed.GetPoint(pointOptionsC); 
                //PromptPointOptions pointOptionsD = new PromptPointOptions("\nD");
                //pointOptionsD.UseBasePoint = true;
                //pointOptionsD.BasePoint = point_cscu;
                //var resD = ed.GetPoint(pointOptionsD);




                ClearHighlightedObjects();
                bool isMultiple = false;
                bool redoOnce = false;

                do
                {
                    redoOnce = false;
                    if (askForPourcent)
                    {


                        doubleOptions.DefaultValue = Convert.ToDouble(mus.default_pourcentage_CCD);
                        var res_p = ed.GetDouble(doubleOptions);
                        if (res_p.Status == PromptStatus.OK)
                        {
                            pourcent = res_p.Value;
                            mus.default_pourcentage_CCD = pourcent.ToString("#.00");
                            mus.Save();
                            askForPourcent = false;
                        }
                        else
                        {
                            return;  // user cancels the process
                        }

                    }


                    //do
                    //{
                    Transaction tr = db.TransactionManager.StartTransaction();
                    using (tr)
                    {

                        ObjectIdCollection ids = new ObjectIdCollection();
                        DBObjectCollection ents = CoteElementGenerator(0.00, new Point3d(0, 0, 0));
                        foreach (Entity ent in ents)
                        {
                            Matrix3d usc_rot = Matrix3d.Rotation(GetUSCRotation(true), Vector3d.ZAxis, new Point3d(0, 0, 0));
                            ent.TransformBy(usc_rot);
                        }
                        //Point3d basePt = pointCoteClass_fp.point_cscu;
                        //Point3d curPt = basePt;

                        Point3d curPt = new Point3d(0, 0, 0);
                        PointMonitorEventHandler handler = null;
                        List<Drawable> drawables = CreateTransGraphics(ents);
                        handler = delegate (object sender, PointMonitorEventArgs e)
                        {
                            //Point3d pt = e.Context.RawPoint;
                            Point3d pt = e.Context.ComputedPoint;
                            // if (IsOrthModeOn())
                            //  {
                            //     pt = GetOrthoPoint(basePt, pt);
                            // }
                            // Update our graphics and the current point
                            //PointCoteClass pointCoteClass_fp2 = new PointCoteClass() { point_cscu = Point3DToCurentSCU(pointCoteClass_fp.point_gp), point_gp = Point3DToCurentSCU(pointCoteClass_fp.point_gp) };
                          
                            UpdateTransGraphics(drawables, curPt, pt, pointCoteClass_fp, pourcent);
                            curPt = pt;
                        };

                        ed.PointMonitor += handler;
                        PromptPointOptions pointOptions;
                        if (isMultiple)
                        {
                            pointOptions = new PromptPointOptions("\nSelectionnez un point à calculer : Pente à " + pourcent.ToString() + "% [Pourcentage]", "Pourcentage");

                        }
                        else
                        {
                            pointOptions = new PromptPointOptions("\nSelectionnez un point à calculer : Pente à " + pourcent.ToString() + "% [Pourcentage/Multiples]", "Pourcentage Multiples");

                        }

                        pointOptions.UseBasePoint = true;
                        pointOptions.BasePoint = pointCoteClass_fp.point_cscu;

                        var res = ed.GetPoint(pointOptions);
                        ClearTransGraphics(drawables);
                        if (res.Status == PromptStatus.OK)
                        {
                            if (handler != null) { ed.PointMonitor -= handler; }

                            Point3d pointToCalc = res.Value;




                            // DrawText(I_cote.ToString("#.00"), curentLayer, pointIntermediaire, 0.2);
                            string cote = CalcCoteFromPente(pointCoteClass_fp, pointToCalc, pourcent);
                            GENENER_Bloc_Cote(Convert.ToDouble(cote), Point3DToCurentSCU(pointToCalc));
                            //tr.Commit();


                        }
                        else if (res.Status == PromptStatus.Keyword)
                        {
                            if (res.StringResult == "Pourcentage")
                            {
                                redoOnce = true;
                                askForPourcent = true;
                            }
                            if (res.StringResult == "Multiples")
                            {
                                isMultiple = true;
                            }
                        }
                        else
                        {
                            tr.Commit();
                            break;
                            // break; //User cancels the process
                        }
                        if (res.Status != PromptStatus.Keyword)
                        {
                            tr.Commit();
                        }


                    }

                    //} while (isMultiple);
                    //if (do_breaking)
                    //{
                    //    break;
                    //}
                } while (isMultiple || redoOnce);

            }

            void UpdateTransGraphics(List<Drawable> drawables, Point3d curPt, Point3d moveToPt, PointCoteClass point_depart, double pourcent_pente)
            {
                // Displace each of our drawables
                Matrix3d mat = Matrix3d.Displacement(curPt.GetVectorTo(moveToPt));
                // Update their graphics
                foreach (Drawable d in drawables)
                {
                    Entity e = d as Entity;
                    if (e is AttributeDefinition attElement)
                    {
                        string cote = CalcCoteFromPente(point_depart, moveToPt, pourcent_pente, true);
                        attElement.TextString = cote;
                        attElement.Tag = cote;
                        e = attElement as Entity;
                        //    DBObject obj = e.ObjectId.GetObject(OpenMode.ForRead);
                        //    AttributeDefinition attDef = obj as AttributeDefinition;
                        //    attDef.TextString = "patate";
                    }
                    e.TransformBy(mat);
                    TransientManager.CurrentTransientManager.UpdateTransient(d, new IntegerCollection());
                }
            }
        }



        public void INTER_Command_CCD2()
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            Transaction tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                ObjectIdCollection ids = new ObjectIdCollection();
                DBObjectCollection ents = CoteElementGenerator(0.00, new Point3d(0, 0, 0));
                foreach (Entity ent in ents)
                {
                    Matrix3d usc_rot = Matrix3d.Rotation(GetUSCRotation(true), Vector3d.ZAxis, new Point3d(0, 0, 0));
                    ent.TransformBy(usc_rot);
                }
                Point3d curPt = new Point3d(0, 0, 0);
                PointMonitorEventHandler handler = null;
                List<Drawable> drawables = CreateTransGraphics(ents);
                handler = delegate (object sender, PointMonitorEventArgs e)
                {
                    Point3d pt = e.Context.ComputedPoint;
                    UpdateTransGraphics(drawables, curPt, pt);
                    curPt = pt;
                };
                ed.PointMonitor += handler;

                PromptDoubleOptions promptDouble = new PromptDoubleOptions("hello");
                promptDouble.AllowArbitraryInput = true;
                promptDouble.AllowNone = true;
                promptDouble.AllowArbitraryInput = true;
                promptDouble.AllowZero = true;
                ed.GetDouble(promptDouble);


            }


            void UpdateTransGraphics(List<Drawable> drawables, Point3d curPt, Point3d moveToPt)
            {
                // Displace each of our drawables
                Matrix3d mat = Matrix3d.Displacement(curPt.GetVectorTo(moveToPt));
                // Update their graphics
                foreach (Drawable d in drawables)
                {
                    Entity e = d as Entity;
                    if (e is AttributeDefinition attElement)
                    {
                        string cote = "100.00";
                        attElement.TextString = cote;
                        attElement.Tag = cote;
                        e = attElement as Entity;
                        //    DBObject obj = e.ObjectId.GetObject(OpenMode.ForRead);
                        //    AttributeDefinition attDef = obj as AttributeDefinition;
                        //    attDef.TextString = "patate";
                    }
                    e.TransformBy(mat);
                    TransientManager.CurrentTransientManager.UpdateTransient(d, new IntegerCollection());
                }
            }
        }

    }

}
