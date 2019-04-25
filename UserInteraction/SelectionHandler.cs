using Algebra3D;
using Graphics3D;
using Graphics3D.Geometry;
using System;
using System.Collections.Generic;

namespace RigidViewer
{
    public class SelectionHandler
    {
        protected SceneGraph sg; public SceneGraph SceneGraph { get { return sg; } }
        protected List<SelectionElement> selection = new List<SelectionElement>();
        protected bool multiSelect = true;
        public bool MultiSelect
        {
            get
            {
                return multiSelect;
            }
            set
            {
                if (value)
                {
                    if (selection.Count > 1)
                        Clear();
                }
                multiSelect = value;
            }
        }

        protected bool enabled = true;
        public bool Enabled { get { return enabled; } set { enabled = value; } }

        public int Count { get { return selection.Count; } }
        public SelectableSceneNode this[int index] { get { return selection[index].Node; } }


        private InputHandler inputHandler; public InputHandler InputHandler { get { return inputHandler; } }
        private bool patchSelectionEnabled = true; public bool PatchSelectionEnabled { get { return patchSelectionEnabled; } set { patchSelectionEnabled = value; } }
        public Func<PickingResult, bool> Validator { get; set; }

        public event Action<PickingResult, bool> Pick;


        public SelectionHandler(SceneGraph sg, InputHandler inputHandler)
        {
            this.sg = sg;
            this.inputHandler = inputHandler;
            Validator = null;
            inputHandler.MouseDown += MouseDown;
            inputHandler.KeyDown += KeyDown;
        }

        private void KeyDown(Key k)
        {
            if (k == Key.Escape)
                Clear();
        }

        public void Clear()
        {
            for (int i = 0; i < selection.Count; ++i)
            {
                SelectionElement e = selection[i];
                e.UnHighlight();
                //e.Node.Selected = false;
                //if (e.PatchID >= 0)
                //    e.Node.Delete();
            }
            selection.Clear();

            //sg.Call<SelectableSceneNode>(delegate(SelectableSceneNode n) { n.Selected = false; });
        }


        private void MouseDown(System.Drawing.Point pt, Double2 xy, MouseKey buttons)
        {
            if (buttons == MouseKey.Left && enabled)
                Select(xy);
        }

        public void Unselect(int index)
        {
            SelectionElement e = selection[index];
            e.UnHighlight();
            selection.RemoveAt(index);
        }

        public void UnselectAll()
        {
            for (int i = 0; i < selection.Count; ++i)
                selection[i].UnHighlight();
            selection.Clear();
        }

        public bool Select(Double2 xy)
        {
            PickingResult r;
            return Select(xy, out r);
        }

        public bool Select(Double2 xy, out PickingResult result)
        {
            bool unselect = inputHandler.IsKeyPressed(Key.ShiftLeft);
            List<PickingResult> r;
            result = null;
            if (sg.Pick(xy, 5, out r))
            {
                if (Validator == null)
                    result = r[0];
                else
                {
                    bool success = false;
                    for (int i = 0; i < r.Count; ++i)
                    {
                        result = r[i];
                        if (Validator(result))
                        {
                            success = true;
                            break;
                        }
                    }
                    if (!success)
                        return false;
                }
            }
            else
                return false;

            if (Pick != null)
                Pick(result, unselect);

            int geometryID = result.GeometryIndex;


            int id = result.PrimitiveIndex;
            PhongPatchPNTriangleGeometry g = result.Node.Geometries[geometryID] as PhongPatchPNTriangleGeometry;
            if (g != null && patchSelectionEnabled)
            {
                int patchID = g.GetRangeIndex(id);
                int index = GetIndex(result.Node, patchID);

                if (index < 0 && !unselect)
                {
                    if (!multiSelect)
                        Clear();

                    SelectionElement se = new SelectionElement(result.Node, geometryID, patchID);
                    se.Highlight();
                    selection.Add(se);
                }
                else if (index >= 0 && unselect)
                {
                    Unselect(index);
                }
                return true;
            }
            else
            {
                SelectableSceneNode n = result.Node as SelectableSceneNode;
                if (n != null)
                {
                    int index = GetIndex(result.Node, -1);

                    if (index < 0 && !unselect)
                    {
                        if (!multiSelect)
                            Clear();

                        SelectionElement se = new SelectionElement(result.Node, geometryID);
                        se.Highlight();
                        selection.Add(se);
                    }
                    else if (index >= 0 && unselect)
                    {
                        Unselect(index);
                    }
                    return true;
                }
            }
            return false;
            //}
            //return false;
            //else if (!multiSelect)
            //    Clear();
        }

        public bool Select(SceneNode node, int geometryID = 0)
        {
            SelectableSceneNode n = node as SelectableSceneNode;
            if (n != null)
            {
                if (!multiSelect)
                    Clear();

                //n.Selected = true;
                //selectedNodes.Add(n);

                SelectionElement se = new SelectionElement(n, geometryID);
                se.Highlight();

                selection.Add(se);
                return true;
            }
            return false;
        }

        private int GetIndex(SceneNode node, int patchID)
        {
            for (int i = 0; i < selection.Count; ++i)
            {
                SelectionElement e = selection[i];
                if (node == e.Origin && patchID == e.PatchID)
                    return i;
            }
            return -1;
        }
    }
}
