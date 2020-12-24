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
        /// <summary>
        /// 将图片灰度化
        /// </summary>
        private void Gray(bool showHist = false)
        {
            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    Color c = bmp.GetPixel(i, j);
                    int tmp = (int)(0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
                    bmp_.SetPixel(i, j, Color.FromArgb(tmp, tmp, tmp));
                }
            }
            UpdateImg(ref bmp_);
            if (showHist) { 
                HistForm histForm = new HistForm(bmp);
                histForm.Show();
            }
        }

        /// <summary>
        /// 拓展压缩线性灰度变化
        /// </summary>
        private void LinerGray(int a, int b, int c, int d)
        {

            double alpha = (double)c / a;
            double beta = (double)(d - c) / (b - a);
            double gama = (double)(255 - d) / (255 - b);
            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    Color color = bmp.GetPixel(i, j);
                    int nc;
                    int tmp = (int)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
                    if (tmp <= a)
                    {
                        nc = (int)(alpha * tmp);
                    }
                    else if (tmp >= b)
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
            UpdateImg(ref bmp_);
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
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    Color color = bmp.GetPixel(i, j);
                    int tmp = (int)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
                    grayValue[tmp]++;
                }
            }
            int sum = bmp.Width * bmp.Height, cnt = 0;
            int[] hp = new int[256];
            for (int i = 0; i < 256; i++)
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
            UpdateImg(ref bmp_);
            HistForm histForm = new HistForm(bmp);
            histForm.Show();
        }
    }
}
