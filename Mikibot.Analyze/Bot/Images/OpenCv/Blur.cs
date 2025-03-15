using OpenCvSharp;

namespace Mikibot.Analyze.Bot.Images.OpenCv;

public static class Blur
{ 
    public static Mat RadialBlur(this Mat src, Point axis, int iterateCount = 10)
    {
        
        var width = src.Cols;
        var height = src.Rows;
        var dest = new Mat(height, width, MatType.CV_32FC3);
        src.ConvertTo(dest, MatType.CV_32FC3);
        
        var center = new Point(width * axis.X, height * axis.X);
        Console.WriteLine($"center: w{center.X}/{width}/{axis.X}, h{center.Y}/{height}/{axis.Y}");
        Parallel.For(0, height, (y) =>
        {
            for (var x = 0; x < width; x++)
            {
                float t1 = 0, t2 = 0, t3 = 0;
                var r = MathF.Sqrt((y - center.Y) * (y - center.Y) + (x - center.X) * (x - center.X));
                var angle = MathF.Atan2(y - center.Y, x - center.X);

                for (var m = 0; m < iterateCount; m++)
                {
                    var t = r - m;
                    var tmr = t > 0 ? t : 0;
                    
                    var newX = (int)(tmr * MathF.Cos(angle) + center.X);
                    var newY = (int)(tmr * MathF.Sin(angle) + center.Y);
                    
                    if (newX < 0) newX = 0;
                    if (newX >= width) newX = width - 1;
                    if (newY < 0) newY = 0;
                    if (newY >= height) newY = height - 1;

                    var vec = src.At<Vec3b>(newY, newX);
                    t1 += vec[0];
                    t2 += vec[1];
                    t3 += vec[2];
                }

                dest.Set(y, x, new Vec3f(t1 / iterateCount, t2 / iterateCount, t3 / iterateCount));
            }
        });

        return dest;
    }
}