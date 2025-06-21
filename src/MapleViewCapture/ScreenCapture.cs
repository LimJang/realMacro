using System;
using System.Drawing;
using System.Windows.Forms;

namespace MapleViewCapture
{
    public class ScreenCapture
    {
        public static Bitmap? CaptureWindow(IntPtr windowHandle)
        {
            try
            {
                // 창의 위치와 크기 가져오기
                if (!GetWindowRect(windowHandle, out RECT windowRect))
                    return null;

                int width = windowRect.Right - windowRect.Left;
                int height = windowRect.Bottom - windowRect.Top;

                // Graphics.CopyFromScreen 사용
                Bitmap bitmap = new Bitmap(width, height);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(windowRect.Left, windowRect.Top, 0, 0, 
                                          new Size(width, height), CopyPixelOperation.SourceCopy);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new Exception($"창 캡처 실패: {ex.Message}");
            }
        }

        public static Bitmap CaptureScreenRegion(int x, int y, int width, int height)
        {
            try
            {
                Bitmap bitmap = new Bitmap(width, height);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height), 
                                          CopyPixelOperation.SourceCopy);
                }
                return bitmap;
            }
            catch (Exception ex)
            {
                throw new Exception($"화면 영역 캡처 실패: {ex.Message}");
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
