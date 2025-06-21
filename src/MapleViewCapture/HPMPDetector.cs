using System;
using System.Drawing;

namespace MapleViewCapture
{
    public class HPMPDetector
    {
        // HP/MP 감지 결과 클래스
        public class StatusResult
        {
            public float Ratio { get; set; }          // 0.0 ~ 1.0 비율
            public int ColorPixels { get; set; }      // 색상 픽셀 수
            public int TotalPixels { get; set; }      // 전체 픽셀 수
            public bool IsLow { get; set; }           // 위험 상태 여부
            public string Status { get; set; } = "";  // 상태 문자열
            public Color StatusColor { get; set; }    // 상태 표시 색상
        }

        // HP 감지 (빨간색 기반)
        public static StatusResult DetectHP(Bitmap hpRoi, float threshold = 0.3f)
        {
            return DetectStatusBar(hpRoi, IsHPColor, threshold, "HP");
        }

        // MP 감지 (파란색 기반)
        public static StatusResult DetectMP(Bitmap mpRoi, float threshold = 0.2f)
        {
            return DetectStatusBar(mpRoi, IsMPColor, threshold, "MP");
        }

        // 핵심 가로 스캔라인 분석 메서드
        private static StatusResult DetectStatusBar(Bitmap roi, Func<Color, bool> colorChecker, float threshold, string type)
        {
            if (roi == null || roi.Width == 0 || roi.Height == 0)
            {
                return new StatusResult
                {
                    Ratio = 0f,
                    Status = $"{type}: ROI 없음",
                    StatusColor = Color.Gray
                };
            }

            // 1단계: 중앙 라인 결정
            int centerY = roi.Height / 2;
            
            // 2단계: 왼쪽부터 스캔하면서 연속된 색상 픽셀 찾기
            int colorPixelLength = 0;
            bool colorStarted = false;

            for (int x = 0; x < roi.Width; x++)
            {
                Color pixel = roi.GetPixel(x, centerY);
                
                if (colorChecker(pixel))
                {
                    colorStarted = true;
                    colorPixelLength++;
                }
                else if (colorStarted)
                {
                    // 연속된 색상이 끝나면 중단 (바의 끝)
                    break;
                }
            }

            // 3단계: 비율 계산
            float ratio = (float)colorPixelLength / roi.Width;

            // 4단계: 상태 색상 결정
            Color statusColor = GetStatusColor(ratio, type == "HP");

            // 5단계: 결과 생성
            return new StatusResult
            {
                Ratio = ratio,
                ColorPixels = colorPixelLength,
                TotalPixels = roi.Width,
                IsLow = ratio < threshold,
                Status = $"{type}: {ratio:P0} ({colorPixelLength}/{roi.Width}px)",
                StatusColor = statusColor
            };
        }

        // HP 색상 감지 (빨간색) - 후하게 조정
        private static bool IsHPColor(Color color)
        {
            return color.R >= 150 &&           // 빨간색 강도
                   color.G <= 100 &&           // 녹색 허용
                   color.B <= 100 &&           // 파란색 허용
                   (color.R - color.G) >= 80;  // 차이 완화
        }

        // MP 색상 감지 (파란색) - 후하게 조정
        private static bool IsMPColor(Color color)
        {
            return color.R <= 50 &&            // 빨간색 허용
                   color.G >= 50 &&            // 녹색 범위
                   color.B >= 150 &&           // 파란색 범위
                   (color.B - color.R) >= 100; // 파란색 우세
        }

        // 상태에 따른 색상 반환
        private static Color GetStatusColor(float ratio, bool isHP)
        {
            if (isHP)
            {
                if (ratio <= 0.15f) return Color.DarkRed;      // 15% - 매우 위험
                if (ratio <= 0.30f) return Color.Red;          // 30% - 위험
                if (ratio <= 0.60f) return Color.Orange;       // 60% - 보통
                return Color.Green;                             // 안전
            }
            else // MP
            {
                if (ratio <= 0.10f) return Color.DarkBlue;     // 10% - 매우 위험
                if (ratio <= 0.20f) return Color.Blue;         // 20% - 위험
                if (ratio <= 0.50f) return Color.CornflowerBlue; // 50% - 보통
                return Color.LightBlue;                         // 안전
            }
        }
    }
}
