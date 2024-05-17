using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static System.Windows.Forms.LinkLabel;

namespace SmokeviewVisibilityMaps
{    
    public class SMV_data
    {
        public string smvFile; //location of file
        private static string[]? smvText; //contains all lines from smv file
        public int NMESHES;
        public MeshCollection meshCollection;
        public SliceCollection sliceCollection;
        //public SubSlice? combinedSlice;
        //public MeshData? combinedMesh;        

        public SMV_data(string file)
        {
            this.smvFile = file;
            NMESHES = -1;
            meshCollection = new MeshCollection();
            sliceCollection = new SliceCollection();

            ReadSMVFile(file);
        }

        private void ReadSMVFile(string file)
        {
            if (File.Exists(file))
            {
                if (!file.EndsWith(".smv"))
                {
                    Console.WriteLine("Invalid file, please select a *.smv-file.");
                    return;
                }

                string[] lines = File.ReadAllLines(file);
                smvText = lines;
                int meshIndex = 0;

                for (int i = 0; i < lines.Length; ++i)
                {
                    if (NMESHES < 0 && lines[i].StartsWith("NMESHES"))
                    {
                        ReadNMESHES(lines, i);
                    }

                    if (meshCollection.meshData.Count < NMESHES && lines[i].StartsWith("GRID"))
                    {
                        ++meshIndex;
                        ReadGRID(meshIndex, lines, i);
                    }

                    if (lines[i].StartsWith("SLCC") || lines[i].StartsWith("SLCF")) //Read also SLCF since we need to keep track of max slice index for later creation of new modified smv file
                    {
                        ReadSLC(lines, i, file, lines[i].StartsWith("SLCC"));
                    }
                }

                if (Program.targets != null)
                {
                    //Build all the relevant combined data
                    Dictionary<float, CombinedData> combinedData = new Dictionary<float, CombinedData>();

                    for (int i = 0; i < Program.targets.Count; i++)
                    {
                        Target target = Program.targets[i];
                        if (!combinedData.ContainsKey(target.pos.Z))
                        {
                            Slice? extHorSlice = sliceCollection.GetSuitableExtinctionSlice(target);
                            //try to build extinction data from visibility data
                            if (extHorSlice == null)
                            {
                                Console.WriteLine("Smokeview file does not contain suitable extinction coefficient data in z-plane [m] " + target.pos.Z + " for target " + target.name + ", attempting to create from visibility slice.");
                                sliceCollection.BuildExtinctionFromVisibility(target);
                                extHorSlice = sliceCollection.GetSuitableExtinctionSlice(target);
                            }

                            if (extHorSlice != null)
                            {
                                MeshData m;
                                SubSlice s;
                                if (NMESHES > 1)
                                {
                                    BuildCombinedSliceAndMesh(extHorSlice.sliceIndex, out m, out s, target.pos.Z);
                                }
                                else
                                {
                                    s = extHorSlice.subSlices[0];
                                    m = extHorSlice.subSlices[0].meshData;
                                }                                
                                combinedData.Add(target.pos.Z, new CombinedData(m, s));
                            }
                            else
                            {
                                Console.WriteLine("Smokeview file does not contain any suitable data in the specified z-plane [m] " + target.pos.Z + " for target " + target.name);
                            }
                        }  
                    }

                    if(combinedData.Count == 0)
                    {
                        return;
                    }

                    //Then do ray-marching if any relevant data saved
                    List<string> extraSMVText = new List<string>();
                    for (int i = 0; i < Program.targets.Count; i++)
                    {
                        Target target = Program.targets[i];
                        CombinedData cD;   
                        if (combinedData.TryGetValue(target.pos.Z, out cD))
                        {                                
                            Console.WriteLine("Starting ray-marching towards target " + target.name + "...");
                            Console.Write("\rProgress: 0%");
                            int tasks = cD.slice.data.Count, taskIndex = 0;

                            List<float[,]> visMapData = new List<float[,]>();
                            for (int j = 0; j < cD.slice.data.Count; j++)
                            {
                                float[,] data = cD.slice.data[j];
                                float[,] vData = Program.CalculateVisibilityMap(data, cD.mesh, target, cD.slice.timeStamps[j] + "s");
                                visMapData.Add(vData);

                                taskIndex += 100;
                                Console.Write("\rProgress: " + taskIndex / tasks + "%");
                            }
                            Console.WriteLine();
                            sliceCollection.WriteSliceFiles(target, visMapData, cD.mesh, extraSMVText);
                        }

                        
                    }
                    CreateVismapsSMVFile(extraSMVText);
                }

                Console.WriteLine("Ray-marching done and smv/slice data saved.");
                
            }
            else
            {
                Console.WriteLine("File does not exist.");
            }           
        }

