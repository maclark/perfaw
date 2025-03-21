using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class Imaginer
{
    public static void ConvertToPNG(byte[] rawData)
    {
        int width = 64;
        int height = 64;
        string outputPath = "rb-gradient-for-casey.png";

      // Convert to a Bitmap
        using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        {
            int index = 64 * 4;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = Color.FromArgb(
                        rawData[index + 3], // A
                        rawData[index + 0], // R
                        rawData[index + 1], // G
                        rawData[index + 2]  // B
                    );
                    bitmap.SetPixel(x, y, color);
                    index += 4;
                }
            }

            // Save as PNG
            bitmap.Save(outputPath, ImageFormat.Png);
        }

        Console.WriteLine($"Conversion complete. Open '{outputPath}'");
    }
}
