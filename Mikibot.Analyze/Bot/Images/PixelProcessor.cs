using OpenCvSharp;
using OpenCvSharp.XImgProc;

namespace Mikibot.Analyze.Bot.Images;

public static class PixelProcessor
{
    public static ReadOnlySpan<byte> Process(ReadOnlySpan<byte> input)
    {
        using var t = new ResourcesTracker();
        var srcImage = t.T(Cv2.ImDecode(input, ImreadModes.Color));
        
        const double maxLenght = 512D;
        var scaleSize = new Size(srcImage.Width, srcImage.Height);
        if (scaleSize.Width > maxLenght) scaleSize = new Size(maxLenght, scaleSize.Height * (maxLenght / scaleSize.Width)); 
        if (scaleSize.Height > maxLenght) scaleSize = new Size(scaleSize.Width * (maxLenght / scaleSize.Height), maxLenght); 

        var image = t.T(srcImage.Resize(scaleSize, interpolation: InterpolationFlags.Cubic));

        using var edges = t.T(image.Canny(128, 200));
        using var kernel = t.T(Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2)));
        Cv2.MorphologyEx(edges, edges, MorphTypes.Close, kernel, iterations: 1);
        using var dilatedEdges = t.T(edges.Dilate(kernel, iterations: 2));

        using var borderMask = t.T(edges.CvtColor(ColorConversionCodes.GRAY2BGR));
        using var originalColorEdges = t.T(image.BitwiseAnd(borderMask));
        using var darkerEdges = t.T(originalColorEdges * 0.25);

        darkerEdges.ToMat().CopyTo(image, edges);

        using var algorithm = t.T(SuperpixelSLIC.Create(image, regionSize: 8));

        algorithm.Iterate();
        algorithm.EnforceLabelConnectivity();

        var labelCount = algorithm.GetNumberOfSuperpixels();

        var pixelImage = t.T(t.T(Mat.Zeros(image.Size(), image.Type())).ToMat());
        var labels = t.NewMat();
        algorithm.GetLabels(labels);

        const double lowSize = 128;
        var smallSize = new Size(scaleSize.Width, scaleSize.Height);
        if (smallSize.Width > lowSize) smallSize = new Size(lowSize, smallSize.Height * (lowSize / smallSize.Width)); 
        if (smallSize.Height > lowSize) smallSize = new Size(smallSize.Width * (lowSize / smallSize.Height), lowSize); 
        using var smallImage = t.T(image.Resize(smallSize, interpolation: InterpolationFlags.Linear));
        var lowPixelImage = t.T(smallImage.Resize(scaleSize, interpolation: InterpolationFlags.Nearest));

        Enumerable.Range(0, labelCount).AsParallel().ForAll(label =>
        {
            using var mask = new Mat();
            Cv2.Compare(labels, label, mask, CmpType.EQ);
            pixelImage[Cv2.BoundingRect(mask)].SetTo(Cv2.Mean(lowPixelImage, mask));
        });

        using var disposePixelImage = pixelImage;
        using var disposeLabels = labels;
        using var disposeImage = image;
        using var disposeLowPixelImage = lowPixelImage;
        
        return pixelImage.ImEncode();
    }
}