        private void CreateVismapsSMVFile(List<string> extraSMVText)
        {
            string? path = Path.GetDirectoryName(smvFile);
            if(path != null && smvText != null)
            {
                string newFile = Path.Combine(path, Path.GetFileNameWithoutExtension(smvFile) + "_vismaps.smv");
                using (StreamWriter outputFile = new StreamWriter(newFile))
                {
                    for (int i = 0; i < smvText.Length; i++)
                    {
                        outputFile.WriteLine(smvText[i]);
                    }

                    for (int i = 0; i < extraSMVText.Count; i++)
                    {
                        outputFile.WriteLine(extraSMVText[i]);
                    }
                }
                Console.WriteLine("Wrote new Smokeview file " + newFile);
            }            
        }

        void ReadSLC(string[] lines, int startIndex, string smokeviewFile, bool isCellCentered)
        {
            int meshIndex, sliceIndex;
            string name, file, quantity, shortName, unit;
            IIJJKK bounds = new IIJJKK();

            string[] lineData = lines[startIndex].Split(space, StringSplitOptions.RemoveEmptyEntries);
            int.TryParse(lineData[1], out meshIndex);

            if (lineData[4].StartsWith("%"))
            {
                name = lineData[4];
                name = name.Remove(0, 1);
            }
            else
            {
                name = "NO_NAME";
            }
            lineData = lines[startIndex].Split("&");
            lineData = lineData[1].Split(space, StringSplitOptions.RemoveEmptyEntries);
            int.TryParse(lineData[0], out bounds.i1);
            int.TryParse(lineData[1], out bounds.i2);
            int.TryParse(lineData[2], out bounds.j1);
            int.TryParse(lineData[3], out bounds.j2);
            int.TryParse(lineData[4], out bounds.k1);
            int.TryParse(lineData[5], out bounds.k2);

            lineData = lines[startIndex].Split("!");
            lineData = lineData[1].Split(space, StringSplitOptions.RemoveEmptyEntries);
            int.TryParse(lineData[0], out sliceIndex);

            string line = lines[startIndex + 1];            
            file = line.Trim();
            string? path = Path.GetDirectoryName(smokeviewFile);
            if(path!= null)
            {
                file = Path.Combine(path, file);
            }

            line = lines[startIndex + 2];
            quantity = line.Trim();

            line = lines[startIndex + 3];
            shortName = line.Trim();            

            line = lines[startIndex + 4];
            unit = line.Trim();

            sliceCollection.CollectSliceData(isCellCentered, meshIndex, name, bounds, sliceIndex, file, quantity, shortName, unit);
        }

        private void BuildCombinedSliceAndMesh(int sliceIndex, out MeshData combinedMesh, out SubSlice combinedSlice, float z)
        {
            Console.WriteLine("Building combined slice and mesh...");

            double minX1 = double.MaxValue, maxX2 = double.MinValue, minY1 = double.MaxValue, maxY2 = double.MinValue, minCellSizeX = double.MaxValue, minCellSizeY = double.MaxValue;

            List<MeshData> m = meshCollection.meshData;

            for (int i = 0; i < m.Count; i++)
            {
                minX1 = Math.Min(minX1, (float)m[i].PDIM.x1);
                maxX2 = Math.Max(maxX2, (float)m[i].PDIM.x2);

                minY1 = Math.Min(minY1, (float)m[i].PDIM.y1);
                maxY2 = Math.Max(maxY2, (float)m[i].PDIM.y2);

                minCellSizeX = Math.Min(minCellSizeX, (float)m[i].cellSizeX);
                minCellSizeY = Math.Min(minCellSizeY, (float)m[i].cellSizeY);
            }

            XB pdim = new XB(minX1, maxX2, minY1, maxY2, z, z);
            Vector2 origin = new Vector2((float)minX1, (float)minY1);

            int xCells = (int)(0.5f + (pdim.x2 - pdim.x1) / minCellSizeX);
            int yCells = (int)(0.5f + (pdim.y2 - pdim.y1) / minCellSizeY);
            IJK ijk = new IJK(xCells, yCells, 1);

            combinedMesh = new MeshData(1, "combinedMesh", ijk, pdim, null);
            Console.WriteLine("Combined mesh created, XB:" + combinedMesh.PDIM.Print() + ", IJK: " + combinedMesh.ijk.Print());

            List<float[,]> combinedData = new List<float[,]>();
            List<float> timeStamps = sliceCollection.GetTimeStamps(sliceIndex);
            int timeSamplesCount = timeStamps.Count;

            for (int t = 0; t < timeSamplesCount; t++)
            {
                float[,] newData = new float[xCells, yCells];
                for (int y = 0; y < yCells; y++)
                {
                    for (int x = 0; x < xCells; x++)
                    {
                        Vector2 pos = origin + new Vector2((x + 0.5f) * (float)minCellSizeX, (y + 0.5f) * (float)minCellSizeY);

                        newData[x, y] = GetDataValue(pos, t, sliceIndex);
                    }
                }
                combinedData.Add(newData);
            }

            IIJJKK iijjkk = new IIJJKK(0, xCells, 0, yCells, 0, 1);
            combinedSlice =  new SubSlice(combinedMesh, iijjkk, combinedData, timeStamps);    
        }

