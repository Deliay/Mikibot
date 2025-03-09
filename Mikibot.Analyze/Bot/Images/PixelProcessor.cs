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
        if (scaleSize.Width > 1000) scaleSize = new Size(maxLenght, scaleSize.Height * (maxLenght / scaleSize.Width)); 
        if (scaleSize.Height > 1000) scaleSize = new Size(scaleSize.Width * (maxLenght / scaleSize.Height), maxLenght); 

        using var image = t.T(srcImage.Resize(scaleSize, interpolation: InterpolationFlags.Cubic));

        using var edges = t.T(image.Canny(128, 200));
        using var kernel = t.T(Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2)));
        using var dilatedEdges = t.T(edges.Dilate(kernel, iterations: 2));

        using var borderMask = t.T(edges.CvtColor(ColorConversionCodes.GRAY2BGR));
        using var originalColorEdges = t.T(image.BitwiseAnd(borderMask));
        using var darkerEdges = t.T(originalColorEdges * 0.5);

        darkerEdges.ToMat().CopyTo(image, edges);

        using var algorithm = t.T(SuperpixelSLIC.Create(image, regionSize: 5));

        algorithm.Iterate(10);
        algorithm.EnforceLabelConnectivity();

        var labelCount = algorithm.GetNumberOfSuperpixels();

        using var pixelImage = t.T(t.T(Mat.Zeros(image.Size(), image.Type())).ToMat());

        using var labels = t.NewMat();
        algorithm.GetLabels(labels);

        Enumerable.Range(0, labelCount).AsParallel().ForAll(label =>
        {
            using var mask = new Mat();
            Cv2.Compare(labels, label, mask, CmpType.EQ);
            pixelImage[Cv2.BoundingRect(mask)].SetTo(Cv2.Mean(image, mask));
        });

        return pixelImage.ImEncode();
    }
}