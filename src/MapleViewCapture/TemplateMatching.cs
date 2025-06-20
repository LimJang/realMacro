using OpenCvSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MapleViewCapture
{
    public class TemplateMatching
    {
        public class MatchResult
        {
            public System.Drawing.Point Location { get; set; }
            public double Confidence { get; set; }
            public System.Drawing.Rectangle BoundingBox { get; set; }
            public System.Drawing.Point CenterPoint { get; set; }
            public bool IsMatch { get; set; }
        }

        public static MatchResult FindTemplate(Bitmap sourceImage, Bitmap templateImage, double threshold = 0.8)
        {
            try
            {
                // Bitmap을 OpenCV Mat으로 변환
                Mat sourceMat = BitmapToMat(sourceImage);
                Mat templateMat = BitmapToMat(templateImage);

                // 템플릿 매칭 수행
                Mat result = new Mat();
                Cv2.MatchTemplate(sourceMat, templateMat, result, TemplateMatchModes.CCoeffNormed);

                // 최고 매칭 위치 찾기
                Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc);

                // 결과 생성
                var matchResult = new MatchResult
                {
                    Location = new System.Drawing.Point(maxLoc.X, maxLoc.Y),
                    Confidence = maxVal,
                    IsMatch = maxVal >= threshold
                };

                if (matchResult.IsMatch)
                {
                    matchResult.BoundingBox = new System.Drawing.Rectangle(
                        maxLoc.X, maxLoc.Y, 
                        templateImage.Width, templateImage.Height);
                    
                    matchResult.CenterPoint = new System.Drawing.Point(
                        maxLoc.X + templateImage.Width / 2,
                        maxLoc.Y + templateImage.Height / 2);
                }

                // 리소스 정리
                sourceMat.Dispose();
                templateMat.Dispose();
                result.Dispose();

                return matchResult;
            }
            catch (Exception ex)
            {
                throw new Exception($"템플릿 매칭 실패: {ex.Message}");
            }
        }

        private static Mat BitmapToMat(Bitmap bitmap)
        {
            // Bitmap을 BGR 형식의 Mat으로 변환
            BitmapData bmpData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            Mat mat = new Mat(bitmap.Height, bitmap.Width, MatType.CV_8UC3, bmpData.Scan0, bmpData.Stride);
            
            // BGR로 변환 (OpenCV는 BGR 사용)
            Mat bgrMat = new Mat();
            Cv2.CvtColor(mat, bgrMat, ColorConversionCodes.RGB2BGR);
            
            bitmap.UnlockBits(bmpData);
            mat.Dispose();
            
            return bgrMat;
        }

        public static Bitmap MatToBitmap(Mat mat)
        {
            // Mat을 Bitmap으로 변환
            Mat rgbMat = new Mat();
            Cv2.CvtColor(mat, rgbMat, ColorConversionCodes.BGR2RGB);
            
            Bitmap bitmap = new Bitmap(rgbMat.Width, rgbMat.Height, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* src = (byte*)rgbMat.DataPointer;
                byte* dst = (byte*)bmpData.Scan0;
                
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        int srcIdx = y * (int)rgbMat.Step() + x * 3;
                        int dstIdx = y * bmpData.Stride + x * 3;
                        
                        dst[dstIdx] = src[srcIdx];         // R
                        dst[dstIdx + 1] = src[srcIdx + 1]; // G
                        dst[dstIdx + 2] = src[srcIdx + 2]; // B
                    }
                }
            }
            
            bitmap.UnlockBits(bmpData);
            rgbMat.Dispose();
            
            return bitmap;
        }
    }
}
