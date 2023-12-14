using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Security;
using System.Windows.Forms;

namespace SmokeviewVisibilityMaps
{
    internal static class Program
    {
        private static string? smvFile;   

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            //ApplicationConfiguration.Initialize();

            SelectInput();
            if(smvFile != null && targetsFile != null)
            {
                ReadTargetData();
                ReadSmokeview();
            }
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
                
        static void SelectInput()
        {
            Console.WriteLine("Select Smokeview file...");
            OpenFileDialog smvDialog = new OpenFileDialog()
            {
                FileName = "Select a Smokeview file",
                Filter = "Smokeview files (*.smv)|*.smv",
                Title = "Open Smokeview file"
            };

            OpenSMV(smvDialog);

            if(smvFile != null)
            {
                Console.WriteLine("Select target file...");
                OpenFileDialog targetDialog = new OpenFileDialog()
                {
                    FileName = "Select target input file",
                    Filter = "Text files (*.txt)|*.txt",
                    Title = "Open target input file"
                };

                OpenTargetData(targetDialog);
            }                       
        }

        private static void OpenSMV(OpenFileDialog dialog)
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    smvFile = dialog.FileName;
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }

        private static void OpenTargetData(OpenFileDialog dialog)
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    targetsFile = dialog.FileName;
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }

        private static string? targetsFile;
        public static float massExtCoeff, lowerC, upperC;
        public static List<Target>? targets;
        
        static void ReadTargetData()
        {
            if(targetsFile != null)
            {
                targets = new List<Target>();

                string[] lines = File.ReadAllLines(targetsFile);
                int targetIndex = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("Target"))
                    {
                        ++targetIndex;
                        string[] targetLine = lines[i].Split("=")[1].Split(",", StringSplitOptions.RemoveEmptyEntries);                        
                        float x, y, z;

                        string targetName = targetLine[0];
                        float.TryParse(targetLine[1], out x);
                        float.TryParse(targetLine[2], out y);
                        float.TryParse(targetLine[3], out z);

                        Target newTarget = new Target(targetName, new Vector3(x, y, z), targetIndex);                        
                        targets.Add(newTarget);
                        Console.WriteLine("Found target: " + targetName + " (" + x + ", " + y + ", " + z + ")");
                    }
                    else if (lines[i].StartsWith("MassExtinctionCoefficient"))
                    {
                        float.TryParse(lines[i].Split("=")[1], out massExtCoeff);
                    }
                    else if (lines[i].StartsWith("lowerC"))
                    {
                        float.TryParse(lines[i].Split("=")[1], out lowerC);
                        Console.WriteLine("Using lower C value of : " + lowerC);
                    }
                    else if (lines[i].StartsWith("upperC"))
                    {
                        float.TryParse(lines[i].Split("=")[1], out upperC);
                        Console.WriteLine("Using upper C value of : " + upperC);
                    }
                }               
            }
        }

        static void ReadSmokeview()
        {
            if(smvFile != null)
            {
                SMV_data smvData = new SMV_data(smvFile);
            }            
        }

        /*public static void TakeInput()
        {
            float xTarget, yTarget;
            int okInput = 0;

            Console.WriteLine("Enter target x-position:");
            okInput += float.TryParse(Console.ReadLine(), out xTarget) ? 1 : 0;

            Console.WriteLine("Enter target y-position:");
            okInput += float.TryParse(Console.ReadLine(), out yTarget) ? 1 : 0;

            Console.WriteLine("Enter lower C value:");
            okInput += float.TryParse(Console.ReadLine(), out lowerC) ? 1 : 0;

            Console.WriteLine("Enter upper C value:");
            okInput += float.TryParse(Console.ReadLine(), out upperC) ? 1 : 0;            

            if (okInput == 4)
            {
                Console.WriteLine("Enter file suffix:");
                fileSuffix = Console.ReadLine(); 

                target = new Vector2(xTarget, yTarget);
                //form.OpenDialog();
            }
            else
            {
                Console.WriteLine("Incorrect input, please try again.");
                TakeInput();
            }
            
            //Application.Run(new Form1());
        }*/
         
        public static float[,] CalculateVisibilityMap(float[,] data, MeshData meshData, Target target, string prefix)
        {
            //clamp target inside data
            float xTarget = MathF.Max((float)meshData.PDIM.x1 + 0.01f, MathF.Min((float)meshData.PDIM.x2 - 0.01f, target.pos.X));
            float yTarget = MathF.Max((float)meshData.PDIM.y1 + 0.01f, MathF.Min((float)meshData.PDIM.y2 - 0.01f, target.pos.Y));
            Vector2 clampedTarget = new Vector2(xTarget, yTarget);

            //int targetXindex = (int)(target.X / meshData.cellSizeX);
            //int targetYindex = (int)(target.Y / meshData.cellSizeY);

            float[,] visMapData = new float[data.GetLength(0), data.GetLength(1)];
            //Bitmap bmp = new Bitmap(data.GetLength(0), data.GetLength(1) * 2);
            
            for (int y = 0; y < data.GetLength(1); y++)
            {
                for (int x = 0; x < data.GetLength(0); x++)
                {
                    float totalDistance, extCoeff;
                    bool startInSolid;
                    Vector2 startPos = new Vector2((float)meshData.PDIM.x1, (float)meshData.PDIM.y1) + new Vector2((float)meshData.cellSizeX * (x + 0.5f), (float)meshData.cellSizeY * (y + 0.5f));
                    float transmittance = CalculateTransmittance(x, y, startPos, clampedTarget, (float)meshData.cellSizeX, (float)meshData.cellSizeY, data, out totalDistance, out extCoeff, out startInSolid);

                    if (transmittance > 0f && totalDistance > 0f)
                    {
                        float K = -MathF.Log(transmittance) / totalDistance;
                        K = MathF.Max(0.001f, K);
                        float lowerVisibilityLimit = lowerC / K;
                        float upperVisibilityLimit = upperC / K;

                        float ratio = (totalDistance - lowerVisibilityLimit) / (upperVisibilityLimit - lowerVisibilityLimit);
                        ratio = MathF.Max(0.0f, MathF.Min(1.0f, ratio));
                        transmittance = 1.0f - ratio;
                    }

                    visMapData[x, y] = 1.0f - transmittance;

                    /*Color c = Color.White;
                    if (!startInSolid)
                    {
                        c = HSVtoRGB(transmittance * 0.333333333f, 1.0f, 1.0f, 1.0f); //Color.FromArgb((int)(255 * transmittance));
                    }

                    if (x == targetXindex && y == targetYindex)
                    {
                        c = Color.Black;
                    }

                    //pixel index is 0,0 at UPPER LEFT CORNER, so we have to flip Y-axis
                    bmp.SetPixel(x, data.GetLength(1) - 1 - y, c);

                    //int greyScale = (int)(255.0f * MathF.Pow(transmission, 0.25f)); //(int)(255.0f * transmission / 6.0f);
                    c = Color.White;
                    if (!startInSolid)
                    {
                        c = HSVtoRGB(0.67f - extCoeff * 0.666667f, 1.0f, 1.0f, 1.0f);                        
                    }
                    bmp.SetPixel(x, data.GetLength(1) * 2 - 1 - y, c);*/
                }
            }

            /*string fileName = prefix + ".png";
            bmp.Save(Path.Combine(Directory.GetCurrentDirectory(), "_out", fileName), ImageFormat.Png);*/

            return visMapData;
        }

        private static float CalculateTransmittance(int x, int y, Vector2 startPos, Vector2 target, float cellSizeX, float CellSizeY, float[,] data, out float distanceToTarget, out float extCoeff, out bool startInSolid)
        {
            startInSolid = false;
            extCoeff = data[x, y];
            //starting in solid
            if (extCoeff == -9999.0f)
            {
                startInSolid = true;
                distanceToTarget = 0f;
                return 0f;
            }

            Vector2 direction = target - startPos;
            distanceToTarget = direction.Length();
            //normalize vector
            direction /= distanceToTarget;

            //determines which cell to jump to once we have reached the closest edge
            int stepX = direction.X < 0.0f ? -1 : 1;
            int stepY = direction.Y < 0.0f ? -1 : 1;
            //since we are starting in center of cell
            float distX = 0.5f * cellSizeX;
            float distY = 0.5f * CellSizeY;      
            int xIndex = x;
            int yIndex = y;
            Vector2 currentPos = startPos;
            float steppedDistance = 0.0f, oldSteppedDistance = 0.0f;
            float transmittance = 1.0f;

            while(steppedDistance < distanceToTarget)
            {
                //get extinction coefficient
                float cellExtCoeff = data[xIndex, yIndex];
                //hit solid
                if (cellExtCoeff ==-9999.0f) 
                {
                    distanceToTarget = 0f;
                    return 0f;
                }

                //evaluate which edge we will hit the fastest
                float timeX = distX / (direction.X * stepX);
                float timeY = distY / (direction.Y * stepY);

                if (timeX < timeY)
                {
                    currentPos += direction * timeX;
                    xIndex += stepX;
                    //full cell size as we just crossed that border
                    distX = cellSizeX;
                    //this is distance left until we hit the next y-border
                    distY -= direction.Y * timeX * stepY;   
                }
                else
                {
                    currentPos += direction * timeY;
                    yIndex += stepY;
                    distX -= direction.X * timeY * stepX;
                    distY = CellSizeY;
                }

                oldSteppedDistance = steppedDistance;
                steppedDistance = (currentPos - startPos).Length();
                if(steppedDistance > distanceToTarget)
                {
                    steppedDistance = distanceToTarget;
                }
                float takenStepLength = steppedDistance - oldSteppedDistance;

                //calculate transmission using Riemann sum
                transmittance *= MathF.Exp(-takenStepLength * cellExtCoeff);
            }
            
            return transmittance;
        }

        public static Color HSVtoRGB(float hue, float saturation, float value, float alpha)
        {
            alpha *= 255.0f;

            while (hue > 1f) { hue -= 1f; }
            while (hue < 0f) { hue += 1f; }
            while (saturation > 1f) { saturation -= 1f; }
            while (saturation < 0f) { saturation += 1f; }
            while (value > 1f) { value -= 1f; }
            while (value < 0f) { value += 1f; }
            if (hue > 0.999f) { hue = 0.999f; }
            if (hue < 0.001f) { hue = 0.001f; }
            if (saturation > 0.999f) { saturation = 0.999f; }
            if (saturation < 0.001f) { return Color.FromArgb((int)alpha, (int)(value * 255.0f), (int)(value * 255.0f), (int)(value * 255.0f)); }
            if (value > 0.999f) { value = 0.999f; }
            if (value < 0.001f) { value = 0.001f; }

            value *= 255.0f;

            float h6 = hue * 6f;
            if (h6 == 6f) { h6 = 0f; }
            int ihue = (int)(h6);
            float p = value * (1f - saturation);
            float q = value * (1f - (saturation * (h6 - (float)ihue)));
            float t = value * (1f - (saturation * (1f - (h6 - (float)ihue))));
            switch (ihue)
            {
                case 0:
                    return Color.FromArgb((int)alpha, (int)value, (int)t, (int)p);
                case 1:
                    return Color.FromArgb((int)alpha, (int)q, (int)value, (int)p);
                case 2:
                    return Color.FromArgb((int)alpha, (int)p, (int)value, (int)t);
                case 3:
                    return Color.FromArgb((int)alpha, (int)p, (int)q, (int)value);
                case 4:
                    return Color.FromArgb((int)alpha, (int)t, (int)p, (int)value);
                default:
                    return Color.FromArgb((int)alpha, (int)value, (int)p, (int)q);
            }
        }
    }    
}