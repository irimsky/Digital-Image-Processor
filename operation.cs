using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;


namespace DIP
{
    
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
            res += "■ 位图文件名称：" + path + "\n";
            res += "■ 位图文件类型：";
            res += (char)bmpdata[0];
            res += (char)bmpdata[1];
            res += "\n";
            res += string.Format("■ 位图文件的大小：{0} \n", bmpdata.Length);
            res += string.Format("■ 位图的宽度：{0}点\n", bmpdata[18] + (bmpdata[19] << 8) + (bmpdata[20] << 16) + (bmpdata[21] << 24));
            res += string.Format("■ 位图的高度：{0}点\n", bmpdata[22] + (bmpdata[23] << 8) + (bmpdata[24] << 16) + (bmpdata[25] << 24));
            res += "■ ";
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
        /// 旋转
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
                        //bmp_.SetPixel(j, i, Color.White);
                        ;
                    else
                        bmp_.SetPixel(j, i, bmp.GetPixel(oy, ox));
                }
            }
            img.Source = bmp2img(ref bmp_);
        }

        /// <summary>
        /// 缩小
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
        /// 放大
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
                    }                    
                }
            }
            widx.Sort();
            hidx.Sort();
            
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

        /// <summary>
        /// 错切
        /// </summary>
        /// <param name="c"></param>
        /// <param name="b"></param>
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

        /// <summary>
        /// 将图片灰度化
        /// </summary>
        private void Gray()
        {
            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);
            for(int i=0;i<bmp.Width;i++)
            {
                for(int j=0;j<bmp.Height;j++)
                {
                    Color c = bmp.GetPixel(i, j);
                    int tmp = (int)(0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
                    bmp_.SetPixel(i, j, Color.FromArgb(tmp, tmp, tmp));
                }
            }
            img.Source = bmp2img(ref bmp_);
            HistForm histForm = new HistForm(bmp);
            histForm.Show();
        }

        /// <summary>
        /// 拓展压缩线性灰度变化
        /// </summary>
        private void LinerGray(int a, int b, int c, int d)
        {
           
            double alpha = (double)c/a;
            double beta = (double)(d-c)/(b-a);
            double gama = (double)(255-d)/(255-b);
            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);
            for(int i=0;i<bmp.Width;i++)
            {
                for(int j=0;j<bmp.Height;j++)
                {
                    Color color = bmp.GetPixel(i, j);
                    int nc;
                    int tmp = (int)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
                    if(tmp <= a)
                    {
                        nc = (int)(alpha * tmp);
                    }
                    else if(tmp >= b)
                    {
                        nc = (int)(d + gama * (tmp - b));
                    }
                    else
                    {
                        nc = (int)(c + beta * (tmp - a));
                    }

                    bmp_.SetPixel(i, j, Color.FromArgb(nc, nc, nc));
                }
            }
            img.Source = bmp2img(ref bmp_);
            HistForm histForm = new HistForm(bmp);
            histForm.Show();
        }

        /// <summary>
        /// 将灰度图像的灰度直方图均衡化
        /// </summary>
        private void Equalization()
        {
            int[] grayValue = new int[256];
            Array.Clear(grayValue, 0, 256);
            for (int i=0;i<bmp.Width;i++)
            {
                for(int j=0;j<bmp.Height;j++)
                {
                    Color color = bmp.GetPixel(i, j);
                    int tmp = (int)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
                    grayValue[tmp]++;
                }
            }
            int sum = bmp.Width * bmp.Height, cnt = 0;
            int[] hp= new int[256];
            for(int i=0;i<256;i++)
            {
                cnt += grayValue[i];
                hp[i] = (int)Math.Round(cnt * 255.0 / sum);
            }
            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    Color color = bmp.GetPixel(i, j);
                    int tmp = (int)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
                    bmp_.SetPixel(i, j, Color.FromArgb(hp[tmp], hp[tmp], hp[tmp]));
                }
            }
            img.Source = bmp2img(ref bmp_);
            HistForm histForm = new HistForm(bmp);
            histForm.Show();
        }

        /// <summary>
        /// 生成随机种子
        /// </summary>
        int GetRandomSeed()
        {
            byte[] bytes = new byte[4];
            System.Security.Cryptography.RNGCryptoServiceProvider rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            rng.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        /// 为图片添加高斯噪声
        /// </summary>
        private void GaussNoise(int k)
        {
            Random ran = new Random(GetRandomSeed());
            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);
            for(int i=0;i<bmp.Width;i++)
            {
                for(int j=0;j<bmp.Height;j++)
                {
                    double r1 = ran.NextDouble();
                    double r2 = ran.NextDouble();
                    double result = Math.Sqrt((-2) * Math.Log(r2)) * Math.Sin(2 * Math.PI * r1);
                    result *= k;
                    Color c = bmp.GetPixel(i, j);

                    int rr = (int)(c.R + result),
                        gg = (int)(c.G + result),
                        bb = (int)(c.B + result);
                    if (rr > 255) rr = 255;
                    else if (rr < 0) rr = 0;
                    if (gg > 255) gg = 255;
                    else if (gg < 0) gg = 0;
                    if (bb > 255) bb = 255;
                    else if (bb < 0) bb = 0;
                    bmp_.SetPixel(i, j, Color.FromArgb(c.A, rr, gg, bb));
                }
            }
            img.Source = bmp2img(ref bmp_);
        }

        /// <summary>
        /// 为图片添加椒盐噪声
        /// </summary>
        /// <param name="SNR">信噪比</param>
        /// <param name="pa">图片为暗点的概率</param>
        private void SaltNoise(double SNR, double pa)
        {
            // 噪声点的数量
            int NP = (int)(bmp.Width * bmp.Height * (1 - SNR));
            Bitmap bmp_ = new Bitmap(bmp);
            Random rand = new Random();
            for (int i=0;i<NP;i++)
            {
                int r = rand.Next(0, bmp.Height), c = rand.Next(0, bmp.Width);
                double prob = rand.NextDouble();
                if(prob > pa)
                {
                    bmp_.SetPixel(c, r, Color.FromArgb(255, 255, 255));
                }
                else
                {
                    bmp_.SetPixel(c, r, Color.FromArgb(0, 0, 0));
                }
            }
            img.Source = bmp2img(ref bmp_);
        }

        /// <summary>
        /// 均值滤波
        /// </summary>
        private void EvenFilter()
        {
            int[,] dir = new int[,] { { -1, -1 }, { -1, 0 }, { -1, 1 },
                                      { 0, -1 }, { 0, 1 },
                                      { 1, -1 },  {  1, 0 }, { 1,  1}
            };
            Bitmap bmp_ = new Bitmap(bmp);
            for(int i=1;i<bmp.Width-1;i++)
            {
                for(int j=1;j<bmp.Height-1;j++)
                {
                    Color now = bmp.GetPixel(i, j);
                    int rsum = now.R, gsum = now.G, bsum = now.B;
                    for(int d=0;d<8;d++)
                    {
                        int xx = i + dir[d, 0], yy = j + dir[d, 1];
                        Color c = bmp.GetPixel(xx, yy);
                        rsum += c.R; gsum += c.G; bsum += c.B;
                    }
                    bmp_.SetPixel(i, j, Color.FromArgb(rsum / 9, gsum / 9, bsum / 9));
                }
            }
            img.Source = bmp2img(ref bmp_);
        }

        /// <summary>
        /// 中值滤波
        /// </summary>
        private void MidFilter()
        {
            int[,] dir = new int[,] { { -1, -1 }, { -1, 0 }, { -1, 1 },
                                      { 0, -1 }, { 0, 1 },
                                      { 1, -1 },  {  1, 0 }, { 1,  1}
            };
            Bitmap bmp_ = new Bitmap(bmp);
            for (int i = 1; i < bmp.Width - 1; i++)
            {
                for (int j = 1; j < bmp.Height - 1; j++)
                {
                    Color now = bmp.GetPixel(i, j);
                    ArrayList rarr = new ArrayList(),
                        garr = new ArrayList(),
                        barr = new ArrayList();
                    for (int d = 0; d < 8; d++)
                    {
                        int xx = i + dir[d, 0], yy = j + dir[d, 1];
                        Color c = bmp.GetPixel(xx, yy);
                        rarr.Add(c.R);
                        garr.Add(c.G);
                        barr.Add(c.B);
                    }
                    rarr.Sort();
                    garr.Sort();
                    barr.Sort();
                    bmp_.SetPixel(i, j, Color.FromArgb(Convert.ToInt32(rarr[4]),
                        Convert.ToInt32(garr[4]),
                        Convert.ToInt32(barr[4])));
                }
            }
            img.Source = bmp2img(ref bmp_);
        }

        /// <summary>
        /// 二值化
        /// </summary>
        /// <param name="p">二值化阈值</param>
        private void Binarize(int p)
        {
            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    Color c = bmp.GetPixel(i, j);
                    int tmp = (int)(0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
                    if(tmp > p)
                        bmp_.SetPixel(i, j, Color.FromArgb(255, 255, 255));
                    else
                        bmp_.SetPixel(i, j, Color.FromArgb(0, 0, 0));

                }
            }
            img.Source = bmp2img(ref bmp_);
        }

        /// <summary>
        /// 二值图像去噪
        /// </summary>
        private void BinaryFilter()
        {
            int[,] dir = new int[,] { { -1, -1 }, { -1, 0 }, { -1, 1 },
                                      { 0, -1 }, { 0, 1 },
                                      { 1, -1 },  {  1, 0 }, { 1,  1}
            };
            Bitmap bmp_ = new Bitmap(bmp);
            for (int i = 1; i < bmp.Width - 1; i++)
            {
                for (int j = 1; j < bmp.Height - 1; j++)
                {
                    Color now = bmp.GetPixel(i, j);
                    double sum = 0;
                    for (int d = 0; d < 8; d++)
                    {
                        int xx = i + dir[d, 0], yy = j + dir[d, 1];
                        Color c = bmp.GetPixel(xx, yy);
                        sum += c.R;
                    }
                    sum /= 8;
                    if(Math.Abs(now.R - sum) > 127.5)
                        bmp_.SetPixel(i, j, Color.FromArgb(255-now.R, 255-now.R, 255-now.R));
                }
            }
            img.Source = bmp2img(ref bmp_);
        }

        /// <summary>
        /// 二值图像消除孤立点（四连通）
        /// </summary>
        private void BinIsoRemove()
        {
            int[,] dir = new int[,] { { -1, 0 }, { 0, -1 }, { 0, 1 }, { 1, 0 } };
            Bitmap bmp_ = new Bitmap(bmp);
            for (int i = 1; i < bmp.Width-1; i++)
            {
                for (int j = 1; j < bmp.Height-1; j++)
                {
                    Color now = bmp.GetPixel(i, j);
                    if (now.R == 255)
                        continue;
                    bool flag = false;
                    for (int d = 0; d < 4; d++)
                    {
                        int xx = i + dir[d, 0], yy = j + dir[d, 1];                       
                        Color c = bmp.GetPixel(xx, yy);
                        if (c.R == 0)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                        bmp_.SetPixel(i, j, Color.White);
                }
            }
            img.Source = bmp2img(ref bmp_);
        }

        /// <summary>
        /// 灰度最小方差均值滤波器
        /// 此为灰度最小方差的均值滤波器：在去噪能力上弱于传统的均值、中值滤波，但在保留图像边缘和细节能力方面要强于前者。
        /// </summary>
        private void LSMF()
        {
            int[][,] dir = new int[9][,];
            dir[0] = new int[,] {
               {-1, -1}, {-1, 0}, {-1, 1}, {0, -1}, {0, 0}, {0, 1}, {1, -1}, {1, 0}, {1, 1}
            };
            dir[1] = new int[,] {
               {-1, -2}, {-1, -1}, {0, -2}, {0, -1}, {0, 0}, {1, -2}, {1, -1}
            };
            dir[2] = new int[,] {
               {-2, -1}, {-2, 0}, {-2, 1}, {-1, -1}, {-1, 0}, {-1, 1}, {0, 0}
            };
            dir[3] = new int[,] {
               {-1, 1}, {-1, 2}, {0, 0}, {0, 1}, {0, 2}, {1, 1}, {1, 2}
            };
            dir[4] = new int[,] {
               {0, 0}, {1, -1}, {1, 0}, {1, 1}, {2, -1}, {2, 0}, {2, 1}
            };
            dir[5] = new int[,] {
                 {-2, -2}, {-2, -1}, {-1, -2}, {-1, -1}, {-1, 0}, {0, -1}, {0, 0}
            };
            dir[6] = new int[,] {
                {-2, 1}, {-2, 2}, {-1, 0}, {-1, 1}, {-1, 2}, {0, 0}, {0, 1}
            };
            dir[7] = new int[,] {
               {0, 0}, {0, 1}, {1, 0}, {1, 1}, {1, 2}, {2, 1}, {2, 2}
            };
            dir[8] = new int[,] {
               {0, -1}, {0, 0}, {1, -2}, {1, -1}, {1, 0}, {2, -2}, {2, -1}
            };

            Bitmap bmp_ = new Bitmap(bmp);
            for (int i = 2; i < bmp.Width - 2; i++)
            {
                for (int j = 2; j < bmp.Height - 2; j++)
                {
                    double minsq = 1000000000;
                    int minavg = 0;
                    for (int d = 0; d < 9; d++)
                    {
                        double avg = 0, sq_dif = 0;
                        List<int> arr = new List<int>();
                        for (int dd=0;dd<dir[d].Length;dd+=2)
                        {
                            Color c = bmp.GetPixel(i + dir[d][dd / 2, 0], j + dir[d][dd / 2, 1]);
                            arr.Add(c.R);
                        }
                        avg = arr.Average();
                        foreach(var r in arr)
                        {
                            sq_dif += (r - avg) * (r - avg);
                        }
                        if (sq_dif < minsq)
                        {
                            minsq = sq_dif;
                            minavg = (int)avg;
                        }
                    }
                    bmp_.SetPixel(i, j, Color.FromArgb(minavg, minavg, minavg));
                }
            }
            img.Source = bmp2img(ref bmp_);


        }

        /// <summary>
        /// KNN中值平滑滤波
        /// </summary>
        /// <param name="m">模板大小（奇数)</param>
        /// <param name="K">K</param>
        private void KNNFilter(int m, int K)
        {
            if (m >= bmp.Width / 2 || K > m * m || m%2!=1)
                return;
            Bitmap bmp_ = new Bitmap(bmp);
            int kernel = m / 2;
            List<Tuple<int, int>> sort_list = new List<Tuple<int, int>>(m*m);
            for (int i=kernel;i<bmp.Width-kernel;i++)
            {
                for(int j=kernel;j<bmp.Height-kernel;j++)
                {
                    Color now = bmp.GetPixel(i, j);
                    sort_list.Clear();
                    for(int ii=i-kernel;ii<=i+kernel;ii++)
                    {
                        for(int jj=j-kernel;jj<=j+kernel;jj++)
                        {
                            Color tmp = bmp.GetPixel(ii, jj);
                            sort_list.Add(
                                new Tuple<int, int>(
                                    Math.Abs(now.R - tmp.R),
                                    tmp.R
                                )
                            );
                        }
                    }

                    sort_list.Sort((x, y) =>
                    {
                        return x.Item1 - y.Item1;
                    });
                    int sum = 0;
                    
                    sum = sort_list[K / 2].Item2;
                    bmp_.SetPixel(i, j, Color.FromArgb(sum, sum, sum));
                }
            }
            img.Source = bmp2img(ref bmp_);
        }
        
        /// <summary>
        /// 双向一次微分锐化 (给定灰度值)
        /// </summary>
        private void BidirectionalFirstOrderDifferential()
        {
            Bitmap bmp_ = new Bitmap(bmp);
            int[,] gray = new int[bmp.Width, bmp.Height];
            for(int i=0;i<bmp.Width;i++)
            {
                for(int j=0;j<bmp.Height;j++)
                {
                    gray[i, j] = bmp.GetPixel(i, j).R;
                }
            }
            for (int i = 1; i < bmp.Width; i++)
            {
                for (int j = 1; j < bmp.Height; j++)
                {
                    double grad = Math.Sqrt(
                        (gray[i, j] - gray[i - 1, j]) * (gray[i, j] - gray[i - 1, j]) +
                        (gray[i, j] - gray[i, j - 1]) * (gray[i, j] - gray[i, j - 1]));
                    if(grad >= 30)
                    {
                        bmp_.SetPixel(i, j, Color.FromArgb(255,255,255));
                    }
                    //else
                    //{
                        //bmp_.SetPixel(i, j, Color.FromArgb(0, 0, 0));
                    //}
                }
            }

            img.Source = bmp2img(ref bmp_);
        }

        /// <summary>
        /// Roberts算子锐化
        /// </summary>
        private void Roberts()
        {
            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);
            for(int i=0;i<bmp.Width;i++)
            {
                for(int j=0;j<bmp.Height;j++)
                {
                    if(i==bmp.Width-1 || j==bmp.Height-1)
                        bmp_.SetPixel(i, j, Color.FromArgb(0, 0, 0));
                    else
                    {
                        int gray1 = bmp.GetPixel(i, j).R, gray2 = bmp.GetPixel(i+1, j).R,
                            gray3 = bmp.GetPixel(i, j+1).R, gray4 = bmp.GetPixel(i+1, j+1).R;
                        int newGray = (int)Math.Sqrt((gray4 - gray1) * (gray4 - gray1) + (gray3 - gray2) * (gray3 - gray2));
                        //newGray += bmp.GetPixel(i, j).R;
                        if (newGray > 255)
                            newGray = 255;
                        if (newGray < 0)
                            newGray = 0;
                        bmp_.SetPixel(i, j, Color.FromArgb(newGray, newGray, newGray));
                    }
                }
            }
            img.Source = bmp2img(ref bmp_);
        }

        /// <summary>
        /// Sobel算子锐化
        /// </summary>
        private void Sobel()
        {
            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    if (i==0 || j==0 || i == bmp.Width - 1 || j == bmp.Height - 1)
                        bmp_.SetPixel(i, j, Color.FromArgb(0, 0, 0));
                    else
                    {
                        int gray00 = bmp.GetPixel(i, j).R, gray10 = bmp.GetPixel(i + 1, j).R,
                            gray01 = bmp.GetPixel(i, j + 1).R, gray11 = bmp.GetPixel(i + 1, j + 1).R,
                            gray22 = bmp.GetPixel(i-1, j-1).R, gray21 = bmp.GetPixel(i-1, j).R, gray12 = bmp.GetPixel(i, j-1).R,
                            gray02 = bmp.GetPixel(i, j-1).R, gray20 = bmp.GetPixel(i-1, j).R;
                        int dx = (gray21 + 2 * gray01 + gray11) - (gray22 + 2 * gray02 + gray12);
                        int dy = (gray22 + 2 * gray20 + gray21) - (gray12 + 2 * gray10 + gray11);
                        int newGray = (int)Math.Sqrt(dx*dx+dy*dy);
                        // newGray += bmp.GetPixel(i, j).R;
                        if (newGray > 255)
                            newGray = 255;
                        if (newGray < 0)
                            newGray = 0;
                        bmp_.SetPixel(i, j, Color.FromArgb(newGray, newGray, newGray));
                    }
                }
            }
            img.Source = bmp2img(ref bmp_);
        }

        /// <summary>
        /// Laplacian算子锐化
        /// </summary>
        private void Laplacian()
        {
            int[,] tmp = new int[bmp.Width, bmp.Height];
            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    if (i == 0 || j == 0 || i == bmp.Width - 1 || j == bmp.Height - 1)
                        tmp[i, j] = 0;
                    else
                    {
                        int newGray = 4 * bmp.GetPixel(i, j).R - bmp.GetPixel(i - 1, j).R
                            - bmp.GetPixel(i, j - 1).R - bmp.GetPixel(i, j + 1).R
                            - bmp.GetPixel(i + 1, j).R;
                        tmp[i, j] = newGray;
                    }
                }
            }
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    int gg = tmp[i, j];
                    //锐化效果则需要原图加上该点
                    //gg += bmp.GetPixel(i, j).R;
                    gg = Math.Min(gg, 255);
                    gg = Math.Max(gg, 0);
                    bmp_.SetPixel(i, j, Color.FromArgb(gg, gg, gg));
                }
            }

            img.Source = bmp2img(ref bmp_);
        }

    }
}