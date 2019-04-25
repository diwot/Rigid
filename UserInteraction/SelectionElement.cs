using Graphics3D;
using Graphics3D.Geometry;

namespace RigidViewer
{
    public class SelectionElement
    {
        public SelectableSceneNode Node { get; private set; } //Refers to the patch that is selected, for single-patch geometries it's the same as origin
        private string originName;
        public SceneNode Origin { get; private set; } //Refers to the node with the full geometry
        public int GeometryID { get; private set; }
        public int PatchID { get; private set; }


        public SelectionElement()
            : this(null)
        { }

        public SelectionElement(SceneNode origin, int geometryID = -1, int patchID = -1)
        {
            Origin = origin;
            if (origin != null)
                originName = origin.Name;
            GeometryID = geometryID;
            PatchID = patchID;
        }

        public void Highlight()
        {
            PhongPatchPNTriangleGeometry g = Origin.Geometries[GeometryID] as PhongPatchPNTriangleGeometry;
            if (g != null && PatchID >= 0)
            {
                IDrawable r = g.ExportRange(PatchID);

                Node = new SelectableSceneNode(r) { Selected = true, Priority = 100 };
                Origin.Add(Node);
            }
            else
            {
                SelectableSceneNode n = Origin as SelectableSceneNode;
                if (n != null)
                {
                    n.Selected = true;
                    Node = n;
                }
            }
        }

        public void UnHighlight()
        {
            if (Node != null)
            {
                Node.Selected = false;
                if (PatchID >= 0)
                {
                    Node.Delete();
                    //Node = null;
                }
            }
        }
    }
}