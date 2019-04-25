using Algebra3D;
using Graphics3D;
using Graphics3D.Graph.Interactive;
using System;

namespace RigidViewer
{
    public class ShiftingHandler
    {
        private SelectionHandler selectionHandler; public SelectionHandler SelectionHandler { get { return selectionHandler; } }
        private Camera camera; public Camera Camera { get { return camera; } }
        private InputHandler inputHandler; public InputHandler InputHandler { get { return inputHandler; } }
        private bool enabled;
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                //selectionHandler.Enabled = !value;
                enabled = value;
            }
        }


        private bool xDirectionEnabled = true; public bool XDirectionEnabled { get { return xDirectionEnabled; } set { xDirectionEnabled = value; } }
        private bool yDirectionEnabled = true; public bool YDirectionEnabled { get { return yDirectionEnabled; } set { yDirectionEnabled = value; } }
        private bool zDirectionEnabled = true; public bool ZDirectionEnabled { get { return zDirectionEnabled; } set { zDirectionEnabled = value; } }

        private bool shifting = false;
        public bool Shifting { get { return shifting; } }

        //If set to true then only directions can be edited interactively
        private bool lockPositions = false;
        public bool LockPositions { get { return lockPositions; } set { lockPositions = value; } }

        private bool rotateAroundViewDirectionOnly = false;
        public bool RotateAroundViewDirectionOnly { get { return rotateAroundViewDirectionOnly; } set { rotateAroundViewDirectionOnly = value; } }

        private bool directionEditingEnabled = true; public bool DirectionEditingEnabled { get { return directionEditingEnabled; } set { directionEditingEnabled = value; } }

        public event Action PositionChanged;
        public event Action<SceneNode> NodeChanged;
        //public event Action EndShift;


        public ShiftingHandler(InputHandler inputHandler, Camera camera, SelectionHandler selector, bool enabled = false)
        {
            this.inputHandler = inputHandler;
            this.camera = camera;
            this.selectionHandler = selector;

            inputHandler.MouseDown += MouseDown;
            inputHandler.MouseMove += MouseMove;
            inputHandler.MouseUp += MouseUp;

            this.enabled = enabled;
        }

        public void EnableAllDirection()
        {
            xDirectionEnabled = true;
            yDirectionEnabled = true;
            zDirectionEnabled = true;
        }

        private Double2 lastPos;
        //private SelectableSceneNode node = null;
        private void MouseDown(System.Drawing.Point pt, Double2 xy, MouseKey buttons)
        {
            if (buttons == MouseKey.Left && enabled)
            {
                lastPos = xy;
                PickingResult r;
                shifting = selectionHandler.Select(xy, out r);
                if (r != null && r.Node is IInteractiveDirectionSceneNode)
                    shifting = false;

            }
        }

        public void Select(SceneNode n)
        {
            selectionHandler.Select(n);
            shifting = false;
        }

        private void MouseMove(System.Drawing.Point pt, Double2 xy, MouseKey buttons)
        {
            if (buttons == MouseKey.Left && enabled)
            {
                if (shifting && !lockPositions)
                {
                    double zoomNormalizedScreenHeight = camera.ZoomFactor;
                    Double2 offset = (xy - lastPos);
                    offset.X *= zoomNormalizedScreenHeight * camera.AspectRatio;
                    offset.Y *= zoomNormalizedScreenHeight;

                    Double3 o = camera.Up * offset.Y + camera.Right * offset.X;

                    for (int i = 0; i < selectionHandler.Count; ++i)
                    {
                        SelectableSceneNode node = selectionHandler[i];

                        Double4x4 m = node.AbsoluteTransformation;
                        m.ClearTranslation();
                        Double4x4 mInv = m;
                        mInv.Invert();

                        Double4x4 translation = (mInv * Double4x4.CreateTranslation(o) * m);
                        if (!xDirectionEnabled)
                            translation.M14 = 0;
                        if (!yDirectionEnabled)
                            translation.M24 = 0;
                        if (!zDirectionEnabled)
                            translation.M34 = 0;

                        node.RelativeTransformation = translation * node.RelativeTransformation;
                        OnNodeChanged(node);
                    }

                    lastPos = xy;
                }
                else if (directionEditingEnabled)
                {
                    for (int i = 0; i < selectionHandler.Count; ++i)
                    {
                        IInteractiveDirectionSceneNode n = selectionHandler[i] as IInteractiveDirectionSceneNode;
                        //object test = selectionHandler[i];
                        if (n != null)
                        {
                            if (rotateAroundViewDirectionOnly)
                            {
                                Double3 dir = selectionHandler[i].AbsoluteTransformation.Inverted().TransformDirection(camera.LookAt).Normalized();
                                n.SetEndInPlane(xy, camera, dir);
                            }
                            else
                                n.SetEnd(xy, camera);

                            OnNodeChanged(selectionHandler[i]);
                        }
                    }
                }

                if (selectionHandler.Count > 0)
                    OnPositionChanged();
            }
        }

        protected virtual void OnPositionChanged()
        {
            if (PositionChanged != null)
                PositionChanged();
        }

        protected virtual void OnNodeChanged(SceneNode n)
        {
            if (NodeChanged != null)
                NodeChanged(n);
        }

        private void MouseUp(System.Drawing.Point pt, Double2 xy, MouseKey buttons)
        {
            if (buttons == MouseKey.Left && enabled && shifting)
            {
                shifting = false;
                if (selectionHandler.Count > 0 && PositionChanged != null)
                    PositionChanged();
            }
        }
    }
}
