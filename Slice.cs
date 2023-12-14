using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmokeviewVisibilityMapper
{
    public class Slice
    {
        public bool cellCentered;
        public string name;
        public string quantity;
        public string shortName;
        public string unit;
        public int sliceIndex;
        public List<SubSlice> subSlices;
        public SliceCollection parentSliceCollection;

        public Slice(bool cellCentered, string name, string quantity, string shortName, string unit, int sliceIndex, SliceCollection sliceCollection)
        {
            this.cellCentered = cellCentered;
            this.name = name;
            this.quantity = quantity;
            this.shortName = shortName;
            this.unit = unit;
            this.sliceIndex = sliceIndex;
            subSlices = new List<SubSlice>();
            parentSliceCollection = sliceCollection;
        }

        public void AddSubSlice(int meshIndex, IIJJKK bounds, string file)
        {
            SubSlice s = new SubSlice(meshIndex, bounds, file, this);
            subSlices.Add(s);
        }

        public bool IsInCorrectZPlane(Target target)
        {
            bool result = false;

            if (subSlices != null && subSlices.Count > 0)
            {
                //we have a plane
                if(subSlices[0].bounds.k1 == subSlices[0].bounds.k2)
                {
                    //reduce by half a cell as that is the center
                    result = Math.Abs(target.pos.Z - (subSlices[0].bounds.k1 - 0.5) * subSlices[0].meshData.cellSizeZ - subSlices[0].meshData.PDIM.z1) <= subSlices[0].meshData.cellSizeZ;
                }
            }

            return result;
        }

        public void ReplaceData(Target target, List<float[,]> visMapData, MeshData mesh, List<string> smvText)
        {
            string oldQuantity = quantity, oldUnit = unit;
            quantity = "POTENTIAL VISIBILITY";
            unit = "C_RATIO";

            for (int i = 0; i < subSlices.Count; i++)
            {
                subSlices[i].OverwriteDataWithVisMap(target, visMapData, mesh, this);
                smvText.AddRange(subSlices[i].GetSMVLines(target));
            }

            quantity = oldQuantity;
            unit = oldUnit;
        }
    }
}
