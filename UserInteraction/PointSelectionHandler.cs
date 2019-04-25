using Algebra3D;
using Graphics3D;
using Graphics3D.Geometry;
using System;
using System.Drawing;

namespace RigidViewer
{
    public class PointSelectionHandler
    {
        public event Action<SelectionFrustum> SelectionRequested;


        private InputHandler _inputHandler;
        private SceneGraph _sceneGraph;
        private Camera _camera;

        private Double3 rectangleColor = new Double3(0.8, 0.9, 0.95);
        private ScreenQuadGeometry selectionRectangle;
        private SceneNode selectionRectangleNode;

        private bool enabled = true;
        public bool Enabled { get { return enabled; } set { enabled = value; } }

        private Point mouseDownPoint;
        private Double2 mouseDownXY;

        private MouseKey _selectButton = MouseKey.Right;


        public PointSelectionHandler(InputHandler inputHandler, SceneGraph sceneGraph, Camera camera)
        {
            _inputHandler = inputHandler;
            _sceneGraph = sceneGraph;
            _camera = camera;

            inputHandler.MouseDown += MouseDown;
            inputHandler.MouseUp += MouseUp;
            inputHandler.MouseMove += MouseMove;

            selectionRectangle = new ScreenQuadGeometry(rectangleColor, rectangleColor, rectangleColor, rectangleColor) { Depth = -0.9999f };
            selectionRectangleNode = new SceneNode(selectionRectangle, "Selection Rectangle") { Visible = false, Transparent = true };
            sceneGraph.Add(selectionRectangleNode);
        }
        
        private void MouseDown(Point p, Double2 xy, MouseKey button)
        {
            if (button == _selectButton && Enabled)
            {
                mouseDownXY = xy;
                mouseDownPoint = p;
            }
        }

        private void MouseMove(Point p, Double2 xy, MouseKey button)
        {
            if (button == _selectButton && enabled)
            {
                selectionRectangle.Origin = new Double2((Math.Min(mouseDownXY.X, xy.X) + 1) * 0.5, (Math.Min(mouseDownXY.Y, xy.Y) + 1) * 0.5);
                double sizeX = Math.Max(mouseDownXY.X, xy.X) - Math.Min(mouseDownXY.X, xy.X);
                double sizeY = Math.Max(mouseDownXY.Y, xy.Y) - Math.Min(mouseDownXY.Y, xy.Y);
                selectionRectangle.Scaling = new Double2(0.5 * sizeX, 0.5 * sizeY);

                selectionRectangleNode.Visible = true;
            }
        }

        private void MouseUp(Point p, Double2 xy, MouseKey button)
        {
            if (button == _selectButton && Enabled)
            {
                if (p.X != mouseDownPoint.X || p.Y != mouseDownPoint.Y)                
                    Select(mouseDownXY, xy);                

                selectionRectangleNode.Visible = false;
            }
        }

        public void Select(Double2 rangeStart, Double2 rangeEnd)
        {
            Double2 min = new Double2(Math.Min(rangeStart.X, rangeEnd.X), Math.Min(rangeStart.Y, rangeEnd.Y));
            Double2 max = new Double2(Math.Max(rangeStart.X, rangeEnd.X), Math.Max(rangeStart.Y, rangeEnd.Y));

            Double3 originA;
            Double3 dirA = _camera.GetRayDirection(min, out originA);

            Double3 originB;
            Double3 dirB = _camera.GetRayDirection(new Double2(min.X, max.Y), out originB);

            Double3 originC;
            Double3 dirC = _camera.GetRayDirection(max, out originC);

            Double3 originD;
            Double3 dirD = _camera.GetRayDirection(new Double2(max.X, min.Y), out originD);
            

            SelectionFrustum frustum = new SelectionFrustum(new FrustumPlane(_camera.LookAt, _camera.Position), 
                new FrustumPlane(originA, originB, originB + dirB), new FrustumPlane(originC, originD, originD + dirD),
                new FrustumPlane(originB, originC, originC + dirC), new FrustumPlane(originD, originA, originA + dirA));

            OnSelectionRequested(frustum);
        }

        protected virtual void OnSelectionRequested(SelectionFrustum frustum)
        {
            SelectionRequested?.Invoke(frustum);
        }
    }
}
