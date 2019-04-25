using Algebra3D;

namespace RigidViewer
{
    /// <summary>
    /// Represents a basic sphere in 3D 
    /// </summary>
    public struct Sphere
    {
        public Double3 Center;
        public double Radius;

        public Sphere(Double3 center, double radius)
        {
            Center = center;
            Radius = radius;
        }
    }
}