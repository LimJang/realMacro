using System;
using System.Windows.Forms;

namespace MapleViewCapture
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}\n\n{ex.StackTrace}", "프로그램 오류");
            }
        }
    }
}
