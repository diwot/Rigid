using Algebra3D;
using Graphics3D.Graph.Interactive;
using System;
using System.Collections.Generic;

namespace RigidViewer
{
    /// <summary>
    /// Calculates the deformations based on the transformations defined for each constrained section
    /// Each vertex that is part of a constrained section will be transformed by the corresponding transformation
    /// and is then written to a buffer containing points with fixed locations. The position of all free points is
    /// then computed by the excellent ARAP implementation of the libigl C++ library
    /// </summary>
    public class RigidSimulator : IDisposable
    {
        private List<BaseConstrainedSection> _constrainedSections = new List<BaseConstrainedSection>();

        private IntPtr _handle = IntPtr.Zero; //Pointer to the simulation instance in the native C++ library
        private double[] _points; //The points of the full simulation mesh as array
        private int[] _tris; //The indices of the full simulation mesh's triangles as array. A triple of 3 consecutive elements represents a triangle.

        private double[] _boundaryConditions; //Buffer for all positons of fixed (=constrained) vertices
        private double[] _solution; //Buffer for the positions of all free vertices
        private bool _constraintConfigChanged = false; //Flag to support lazy update of the simulation configuration


        public RigidSimulator(List<Double3> points, List<Triangle> triangles)
        {
            _points = new double[3 * points.Count];
            int indexer = 0;
            for (int i = 0; i < points.Count; ++i)
            {
                Double3 p = points[i];
                _points[indexer++] = p.X;
                _points[indexer++] = p.Y;
                _points[indexer++] = p.Z;
            }
            _tris = new int[3 * triangles.Count];
            indexer = 0;
            for (int i = 0; i < triangles.Count; ++i)
            {
                Triangle t = triangles[i];
                _tris[indexer++] = t.A;
                _tris[indexer++] = t.B;
                _tris[indexer++] = t.C;
            }
        }

        public void AddConstraint(BaseConstrainedSection constraint)
        {
            _constrainedSections.Add(constraint);
            _constraintConfigChanged = true;
        }

        public bool RemoveConstraint(CoordinateSystemSceneNode n)
        {
            //TODO: Not really an elegant solution
            for (int i = 0; i < _constrainedSections.Count; ++i)
            {
                ConstrainedSection s = _constrainedSections[i] as ConstrainedSection;
                if (s != null && s.Node == n)
                {
                    _constrainedSections.RemoveAt(i);
                    _constraintConfigChanged = true;
                    return true;
                }
            }
            return false;
        }

        public void Reset()
        {
            //TODO: Not really an elegant solution
            for (int i = 0; i < _constrainedSections.Count; ++i)
            {
                ConstrainedSection s = _constrainedSections[i] as ConstrainedSection;
                s.Node.Delete();
            }
            _constrainedSections.Clear();
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                Interop.Dispose(_handle);
                _handle = IntPtr.Zero;
            }
        }

        public unsafe IntPtr DefineConstraindedNodes(int[] nodeIndices)
        {
            Dispose();

            fixed (double* ptsPtr = &_points[0])
            {
                fixed (int* triPtr = &_tris[0])
                {
                    fixed (int* indPtr = &nodeIndices[0])
                    {
                        _handle = Interop.Initialize(ptsPtr, _points.Length / 3, triPtr, _tris.Length / 3, indPtr, nodeIndices.Length, 100);
                    }
                }
            }
            return _handle;
        }

        public void UpdateConstraintConfiguration()
        {
            int numConstraints = 0;
            for (int i = 0; i < _constrainedSections.Count; ++i)
                numConstraints += _constrainedSections[i].Count;

            int[] nodeIndices = new int[numConstraints];
            int indexer = 0;
            for (int i = 0; i < _constrainedSections.Count; ++i)
                indexer = _constrainedSections[i].WriteIndices(nodeIndices, indexer);

            DefineConstraindedNodes(nodeIndices);
            _constraintConfigChanged = false;
        }

        public unsafe void PerformSimulationStep(List<Double3> solution)
        {
            if (_constraintConfigChanged)
                UpdateConstraintConfiguration();

            int numConstraints = 0;
            for (int i = 0; i < _constrainedSections.Count; ++i)
                numConstraints += _constrainedSections[i].Count;

            if (_boundaryConditions == null || numConstraints * 3 != _boundaryConditions.Length)
                _boundaryConditions = new double[numConstraints * 3];

            int indexer = 0;
            for (int i = 0; i < _constrainedSections.Count; ++i)
                indexer = _constrainedSections[i].WritePositions(_boundaryConditions, _points, indexer);

            if (_solution == null || _solution.Length != _points.Length)
                _solution = new double[_points.Length];

            if (_boundaryConditions.Length > 0)
            {
                fixed (double* bcPtr = &_boundaryConditions[0])
                {
                    fixed (double* solPtr = &_solution[0])
                    {
                        Interop.Step(_handle, bcPtr, solPtr);
                    }
                }

                indexer = 0;
                for (int i = 0; i < solution.Count; ++i)
                {
                    solution[i] = new Double3(_solution[indexer], _solution[indexer + 1], _solution[indexer + 2]);
                    indexer += 3;
                }
            }
            else
            {
                indexer = 0;
                for (int i = 0; i < solution.Count; ++i)
                {
                    solution[i] = new Double3(_points[indexer], _points[indexer + 1], _points[indexer + 2]);
                    indexer += 3;
                }
            }
        }
    }
}
