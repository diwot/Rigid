using System;
using System.Runtime.InteropServices;

namespace RigidViewer
{
    /// <summary>
    /// All entry points to the native C++ library that calculates the deformations
    /// The PInvoke technique is used since there were problems with a threading library that is used by the 
    /// C++ dll that is not compatible with C++/CLI
    /// </summary>
    public static class Interop
    {
        //Initializes the simulation and returnes a pointer to the native C++ instance of the simulator
        [DllImport(@"Rigid.dll")]
        public unsafe extern static IntPtr Initialize(double* points, int numPoints, int* triangles, int numTris,
            int* indicesOfConstraindedPoints, int numConstraints, int maxIter);
                
        //Performs a single simulation step
        [DllImport(@"Rigid.dll")]
        public unsafe extern static void Step(IntPtr handle, double* constrainedPositionValues, double* solution);

        //Releases the native C++ resources that belong to the instance referenced by a pointer
        [DllImport(@"Rigid.dll")]
        public unsafe extern static void Dispose(IntPtr handle);        
    }
}