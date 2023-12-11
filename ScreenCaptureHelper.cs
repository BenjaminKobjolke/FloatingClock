using System;
using System.Drawing; // Explicitly using System.Drawing

public static class ScreenCaptureHelper
{
    public static Bitmap CaptureScreenArea(int x, int y, int width, int height)
    {
        Bitmap bmp = new Bitmap(width, height);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(x, y, 0, 0, new Size(width, height));
        }
        return bmp;
    }

    public static double CalculateAverageBrightness(Bitmap bmp)
    {
        double totalBrightness = 0;
        int count = 0;

        for (int x = 0; x < bmp.Width; x++)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                Color pixelColor = bmp.GetPixel(x, y);
                double brightness = (0.299 * pixelColor.R + 0.587 * pixelColor.G + 0.114 * pixelColor.B) / 255;
                totalBrightness += brightness;
                count++;
            }
        }

        return totalBrightness / count;
    }
}
