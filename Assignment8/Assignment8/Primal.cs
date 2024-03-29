﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assignment8
{
    class Primal
    {
        public Vector[] Vertices { get; set; }
        public int[][] Verges { get; set; }

        public Vector View { get; set; }

        public Vector Center
        {
            get
            {
                Vector center = new Vector();
                foreach (var v in Vertices)
                {
                    center.X += v.X;
                    center.Y += v.Y;
                    center.Z += v.Z;
                }
                center.X /= Vertices.Length;
                center.Y /= Vertices.Length;
                center.Z /= Vertices.Length;
                return center;
            }
        }

        public Primal(Tuple<Vector[], int[][]> data)
            : this(data.Item1, data.Item2)
        {
        }

        public Primal(Vector[] vertices, int[][] verges)
        {
            Vertices = vertices;
            Verges = verges;
        }

        public Primal(string path)
        {
            var vertices = new List<Vector>();
            var verges = new List<List<int>>();
            var info = File.ReadAllLines(path);
            int index = 0;
            while (info[index].Equals("") || !info[index][0].Equals('v'))
                index++;
            while (info[index].Equals("") || info[index][0].Equals('v'))
            {
                var infoPoint = info[index].Split(' ');
                double x = double.Parse(infoPoint[1]);
                double y = double.Parse(infoPoint[2]);
                double z = double.Parse(infoPoint[3]);
                vertices.Add(new Vector(x, y, z));
                index++;
            }
            while (info[index].Equals("") || !info[index][0].Equals('f'))
                index++;
            int indexPointSeq = 0;
            while (info[index].Equals("") || info[index][0].Equals('f'))
            {
                var infoPointSeq = info[index].Split(' ');
                var listPoints = new List<int>();
                for (int i = 1; i < infoPointSeq.Length; ++i)
                {
                    int elem;
                    if (int.TryParse(infoPointSeq[i], out elem))
                        listPoints.Add(elem - 1);
                }
                verges.Add(listPoints);
                index++;
                indexPointSeq++;
            }
            Vertices = vertices.ToArray();
            Verges = verges.Select(x => x.ToArray()).ToArray();
        }

        public void Apply(Matrix transformation)
        {
            for (int i = 0; i < Vertices.Length; ++i)
                Vertices[i] *= transformation;
        }

        public virtual void Draw(Graphic graphics)
        {
            Random r = new Random(256);
            foreach (var verge in Verges)
            {
                int k = r.Next(0, 256);
                int k2 = r.Next(0, 256);
                int k3 = r.Next(0, 256);

                for (int i = 1; i < verge.Length - 1; ++i)
                {
                    var a = new Nodes(Vertices[verge[0]], new Vector(), Color.FromArgb(k2, k, 0));
                    var b = new Nodes(Vertices[verge[i]], new Vector(), Color.FromArgb(k2, k, 0));
                    var c = new Nodes(Vertices[verge[i + 1]], new Vector(), Color.FromArgb(k2, k, 0));
                    graphics.DrawTriangle(a, b, c);
                }
            }
        }

        public virtual void DrawNonFace(Graphic graphics)
        {
            foreach (var verge in Verges)
            {
                Vector p1 = Vertices[verge[0]];
                Vector p2 = Vertices[verge[1]];
                Vector p3 = Vertices[verge[2]];

                double[,] matrix = new double[2, 3];
                matrix[0, 0] = p2.X - p1.X;
                matrix[0, 1] = p2.Y - p1.Y;
                matrix[0, 2] = p2.Z - p1.Z;
                matrix[1, 0] = p3.X - p1.X;
                matrix[1, 1] = p3.Y - p1.Y;
                matrix[1, 2] = p3.Z - p1.Z;

                double ni = matrix[0, 1] * matrix[1, 2] - matrix[0, 2] * matrix[1, 1];
                double nj = matrix[0, 2] * matrix[1, 0] - matrix[0, 0] * matrix[1, 2];
                double nk = matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];
                double d = -(ni * p1.X + nj * p1.Y + nk * p1.Z);

                Vector pp = new Vector(p1.X + ni, p1.Y + nj, p1.Z + nk);
                double val1 = ni * pp.X + nj * pp.Y + nk * pp.Z + d;
                double val2 = ni * Center.X + nj * Center.Y + nk * Center.Z + d;

                if (val1 * val2 > 0)
                {
                    ni = -ni;
                    nj = -nj;
                    nk = -nk;
                }

                if (ni * (-graphics.CamPosition.X) + nj * (-graphics.CamPosition.Y) + nk * (-graphics.CamPosition.Z) + ni * p1.X + nj * p1.Y + nk * p1.Z < 0)
                {
                    graphics.DrawPoint(Vertices[verge[0]], Color.Black);
                    for (int i = 1; i < verge.Length; ++i)
                    {
                        graphics.DrawPoint(Vertices[verge[i]], Color.Black);
                        graphics.DrawLine(Vertices[verge[i - 1]], Vertices[verge[i]]);
                    }
                    graphics.DrawLine(Vertices[verge[verge.Length - 1]], Vertices[verge[0]]);
                }
            }
        }



        public void Save(string path)
        {
            string info = "# File Created: " + DateTime.Now.ToString() + "\r\n";
            foreach (var v in Vertices)
                info += "v " + v.X + " " + v.Y + " " + v.Z + "\r\n";
            info += "# " + Vertices.Length + " vertices\r\n";
            foreach (var verge in Verges)
            {
                info += "f ";
                for (int i = 0; i < verge.Length; ++i)
                    info += (verge[i] + 1) + " ";
                info += "\r\n";
            }
            info += "# " + Verges.Length + " polygons\r\n";
            File.WriteAllText(path, info);
        }
    }
    class Tetrahedron : Primal
    {
        public Tetrahedron(double size)
            : base(Construct(size))
        {
        }

        private static Tuple<Vector[], int[][]> Construct(double size)
        {
            var vertices = new Vector[4];
            var indices = new int[4][];
            double h = Math.Sqrt(2.0 / 3.0) * size;
            vertices[0] = new Vector(-size / 2, 0, h / 3);
            vertices[1] = new Vector(0, 0, -h * 2 / 3);
            vertices[2] = new Vector(size / 2, 0, h / 3);
            vertices[3] = new Vector(0, h, 0);


            indices[0] = new int[3] { 0, 1, 2 };
            indices[1] = new int[3] { 1, 3, 0 };
            indices[2] = new int[3] { 0, 3, 2 };
            indices[3] = new int[3] { 2, 3, 1 };
            return new Tuple<Vector[], int[][]>(vertices, indices);
        }
    }

    class Hexahedron : Primal
    {
        public Hexahedron(double size) : base(Construct(size)) { }

        private static Tuple<Vector[], int[][]> Construct(double size)
        {
            var vertices = new Vector[8];
            var indices = new int[6][];

            vertices[0] = new Vector(-size / 2, -size / 2, -size / 2);
            vertices[1] = new Vector(-size / 2, -size / 2, size / 2);
            vertices[2] = new Vector(-size / 2, size / 2, -size / 2);
            vertices[3] = new Vector(size / 2, -size / 2, -size / 2);
            vertices[4] = new Vector(-size / 2, size / 2, size / 2);
            vertices[5] = new Vector(size / 2, -size / 2, size / 2);
            vertices[6] = new Vector(size / 2, size / 2, -size / 2);
            vertices[7] = new Vector(size / 2, size / 2, size / 2);

            indices[0] = new int[4] { 0, 1, 5, 3 };
            indices[1] = new int[4] { 2, 6, 3, 0 };
            indices[2] = new int[4] { 4, 1, 0, 2 };
            indices[3] = new int[4] { 7, 5, 3, 6 };
            indices[4] = new int[4] { 2, 4, 7, 6 };
            indices[5] = new int[4] { 4, 1, 5, 7 };

            return new Tuple<Vector[], int[][]>(vertices, indices);
        }
    }
}

