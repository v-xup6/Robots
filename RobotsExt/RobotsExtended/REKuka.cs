using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RobotsExtended
{
    public class KukaDefJoints : GH_Component
    {
        public KukaDefJoints()
          : base("Define Joints", "DefJoints",
              "Define joint angle of each axis of the robot in degrees and outputs it as string of radians",
              "Robots", "Util")
        { }
        public override Guid ComponentGuid => new Guid("cd62f0e9-b8bc-49d9-b423-5e181b71f22f");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Define_Joints;
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Axis 1", "A1", "Degree of rotation for Axis 1", GH_ParamAccess.item);
            pManager.AddTextParameter("Axis 2", "A2", "Degree of rotation for Axis 2", GH_ParamAccess.item);
            pManager.AddTextParameter("Axis 3", "A3", "Degree of rotation for Axis 3", GH_ParamAccess.item);
            pManager.AddTextParameter("Axis 4", "A4", "Degree of rotation for Axis 4", GH_ParamAccess.item);
            pManager.AddTextParameter("Axis 5", "A5", "Degree of rotation for Axis 5", GH_ParamAccess.item);
            pManager.AddTextParameter("Axis 6", "A6", "Degree of rotation for Axis 6", GH_ParamAccess.item);
        }
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "External Axis", ChangeInput, true, external);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Joints", "J", "Joint rotations in radians", GH_ParamAccess.item);
        }
        public override void AddedToDocument(GH_Document doc)
        {
            int i = 0;
            foreach (var p in Params.Input)
            {
                if (p.SourceCount == 0)
                {
                    GH_NumberSlider slider = new GH_NumberSlider();
                    slider.CreateAttributes();

                    slider.Attributes.Pivot = new System.Drawing.PointF(
                        this.Attributes.Pivot.X - slider.Attributes.Bounds.Width - this.Attributes.Bounds.Width / 2 - 30,
                        this.Attributes.Pivot.Y - this.Attributes.Bounds.Height / 2 + i * slider.Attributes.Bounds.Height);
                        slider.Slider.Maximum = limits[1, i];
                        slider.Slider.Minimum = limits[0, i];
                    slider.Slider.DecimalPlaces = 0;
                    slider.SetSliderValue(0);
                    OnPingDocument().AddObject(slider, false);
                    p.AddSource(slider);
                    p.CollectData();
                }
                i++;
            }
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string[] theta = new string[6];
            for (int i = 0; i < (external ? 2 : 6); i++)
            {
                DA.GetData(i, ref theta[i]);
                double deg;
                try { deg = Convert.ToDouble(theta[i]); }
                catch { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input not a number."); return; }
                theta[i] = (deg % 45 == 0 && deg != 0) ? (deg == 180 ? "Pi" : (deg / 180 + " * Pi")) : RhinoMath.ToRadians(deg).ToString();
            }
            StringBuilder str = new StringBuilder(theta[0].ToString() + ", " + theta[1].ToString());
            if (!external)
            {
                for (int i = 2; i < 6; i++)
                {
                    str.Append(", " + theta[i]);
                }
            }
            DA.SetData(0, str);
        }
        private void ChangeInput(object sender, EventArgs e)
        {
            external = !external;
            if (external)
            {
                for (int i = 5; i > 1; i--)
                {
                    Params.UnregisterInputParameter(Params.Input[i], true);
                }
                Params.Input[0].Name = "External 1";
                Params.Input[0].NickName = "E1";
                Params.Input[0].Description = "Degree of rotation for External 1";
                Params.Input[1].Name = "External 2";
                Params.Input[1].NickName = "E2";
                Params.Input[1].Description = "Degree of rotation for External 2";
            }
            else
            {
                Params.Input[0].Name = "Axis 1";
                Params.Input[0].NickName = "A1";
                Params.Input[0].Description = "Degree of rotation for Axis 1";
                Params.Input[1].Name = "Axis 2";
                Params.Input[1].NickName = "A2";
                Params.Input[1].Description = "Degree of rotation for Axis 2";
                foreach (var p in parameters)
                {
                    Params.RegisterInputParam(p);
                }
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        bool external = false;
        readonly IGH_Param[] parameters = new IGH_Param[4]
        {
         new Param_String { Name = "Axis 3", NickName = "A3", Description = "Degree of rotation for Axis 3", Optional = false },
         new Param_String { Name = "Axis 4", NickName = "A4", Description = "Degree of rotation for Axis 4", Optional = false },
         new Param_String { Name = "Axis 5", NickName = "A5", Description = "Degree of rotation for Axis 5", Optional = false },
         new Param_String { Name = "Axis 6", NickName = "A6", Description = "Degree of rotation for Axis 6", Optional = false }
        };
        readonly int[,] limits = new int[2, 6]
        {
            {-170,-135,-120,-185,-119,-350},
            { 170,  35, 156, 185, 119, 350},
        };
    }
    /*public class KukaDefJointSnip : GH_Component
    {
        public KukaDefJointSnip()
          : base("Define Joints", "DefJoints",
              "Define joint angle of each axis of the robot in degrees",
              "Robots", "Util")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        { }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Archive archive = new GH_Archive();
            byte[] binaryRep = Convert.FromBase64String(snip);
            archive.Deserialize_Binary(binaryRep);
            string xmlSnippet = archive.Serialize_Xml();
            System.Windows.Forms.Clipboard.Clear();
            System.Windows.Forms.Clipboard.SetText(xmlSnippet);
            Paste();
            Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(10, DeleteThis);
        }

        readonly string snip = "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";
        void DeleteThis(GH_Document doc)
        {
            Grasshopper.Instances.ActiveCanvas.Document.RemoveObject(this, false);
        }
        void Paste()
        {
            GH_DocumentIO documentIO = new GH_DocumentIO();
            documentIO.Paste(GH_ClipboardType.System);
            var thispivot = this.Attributes.Pivot;

            int smallestX = Int32.MaxValue;
            int smallestY = Int32.MaxValue;
            foreach (IGH_DocumentObject obj in documentIO.Document.Objects)
            {
                var pivot = obj.Attributes.Pivot;
                if (pivot.X < smallestX) smallestX = (int)pivot.X;
                if (pivot.Y < smallestY) smallestY = (int)pivot.Y;
            }

            System.Drawing.Size offset = new System.Drawing.Size((int)thispivot.X - smallestX, (int)thispivot.Y - smallestY);

            documentIO.Document.TranslateObjects(offset, false);
            documentIO.Document.SelectAll();
            documentIO.Document.ExpireSolution();
            documentIO.Document.MutateAllIds();
            IEnumerable<IGH_DocumentObject> objs = documentIO.Document.Objects;
            Grasshopper.Instances.ActiveCanvas.Document.DeselectAll();
            Grasshopper.Instances.ActiveCanvas.Document.MergeDocument(documentIO.Document);
            Grasshopper.Instances.ActiveCanvas.Document.UndoUtil.RecordAddObjectEvent("Paste", objs);
            Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(10);
        }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Define_Joints;
        public override Guid ComponentGuid => new Guid("cd62f0e9-b8bc-49d9-b423-5e181b71f22f");
    }
    */
    public class KukaMergeKRL : GH_Component
    {
        public KukaMergeKRL()
          : base("Merge KRL", "KRL",
              "Merges robots codes into a single KRL file",
              "Robots", "Components")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "C", "Code from create program", GH_ParamAccess.tree);
            pManager.AddTextParameter("Program Name", "N", "Set program Name", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("FileName", "src", "Name of program file", GH_ParamAccess.item);
            pManager.AddTextParameter("KRL Code", "KRL", "Merged KRL Code", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool cvel = false;
            bool trigger = false;
            string prevApoCVEL = string.Empty;

            DA.GetDataTree(0, out GH_Structure<GH_String> code);
            string name = string.Empty;
            DA.GetData(1, ref name);
            if (name == string.Empty) name = "DefaultProgram";

            List<string> header =
                code.Branches[0][0].Value
                .Split(new string[] { "\r\n","\r","\n",Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Dictionary<string, string> declare = new Dictionary<string, string>();
            List<string> prog = new List<string>();
            for (int i = 1; i < code.Branches[1].Count - 1; i++)// Skip RVP+REL and ENDDAT
            {
                string[] de = code.Branches[1][i].Value.Split(new string[] { " = ", " =", "= ", "=" }, StringSplitOptions.RemoveEmptyEntries);
                de[0] = de[0].Replace("GLOBAL ", string.Empty);
                if (de[0].Contains("Zonev")) cvel = true;
                if (de[0].Contains("Zone000")) trigger = true;
                if (!declare.ContainsKey(de[0]))
                    declare.Add(de[0], de[1] ?? de[0]);
            }
            for (int i = 1; i < code.Branches[2].Count; i++)// Skip RVP+REL
            {
                prog.Add(code.Branches[2][i].Value);
            }

            List<string> main = new List<string>();
            main.AddRange(prog);
            main.Insert(2, "\r\n;START PROG");

            for (int i = 0; i < main.Count; i++)
            {
                if (cvel)
                {
                    if (main[i].StartsWith(@"\b C_VEL"))
                    {
                        string apoCVEL = main[i].Split(';')[1];
                        if (main[i - 1].Substring(main[i - 1].Length - 5, 5) == "C_DIS")
                            main[i - 1] = main[i - 1].Replace("C_DIS", "C_VEL");
                        main.RemoveAt(i);
                        if (prevApoCVEL != apoCVEL) main.Insert(i - 1, apoCVEL);
                        else i -= 1;
                        prevApoCVEL = apoCVEL;
                    }
                }
                if (trigger)
                {
                    if (main[i].StartsWith("CONTINUE\r\n"))
                    {
                        if (main[i].StartsWith("CONTINUE\r\nWAIT")) goto EndofProg;
                        if (i + 8 >= main.Count)
                        {
                            for (int j = 1; j < 8; j++)
                            {
                                if (main[i + j].StartsWith("END"))
                                {
                                    goto EndofProg;
                                }
                                else if (main[i + j].StartsWith("CONTINUE\r\n"))
                                {
                                    continue;
                                }
                                else break;
                            }
                        }
                        main[i] = main[i].Replace("CONTINUE\r\n", "TRIGGER WHEN DISTANCE=0 DELAY=0 DO ");
                        continue;
                    EndofProg:
                        main[i] = main[i].Replace("CONTINUE\r\n", "");
                        continue;
                    }
                }
            }

            char[] arr = name.Where(c => (char.IsLetterOrDigit(c) || c == '_')).ToArray();
            StringBuilder nameFix = new StringBuilder();
            if (Char.IsDigit(arr[0])) nameFix.Append("KUKA_");
            nameFix.Append(arr);
            name = nameFix.ToString().Substring(0, Math.Min(nameFix.Length, 24));

            List<string> all = header.GetRange(0, 3);
            all[2] = "DEF " + name + "()";
            all.Add("\r\n;DAT DECL");
            all.AddRange(declare.Keys);
            all.Add("\r\n;INI");
            foreach (var de in declare.Keys)
            {
                all.Add(de.Split(' ')[2] + " = " + declare[de]);
            }
            all.AddRange(header.GetRange(3, header.Count - 3));
            all.AddRange(main);

            DA.SetData(0, name + ".src");
            DA.SetDataList(1, all);
        }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.KRL;
        public override Guid ComponentGuid => new Guid("309454cf-ea5e-470f-80a8-fc19e3729dfc");
    }

    public class KukaCVEL : GH_Component
    {
        public KukaCVEL()
          : base("Speed Aproximation", "CVEL",
              "Commands the robot to maintain defined speed percentage by zoning (Custom Command)",
              "Robots", "Commands")
        { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Speed Percentage", "%", "Speed % to maintain [0-100]", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new Robots.Grasshopper.CommandParameter(), "Command", "C", "Command", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int percentage = 50;
            DA.GetData(0, ref percentage);
            percentage = Math.Max(percentage, 0);
            percentage = Math.Min(percentage, 100);
            string manufacturerText = "KUKA",
                code = $"\\b C_VEL;$APO.CVEL=Zonev{percentage}",
                declaration = $"DECL GLOBAL REAL Zonev{percentage} = {percentage}";

            var command = new Robots.Commands.Custom("Speed Aproximation");
            if (!Enum.TryParse<Robots.Manufacturers>(manufacturerText, out var manufacturer))
            {
                throw new ArgumentException($"Manufacturer {manufacturerText} not valid.");
            }
            command.AddCommand(manufacturer, code, declaration);
            DA.SetData(0, new Robots.Grasshopper.GH_Command(command));
        }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Speed_Approximation;
        public override Guid ComponentGuid => new Guid("79B3841F-6BCE-4D80-B665-B6DF637C1797");
    }

    public class KukaRotateEuler : GH_Component
    {
        public KukaRotateEuler() : base("Rotate Euler", "RotEuler", "Rotate an object with (KUKA) Euler notation", "Transform", "Euclidean") { }
        public override Guid ComponentGuid => new Guid("F838B4F6-42FA-4D77-9615-F6B2D142BA68");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Rotate_Euler;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "Geo", "Geometry or plane to rotate", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Rotation Plane", "Pln", "Plane to use for center or rotation", GH_ParamAccess.item);
            pManager.AddNumberParameter("Z-Axis", "A", "Degree of rotation on Z-Axis", GH_ParamAccess.item);
            pManager.AddNumberParameter("Y-Axis", "B", "Degree of rotation on Y-Axis", GH_ParamAccess.item);
            pManager.AddNumberParameter("X-Axis", "C", "Degree of rotation on X-Axis", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "Rotated geometry", GH_ParamAccess.item);
            pManager.AddTransformParameter("Transform", "X", "Transformation data", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            IGH_GeometricGoo geo = null;
            Plane pln = Plane.Unset;
            double a = 0, b = 0, c = 0;
            DA.GetData(0, ref geo);
            DA.GetData(1, ref pln);
            DA.GetData(2, ref a);
            DA.GetData(3, ref b);
            DA.GetData(4, ref c);
            if (geo != null)
                geo = geo.DuplicateGeometry();
            if (pln == Plane.Unset && geo is GH_Plane p)
            {
                GH_Convert.ToPlane(p, ref pln, GH_Conversion.Both);
            }
            else if (geo == null && pln != Plane.Unset)
            {
                geo = GH_Convert.ToGeometricGoo(pln);
            }
            else if (pln == Plane.Unset || geo == null)
            {
                string e;
                if (pln == Plane.Unset)
                    e = Grasshopper.CentralSettings.CanvasFullNames ? Params.Input[1].Name : Params.Input[1].NickName;
                else
                    e = Grasshopper.CentralSettings.CanvasFullNames ? Params.Input[0].Name : Params.Input[0].NickName;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Input parameter {e} failed to collect data");
                return;
            }

            double r = Math.PI / 180;
            Transform x = Transform.Rotation(a * r, pln.ZAxis, pln.Origin);
            x *= Transform.Rotation(b * r, pln.YAxis, pln.Origin);
            x *= Transform.Rotation(c * r, pln.XAxis, pln.Origin);

            DA.SetData(0, geo.Transform(x));
            DA.SetData(1, x);
        }
    }
}
