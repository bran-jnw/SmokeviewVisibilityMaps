using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SmokeviewVisibilityMaps
{    
    public class SliceCollection
    {
        public Dictionary<int, Slice> slices;
        public int maxSliceIndex = 0;

        public SliceCollection() 
        {
            slices = new Dictionary<int, Slice>();
        }

        public void CollectSliceData(bool cellCentered, int meshIndex, string name, IIJJKK bounds, int sliceIndex, string file, string quantity, string shortName, string unit)
        {
            //save this info for when we add are new slices in the vismaps smv
            maxSliceIndex = sliceIndex > maxSliceIndex ? sliceIndex : maxSliceIndex;

            if (quantity != "SOOT EXTINCTION COEFFICIENT" || bounds.k1 != bounds.k2 || !cellCentered)
            {
                return;
            }

            bool sliceExists = false;

            Slice? slice;
            slices.TryGetValue(sliceIndex, out slice);

            if(slice != null)
            {
                sliceExists = true;
                slice.AddSubSlice(meshIndex, bounds, file);
            }

            if(!sliceExists)
            {
                Console.WriteLine("New data found: " + quantity + ", plane: " + bounds.k1 + ", " + bounds.k2);
                Slice s = new Slice(cellCentered, name, quantity, shortName, unit, sliceIndex, this);
                s.AddSubSlice(meshIndex, bounds, file);
                slices.Add(sliceIndex, s);
            }           
        }

        public float GetDataValue(Vector2 pos, int timeSample, int sliceIndex, int meshIndex)
        {
            float result = -9999f;
            Slice? slice;
            slices.TryGetValue(sliceIndex, out slice);
            if (slice != null)
            {
                result = slice.subSlices[meshIndex - 1].GetDataValue(pos, timeSample);
            }

            return result;
        }

        public int GetSampleCount(int sliceIndex)
        {
            int result = -1;
            Slice? slice;
            slices.TryGetValue(sliceIndex, out slice);
            if (slice != null)
            {
                result = slice.subSlices[0].data.Count;
            }

            return result;
        }

        public List<float>? GetTimeStamps(int sliceIndex)
        {
            List<float>? result = null;
            Slice? slice;
            slices.TryGetValue(sliceIndex, out slice);
            if (slice != null)
            {
                result = slice.subSlices[0].timeStamps;
            }

            return result;

        }

        public Slice? GetSuitableExtinctionSlice(Target target)
        {
            Slice? result = null;

            foreach (var slice in slices)
            {
                if (slice.Value.quantity == "SOOT EXTINCTION COEFFICIENT" && slice.Value.IsInCorrectZPlane(target))
                {
                    result = slice.Value;
                }
            }

            return result; 
        }        

        public void BuildExtinctionFromVisibility(Target target)
        {
            foreach (var slice in slices)
            {
                if (slice.Value.quantity == "SOOT VISIBILITY" && slice.Value.IsInCorrectZPlane(target))
                {
                    slice.Value.quantity = "SOOT EXTINCTION COEFFICIENT";
                    for (int j = 0; j < slice.Value.subSlices.Count; j++)
                    {
                        SubSlice s = slice.Value.subSlices[j];
                        for (int t = 0; t < s.data.Count; t++)
                        {
                            for (int y = 0; y < s.data[t].GetLength(1); y++)
                            {
                                for (int x = 0; x < s.data[t].GetLength(0); x++)
                                {
                                    float K = 8.0f / s.data[t][x, y];

                                    s.data[t][x, y] = K;
                                }
                            }
                        }
                    }                    
                }
            }
        }

        public void WriteSliceFiles(Target target, List<float[,]> visMapData, MeshData combinedMesh, List<string> smvText)
        {
            Slice? extSlice = GetSuitableExtinctionSlice(target);

            if(extSlice != null)
            {
                List<string> smvLines = new List<string>();
                extSlice.ReplaceData(target, visMapData, combinedMesh, smvLines);
                smvText.AddRange(smvLines);
            }            
        }
    }
    
}
