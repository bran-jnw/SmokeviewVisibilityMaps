using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SmokeviewVisibilityMapper
{
    public struct CombinedData
    {
        public MeshData mesh;
        public SubSlice slice;

        public CombinedData(MeshData m, SubSlice s)
        {
            this.mesh = m;
            this.slice = s;
        }
    }

    public struct Target
    {
        public string name;
        public Vector3 pos;
        public int index;

        public Target(string name, Vector3 pos, int index)
        {
            this.name = name;
            this.pos = pos;
            this.index = index;
        }
    }

    public struct XB
    {
        public double x1, x2, y1, y2, z1, z2;
        string print;

        public XB(double x1, double x2, double y1, double y2, double z1, double z2)
        {
            this.x1 = x1;
            this.x2 = x2;
            this.y1 = y1;
            this.y2 = y2;
            this.z1 = z1;
            this.z2 = z2;

            print = x1 + ", " + x2 + ", " + y1 + ", " + y2 + ", " + z1 + ", " + z2;
        }

        public string Print()
        {
            return print;
        }
    }

    public struct IIJJKK
    {
        public int i1, i2, j1, j2, k1, k2;
        string print;

        public IIJJKK(int i1, int i2, int j1, int j2, int k1, int k2)
        {
            this.i1 = i1;
            this.i2 = i2;
            this.j1 = j1;
            this.j2 = j2;
            this.k1 = k1;
            this.k2 = k2;

            print = i1 + ", " + i2 + ", " + j1 + ", " + j2 + ", " + k1 + ", " + k2;
        }

        public string Print()
        {
            return print;
        }
    }

    public struct OBST
    {
        public XB xb;
        public IIJJKK iijjkk;

        public OBST(XB xb, IIJJKK iijjkk)
        {
            this.xb = xb;
            this.iijjkk = iijjkk;
        }

        public void Print()
        {
            Console.WriteLine("OBST data, [x1, x2, y1, y2, z1, z2]: " + xb.Print() + ", [i1, i1, j1, j2, k1, k2]: " + iijjkk.Print());
        }
    }

    public struct IJK
    {
        public int i, j, k;
        string print;

        public IJK(int i, int j, int k)
        {
            this.i = i;
            this.j = j;
            this.k = k;

            print = i + ", " + j + ", " + k;
        }

        public string Print()
        {
            return print;
        }
    }
}
