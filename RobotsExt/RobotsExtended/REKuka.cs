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
using GH_IO.Serialization;

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
    public class RenderColour : GH_Component
    {
        public RenderColour()
          : base("Render Colour", "Colour",
              "Adds render colour to robot mesh",
              "Robots", "Util")
        { }
        public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;

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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Colourful;
        public override Guid ComponentGuid => new Guid("5AE1E121-11B3-499A-AB30-82B02FAD533A");
    }
    
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
