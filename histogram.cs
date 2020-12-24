using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DIP
{
    public partial class HistForm : Form
    {
        //利用构造函数实现窗体之间的数据传递
        public HistForm(Bitmap bmp)
        {
            InitializeComponent();

            //把主窗体的图像数据传递给从窗体
            bmpHist = bmp;
            //灰度级计数
            countPixel = new int[256];  //8位可表示256个灰度级
        }

        //图像数据
        private Bitmap bmpHist;
        //灰度等级
        private int[] countPixel;

        /// <summary>
        /// 计算各个灰度级所具有的像素个数
        /// </summary>
        private void HistForm_Load(object sender, EventArgs e)
        {
            int bytes = bmpHist.Width * bmpHist.Height;
            byte[] grayValues = new byte[bytes];
            
            //灰度等级数组清零
            Array.Clear(countPixel, 0, 256);
            //计算各个灰度级的像素个数
            for (int i=0;i<bmpHist.Width;i++)
                for(int j=0;j<bmpHist.Height;j++)
                {
                    Color color = bmpHist.GetPixel(i, j);
                    byte temp = (byte)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
                    countPixel[temp]++;
                }
        }


        /// <summary>
        /// 绘制直方图
        /// </summary>
        private void HistForm_Paint(object sender, PaintEventArgs e)
        {
            //获取Graphics对象
            Graphics g = e.Graphics;
            //创建一个宽度为1的黑色钢笔
            Pen curPen = new Pen(Brushes.Black, 1);

            //绘制坐标轴
            g.DrawLine(curPen, 50, 240, 320, 240);//横坐标
            g.DrawLine(curPen, 50, 240, 50, 30);//纵坐标

            //绘制并标识坐标刻度
            g.DrawLine(curPen, 100, 240, 100, 242);
            g.DrawLine(curPen, 150, 240, 150, 242);
            g.DrawLine(curPen, 200, 240, 200, 242);
            g.DrawLine(curPen, 250, 240, 250, 242);
            g.DrawLine(curPen, 300, 240, 300, 242);
            g.DrawString("0", new Font("New Timer", 8), Brushes.Black, new PointF(46, 242));
            g.DrawString("50", new Font("New Timer", 8), Brushes.Black, new PointF(92, 242));
            g.DrawString("100", new Font("New Timer", 8), Brushes.Black, new PointF(139, 242));
            g.DrawString("150", new Font("New Timer", 8), Brushes.Black, new PointF(189, 242));
            g.DrawString("200", new Font("New Timer", 8), Brushes.Black, new PointF(239, 242));
            g.DrawString("250", new Font("New Timer", 8), Brushes.Black, new PointF(289, 242));
            g.DrawLine(curPen, 48, 40, 50, 40);
            g.DrawString("0", new Font("New Timer", 8), Brushes.Black, new PointF(34, 234));

            //绘制直方图
            double temp = 0;
            int bytes = bmpHist.Width * bmpHist.Height;
            for (int i = 0; i < 256; i++)
            { 
                //纵坐标长度
                temp = 800.0 * countPixel[i] / bytes;
                g.DrawLine(curPen, 50 + i, 240, 50 + i, 240 - (int)temp);
            }
            //释放对象
            curPen.Dispose();
        }

    }
}