using System;
using System.Diagnostics;
using System.Drawing; // For Bitmap and Graphics
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media; // For WPF Color
using Color = System.Windows.Media.Color; // Explicitly use WPF Color

namespace FloatingClock
{
    internal class ScreenCaptureHelper
    {
        private static double _lastAlpha = 0.0;
        private static double _lastBrightness = 0.0;
        public static double BrightnessChangeThreshold { get; set; } = 0.05; // Default value
        public static double MinBrightnessThreshold { get; set; } = 0.2; // Default value

        public static double MaxBrightnessThreshold { get; set; } = 0.7; // Default value

        public static double AlphaMax { get; set; } = 1.0; // Default value

        public static double AlphaMin { get; set; } = 0.1; // Default value

        public static double DampingFactor { get; set; } = 0.1; // Default value

        public static bool AdjustBackgroundTransparency(Window window, Color backgroundColor)
        {
            using (Bitmap bmp = CaptureScreenArea(window)) // Updated call
            {
                if(bmp == null)
                {
                    return false;
                }
                double averageBrightness = CalculateAverageBrightness(bmp);
                byte alpha = CalculateAlpha(averageBrightness);

                window.Dispatcher.Invoke(() =>
                {
                    // Create new color preserving RGB components, only changing Alpha
                    Color adjustedColor = Color.FromArgb(alpha, backgroundColor.R, backgroundColor.G, backgroundColor.B);
                    window.Background = new SolidColorBrush(adjustedColor);
                });
            }

            return true;
        }



        public static byte CalculateAlpha(double brightness)
        {
            double targetAlpha;
            double alphaMin = (255 * AlphaMin);
            double alphaMax = 255 * AlphaMax;

            if (brightness < MinBrightnessThreshold)
            {
                targetAlpha = 0 + alphaMin; // Maximum alpha for 100% transparency
            }
            else if (brightness > MaxBrightnessThreshold)
            {
                targetAlpha = alphaMax;
            }
            else
            {
                targetAlpha = 255 * brightness;
                if (targetAlpha < alphaMin)
                {
                    targetAlpha = alphaMin;
                } else if (targetAlpha > alphaMax)
                {
                    targetAlpha = alphaMax;
                }
            }


            if (Math.Abs(targetAlpha - _lastAlpha) >= BrightnessChangeThreshold)
            {
                _lastAlpha += (targetAlpha - _lastAlpha) * DampingFactor;
            }

            //Trace.WriteLine($"Brightness: {brightness}, Alpha: {_lastAlpha}");
            return (byte)_lastAlpha;
        }

        public static Bitmap CaptureScreenArea(Window window)
        {
            int offset = 100; // Offset to capture outside the window
            int x = Math.Max((int)window.Left - offset, 0);
            int y = Math.Max((int)window.Top - offset, 0);
            int width = (int)window.Width + (2 * offset);
            int height = (int)window.Height + (2 * offset);

            // Adjusting capture area to be within the bounds of the screen
            System.Drawing.Rectangle screenBounds = Screen.FromHandle(new WindowInteropHelper(window).Handle).Bounds;
            width = Math.Min(width, screenBounds.Width - x);
            height = Math.Min(height, screenBounds.Height - y);

            try
            {
                Bitmap bmp = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));
                }
                return bmp;
            }
            catch (Exception ex)
            {
                // Handle the exception
                // You can log the error, display a message, etc.
                Trace.WriteLine("Error capturing screen area: " + ex.Message);
                return null; // Or handle differently as per your requirement
            }        
        }

        private static double CalculateAverageBrightness(Bitmap bmp)
        {
            double totalBrightness = 0;
            int count = 0;

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    // Convert System.Drawing.Color to System.Windows.Media.Color
                    System.Drawing.Color drawingColor = bmp.GetPixel(x, y);
                    Color mediaColor = ConvertDrawingColorToMediaColor(drawingColor);

                    double brightness = (0.299 * mediaColor.R + 0.587 * mediaColor.G + 0.114 * mediaColor.B) / 255;
                    totalBrightness += brightness;
                    count++;
                }
            }

            return totalBrightness / count;
        }

        private static Color ConvertDrawingColorToMediaColor(System.Drawing.Color drawingColor)
        {
            return Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
        }
    }
}
