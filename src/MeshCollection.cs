using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SmokeviewVisibilityMaps
{    
    public class MeshCollection
    {
        private static MeshCollection? MESH_COLLECTION;

        public List<MeshData> meshData;

        public MeshCollection() 
        { 
            meshData = new List<MeshData>();
            MESH_COLLECTION = this;
        }

        public void AddMesh(MeshData m)
        {
            meshData.Add(m);
        }

        public MeshData? GetMesh(Vector2 pos)
        {
            MeshData? result = null;

            for (int i = 0; i < meshData.Count; i++)
            {
                if (meshData[i].ContainsPoint(pos))
                {
                    result = meshData[i];
                    break;
                }
            }

            return result;
        }

        public int GetMeshIndex(Vector2 pos)
        {
            int result = -1;

            for (int i = 0; i < meshData.Count; i++)
            {
                if (meshData[i].ContainsPoint(pos))
                {
                    //mesh index starts at 1
                    result = i + 1;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Mesh indicees start at 1 (due to Smokeview format)
        /// </summary>
        /// <param name="meshIndex"></param>
        /// <returns></returns>
        public MeshData GetMesh(int meshIndex)
        {
            //mesh indices start at 1
            return meshData[meshIndex - 1];
        }

        public MeshData? GetMesh(string meshName)
        {
            MeshData? result = null;

            for (int i = 0; i < meshData.Count; i++)
            {
                if (meshData[i].name == meshName)
                {
                    result= meshData[i];
                    break;
                }
            }
            return result;
        }

        public static MeshCollection Instance()
        {
            if(MESH_COLLECTION == null)
            {
                MESH_COLLECTION = new MeshCollection();
            }
            return MESH_COLLECTION;
        }
    }
}