        float GetDataValue(Vector2 pos, int timeSample, int sliceIndex)
        {
            float result = -9999.0f;

            int meshIndex = meshCollection.GetMeshIndex(pos);
            if (meshIndex > 0)
            {
                result = sliceCollection.GetDataValue(pos, timeSample, sliceIndex, meshIndex);
            }

            return result;
        }

        public void ReadNMESHES(string[] lines, int startIndex)
        {
            int.TryParse(lines[startIndex + 1], out NMESHES);
            Console.WriteLine("Will look for " + NMESHES + " meshes.");
        }

        public void ReadGRID(int index, string[] lines, int startIndex)
        {
            //data to read
            string name;
            int i, j, k;
            double x1, x2, y1, y2, z1, z2;

            //GRID
            string[] lineData = lines[startIndex].Split(space, StringSplitOptions.RemoveEmptyEntries);
            name = lineData[1];

            lineData = lines[startIndex + 1].Split(space, StringSplitOptions.RemoveEmptyEntries);
            int.TryParse(lineData[0], out i);
            int.TryParse(lineData[1], out j);
            int.TryParse(lineData[2], out k);
            IJK ijk = new IJK(i, j, k);

            //PDIM
            lineData = lines[startIndex + 4].Split(space, StringSplitOptions.RemoveEmptyEntries);
            double.TryParse(lineData[0], out x1);
            double.TryParse(lineData[1], out x2);
            double.TryParse(lineData[2], out y1);
            double.TryParse(lineData[3], out y2);
            double.TryParse(lineData[4], out z1);
            double.TryParse(lineData[5], out z2);
            XB pdim = new XB(x1, x2, y1, y2, z1, z2);

            OBST[] OBSTs = ReadMeshOBSTs(lines, startIndex);

            MeshData mData = new MeshData(index, name, ijk, pdim, OBSTs);            
            meshCollection.AddMesh(mData);
            Console.WriteLine("Found mesh " + name + ", have found " + meshCollection.meshData.Count + " meshes so far.");
            Console.WriteLine("Mesh data [IJK]: " + mData.ijk.Print() + ", [XB]: " + mData.PDIM.Print());
            /*Console.WriteLine("Contained the following OBSTs: ");
            for (int o = 0; o < OBSTs.Length; o++)
            {
                OBSTs[o].Print();
            }*/
        }

        private OBST[] ReadMeshOBSTs(string[] lines, int startIndex)
        {
            List<OBST> result = new List<OBST>();

            for (int i = startIndex; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("OBST"))
                {
                    string[] lineData = lines[i + 1].Split(space, StringSplitOptions.RemoveEmptyEntries);
                    int obstacleCount;
                    int.TryParse(lineData[0], out obstacleCount);

                    if(obstacleCount > 0)
                    {
                        for (int j = 0; j < obstacleCount; j++)
                        {
                            //XB values
                            double x1, x2, y1, y2, z1, z2;
                            lineData = lines[i + 2 + j].Split(space, StringSplitOptions.RemoveEmptyEntries);
                            double.TryParse(lineData[0], out x1);
                            double.TryParse(lineData[1], out x2);
                            double.TryParse(lineData[2], out y1);
                            double.TryParse(lineData[3], out y2);
                            double.TryParse(lineData[4], out z1);
                            double.TryParse(lineData[5], out z2);
                            XB xb = new XB(x1, x2, y1, y2, z1, z2);

                            //IIJJKK values
                            int i1, i2, j1, j2, k1, k2;
                            lineData = lines[i + 2 + j + obstacleCount].Split(space, StringSplitOptions.RemoveEmptyEntries);
                            int.TryParse(lineData[0], out i1);
                            int.TryParse(lineData[1], out i2);
                            int.TryParse(lineData[2], out j1);
                            int.TryParse(lineData[3], out j2);
                            int.TryParse(lineData[4], out k1);
                            int.TryParse(lineData[5], out k2);
                            IIJJKK iijjkk = new IIJJKK(i1, i2, j1, j2, k1, k2);

                            OBST obst = new OBST(xb, iijjkk);
                            result.Add(obst);
                        }
                    }

                    //nothing more to read after finding OBST line
                    break;
                }                
            }

            return result.ToArray();
        }

        private static string[] space = new[] { " " };         
    }
}
