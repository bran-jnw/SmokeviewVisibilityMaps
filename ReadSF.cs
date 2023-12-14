using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace SmokeviewVisibilityMapper
{
    internal static class ReadSF
    {
        //uses 4 byte padding before and after data
        public static List<float[,]>? ReadData(string file, out List<float>? timeStamps)
        {
            int iSize, jSize, kSize;

            //uses little endian
            BinaryReader fid = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read));
            if (fid == null)
            {
                Console.WriteLine("Could not open file " + file);
                timeStamps = null;
                return null;
            }
            //read headers
            read4bytes(fid);
            string QUANTITY = Encoding.UTF8.GetString(fid.ReadBytes(30));
            read4bytes(fid);
            read4bytes(fid);
            string SHORT_NAME = Encoding.UTF8.GetString(fid.ReadBytes(30));
            read4bytes(fid);
            read4bytes(fid);
            string UNITS = Encoding.UTF8.GetString(fid.ReadBytes(30));
            read4bytes(fid);
            //reads slice bounds based on mesh cell index
            read4bytes(fid);
            int[] bounds = new int[6];
            for (int i = 0; i < 6; i++)
            {
                bounds[i] = fid.ReadInt32();
            }
            read4bytes(fid);
            //read all the data
            iSize = bounds[1] - bounds[0] + 1;
            jSize = bounds[3] - bounds[2] + 1;
            kSize = bounds[5] - bounds[4] + 1;

            Console.WriteLine("Cells [x, y]: " + (iSize - 1) + ", " + (jSize - 1));// + ", " + kSize) ;

            List<float[,]> dataCollection = new List<float[,]>();
            timeStamps = new List<float>();
            while (fid.BaseStream.Position != fid.BaseStream.Length)
            {
                read4bytes(fid);
                float currTime = fid.ReadSingle();
                timeStamps.Add(currTime);
                read4bytes(fid);

                read4bytes(fid);
                //we will discard the first value on each axis as they are not used when reading cell centered data
                float[,] data = new float[iSize - 1,  jSize - 1];
                for (int k = 0; k < kSize; k++)
                {
                    for (int j = 0; j < jSize; j++)
                    {
                        for (int i = 0; i < iSize; i++)
                        {
                            if(i > 0 && j > 0)
                            {
                                data[i - 1, j - 1] = fid.ReadSingle();
                            }
                            else
                            {
                                fid.ReadSingle();
                            }
                        }
                    }
                }
                dataCollection.Add(data);
                read4bytes(fid);
            }
            fid.Close();
            Console.WriteLine("Done reading smoke file " + file);

            return dataCollection;
        }

        //skip 4 bytes ahead
        static void read4bytes(BinaryReader fid)
        {
            fid.ReadBytes(4);
        }

        static void write4bytes(BinaryWriter fid)
        {
            fid.Write(0);
        }

        public static void WriteSF(string file, Slice slice, SubSlice subSlice)
        {
            BinaryWriter fid = new BinaryWriter(File.Open(file, FileMode.Create, FileAccess.Write));
            if (fid == null)
            {
                Console.WriteLine("Could not write to file " + file);
                return;
            }
            //read headers
            write4bytes(fid);
            byte[] text = Encoding.UTF8.GetBytes(slice.quantity.PadRight(30));
            fid.Write(text);
            write4bytes(fid);

            write4bytes(fid);
            text = Encoding.UTF8.GetBytes(slice.shortName.PadRight(30));
            fid.Write(text);
            write4bytes(fid);

            write4bytes(fid);
            //string UNITS = Encoding.UTF8.GetString(fid.ReadBytes(30));
            text = Encoding.UTF8.GetBytes(slice.unit.PadRight(30));
            fid.Write(text);
            write4bytes(fid);

            //reads slice bounds based on mesh cell index
            write4bytes(fid);
            fid.Write(subSlice.bounds.i1);
            fid.Write(subSlice.bounds.i2);
            fid.Write(subSlice.bounds.j1);
            fid.Write(subSlice.bounds.j2);
            fid.Write(subSlice.bounds.k1);
            fid.Write(subSlice.bounds.k2);
            write4bytes(fid);

            //bounds
            int iSize = subSlice.bounds.i2 - subSlice.bounds.i1 + 1;
            int jSize = subSlice.bounds.j2 - subSlice.bounds.j1 + 1;
            int kSize = subSlice.bounds.k2 - subSlice.bounds.k1 + 1;

            //Console.WriteLine("Writing timestamps, total of " + subSlice.timeStamps.Count);

            for (int t = 0; t < subSlice.timeStamps.Count; t++)
            {
                write4bytes(fid);
                fid.Write(subSlice.timeStamps[t]);
                write4bytes(fid);

                write4bytes(fid);
                //we will discard the first value on each axis as they are not used when reading cell centered data
                for (int k = 0; k < kSize; k++)
                {
                    for (int j = 0; j < jSize; j++)
                    {
                        for (int i = 0; i < iSize; i++)
                        {
                            if (i > 0 && j > 0)
                            {
                                fid.Write(subSlice.data[t][i - 1, j - 1]);
                            }
                            else
                            {
                                fid.Write(0.0f);
                            }
                        }
                    }
                }
                write4bytes(fid);
            }
            fid.Close();
            Console.WriteLine("Wrote new slice file " + file);
        }
    }
}
