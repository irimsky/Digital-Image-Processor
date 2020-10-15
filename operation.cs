using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;


namespace DIP
{
    class Matrix
    { 
        public double[] v;
        int w { get; }
        int h { get; }

        public Matrix(int H, int W)
        {
            h = H;
            w = W;
            v = new double[h * w];
        }

        public double val(int x, int y)
        {
            return v[x * w + y];
        }

        public void setVal(int x, int y, double vv)
        {
            v[x*w+y] = vv;
        }

        Matrix Mul(ref Matrix b)
        {
            if (h != b.w)
                return null;
            Matrix res = new Matrix(h, b.w);
            for(int i=0;i<h;i++)
            {
                for(int j=0;j<b.w;j++)
                {
                    double sum = 0;
                    for(int k=0;k<w;k++)
                    {
                        sum += val(i, k) * b.val(k, j);
                    }
                    res.setVal(i, j, sum);
                }
            }
            return res;
        }

    }

    public partial class MainWindow : Window
    {

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// 更新bmp
        /// </summary>
        /// <param name="bmp_">新的图片</param>
        /// <returns>返回新位图的bitmapSource</returns>
        private BitmapSource bmp2img(ref Bitmap bmp_)
        {
            DeleteObject(bip);
            bmp = (Bitmap)bmp_.Clone();
            bip = bmp.GetHbitmap();
            IntPtr ip = bmp_.GetHbitmap();
            DeleteObject(ip);
            
            BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bip, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            return bitmapSource;
        }

        /// <summary>
        /// 获取打开的图片的信息
        /// </summary>
        /// <param name="path">图片路径</param>
        /// <returns></returns>
        private string getInfo(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open);
            if (fs == null)
                return "文件打开错误";
            string res = "";
            byte[] bmpdata = new byte[fs.Length];
            fs.Read(bmpdata, 0, bmpdata.Length);
            fs.Close();
            res += "位图文件名称：" + path + "\n";
            res += "位图文件类型：";
            res += (char)bmpdata[0];
            res += (char)bmpdata[1];
            res += "\n";
            res += string.Format("位图文件的大小：{0} \n", bmpdata.Length);
            res += string.Format("位图的宽度：{0}点\n", bmpdata[18] + (bmpdata[19] << 8) + (bmpdata[20] << 16) + (bmpdata[21] << 24));
            res += string.Format("位图的高度：{0}点\n", bmpdata[22] + (bmpdata[23] << 8) + (bmpdata[24] << 16) + (bmpdata[25] << 24));
            switch (bmpdata[16 + 12] + (bmpdata[16 + 13] << 8))
            {
                case 0: res += "JPEG图"; break;
                case 1: res += "单色图"; break;
                case 4: res += "16色图"; break;
                case 8: res += "256色图"; break;
                case 16: res += "64K图"; break;
                case 24: res += "16M真彩色图"; break;
                case 32: res += "4G真彩色图"; break;
                default: res += "单位像素位数未知"; break;
            }
            return res;
        }

        /// <summary>
        /// 求一个位图图旋转后的坐标范围
        /// </summary>
        /// <param name="h">高</param>
        /// <param name="w">宽</param>
        /// <param name="res">坐标范围,长度为4,前两个为x,后两个为y</param>
        private void MaxXY(int h, int w, double angle, ref int[] res)
        {
            double cos = Math.Cos(angle), sin = Math.Sin(angle);
            int xx, yy;

            // 对于(h, 0)
            xx = (int)Math.Round(cos * h, 0);
            yy = (int)Math.Round(sin * h, 0);
            res[0] = Math.Min(xx, res[0]);
            res[1] = Math.Max(xx, res[1]);
            res[2] = Math.Min(yy, res[2]);
            res[3] = Math.Max(yy, res[3]);
            // 对于(0, w)
            xx = (int)Math.Round(- sin * w, 0);
            yy = (int)Math.Round(cos * w, 0);
            res[0] = Math.Min(xx, res[0]);
            res[1] = Math.Max(xx, res[1]);
            res[2] = Math.Min(yy, res[2]);
            res[3] = Math.Max(yy, res[3]);
            // 对于(h, w)
            xx = (int)Math.Round(-sin * w + h * cos, 0);
            yy = (int)Math.Round(cos * w + sin * h, 0);
            res[0] = Math.Min(xx, res[0]);
            res[1] = Math.Max(xx, res[1]);
            res[2] = Math.Min(yy, res[2]);
            res[3] = Math.Max(yy, res[3]);
            // 对于(0,0)
            xx = yy = 0;
            res[0] = Math.Min(xx, res[0]);
            res[1] = Math.Max(xx, res[1]);
            res[2] = Math.Min(yy, res[2]);
            res[3] = Math.Max(yy, res[3]);
        }

        /// <summary>
        /// 将bmp旋转，用反变换方法
        /// </summary>
        /// <param name="angle">角度</param>
        private void rotate(double angle)
        {
            angle = angle / 180 * Math.PI;
            int h = bmp.Height, w = bmp.Width;
            int[] range = { h, 0, w, 0 };
            MaxXY(h-1, w-1, angle, ref range);
            // MessageBox.Show(string.Format("{0},{1},{2},{3}", range[0], range[1], range[2], range[3]));
            int offsetx = 0, offsety = 0; // 新图中x, y的偏移量（坐标取正）
            if (range[0] < 0)
                offsetx = - range[0];
            if (range[2] < 0)
                offsety = - range[2];
            int nw = (range[3] - range[2]), nh = (range[1] - range[0]);
            Bitmap bmp_ = new Bitmap(nw, nh);
            double cos = Math.Cos(angle), sin = Math.Sin(angle);
            for(int i=0;i<nh;i++)
            {
                for(int j=0;j<nw;j++)
                {
                    int x = i - offsetx, y = j - offsety;
                    int ox = (int)Math.Round(x * cos + y * sin),
                        oy = (int)Math.Round(-x * sin + y * cos);
                    if (ox < 0 || ox >= h || oy < 0 || oy >= w)
                        bmp_.SetPixel(j, i, Color.White);
                    else
                        bmp_.SetPixel(j, i, bmp.GetPixel(oy, ox));
                }
            }
            img.Source = bmp2img(ref bmp_);
        }

    }
}