using Algebra3D;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RigidViewer
{
    /// <summary>
    /// Defines the mapping from an entire low resolution mesh to a high resolution mesh.
    /// Each high resolution point is assigned to a triangle in the low resolution mesh.The barycentric coordinates
    /// of the high-res point with respect to that triangle and an offset in normal direction are required for this
    /// calculation. The normal direction is interpolated using barycentric coordinates as known from 3D rendering.
    /// A pattern based optimizer calculates the best mapping in to improve the visual quality and to reduce artifacts.
    /// There is still a lot of potential for improvements.
    /// </summary>
    public class LowResToHighResMapper
    {
        private IList<Double3> pointsLowRes;
        private IList<Double3> normalsLowRes;
        private PointMapping[] lowToHighResMappings;


        public LowResToHighResMapper(IList<Double3> lowResPoints, IList<Double3> lowResNormals, IList<Triangle> lowResTriangles,
            IList<Double3> highResPoints)
        {
            pointsLowRes = lowResPoints;
            normalsLowRes = lowResNormals;

            //Calculate bounding spheres for triangles as a very basic acceleration structure
            //TODO: Use boundary volume hierarchy if the overhead of creating the hierarchy can be amortized
            Sphere[] spheres = new Sphere[lowResTriangles.Count];
            for (int i = 0; i < spheres.Length; ++i)
            {
                Triangle tri = lowResTriangles[i];
                spheres[i] = CircumSphereFromTriangle(lowResPoints[tri.A], lowResPoints[tri.B], lowResPoints[tri.C]);
            }

            lowToHighResMappings = new PointMapping[highResPoints.Count];
            //for (int i = 0; i < mappings.Length; ++i)
            Parallel.For(0, lowToHighResMappings.Length, i =>
                lowToHighResMappings[i] = GetMapping(highResPoints[i], lowResPoints, lowResNormals, lowResTriangles, spheres));
        }

        private static PointMapping GetMapping(Double3 point, IList<Double3> lowResPoints,
            IList<Double3> lowResNormals, IList<Triangle> lowResTriangles, Sphere[] spheres)
        {
            //Find the closest triangle in the low resoltuion mesh
            int l = spheres.Length;
            int indexOfClosest = -1;
            double closestDist = double.MaxValue * 1e-8;
            Triangle tri;
            Double3 a, b, c;
            double uBest = 1.0 / 3.0, vBest = 1.0 / 3.0;
            for (int i = 0; i < l; ++i)
            {
                Sphere s = spheres[i];
                double d2 = (point - spheres[i].Center).LengthSquared;

                double sum = closestDist + s.Radius;
                if (sum * sum > d2) //The solution can only improve if this condition is met
                {
                    tri = lowResTriangles[i];
                    a = lowResPoints[tri.A];
                    b = lowResPoints[tri.B];
                    c = lowResPoints[tri.C];
                    double u, v, w;
                    d2 = DistancePointTriangleSquared(a, b, c, point, out u, out v, out w);
                    if (d2 < closestDist * closestDist)
                    {
                        closestDist = Math.Sqrt(d2);
                        indexOfClosest = i;
                        uBest = u;
                        vBest = v;
                    }
                }
            }

            //Compute barycentricCoordinates
            tri = lowResTriangles[indexOfClosest];
            a = lowResPoints[tri.A];
            b = lowResPoints[tri.B];
            c = lowResPoints[tri.C];
            // Optimization reduces artifacts, unfortunately cannot fully avoid them
            Double3 uvw = PatternSearch(uBest, vBest, point, lowResPoints[tri.A], lowResPoints[tri.B], lowResPoints[tri.C], lowResNormals[tri.A], lowResNormals[tri.B], lowResNormals[tri.C]);
            Double3 dir = point - (uvw.X * a + uvw.Y * b + uvw.Z * c);
            Double3 normal = uvw.X * lowResNormals[tri.A] + uvw.Y * lowResNormals[tri.B] + uvw.Z * lowResNormals[tri.C];

            double offset = dir.Length * Math.Sign(Double3.Dot(dir, normal));

            Double3 p = uvw.X * a + uvw.Y * b + uvw.Z * c;
            p = p + offset * normal;

            return new PointMapping(tri, uvw, offset); //uvw are barycentric Coordinates
        }

        public static Sphere CircumSphereFromTriangle(Double3 a, Double3 b, Double3 c, double radiusEnlargement = 1e-6)
        {
            Double3 center = 0.5 * (a + b);
            double radius = (a - b).LengthSquared;
            double distSquare = (c - center).LengthSquared;
            if (distSquare < radius)
                return new Sphere(center, Math.Sqrt(radius) + radiusEnlargement);

            center = 0.5 * (b + c);
            radius = (b - c).LengthSquared;
            distSquare = (a - center).LengthSquared;
            if (distSquare < radius)
                return new Sphere(center, Math.Sqrt(radius) + radiusEnlargement);

            center = 0.5 * (c + a);
            radius = (c - a).LengthSquared;
            distSquare = (b - center).LengthSquared;
            if (distSquare < radius)
                return new Sphere(center, Math.Sqrt(radius) + radiusEnlargement);

            double a2 = (c - b).LengthSquared;
            double b2 = (a - c).LengthSquared;
            double c2 = (b - a).LengthSquared;
            center = (a2 * (b2 + c2 - a2)) * a + (b2 * (c2 + a2 - b2)) * b + (c2 * (a2 + b2 - c2)) * c;
            return new Sphere(center, (a - center).Length + radiusEnlargement);
        }

        //http://www.geometrictools.com/GTEngine/Include/Mathematics/GteDistPointTriangleExact.h
        public static double DistancePointTriangleSquared(Double3 triA, Double3 triB, Double3 triC, Double3 point, out double u, out double v, out double w)
        {
            Double3 diff = point - triA;
            Double3 edge0 = triB - triA;
            Double3 edge1 = triC - triA;
            double a00 = Double3.Dot(edge0, edge0);
            double a01 = Double3.Dot(edge0, edge1);
            double a11 = Double3.Dot(edge1, edge1);
            double b0 = -Double3.Dot(diff, edge0);
            double b1 = -Double3.Dot(diff, edge1);
            double det = a00 * a11 - a01 * a01;
            double t0 = a01 * b1 - a11 * b0;
            double t1 = a01 * b0 - a00 * b1;

            if (t0 + t1 <= det)
            {
                if (t0 < 0)
                {
                    if (t1 < 0)  // region 4
                    {
                        if (b0 < 0)
                        {
                            t1 = 0;
                            if (-b0 >= a00)  // V0
                            {
                                t0 = 1;
                            }
                            else  // E01
                            {
                                t0 = -b0 / a00;
                            }
                        }
                        else
                        {
                            t0 = 0;
                            if (b1 >= 0)  // V0
                            {
                                t1 = 0;
                            }
                            else if (-b1 >= a11)  // V2
                            {
                                t1 = 1;
                            }
                            else  // E20
                            {
                                t1 = -b1 / a11;
                            }
                        }
                    }
                    else  // region 3
                    {
                        t0 = 0;
                        if (b1 >= 0)  // V0
                        {
                            t1 = 0;
                        }
                        else if (-b1 >= a11)  // V2
                        {
                            t1 = 1;
                        }
                        else  // E20
                        {
                            t1 = -b1 / a11;
                        }
                    }
                }
                else if (t1 < 0)  // region 5
                {
                    t1 = 0;
                    if (b0 >= 0)  // V0
                    {
                        t0 = 0;
                    }
                    else if (-b0 >= a00)  // V1
                    {
                        t0 = 1;
                    }
                    else  // E01
                    {
                        t0 = -b0 / a00;
                    }
                }
                else  // region 0, interior
                {
                    double invDet = 1 / det;
                    t0 *= invDet;
                    t1 *= invDet;
                }
            }
            else
            {
                double tmp0, tmp1, numer, denom;

                if (t0 < 0)  // region 2
                {
                    tmp0 = a01 + b0;
                    tmp1 = a11 + b1;
                    if (tmp1 > tmp0)
                    {
                        numer = tmp1 - tmp0;
                        denom = a00 - ((double)2) * a01 + a11;
                        if (numer >= denom)  // V1
                        {
                            t0 = 1;
                            t1 = 0;
                        }
                        else  // E12
                        {
                            t0 = numer / denom;
                            t1 = 1 - t0;
                        }
                    }
                    else
                    {
                        t0 = 0;
                        if (tmp1 <= 0)  // V2
                        {
                            t1 = 1;
                        }
                        else if (b1 >= 0)  // V0
                        {
                            t1 = 0;
                        }
                        else  // E20
                        {
                            t1 = -b1 / a11;
                        }
                    }
                }
                else if (t1 < 0)  // region 6
                {
                    tmp0 = a01 + b1;
                    tmp1 = a00 + b0;
                    if (tmp1 > tmp0)
                    {
                        numer = tmp1 - tmp0;
                        denom = a00 - ((double)2) * a01 + a11;
                        if (numer >= denom)  // V2
                        {
                            t1 = 1;
                            t0 = 0;
                        }
                        else  // E12
                        {
                            t1 = numer / denom;
                            t0 = 1 - t1;
                        }
                    }
                    else
                    {
                        t1 = 0;
                        if (tmp1 <= 0)  // V1
                        {
                            t0 = 1;
                        }
                        else if (b0 >= 0)  // V0
                        {
                            t0 = 0;
                        }
                        else  // E01
                        {
                            t0 = -b0 / a00;
                        }
                    }
                }
                else  // region 1
                {
                    numer = a11 + b1 - a01 - b0;
                    if (numer <= 0)  // V2
                    {
                        t0 = 0;
                        t1 = 1;
                    }
                    else
                    {
                        denom = a00 - ((double)2) * a01 + a11;
                        if (numer >= denom)  // V1
                        {
                            t0 = 1;
                            t1 = 0;
                        }
                        else  // 12
                        {
                            t0 = numer / denom;
                            t1 = 1 - t0;
                        }
                    }
                }
            }

            u = 1 - t0 - t1;
            v = t0;
            w = t1;


            double dx = diff.X - (t0 * edge0.X + t1 * edge1.X);
            double dy = diff.Y - (t0 * edge0.Y + t1 * edge1.Y);
            double dz = diff.Z - (t0 * edge0.Z + t1 * edge1.Z);
            return dx * dx + dy * dy + dz * dz;
        }

        // Simple, reliable optimizer in 2 dimensions
        //https://en.wikipedia.org/wiki/Pattern_search_(optimization)
        public static Double3 PatternSearch(double uInit, double vInit, Double3 o, Double3 a, Double3 b, Double3 c, Double3 nA, Double3 nB, Double3 nC, double minStep = 1e-6, int maxIter = 100)
        {
            double u = uInit;
            double v = vInit;

            double step = 0.025;
            double error = ErrorMeasure(o, u, v, a, b, c, nA, nB, nC);
            int counter = 0;

            while (true)
            {
                ++counter;

                double uNext = u;
                double vNext = v;

                double left = ErrorMeasure(o, u - step, v, a, b, c, nA, nB, nC);
                if (left < error)
                {
                    uNext = u - step;
                    error = left;
                }
                double right = ErrorMeasure(o, u + step, v, a, b, c, nA, nB, nC);
                if (right < error)
                {
                    uNext = u + step;
                    error = right;
                }
                double up = ErrorMeasure(o, u, v - step, a, b, c, nA, nB, nC);
                if (up < error)
                {
                    vNext = v - step;
                    error = up;
                }
                double down = ErrorMeasure(o, u, v + step, a, b, c, nA, nB, nC);
                if (down < error)
                {
                    vNext = v + step;
                    error = down;
                }

                if (uNext == u && vNext == v)
                {
                    step = step * 0.5;
                    if (step < minStep)
                        return new Double3(u, v, 1 - u - v);
                }
                else
                {
                    u = uNext;
                    v = vNext;
                }
                if (counter >= maxIter)
                {
                    return new Double3(u, v, 1 - u - v);
                }
            }
        }

        // Defines the error metric used in the optimization process of the mappings
        // The goal is that the direction from base point on a triangle in of the low resoltuion mesh to the high resolution point is identical
        // to the direction of the interpolated normal at this location
        // o is the point in the high resolution mesh
        // a, b, c are the quantities at the 3 corners of a triangle (simple names since the corners are always labelled a, b, c)
        public static double ErrorMeasure(Double3 o, double u, double v, Double3 a, Double3 b, Double3 c, Double3 nA, Double3 nB, Double3 nC)
        {
            double w = 1 - u - v;
            Double3 p = u * a + v * b + w * c; //Base point on low resolution mesh
            Double3 n = u * nA + v * nB + w * nC; //Interpolated normal
            n.Normalize();
            // Don't normalize p-o since the further a point is away from it's base triangle, 
            // the more important it gets that the direction matches the one of the normal
            return Double3.Cross(n, p - o).LengthSquared; // The magnitude of the cross product becomes zero if the vectors point in the same direction
        }

        public Double3 GetHighResPoint(int index)
        {
            return GetHighResPoint(lowToHighResMappings[index]);
        }
        public Double3 GetHighResPoint(PointMapping m)
        {
            Double3 a = pointsLowRes[m.Triangle.A];
            Double3 b = pointsLowRes[m.Triangle.B];
            Double3 c = pointsLowRes[m.Triangle.C];

            Double3 p = m.BarycentricCoordinates.X * a + m.BarycentricCoordinates.Y * b + m.BarycentricCoordinates.Z * c;


            Double3 nA = normalsLowRes[m.Triangle.A];
            Double3 nB = normalsLowRes[m.Triangle.B];
            Double3 nC = normalsLowRes[m.Triangle.C];
            Double3 n = m.BarycentricCoordinates.X * nA + m.BarycentricCoordinates.Y * nB + m.BarycentricCoordinates.Z * nC;
            //Double3 triangleNormal = Double3.Cross(b - a, c - a).Normalized();
            n.Normalize();

            return p + m.Offset * n;
        }
    }
}
