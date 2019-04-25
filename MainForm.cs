using Algebra3D;
using Graphics3D;
using Graphics3D.Controls;
using Graphics3D.FileFormats.OFF;
using Graphics3D.Geometry;
using Graphics3D.Graph.Interactive;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RigidViewer
{
    /// <summary>
    /// Full GUI of the application
    /// </summary>
    public partial class MainForm : Form
    {
        private Graphics3DControl _gl;

        private PhongPNTriangleGeometry _geo;
        private Channel<Double3> _pos;
        private PointSelectionHandler _pointSelectionHandler;

        private SelectionHandler _selectionHandler;
        private ShiftingHandler _shiftingHandler;

        private LowResToHighResMapper _lowResToHighResMapper;
        private Channel<Double3> _posHighRes;
        private PhongPNTriangleGeometry _geoHighRes;

        private Double2 _mousePos;
        private RigidSimulator _sim;


        public MainForm()
        {
            InitializeComponent();

            _gl = new Graphics3DControl();
            _gl.Dock = DockStyle.Fill;
            splitContainer1.Panel2.Controls.Add(_gl);
            _gl.GLReady += GLReady;

            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.T && e.Control)
            {
                this.Location = new Point(0, 0);
                this.Size = new Size(1280, 720);
            }
        }

        private void GLReady()
        {
            // Now OpenGL is ready to handle draw calls
            // Initialize the scene
            _gl.BackColor = Color.FromArgb(250, 250, 250);

            _gl.SceneGraph.Add(new CoordinateSystemGeometryScreenFixed());
            _gl.InputHandler.MouseMove += InputHandler_MouseMove;

            _pointSelectionHandler = new PointSelectionHandler(_gl.InputHandler, _gl.SceneGraph, _gl.Camera);
            _pointSelectionHandler.SelectionRequested += SelectionRequested;

            _selectionHandler = new SelectionHandler(_gl.SceneGraph, _gl.InputHandler);
            _shiftingHandler = new ShiftingHandler(_gl.InputHandler, _gl.Camera, _selectionHandler, true);
            _selectionHandler.MultiSelect = false;

            _gl.InputHandler.RotationMatrix = Double4x4.RotationX(0.05 * Math.PI) * Double4x4.RotationY(-0.1 * Math.PI);

            _selectionHandler.Validator = delegate (PickingResult r)
            {
                return r.Node is SelectableSceneNode;
            };
        }

        private void SelectionRequested(SelectionFrustum obj)
        {
            if (_pos == null)
                return;

            // The user has interactively edited the constrained sections -> propagate the information to the simulation core
            List<int> indices = new List<int>();
            List<Double3> pos = _pos.GetList();

            Double4x4 trafo = _gl.SceneGraph.AbsoluteTransformation;
            //trafo.Invert();


            Double3 center = Double3.Zero;
            for (int i = 0; i < pos.Count; ++i)
            {
                double d;
                if (obj.IsPointInside(trafo.TransformPoint(pos[i]), out d))
                {
                    indices.Add(i);
                    center += pos[i];
                }
            }

            if (indices.Count > 0)
            {
                center /= indices.Count;

                SceneNode n = new SceneNode();
                n.RelativeTranslation = center;

                CoordinateSystemSceneNode cs = new CoordinateSystemSceneNode();
                //cs.RelativeTranslation = center;

                cs.Call<SceneNode>(m => m.Overlay = true);

                n.Add(cs);


                _gl.SceneGraph.Add(n);


                _sim.AddConstraint(new ConstrainedSection(cs, indices));
                _sim.UpdateConstraintConfiguration();
            }
        }

        private void InputHandler_MouseMove(Point p, Double2 xy, MouseKey button)
        {
            if (button == MouseKey.Left)
                _mousePos = xy;
        }

        
        private unsafe void butOpen_Click(object sender, EventArgs e)
        {
            if (dlgOpen.ShowDialog() != DialogResult.OK)
                return;

            Open3DFile(dlgOpen.FileName);
        }

        /// <summary>
        /// Loads a new Model, compute the low resolution representation, calculate the mapping from the low to the high
        /// resolution representation, initialize the simulator and start the simualtion update timer
        /// </summary>
        private void Open3DFile(string fileName)
        {
            List<Double3> posListH;
            List<Triangle> triListH;
            OFFReader.Read(fileName, out posListH, out triListH);


            Double3 min, max;
            Graphics3D.Extensions.MinMax(posListH, out min, out max);

            //Scale such that model fits into unit bounding box
            Double3 center = 0.5 * (min + max);
            Double3 size = max - min;
            double scaling = 1.0 / Math.Max(size.X, Math.Max(size.Y, size.Z));
            for (int i = 0; i < posListH.Count; ++i)
            {
                posListH[i] = scaling * (posListH[i] - center);
            }


            PairContract c = new PairContract();
            List<Double3> posList;
            List<Triangle> triList;
            c.Simplify(posListH, triListH, 1000, false, out posList, out triList);


            Channel<Double3> nor = MeshBuilder.CreateNormals(posList, triList);

            _lowResToHighResMapper = new LowResToHighResMapper(posList, nor, triList, posListH);
            _sim = new RigidSimulator(posList, triList);

            _pos = new Channel<Double3>(posList);
            _geo = new PhongPNTriangleGeometry(new Channel<Triangle>(triList),
                new NamedChannel(Channel.Position, _pos), new NamedChannel(Channel.Normal, nor));


            _posHighRes = new Channel<Double3>(posListH);
            Channel<Double3> norH = MeshBuilder.CreateNormals(posListH, triListH);
            _geoHighRes = new PhongPNTriangleGeometry(new Channel<Triangle>(triListH),
                new NamedChannel(Channel.Position, _posHighRes), new NamedChannel(Channel.Normal, norH));

            _geo.Color = new Double3(0.7, 0.5, 0);
            _geoHighRes.Color = new Double3(0.7, 0.5, 0);

            //_gl.SceneGraph.Add(geo);
            _gl.SceneGraph.Add(_geoHighRes);
            _gl.InputHandler.Fit();

            tmr.Start();

            // TODO: The result with a separate thread was not staisfactory            
            //Thread t = new Thread(UpdateSimThread);
            //t.Start();
        }


        private unsafe void tmr_Tick(object sender, EventArgs e)
        {
            PerformSimulationStep();
        }
        
        private void UpdateSimThread()
        {
            while (true)
            {
                PerformSimulationStep();
            }
        }

        private void PerformSimulationStep()
        {
            List<Double3> nor = _geo.GetChannel<Double3>(Channel.Normal).GetList();

            List<Double3> pos = _pos.GetList();
            _sim.PerformSimulationStep(pos);
            //MeshBuilder.UpdateNormals(pos, _geo.Triangles.GetList(), nor);


            List<Double3> p = _posHighRes.GetList();
            for (int i = 0; i < p.Count; ++i)
                p[i] = _lowResToHighResMapper.GetHighResPoint(i);
            MeshBuilder.UpdateNormals(p, _geoHighRes.Triangles.GetList(), _geoHighRes.GetChannel<Double3>(Channel.Normal).GetList());
        }

        private void butDelete_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < _selectionHandler.Count; ++i)
            {
                var s = _selectionHandler[i];

                if (s is CoordinateSystemSceneNode)
                {
                    s.Delete();
                    _sim.RemoveConstraint((CoordinateSystemSceneNode)s);
                }
            }
        }

        private void butReset_Click(object sender, EventArgs e)
        {
            if (_sim != null)
                _sim.Reset();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Clean up native resources
            if (_sim != null)
                _sim.Dispose();
        }
    }
}
