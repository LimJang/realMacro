using System;
using System.Windows.Forms;

namespace MapleViewCapture
{
    public partial class SimpleForm : Form
    {
        public SimpleForm()
        {
            this.Text = "테스트 폼";
            this.Size = new System.Drawing.Size(400, 300);
            
            Button testButton = new Button
            {
                Text = "테스트",
                Location = new System.Drawing.Point(50, 50),
                Size = new System.Drawing.Size(100, 30)
            };
            
            this.Controls.Add(testButton);
        }
    }
}
