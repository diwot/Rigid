using Algebra3D;
using System.Collections.Generic;

namespace RigidViewer
{
    // Taken from https://github.com/smiley22/MeshSimplify
    public class VertexSplit
    {
        /// <summary>
        /// Der Index des Vertex, der "gespalten" wird.
        /// </summary>
        public int S;

        /// <summary>
        /// Die neue Position des Vertex nach der Aufspaltung.
        /// </summary>
        public Double3 SPosition;

        /// <summary>
        /// Die Position des zweiten Vertex der bei der Aufspaltung entsteht.
        /// </summary>
        public Double3 TPosition;

        /// <summary>
        /// Eine Untermenge der Facetten von Vertex s die Vertex t nach der Aufspaltung
        /// zugeteilt werden sollen.
        /// </summary>
        public HashSet<int[]> Faces = new HashSet<int[]>();
    }
}