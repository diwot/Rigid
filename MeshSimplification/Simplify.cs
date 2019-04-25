using Algebra3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RigidViewer
{
    // Taken from https://github.com/smiley22/MeshSimplify
    /// <summary>
    /// Implementiert den 'Pair-Contract' Algorithmus nach Garland's
    ///		"Surface Simplification Using Quadric Error Metrics"
    /// Methode.
    /// </summary>	
    public class PairContract
    {
        /// <summary>
        /// Die Q-Matrizen der Vertices.
        /// </summary>
        Dictionary<int, Double4x4> Q;

        /// <summary>
        /// Die Vertices der Mesh. 
        /// </summary>
        SortedDictionary<int, Double3> vertices = new SortedDictionary<int, Double3>();

        /// <summary>
        /// Die Facetten der Mesh.
        /// </summary>
        List<int[]> faces;

        /// <summary>
        /// Die Vertexpaare welche nach und nach kontraktiert werden.
        /// </summary>
        SortedSet<Pair> pairs = new SortedSet<Pair>();

        /// <summary>
        /// Der distanzbasierte Schwellwert zur Bestimmung von gültigen Vertexpaaren.
        /// </summary>
        readonly double distanceThreshold;

        /// <summary>
        /// Speichert für jeden Vertex Referenzen auf seine inzidenten Facetten.
        /// </summary>
        Dictionary<int, ISet<int[]>> incidentFaces = new Dictionary<int, ISet<int[]>>();

        /// <summary>
        /// Speichert für jeden Vertex Referenzen auf die Vertexpaare zu denen er gehört.
        /// </summary>
        Dictionary<int, ISet<Pair>> pairsPerVertex = new Dictionary<int, ISet<Pair>>();

        /// <summary>
        /// Stack der ursprünglichen Indices der Vertices, die kontraktiert wurden.
        /// </summary>
        Stack<int> contractionIndices = new Stack<int>();

        /// <summary>
        /// Speichert VertexSplit Einträge zur Erstellung einer Progressive Mesh.
        /// </summary>
        Stack<VertexSplit> splits = new Stack<VertexSplit>();

        /// <summary>
        /// Gibt an, ob <c>VertexSplit</c> Einträge erzeugt werden sollen.
        /// </summary>
        bool createSplitRecords;

        /// <summary>
        /// Initialisiert eine neue Instanz der PairContract-Klasse.
        /// </summary>
        /// <param name="opts">
        /// Die Argumente, die dem Algorithmus zur Verfügung gestellt werden.
        /// </param>
        public PairContract(double distanceThreshold = 0)
        {
            this.distanceThreshold = distanceThreshold;
        }

        /// <summary>
        /// Vereinfacht die angegebene Eingabemesh.
        /// </summary>
        /// <param name="input">
        /// Die zu vereinfachende Mesh.
        /// </param>
        /// <param name="targetFaceCount">
        /// Die Anzahl der Facetten, die die erzeugte Vereinfachung anstreben soll.
        /// </param>
        /// <param name="createSplitRecords">
        /// true, um <c>VertexSplit</c> Einträge für die Mesh zu erzeugen; andernfalls false.
        /// </param>
        /// <param name="verbose">
        /// true, um diagnostische Ausgaben während der Vereinfachung zu erzeugen.
        /// </param>
        /// <returns>
        /// Die erzeugte vereinfachte Mesh.
        /// </returns>
        public void Simplify(IList<Double3> points, IList<Triangle> triangles, int targetFaceCount, bool createSplitRecords,
            out List<Double3> reducedPoints, out List<Triangle> reducedTriangles, bool strict = true)
        {
            this.createSplitRecords = createSplitRecords;
            // Wir starten mit den Vertices und Faces der Ausgangsmesh.
            faces = new List<int[]>(triangles.Count);
            for (int i = 0; i < triangles.Count; ++i)
            {
                Triangle tri = triangles[i];
                faces.Add(new int[] { tri.A, tri.B, tri.C });
            }

            for (int i = 0; i < points.Count; i++)
                vertices[i] = points[i];
            // 1. Die initialen Q Matrizen für jeden Vertex v berechnen.
            Q = ComputeInitialQForEachVertex(strict);
            // 2. All gültigen Vertexpaare bestimmen.
            pairs = ComputeValidPairs();
            // 3. Iterativ das Paar mit den geringsten Kosten kontraktieren und entsprechend
            //    die Kosten aller davon betroffenen Paare aktualisieren.
            while (faces.Count > targetFaceCount)
            {
                //		var pair = FindLeastCostPair();
                var pair = pairs.First();
                ContractPair(pair);
            }
            // 4. Neue Mesh Instanz erzeugen und zurückliefern.
            BuildMesh(out reducedPoints, out reducedTriangles);
        }

        /// <summary>
        /// Berechnet die Kp-Matrix für die Ebenen aller Facetten.
        /// </summary>
        /// <param name="strict">
        /// true, um eine <c>InvalidOperationException</c> zu werfen, falls ein
        /// degeneriertes Face entdeckt wird.
        /// </param>
        /// <returns>
        /// Eine Liste von Kp-Matrizen.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Es wurde ein degeneriertes Face gefunden, dessen Vertices kollinear sind.
        /// </exception>
        IList<Double4x4> ComputeKpForEachPlane(bool strict)
        {
            var kp = new List<Double4x4>();
            var degenerate = new List<int[]>();
            foreach (var f in faces)
            {
                var points = new[] {
                    vertices[f[0]],
                    vertices[f[1]],
                    vertices[f[2]]
                };
                // Ebene aus den 3 Ortsvektoren konstruieren.
                var dir1 = points[1] - points[0];
                var dir2 = points[2] - points[0];
                var n = Double3.Cross(dir1, dir2);
                // Wenn das Kreuzprodukt der Nullvektor ist, sind die Richtungsvektoren
                // kollinear, d.h. die Vertices liegen auf einer Geraden und die Facette
                // ist degeneriert.
                if (n == Double3.Zero)
                {
                    degenerate.Add(f);
                    if (strict)
                    {
                        var msg = new StringBuilder()
                            .AppendFormat("Encountered degenerate face ({0} {1} {2})",
                                f[0], f[1], f[2])
                            .AppendLine()
                            .AppendFormat("Vertex 1: {0}\n", points[0])
                            .AppendFormat("Vertex 2: {0}\n", points[1])
                            .AppendFormat("Vertex 3: {0}\n", points[2])
                            .ToString();
                        throw new InvalidOperationException(msg);
                    }
                }
                else
                {
                    n.Normalize();
                    var a = n.X;
                    var b = n.Y;
                    var c = n.Z;
                    var d = -Double3.Dot(n, points[0]);
                    // Siehe [Gar97], Abschnitt 5 ("Deriving Error Quadrics").
                    var m = new Double4x4()
                    {
                        M11 = a * a,
                        M12 = a * b,
                        M13 = a * c,
                        M14 = a * d,
                        M21 = a * b,
                        M22 = b * b,
                        M23 = b * c,
                        M24 = b * d,
                        M31 = a * c,
                        M32 = b * c,
                        M33 = c * c,
                        M34 = c * d,
                        M41 = a * d,
                        M42 = b * d,
                        M43 = c * d,
                        M44 = d * d
                    };
                    kp.Add(m);
                }
            }
            if (degenerate.Count > 0)
                System.Diagnostics.Debug.WriteLine("Warning: {0} degenerate faces found.", degenerate.Count);
            foreach (var d in degenerate)
                faces.Remove(d);
            return kp;
        }

        /// <summary>
        /// Berechnet die initialen Q-Matrizen für alle Vertices.
        /// </summary>
        /// <param name="strict">
        /// true, um eine <c>InvalidOperationException</c> zu werfen, falls ein
        /// degeneriertes Face entdeckt wird.
        /// </param>
        /// <returns>
        /// Eine Map der initialen Q-Matrizen.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Es wurde ein degeneriertes Face gefunden, dessen Vertices kollinear sind.
        /// </exception>
        Dictionary<int, Double4x4> ComputeInitialQForEachVertex(bool strict)
        {
            var q = new Dictionary<int, Double4x4>();
            // Kp Matrix für jede Ebene, d.h. jede Facette bestimmen.
            var kp = ComputeKpForEachPlane(strict);
            // Q Matrix für jeden Vertex mit 0 initialisieren.
            for (int i = 0; i < vertices.Count; i++)
                q[i] = new Double4x4();
            // Q ist die Summe aller Kp Matrizen der inzidenten Facetten jedes Vertex.
            for (int c = 0; c < faces.Count; c++)
            {
                var f = faces[c];
                for (int i = 0; i < 3; i++)
                {
                    q[f[i]] = q[f[i]] + kp[c];
                }
            }
            return q;
        }

        /// <summary>
        /// Berechnet alle gültigen Vertexpaare.
        /// </summary>
        /// <returns>
        /// Die Menge aller gültigen Vertexpaare.
        /// </returns>
        SortedSet<Pair> ComputeValidPairs()
        {
            // 1. Kriterium: 2 Vertices sind ein Kontraktionspaar, wenn sie durch eine Kante
            //               miteinander verbunden sind.
            for (int i = 0; i < faces.Count; i++)
            {
                var f = faces[i];
                // Vertices eines Dreiecks sind jeweils durch Kanten miteinander verbunden
                // und daher gültige Paare.
                var s = f.OrderBy(val => val).ToArray();
                for (int c = 0; c < s.Length; c++)
                {
                    if (!pairsPerVertex.ContainsKey(s[c]))
                        pairsPerVertex[s[c]] = new HashSet<Pair>();
                    if (!incidentFaces.ContainsKey(s[c]))
                        incidentFaces[s[c]] = new HashSet<int[]>();
                    incidentFaces[s[c]].Add(f);
                }

                var p = ComputeMinimumCostPair(s[0], s[1]);
                pairs.Add(p);
                pairsPerVertex[s[0]].Add(p);
                pairsPerVertex[s[1]].Add(p);

                p = ComputeMinimumCostPair(s[0], s[2]);
                pairs.Add(p);
                pairsPerVertex[s[0]].Add(p);
                pairsPerVertex[s[2]].Add(p);

                p = ComputeMinimumCostPair(s[1], s[2]);
                pairs.Add(p);
                pairsPerVertex[s[1]].Add(p);
                pairsPerVertex[s[2]].Add(p);

            }
            // 2. Kriterium: 2 Vertices sind ein Kontraktionspaar, wenn die euklidische
            //               Distanz < Threshold-Parameter t.
            if (distanceThreshold > 0)
            {
                // Nur prüfen, wenn überhaupt ein Threshold angegeben wurde.
                for (int i = 0; i < vertices.Count; i++)
                {
                    for (int c = i + 1; c < vertices.Count; c++)
                    {
                        if ((vertices[i] - vertices[c]).Length < distanceThreshold)
                        {
                            pairs.Add(ComputeMinimumCostPair(i, c));
                        }
                    }
                }
            }
            return pairs;
        }

        /// <summary>
        /// Bestimmt die Kosten für die Kontraktion der angegebenen Vertices.
        /// </summary>
        /// <param name="s">
        /// Der erste Vertex des Paares.
        /// </param>
        /// <param name="t">
        /// Der zweite Vertex des Paares.
        /// </param>
        /// <returns>
        /// Eine Instanz der Pair-Klasse.
        /// </returns>
        Pair ComputeMinimumCostPair(int s, int t)
        {
            Double3 target;
            double cost;
            var q = Q[s] + Q[t];
            // Siehe [Gar97], Abschnitt 4 ("Approximating Error With Quadrics").
            var m = new Double4x4()
            {
                M11 = q.M11,
                M12 = q.M12,
                M13 = q.M13,
                M14 = q.M14,
                M21 = q.M12,
                M22 = q.M22,
                M23 = q.M23,
                M24 = q.M24,
                M31 = q.M13,
                M32 = q.M23,
                M33 = q.M33,
                M34 = q.M34,
                M41 = 0,
                M42 = 0,
                M43 = 0,
                M44 = 1
            };
            // Wenn m invertierbar ist, lässt sich die optimale Position bestimmen.
            //			if (m.Determinant != 0) {
            if (Math.Abs(m.Determinant) > 1e-10)
            {
                // Determinante ist ungleich 0 für invertierbare Matrizen.
                var inv = m.Inverted(); // Matrix4d.Invert(m);
                target = new Double3(inv.M14, inv.M24, inv.M34);
                cost = ComputeVertexError(target, q);
            }
            else
            {
                //			} else {
                // Ansonsten den besten Wert aus Position von Vertex 1, Vertex 2 und
                // Mittelpunkt wählen.
                var v1 = vertices[s];
                var v2 = vertices[t];
                var mp = new Double3()
                {
                    X = (v1.X + v2.X) / 2,
                    Y = (v1.Y + v2.Y) / 2,
                    Z = (v1.Z + v2.Z) / 2
                };
                var candidates = new[] {
                    new { cost = ComputeVertexError(v1, q), target = v1 },
                    new { cost = ComputeVertexError(v2, q), target = v2 },
                    new { cost = ComputeVertexError(mp, q), target = mp }
                };
                var best = (from p in candidates
                            orderby p.cost
                            select p).First();
                target = best.target;
                cost = best.cost;
            }
            return new Pair(s, t, target, cost);
        }

        /// <summary>
        /// Bestimmt den geometrischen Fehler des angegebenen Vertex in Bezug auf die
        /// Fehlerquadrik Q.
        /// </summary>
        /// <param name="v">
        /// Der Vertex, dessen geometrischer Fehler bestimmt werden soll.
        /// </param>
        /// <param name="q">
        /// Die zugrundeliegende Fehlerquadrik.
        /// </param>
        /// <returns>
        /// Der geometrische Fehler an der Stelle des angegebenen Vertex.
        /// </returns>
        double ComputeVertexError(Double3 v, Double4x4 q)
        {
            var h = new Double4(v, 1);
            //// Geometrischer Fehler Δ(v) = vᵀQv.
            return Double4.Dot(q * h, h);
        }

        /// <summary>
        /// Findet aus der angegebenen Menge das Paar mit den kleinsten Kosten.
        /// </summary>
        /// <param name="pairs">
        /// Die Menge der Vertexpaare, aus der das Paar mit den kleinsten Kosten
        /// gefunden werden soll.
        /// </param>
        /// <returns>
        /// Das Paar mit den kleinsten Kosten.
        /// </returns>
        Pair FindLeastCostPair(ISet<Pair> pairs)
        {
            double cost = double.MaxValue;
            Pair best = null;
            foreach (var p in pairs)
            {
                if (p.Cost < cost)
                {
                    cost = p.Cost;
                    best = p;
                }
            }
            return best;
        }

        /// <summary>
        /// Kontraktiert das angegebene Vertexpaar.
        /// </summary>
        /// <param name="p">
        /// Das Vertexpaar, das kontraktiert werden soll.
        /// </param>
        void ContractPair(Pair p)
        {
            if (createSplitRecords)
                AddSplitRecord(p);
            // 1. Koordinaten von Vertex 1 werden auf neue Koordinaten abgeändert.
            vertices[p.Vertex1] = p.Target;
            // 2. Matrix Q von Vertex 1 anpassen.
            Q[p.Vertex1] = Q[p.Vertex1] + Q[p.Vertex2];
            // 3. Alle Referenzen von Facetten auf Vertex 2 auf Vertex 1 umbiegen.
            var facesOfVertex2 = incidentFaces[p.Vertex2];
            var facesOfVertex1 = incidentFaces[p.Vertex1];
            var degeneratedFaces = new HashSet<int[]>();

            // Jede Facette von Vertex 2 wird entweder Vertex 1 hinzugefügt, oder
            // degeneriert.
            foreach (var f in facesOfVertex2)
            {
                if (facesOfVertex1.Contains(f))
                {
                    degeneratedFaces.Add(f);
                }
                else
                {
                    // Indices umbiegen und zu faces von Vertex 1 hinzufügen.
                    for (int i = 0; i < 3; i++)
                    {
                        if (f[i] == p.Vertex2)
                            f[i] = p.Vertex1;
                    }
                    facesOfVertex1.Add(f);
                }
            }
            // Nun degenerierte Facetten entfernen.
            foreach (var f in degeneratedFaces)
            {
                for (int i = 0; i < 3; i++)
                    incidentFaces[f[i]].Remove(f);
                faces.FastRemove(f);
            }

            // Vertex 2 aus Map löschen.
            vertices.Remove(p.Vertex2);

            // Alle Vertexpaare zu denen Vertex 2 gehört dem Set von Vertex 1 hinzufügen.
            pairsPerVertex[p.Vertex1].UnionWith(pairsPerVertex[p.Vertex2]);
            // Anschließend alle Paare von Vertex 1 ggf. umbiegen und aktualisieren.
            var remove = new List<Pair>();
            foreach (var pair in pairsPerVertex[p.Vertex1])
            {
                // Aus Collection temporär entfernen, da nach Neuberechnung der Kosten
                // neu einsortiert werden muss.
                pairs.Remove(pair);
                int s = pair.Vertex1, t = pair.Vertex2;
                if (s == p.Vertex2)
                    s = p.Vertex1;
                if (t == p.Vertex2)
                    t = p.Vertex1;
                if (s == t)
                {
                    remove.Add(pair);
                }
                else
                {
                    var np = ComputeMinimumCostPair(Math.Min(s, t), Math.Max(s, t));
                    pair.Vertex1 = np.Vertex1;
                    pair.Vertex2 = np.Vertex2;
                    pair.Target = np.Target;
                    pair.Cost = np.Cost;

                    pairs.Add(pair);
                }
            }
            // "Degenerierte" Paare entfernen.
            foreach (var r in remove)
            {
                pairsPerVertex[p.Vertex1].Remove(r);
            }
        }

        /// <summary>
        /// Erzeugt und speichert einen VertexSplit Eintrag für das angegebene Paar.
        /// </summary>
        /// <param name="p">
        /// Das Paar für welches ein VertexSplit Eintrag erstellt werden soll.
        /// </param>
        void AddSplitRecord(Pair p)
        {
            contractionIndices.Push(p.Vertex2);
            var split = new VertexSplit()
            {
                S = p.Vertex1,
                SPosition = vertices[p.Vertex1],
                TPosition = vertices[p.Vertex2]
            };
            foreach (var f in incidentFaces[p.Vertex2])
            {
                // -1 wird später durch eigentlichen Index ersetzt, wenn dieser bekannt
                // ist.
                split.Faces.Add(new int[] {
                    f[0] == p.Vertex2 ? -1 : f[0],
                    f[1] == p.Vertex2 ? -1 : f[1],
                    f[2] == p.Vertex2 ? -1 : f[2]});
            }
            splits.Push(split);
        }

        /// <summary>
        /// Erzeugt eine neue Mesh Instanz den aktuellen Vertex- und Facettendaten.
        /// </summary>
        /// <returns>
        /// Die erzeugte Mesh Instanz.
        /// </returns>
        void BuildMesh(out List<Double3> points, out List<Triangle> triangles)
        {
            // Mapping von alten auf neue Vertexindices für Facetten erstellen.
            var mapping = new Dictionary<int, int>();
            int index = 0;
            points = new List<Double3>();
            triangles = new List<Triangle>();
            foreach (var p in vertices)
            {
                mapping.Add(p.Key, index++);
                points.Add(p.Value);
            }
            foreach (var f in faces)
            {
                triangles.Add(new Triangle(
                    mapping[f[0]],
                    mapping[f[1]],
                    mapping[f[2]]
                ));
            }
            // Progressive Mesh: Indices im Mapping für "zukünftige" Vertices anlegen.
            foreach (var c in contractionIndices)
                mapping.Add(c, index++);
            int n = points.Count;
            foreach (var s in splits)
            {
                var t = n++;
                s.S = mapping[s.S];
                foreach (var f in s.Faces)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        f[i] = f[i] < 0 ? t : mapping[f[i]];
                    }
                }
            }
            //return new Mesh(points, triangles, splits);
        }
    }
}
