using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace MapleViewCapture
{
    public class AdvancedCapture
    {
        // Windows Graphics Capture API
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hGdiObj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight, 
            IntPtr hSrcDC, int xSrc, int ySrc, uint dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private const uint SRCCOPY = 0x00CC0020;

        public static Bitmap CaptureFullScreen()
        {
            try
            {
                // 전체 화면 크기 가져오기
                int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                // 데스크톱 DC 가져오기
                IntPtr desktopDC = GetWindowDC(GetDesktopWindow());
                IntPtr memoryDC = CreateCompatibleDC(desktopDC);
                IntPtr bitmap = CreateCompatibleBitmap(desktopDC, screenWidth, screenHeight);
                IntPtr oldBitmap = SelectObject(memoryDC, bitmap);

                // 전체 화면 캡처
                BitBlt(memoryDC, 0, 0, screenWidth, screenHeight, desktopDC, 0, 0, SRCCOPY);

                // Bitmap 객체로 변환
                Bitmap result = Image.FromHbitmap(bitmap);

                // 리소스 정리
                SelectObject(memoryDC, oldBitmap);
                DeleteObject(bitmap);
                DeleteDC(memoryDC);
                ReleaseDC(GetDesktopWindow(), desktopDC);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"전체 화면 캡처 실패: {ex.Message}");
            }
        }

        public static Bitmap CaptureWindowRegion(IntPtr windowHandle, Rectangle region)
        {
            try
            {
                // 창의 DC 가져오기
                IntPtr windowDC = GetWindowDC(windowHandle);
                IntPtr memoryDC = CreateCompatibleDC(windowDC);
                IntPtr bitmap = CreateCompatibleBitmap(windowDC, region.Width, region.Height);
                IntPtr oldBitmap = SelectObject(memoryDC, bitmap);

                // 지정된 영역 캡처
                BitBlt(memoryDC, 0, 0, region.Width, region.Height, 
                       windowDC, region.X, region.Y, SRCCOPY);

                // Bitmap 객체로 변환
                Bitmap result = Image.FromHbitmap(bitmap);

                // 리소스 정리
                SelectObject(memoryDC, oldBitmap);
                DeleteObject(bitmap);
                DeleteDC(memoryDC);
                ReleaseDC(windowHandle, windowDC);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"창 영역 캡처 실패: {ex.Message}");
            }
        }
    }
}
