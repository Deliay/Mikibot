using System.Runtime.Intrinsics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Mikibot.Analyze.Bot.Images;

public static class SimdRadialBlurImageProcessor
{
    public static Image<Rgba32> Apply(Image<Rgba32> image, float blurAmount)
    {
        int width = image.Width;
        int height = image.Height;
        var result = new Image<Rgba32>(width, height);

        int centerX = width / 2;
        int centerY = height / 2;

        for (int y = 0; y < height; y++)
        {
            // Process 8 pixels at a time using AVX
            for (int x = 0; x < width; x += 8)
            {
                // Prepare vectors for new pixel calculations
                var blurAmountVector = Vector256.Create(blurAmount);

                // Load vector data
                var srcPixels = LoadPixels(image, x, y);
                var outputPixels = new Vector256<float>[8];

                // Process each pixel in the vector
                for (int i = 0; i < 8; i++)
                {
                    // Calculate distance
                    float dx = x + i - centerX;
                    float dy = y - centerY;
                    float distance = MathF.Sqrt(dx * dx + dy * dy);
                    float factor = Math.Clamp(blurAmount * (distance / MathF.Max(centerX, centerY)), 0, 1);

                    // Source pixel position
                    int sourceX = (int)(x + dx * factor);
                    int sourceY = (int)(y + dy * factor);
                    sourceX = Math.Clamp(sourceX, 0, width - 1);
                    sourceY = Math.Clamp(sourceY, 0, height - 1);

                    outputPixels[i] = LoadPixel(image, sourceX, sourceY);
                }

                // Store the processed pixels
                StorePixels(result, x, y, outputPixels);
            }
        }

        // Replace the original image content with the result
        return result;
    }

    private static Vector256<float>[] LoadPixels(Image<Rgba32> image, int x, int y)
    {
        var pixels = new Vector256<float>[8];
        // Load pixels from the image into vectors
        for (int i = 0; i < 8; i++)
        {
            if (x + i < image.Width)
            {
                pixels[i] = LoadPixel(image, x + i, y);
            }
            else
            {
                pixels[i] = Vector256<float>.Zero; // Out of bounds
            }
        }
        return pixels;
    }

    private static Vector256<float> LoadPixel(Image<Rgba32> image, int x, int y)
    {
        var pixel = image[x, y];
        
        return Vector256.Create([pixel.R / 255f, pixel.G / 255f, pixel.B / 255f, pixel.A / 255f]);
    }

    private static void StorePixels(Image<Rgba32> image, int x, int y, Vector256<float>[] pixels)
    {
        for (int i = 0; i < 8; i++)
        {
            if (x + i < image.Width)
            {
                var pixel = pixels[i];
                image[x + i, y] = new Rgba32((byte)(pixel[0] * 255),
                    (byte)(pixel[1] * 255),
                    (byte)(pixel[2] * 255),
                    (byte)(pixel[3] * 255));
            }
        }
    }
    
}