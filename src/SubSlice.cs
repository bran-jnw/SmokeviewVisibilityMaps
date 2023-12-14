using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SmokeviewVisibilityMaps
{
    public class SubSlice
    {
        public IIJJKK bounds;
        public string? file;
        public List<float[,]> data;
        public List<float> timeStamps;
        public MeshData meshData;
        private Slice parentSlice;

        public SubSlice(int meshIndex, IIJJKK bounds, string file, Slice parentSlice)
        {
            meshData = MeshCollection.Instance().GetMesh(meshIndex);
            this.parentSlice = parentSlice;
            this.bounds = bounds;
            this.file = file;
            data = ReadSF.ReadData(file, out timeStamps);
            CorrectDataForOBSTs();
        }

        //used when building combined slice/mesh
        public SubSlice(MeshData meshData, IIJJKK bounds, List<float[,]> data, List<float> timeStamps)
        {
            this.meshData = meshData;
            this.bounds = bounds;
            this.data = data;
            this.timeStamps = timeStamps;
        }

        public float GetDataValue(Vector2 pos, int timeSample)
        {
            float result = -9999.0f;

            if (data == null)
            {
                return result;
            }

            IJK ijk = meshData.ijk;
            XB PDIM = meshData.PDIM;

            int xIndex = (int)(ijk.i * (pos.X - PDIM.x1) / (PDIM.x2 - PDIM.x1));
            int yIndex = (int)(ijk.j * (pos.Y - PDIM.y1) / (PDIM.y2 - PDIM.y1));
            result = data[timeSample][xIndex, yIndex];

            return result;
        }

        public float GetDataValue(Vector2 pos, int timeSample, MeshData mesh)
        {
            float result = -9999.0f;

            if (data == null)
            {
                return result;
            }

            IJK ijk = mesh.ijk;
            XB PDIM = mesh.PDIM;

            int xIndex = (int)(ijk.i * (pos.X - PDIM.x1) / (PDIM.x2 - PDIM.x1));
            int yIndex = (int)(ijk.j * (pos.Y - PDIM.y1) / (PDIM.y2 - PDIM.y1));
            result = data[timeSample][xIndex, yIndex];

            return result;
        }

        private void CorrectDataForOBSTs()
        {
            //this happens for the combined mesh, but then the values are already correctly set as NO_DATA = -9999.0f
            if (meshData.OBSTs == null || data == null)
            {
                return;
            }

            for (int i = 0; i < meshData.OBSTs.Length; i++)
            {
                int y1 = meshData.OBSTs[i].iijjkk.j1;
                int y2 = meshData.OBSTs[i].iijjkk.j2;

                for (int y = y1; y < y2; y++)
                {
                    int x1 = meshData.OBSTs[i].iijjkk.i1;
                    int x2 = meshData.OBSTs[i].iijjkk.i2;

                    for (int x = x1; x < x2; x++)
                    {
                        if (bounds.k1 >= meshData.OBSTs[i].iijjkk.k1 && bounds.k2 <= meshData.OBSTs[i].iijjkk.k2)
                        {
                            for (int j = 0; j < data.Count; j++)
                            {
                                data[j][x, y] = -9999.0f;
                            }
                        }
                    }
                }
            }
        }

        public void OverwriteDataWithVisMap(Target target, List<float[,]> visMapData, MeshData combinedMesh, Slice slice)
        {
            if (visMapData.Count != data.Count)
            {
                Console.WriteLine("Vis map data not same length as other data.");
                return;
            }

            Vector2 meshOrigin = new Vector2((float)meshData.PDIM.x1, (float)meshData.PDIM.y1);
            Vector2 cellSize = new Vector2((float)(meshData.PDIM.x2 - meshData.PDIM.x1) / (bounds.i2 - bounds.i1), (float)(meshData.PDIM.y2 - meshData.PDIM.y1) / (bounds.j2 - bounds.j1));

            for (int t = 0; t < visMapData.Count; t++)
            {
                for (int y = 0; y < data[t].GetLength(1); y++)
                {
                    for (int x = 0; x < data[t].GetLength(0); x++)
                    {
                        Vector2 pos = new Vector2((x + 0.5f) * cellSize.X + meshOrigin.X, (y + 0.5f) * cellSize.Y + meshOrigin.Y);

                        IJK ijk = combinedMesh.ijk;
                        XB PDIM = combinedMesh.PDIM;

                        int xIndex = (int)(ijk.i * (pos.X - PDIM.x1) / (PDIM.x2 - PDIM.x1));
                        int yIndex = (int)(ijk.j * (pos.Y - PDIM.y1) / (PDIM.y2 - PDIM.y1));
                        float d = visMapData[t][xIndex, yIndex];

                        data[t][x, y] = d;
                    }
                }
            }

            string path = Path.GetDirectoryName(file);
            string fName = Path.Combine(path, target.name + "_" + meshData.index + ".sf"); ;// file + "_" + target.name;
            ReadSF.WriteSF(fName, slice, this);
        }

        public string[] GetSMVLines(Target target)
        {
            string[] lines = new string[5];
            string i1 = bounds.i1.ToString().PadLeft(6);
            string i2 = bounds.i2.ToString().PadLeft(6);
            string j1 = bounds.j1.ToString().PadLeft(6);
            string j2 = bounds.j2.ToString().PadLeft(6);
            string k1 = bounds.k1.ToString().PadLeft(6);
            string k2 = bounds.k2.ToString().PadLeft(6);
            string b = i1 + i2 + j1 + j2 + k1 + k2;
            string sliceIndex = (parentSlice.parentSliceCollection.maxSliceIndex + target.index).ToString().PadLeft(7);

            lines[0] = "SLCC     " + meshData.index + " # STRUCTURED %" + target.name + "_vismap &" + b + " !" + sliceIndex + "      1";
            lines[1] = " " + target.name + "_" + meshData.index + ".sf";
            lines[2] = " VISIBILITY MAP, C=" + Program.lowerC + "-" + Program.upperC;
            lines[3] = " vismap";
            lines[4] = " -";

            return lines;
        }
    }
}
