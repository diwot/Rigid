using Algebra3D;

namespace RigidViewer
{
    /// <summary>
    /// Data struct that defines all quantities to calculate the position of a point in the high resolution mesh
    /// from the points in the low resolution mesh.
    /// Each high resolution point is assigned to a triangle in the low resolution mesh. The barycentric coordinates
    /// of the high-res point with respect to that triangle and an offset in normal direction are required for this
    /// calculation. The normal direction is interpolated using barycentric coordinates as known from 3D rendering.
    /// </summary>
    public struct PointMapping
    {
        public Triangle Triangle;
        public Double3 BarycentricCoordinates;
        public double Offset;

        public PointMapping(Triangle triangle, Double3 uvw, double offset)
        {
            //uvw.X = Saturate(uvw.X);
            //uvw.Y = Saturate(uvw.Y);
            //uvw.Z = Saturate(uvw.Z);
            
            Triangle = triangle;
            BarycentricCoordinates = uvw;
            Offset = offset;
        }

        //public static double Saturate(double d)
        //{
        //    if (d < 0) return 0;
        //    if (d > 1) return 1;
        //    return d;
        //}
    }
}