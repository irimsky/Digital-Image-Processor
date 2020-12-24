using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DIP.MainWindow;

namespace DIP
{
    public partial class FlagWindow : Form
    {

        Image img;
        List<Block> res;

        public FlagWindow(Image img, List<Block> lst)
        {
            InitializeComponent();
            this.Size = new Size(img.Width + 500, img.Height + 500);
            this.textBox1.Location = new Point(this.Width - 250, 5);
            this.textBox1.Size = new Size(200, this.Height - 100);
            this.img = img;
            res = lst;
        }

        private void ResPaint(object sender, PaintEventArgs e)
        {
            //获取Graphics对象
            Graphics g = e.Graphics;

            StringBuilder stringBuilder = new StringBuilder();

            g.DrawImage(img, 5, 5);
            for(int i=0;i<res.Count();i++)
            {
                int y = (res[i].bottom + res[i].up) / 2, x = (res[i].left + res[i].right) / 2;
                g.DrawString(string.Format("{0}", i + 1), new Font("New Timer", 10, FontStyle.Bold), Brushes.Gray, new PointF(x, y));
                stringBuilder.AppendLine(string.Format("标志：{0}", i + 1));
                stringBuilder.AppendLine(string.Format("周长：{0}", res[i].perimeter));
                stringBuilder.AppendLine(string.Format("面积：{0}", res[i].size));

                stringBuilder.AppendLine();

            }
            textBox1.Text = stringBuilder.ToString();
        }
    }
}
