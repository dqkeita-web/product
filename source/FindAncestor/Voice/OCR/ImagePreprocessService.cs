using OpenCvSharp;
using System;

namespace FindAncestor.Voice.OCR
{
    public class ImagePreprocessService
    {
        public Mat Preprocess(string path)
        {
            var src = Cv2.ImRead(path, ImreadModes.Grayscale);

            var bin = new Mat();
            Cv2.Threshold(src, bin, 0, 255, ThresholdTypes.Otsu);

            Cv2.MedianBlur(bin, bin, 3);

            return bin;
        }
    }
}