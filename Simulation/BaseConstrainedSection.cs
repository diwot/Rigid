using Algebra3D;
using System.Collections.Generic;

namespace RigidViewer
{    
    /// <summary>
    /// Represents a group of vertices of a mesh and the transformation that will be applied to those vertices.
    /// The vertices are store indirectly using a list containing their indices in the mesh's vertex list.
    /// </summary>
    public abstract class BaseConstrainedSection
    {
        private List<int> _indices;
        public abstract Double4x4 Transformation { get; }

        public int Count { get { return _indices.Count; } }


        protected BaseConstrainedSection(List<int> indices)
        {
            _indices = indices;
        }

        public int WriteIndices(int[] buffer, int indexer)
        {
            for (int i = 0; i < _indices.Count; ++i)
                buffer[indexer++] = _indices[i];
            return indexer;
        }

        public int WritePositions(double[] buffer, double[] undeformedPositions, int indexer)
        {
            for (int i = 0; i < _indices.Count; ++i)
            {
                int j = 3 * _indices[i];
                Double3 p = Transformation.TransformPoint(new Double3(undeformedPositions[j], undeformedPositions[j + 1], undeformedPositions[j + 2]));
                buffer[indexer++] = p.X;
                buffer[indexer++] = p.Y;
                buffer[indexer++] = p.Z;
                //buffer[indexer++] = undeformedPositions[j];
                //buffer[indexer++] = undeformedPositions[j + 1];
                //buffer[indexer++] = undeformedPositions[j + 2];
            }
            return indexer;
        }
    }
}