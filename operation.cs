using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;


namespace DIP
{
    /// <summary>
    /// 自定义矩阵类
    /// </summary>
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
        /// 求位图旋转后的坐标范围
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

        /// <summary>
        /// 缩小位图尺寸
        /// </summary>
        /// <param name="k1">高缩小幅度</param>
        /// <param name="k2">宽缩小幅度</param>
        private void minimize(double k1, double k2)
        {
            double di = 1 / k1, dj = 1 / k2;
            int nw = (int)Math.Round(bmp.Width * k2), 
                nh = (int)Math.Round(bmp.Height * k1);
            Bitmap bmp_ = new Bitmap(nw, nh);
            for(int i=0;i<nh;i++)
            {
                for (int j = 0; j < nw; j++)
                {
                    int sx = (int)Math.Round(di * i), ex = (int)Math.Round(di * (i+1));
                    int sy = (int)Math.Round(dj * j), ey = (int)Math.Round(dj * (j+1));
                    ey = Math.Min(bmp.Width, ey);
                    ex = Math.Min(bmp.Height, ex);
                    int rsum = 0, gsum = 0, bsum = 0;
                    for (int ii = sx;ii<ex;ii++)
                    {
                        for(int jj = sy; jj < ey; jj++)
                        {
                            Color col = bmp.GetPixel(jj, ii);
                            rsum += col.R;
                            gsum += col.G;
                            bsum += col.B;
                        }
                    }
                    Color color = Color.FromArgb(
                        rsum / ((ex - sx) * (ey - sy)),
                        gsum / ((ex - sx) * (ey - sy)),
                        bsum / ((ex - sx) * (ey - sy))
                        );
                    bmp_.SetPixel(j, i, color);
                }
            }
            img.Source = bmp2img(ref bmp_);
        }

        /// <summary>
        /// 放大位图尺寸
        /// </summary>
        /// <param name="k1">高扩大幅度</param>
        /// <param name="k2">宽扩大幅度</param>
        private void maximize(double k1, double k2)
        {
            //double di = 1 / k1, dj = 1 / k2;
            int nw = (int)Math.Round(bmp.Width * k2),
                nh = (int)Math.Round(bmp.Height * k1);
            Bitmap bmp_ = new Bitmap(nw, nh);

            ArrayList widx = new ArrayList(), hidx = new ArrayList();

            for(int i=0;i<bmp.Height;i++)
            {
                for(int j=0;j<bmp.Width;j++)
                {
                    int sx = (int)Math.Round(k1 * i), ex = (int)Math.Round(k1 * (i + 1)),
                        sy = (int)Math.Round(k2 * j), ey = (int)Math.Round(k2 * (j + 1));
                    if(i==bmp.Height-1 && j==bmp.Width-1)
                    {
                        bmp_.SetPixel(nw - 1, nh - 1, bmp.GetPixel(j, i));
                        widx.Add(nw - 1);
                        hidx.Add(nh - 1);
                    }
                    else if(i==bmp.Height-1)
                    {
                        bmp_.SetPixel(sy, nh - 1, bmp.GetPixel(j, i));
                        widx.Add(sy);
                    }
                    else if(j==bmp.Width-1)
                    {
                        bmp_.SetPixel(nw - 1, sx, bmp.GetPixel(j, i));
                        hidx.Add(sx);
                    }
                    else
                    {
                        bmp_.SetPixel(sy, sx, bmp.GetPixel(j, i));
                        // widx.Add(sy);
                    }                    
                }
            }
            widx.Sort();
            hidx.Sort();
            // MessageBox.Show(widx.IndexOf(2).ToString());
            for(int j = 0;j < nw;j++)
            {
                if (widx.IndexOf(j)>=0) 
                {
                    int prei = 0;
                    for(int i=1;i<nh;i++)
                    {
                        if(hidx.IndexOf(i) == -1)
                        {
                            int posti = (int)hidx[hidx.IndexOf(prei) + 1];
                            double d = (double)(i - prei) / (posti - prei);
                            Color post = bmp_.GetPixel(j, posti);
                            Color prev = bmp_.GetPixel(j, prei);

                            Color tmp =
                                Color.FromArgb(
                                    (int)Math.Round(prev.R + (post.R - prev.R) * d),
                                    (int)Math.Round(prev.G + (post.G - prev.G) * d),
                                    (int)Math.Round(prev.B + (post.B - prev.B) * d)
                                );
                            bmp_.SetPixel(j, i, tmp);
                        }
                        else                        
                            prei = i;
                        
                    }
                }
            }
            for(int i = 0;i < nh;i++)
            {
                    int prej = 0;
                    for (int j = 1; j < nw; j++)
                    {
                        if (widx.IndexOf(j) == -1)
                        {
                            int postj = (int)widx[widx.IndexOf(prej) + 1];
                            double d = (double)(j - prej) / (postj - prej);
                            Color post = bmp_.GetPixel(postj, i);
                            Color prev = bmp_.GetPixel(prej, i);
                            Color tmp =
                                Color.FromArgb(
                                    (int)Math.Round(prev.R + (post.R - prev.R) * d),
                                    (int)Math.Round(prev.G + (post.G - prev.G) * d),
                                    (int)Math.Round(prev.B + (post.B - prev.B) * d)
                                );
                            bmp_.SetPixel(j, i, tmp);
                        }
                    
                        else
                            prej = j;

                    }
                
            }
            img.Source = bmp2img(ref bmp_);
        }


        private void shear(double c, double b)
        {

            int[] range = { bmp.Height, 0, bmp.Width, 0 };
            int nx = (int)Math.Round(bmp.Height + c * bmp.Width),
                ny = (int)Math.Round(bmp.Width + b * bmp.Height);
            range[0] = Math.Min(range[0], nx);
            range[1] = Math.Max(range[1], nx);
            range[2] = Math.Min(range[2], ny);
            range[3] = Math.Max(range[3], ny);

            nx = (int)Math.Round(c * bmp.Width);
            ny = bmp.Width;
            range[0] = Math.Min(range[0], nx);
            range[1] = Math.Max(range[1], nx);
            range[2] = Math.Min(range[2], ny);
            range[3] = Math.Max(range[3], ny);

            nx = bmp.Height;
            ny = (int)Math.Round(b * bmp.Height);
            range[0] = Math.Min(range[0], nx);
            range[1] = Math.Max(range[1], nx);
            range[2] = Math.Min(range[2], ny);
            range[3] = Math.Max(range[3], ny);

            range[0] = Math.Min(range[0], 0);
            range[1] = Math.Max(range[1], 0);
            range[2] = Math.Min(range[2], 0);
            range[3] = Math.Max(range[3], 0);
            int xoff = 0, yoff = 0;
            if (range[0] < 0)
                xoff = -range[0];
            if (range[2] < 0)
                yoff = -range[2];
            Bitmap bmp_ = new Bitmap(range[3]-range[2]+1, range[1]-range[0]+1);
            for(int i=0;i<bmp.Height;i++)
            {
                for(int j=0;j<bmp.Width;j++)
                {
                    int xx = (int)Math.Round(i + c * j) + xoff, 
                        yy = (int)Math.Round(j + b * i) + yoff;
                    bmp_.SetPixel(yy, xx, bmp.GetPixel(j, i));
                }
            }
            img.Source = bmp2img(ref bmp_);
        }
    }
}