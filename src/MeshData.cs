using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SmokeviewVisibilityMaps
{
    public class MeshData
    {
        public int index;
        public string name;
        public IJK ijk;
        public XB PDIM;
        public OBST[]? OBSTs;
        public double cellSizeX, cellSizeY, cellSizeZ;

        public MeshData(int index, string name, IJK ijk, XB PDIM, OBST[]? obstacles)
        {
            this.index = index;
            this.name = name;
            this.ijk = ijk;
            this.PDIM = PDIM;
            OBSTs = obstacles;

            cellSizeX = (PDIM.x2 - PDIM.x1) / ijk.i;
            cellSizeY = (PDIM.y2 - PDIM.y1) / ijk.j;
            cellSizeZ = (PDIM.z2 - PDIM.z1) / ijk.k;
        }      

        public bool ContainsPoint(Vector2 pos)
        {
            bool result = false;
            if (pos.X >= PDIM.x1 && pos.X < PDIM.x2 && pos.Y >= PDIM.y1 && pos.Y < PDIM.y2)
            {
                result = true;
            }

            return result;
        }
    }
}
