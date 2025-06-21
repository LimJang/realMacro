using OpenCV = OpenCvSharp;
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
            return FindTemplateWithMode(sourceImage, templateImage, threshold, OpenCV.TemplateMatchModes.CCoeffNormed);
        }

        // 마스크를 사용한 템플릿 매칭 (배경 제거된 템플릿용)
        public static MatchResult FindTemplateWithMask(Bitmap sourceImage, Bitmap templateImage, Bitmap maskImage, double threshold = 0.8)
        {
            try
            {
                OpenCV.Mat sourceMat = BitmapToMat(sourceImage);
                OpenCV.Mat templateMat = BitmapToMat(templateImage);
                OpenCV.Mat maskMat = BitmapToMat(maskImage);
                
                // 마스크를 그레이스케일로 변환
                OpenCV.Mat grayMask = new OpenCV.Mat();
                OpenCV.Cv2.CvtColor(maskMat, grayMask, OpenCV.ColorConversionCodes.BGR2GRAY);

                OpenCV.Mat result = new OpenCV.Mat();
                OpenCV.Cv2.MatchTemplate(sourceMat, templateMat, result, OpenCV.TemplateMatchModes.CCoeffNormed, grayMask);

                OpenCV.Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out OpenCV.Point minLoc, out OpenCV.Point maxLoc);

                var matchResult = new MatchResult
                {
                    Location = new System.Drawing.Point(maxLoc.X, maxLoc.Y),
                    Confidence = maxVal,
                    IsMatch = maxVal >= threshold
                };

                if (matchResult.IsMatch)
                {
                    matchResult.BoundingBox = new System.Drawing.Rectangle(
                        maxLoc.X, maxLoc.Y, templateImage.Width, templateImage.Height);
                    matchResult.CenterPoint = new System.Drawing.Point(
                        maxLoc.X + templateImage.Width / 2,
                        maxLoc.Y + templateImage.Height / 2);
                }

                // 리소스 정리
                sourceMat.Dispose();
                templateMat.Dispose();
                maskMat.Dispose();
                grayMask.Dispose();
                result.Dispose();

                return matchResult;
            }
            catch (Exception ex)
            {
                throw new Exception($"마스크 템플릿 매칭 실패: {ex.Message}");
            }
        }

        // 다양한 매칭 모드를 지원하는 버전
        public static MatchResult FindTemplateWithMode(Bitmap sourceImage, Bitmap templateImage, double threshold = 0.8, OpenCV.TemplateMatchModes mode = OpenCV.TemplateMatchModes.CCoeffNormed)
        {
            try
            {
                OpenCV.Mat sourceMat = BitmapToMat(sourceImage);
                OpenCV.Mat templateMat = BitmapToMat(templateImage);

                OpenCV.Mat result = new OpenCV.Mat();
                OpenCV.Cv2.MatchTemplate(sourceMat, templateMat, result, mode);

                OpenCV.Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out OpenCV.Point minLoc, out OpenCV.Point maxLoc);

                // 매칭 모드에 따라 최적 값 선택
                double confidence;
                System.Drawing.Point location;
                
                switch (mode)
                {
                    case OpenCV.TemplateMatchModes.SqDiff:
                    case OpenCV.TemplateMatchModes.SqDiffNormed:
                        confidence = 1.0 - minVal; // 낮을수록 좋음 -> 높을수록 좋음으로 변환
                        location = new System.Drawing.Point(minLoc.X, minLoc.Y);
                        break;
                    default:
                        confidence = maxVal; // 높을수록 좋음
                        location = new System.Drawing.Point(maxLoc.X, maxLoc.Y);
                        break;
                }

                var matchResult = new MatchResult
                {
                    Location = location,
                    Confidence = confidence,
                    IsMatch = confidence >= threshold
                };

                if (matchResult.IsMatch)
                {
                    matchResult.BoundingBox = new System.Drawing.Rectangle(
                        location.X, location.Y, templateImage.Width, templateImage.Height);
                    matchResult.CenterPoint = new System.Drawing.Point(
                        location.X + templateImage.Width / 2,
                        location.Y + templateImage.Height / 2);
                }

                sourceMat.Dispose();
                templateMat.Dispose();
                result.Dispose();

                return matchResult;
            }
            catch (Exception ex)
            {
                throw new Exception($"템플릿 매칭 실패 ({mode}): {ex.Message}");
            }
        }

        // 다중 매칭 모드로 최적 결과 찾기
        public static MatchResult FindTemplateBestMode(Bitmap sourceImage, Bitmap templateImage, double threshold = 0.8)
        {
            var modes = new[]
            {
                OpenCV.TemplateMatchModes.CCoeffNormed,    // 현재 기본값
                OpenCV.TemplateMatchModes.CCorrNormed,     // 단순 상관관계
                OpenCV.TemplateMatchModes.SqDiffNormed     // 제곱 차이 (배경 변화에 강함)
            };

            MatchResult? bestResult = null;
            double bestConfidence = 0;

            foreach (var mode in modes)
            {
                try
                {
                    var result = FindTemplateWithMode(sourceImage, templateImage, threshold * 0.7, mode);
                    
                    if (result.Confidence > bestConfidence)
                    {
                        bestConfidence = result.Confidence;
                        bestResult = result;
                    }
                }
                catch
                {
                    // 해당 모드 실패 시 다음 모드로
                    continue;
                }
            }

            if (bestResult != null)
            {
                bestResult.IsMatch = bestResult.Confidence >= threshold;
                return bestResult;
            }

            return new MatchResult { IsMatch = false, Confidence = 0 };
        }

        // 사전변환된 템플릿을 사용하는 최적화된 버전
        public static MatchResult FindTemplateOptimized(Bitmap sourceImage, OpenCV.Mat preconvertedTemplate, double threshold = 0.8)
        {
            try
            {
                // 소스 이미지만 변환 (템플릿은 이미 변환됨)
                OpenCV.Mat sourceMat = BitmapToMat(sourceImage);

                // 템플릿 매칭 수행
                OpenCV.Mat result = new OpenCV.Mat();
                OpenCV.Cv2.MatchTemplate(sourceMat, preconvertedTemplate, result, OpenCV.TemplateMatchModes.CCoeffNormed);

                // 최고 매칭 위치 찾기
                OpenCV.Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out OpenCV.Point minLoc, out OpenCV.Point maxLoc);

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
                        preconvertedTemplate.Width, preconvertedTemplate.Height);
                    
                    matchResult.CenterPoint = new System.Drawing.Point(
                        maxLoc.X + preconvertedTemplate.Width / 2,
                        maxLoc.Y + preconvertedTemplate.Height / 2);
                }

                // 리소스 정리 (소스만, 템플릿은 재사용되므로 해제하지 않음)
                sourceMat.Dispose();
                result.Dispose();

                return matchResult;
            }
            catch (Exception ex)
            {
                throw new Exception($"최적화된 템플릿 매칭 실패: {ex.Message}");
            }
        }

        public static OpenCV.Mat BitmapToMat(Bitmap bitmap)
        {
            // Bitmap을 BGR 형식의 Mat으로 변환
            BitmapData bmpData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            OpenCV.Mat mat = new OpenCV.Mat(bitmap.Height, bitmap.Width, OpenCV.MatType.CV_8UC3, bmpData.Scan0, bmpData.Stride);
            
            // BGR로 변환 (OpenCV는 BGR 사용)
            OpenCV.Mat bgrMat = new OpenCV.Mat();
            OpenCV.Cv2.CvtColor(mat, bgrMat, OpenCV.ColorConversionCodes.RGB2BGR);
            
            bitmap.UnlockBits(bmpData);
            mat.Dispose();
            
            return bgrMat;
        }

        public static Bitmap MatToBitmap(OpenCV.Mat mat)
        {
            // Mat을 Bitmap으로 변환
            OpenCV.Mat rgbMat = new OpenCV.Mat();
            OpenCV.Cv2.CvtColor(mat, rgbMat, OpenCV.ColorConversionCodes.BGR2RGB);
            
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
