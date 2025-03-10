using OpenCvSharp;
using OpenCvSharp.XImgProc;

namespace Mikibot.Analyze.Bot.Images;

public static class OpenCvUtils
{
    public static Mat ResizeMax(this Mat src, double maxSize,
        InterpolationFlags interpolation = InterpolationFlags.Cubic)
    {
        var scaleSize = new Size(src.Width, src.Height);
        if (scaleSize.Width > maxSize) scaleSize = new Size(maxSize, scaleSize.Height * (maxSize / scaleSize.Width)); 
        if (scaleSize.Height > maxSize) scaleSize = new Size(scaleSize.Width * (maxSize / scaleSize.Height), maxSize); 

        return src.Resize(scaleSize, interpolation: InterpolationFlags.Cubic);
    }

    public static Mat ScaleTo(this Mat dest, Mat src, 
        InterpolationFlags interpolation = InterpolationFlags.Cubic)
    {
        return dest.Resize(src.Size(), interpolation: InterpolationFlags.Nearest);
    }

    public static (Mat coloredEdge, Mat originalEdge) GetMorphologyCannyEdges(this Mat dest,
        MorphShapes morphShape = MorphShapes.Rect, int morphSize = 2, MorphTypes morphType = MorphTypes.Close,
        int thresholdMin = 128, int thresholdMax = 200,
        int morphIteration = 1, int dilateIteration = 2,
        double edgeColorMultiply = 0.7)
    {
        var edges = dest.Canny(thresholdMin, thresholdMax);
        using var kernel = Cv2.GetStructuringElement(morphShape, new Size(morphSize, morphSize));
        Cv2.MorphologyEx(edges, edges, morphType, kernel, iterations: morphIteration);
        using var dilatedEdges = edges.Dilate(kernel, iterations: dilateIteration);

        using var borderMask = edges.CvtColor(ColorConversionCodes.GRAY2BGR);
        using var originalColorEdges = dest.BitwiseAnd(borderMask);
        var darkerEdges = originalColorEdges * edgeColorMultiply;

        return (darkerEdges, edges);
    }

    public delegate void SlicIterator(Mat labels, int label, Mat dest);
    
    public static Mat IterateSlic(this Mat src, SlicIterator iterator, 
        int regionSize = 8, SLICType slicAlgorithm = SLICType.SLICO, float ruler = 10)
    {
        using var slic = SuperpixelSLIC.Create(src, slicAlgorithm, regionSize, ruler);

        slic.Iterate();
        slic.EnforceLabelConnectivity();

        var labelCount = slic.GetNumberOfSuperpixels();

        var dest = Mat.Zeros(src.Size(), src.Type()).ToMat();
        var labels = new Mat();
        slic.GetLabels(labels);

        Enumerable.Range(0, labelCount).AsParallel().ForAll(InnerIterator);

        using var disposeLabels = labels;

        return dest;

        void InnerIterator(int label) => iterator(labels, label, dest);
    }

    public static SlicIterator FillLabels(Mat src)
    {
        return (labels, label, dest) =>
        {
            using var mask = new Mat();
            Cv2.Compare(labels, label, mask, CmpType.EQ);
            dest[Cv2.BoundingRect(mask)].SetTo(Cv2.Mean(src, mask));
        };
    }
    
    public static ReadOnlySpan<byte> Process(ReadOnlySpan<byte> input)
    {
        using var t = new ResourcesTracker();
        var srcImage = t.T(Cv2.ImDecode(input, ImreadModes.Color));

        var image = t.T(srcImage.ResizeMax(512));

        var (coloredEdge, originalEdge) = image.GetMorphologyCannyEdges();
        t.T(coloredEdge).CopyTo(image, t.T(originalEdge));

        using var smallImage = image.ResizeMax(128, interpolation: InterpolationFlags.Linear);
        using var lowPixelImage = smallImage.ScaleTo(image, interpolation: InterpolationFlags.Nearest);

        using var pixelImage = IterateSlic(image, FillLabels(lowPixelImage));
        
        return pixelImage.ImEncode();
    }
}