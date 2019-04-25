using Algebra3D;
using Graphics3D.Graph.Interactive;
using System.Collections.Generic;

namespace RigidViewer
{
    /// <summary>
    /// Adds a coordinate system that can be displayed in the 3D viewer to a constrained section
    /// TODO: This is not really the most elegant solution, it would be favorable to keep data and GUI elements more separate
    /// </summary>
    public class ConstrainedSection : BaseConstrainedSection
    {
        private CoordinateSystemSceneNode _cs; public CoordinateSystemSceneNode Node { get { return _cs; } }

        //public override Double4x4 Transformation { get { return _cs.SysTrafo; } }

        public override Double4x4 Transformation
        {
            get
            {
                return _cs.RelativeTransformation * Double4x4.CreateTranslation(_cs.Parent.RelativeTranslation) *
                    _cs.SysNode.RelativeTransformation * Double4x4.CreateTranslation(-_cs.Parent.RelativeTranslation);
            }
        }

        public ConstrainedSection(CoordinateSystemSceneNode cs, List<int> indices) : base(indices)
        {
            _cs = cs;
        }
    }
}