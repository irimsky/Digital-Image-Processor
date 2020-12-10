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
        /// 双向一次微分锐化 (根据梯度二值化)
        /// </summary>
        private void BidirectionalFirstOrderDifferential()
        {
            Bitmap bmp_ = new Bitmap(bmp);
            int[,] gray = new int[bmp.Width, bmp.Height];
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
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
                    if (grad >= 30)
                    {
                        bmp_.SetPixel(i, j, Color.FromArgb(255, 255, 255));
                    }
                    else
                    {
                        bmp_.SetPixel(i, j, Color.FromArgb(0, 0, 0));
                    }
                }
            }

            UpdateImg(ref bmp_);
        }

        /// <summary>
        /// Roberts算子锐化
        /// </summary>
        private void Roberts()
        {
            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    if (i == bmp.Width - 1 || j == bmp.Height - 1)
                        bmp_.SetPixel(i, j, Color.FromArgb(0, 0, 0));
                    else
                    {
                        int gray1 = bmp.GetPixel(i, j).R, gray2 = bmp.GetPixel(i + 1, j).R,
                            gray3 = bmp.GetPixel(i, j + 1).R, gray4 = bmp.GetPixel(i + 1, j + 1).R;
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
            UpdateImg(ref bmp_);
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
                    if (i == 0 || j == 0 || i == bmp.Width - 1 || j == bmp.Height - 1)
                        bmp_.SetPixel(i, j, Color.FromArgb(0, 0, 0));
                    else
                    {
                        int gray00 = bmp.GetPixel(i, j).R, gray10 = bmp.GetPixel(i + 1, j).R,
                            gray01 = bmp.GetPixel(i, j + 1).R, gray11 = bmp.GetPixel(i + 1, j + 1).R,
                            gray22 = bmp.GetPixel(i - 1, j - 1).R, gray21 = bmp.GetPixel(i - 1, j).R, gray12 = bmp.GetPixel(i, j - 1).R,
                            gray02 = bmp.GetPixel(i, j - 1).R, gray20 = bmp.GetPixel(i - 1, j).R;
                        int dx = (gray21 + 2 * gray01 + gray11) - (gray22 + 2 * gray02 + gray12);
                        int dy = (gray22 + 2 * gray20 + gray21) - (gray12 + 2 * gray10 + gray11);
                        int newGray = (int)Math.Sqrt(dx * dx + dy * dy);
                        // newGray += bmp.GetPixel(i, j).R;
                        if (newGray > 255)
                            newGray = 255;
                        if (newGray < 0)
                            newGray = 0;
                        bmp_.SetPixel(i, j, Color.FromArgb(newGray, newGray, newGray));
                    }
                }
            }
            UpdateImg(ref bmp_);
        }

        /// <summary>
        /// Laplacian算子锐化
        /// 拉普拉斯对噪声敏感，会产生双边效果。不能检测出边的方向。通常不直接用于边的检测，只起辅助的角色，检测一个像素是在边的亮的一边还是暗的一边利用零跨越，确定边的位置。
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
                        int newGray = 8 * bmp.GetPixel(i, j).R - bmp.GetPixel(i - 1, j).R
                            - bmp.GetPixel(i, j - 1).R - bmp.GetPixel(i, j + 1).R
                            - bmp.GetPixel(i + 1, j).R - bmp.GetPixel(i - 1, j - 1).R
                            - bmp.GetPixel(i - 1, j + 1).R - bmp.GetPixel(i + 1, j + 1).R
                            - bmp.GetPixel(i + 1, j - 1).R;
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

            UpdateImg(ref bmp_);
        }

        /// <summary>
        /// Wallis算子锐化
        /// </summary>
        private void Wallis()
        {
            double[,] tmp = new double[bmp.Width, bmp.Height];
            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);
            double minn = 1000;
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    if (i == 0 || j == 0 || i == bmp.Width - 1 || j == bmp.Height - 1)
                        tmp[i, j] = 0;
                    else
                    {
                        double x0 = 46 * Math.Log(bmp.GetPixel(i, j).R + 1),
                            x1 = 46 * Math.Log(bmp.GetPixel(i - 1, j).R + 1),
                            x2 = 46 * Math.Log(bmp.GetPixel(i + 1, j).R + 1),
                            x3 = 46 * Math.Log(bmp.GetPixel(i, j - 1).R + 1),
                            x4 = 46 * Math.Log(bmp.GetPixel(i, j + 1).R + 1);

                        double newGray = 4 * x0 - (x1 + x2 + x3 + x4);
                        minn = Math.Min(newGray, minn);

                        tmp[i, j] = newGray;
                    }
                }
            }
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    double g = tmp[i, j];
                    //g = 46 * (g - minn);
                    int gg = (int)g;
                    if (g > 8)
                        gg = 255;
                    else gg = 0;

                    //锐化效果则需要原图加上该点
                    //gg += bmp.GetPixel(i, j).R;
                    gg = Math.Min(gg, 255);
                    gg = Math.Max(gg, 0);
                    bmp_.SetPixel(i, j, Color.FromArgb(gg, gg, gg));
                }
            }
            UpdateImg(ref bmp_);
        }

        /// <summary>
        /// 高斯滤波器
        /// </summary>
        private void GaussFilter()
        {
            int[] dir = { 1, 2, 1, 2, 4, 2, 1, 2, 1 };
            Bitmap bmp_ = new Bitmap(bmp);
            for (int i = 1; i < bmp.Width - 1; i++)
            {
                for (int j = 1; j < bmp.Height - 1; j++)
                {
                    int rsum = 0, gsum = 0, bsum = 0;
                    for (int k = 0; k < 9; k++)
                    {
                        int jj = k / 3, ii = k % 3;
                        Color c = bmp.GetPixel(i - 1 + ii, j - 1 + jj);
                        rsum += c.R * dir[k];
                        gsum += c.G * dir[k];
                        bsum += c.B * dir[k];
                    }
                    bmp_.SetPixel(i, j, Color.FromArgb(rsum / 16, gsum / 16, bsum / 16));
                }
            }
            UpdateImg(ref bmp_);
        }

        /// <summary>
        /// 高斯拉普拉斯算子锐化
        /// </summary>
        private void LoG()
        {
            // 高斯模糊
            int[,] tmp = new int[bmp.Width, bmp.Height];
            int[] dir = { 1, 2, 1, 2, 4, 2, 1, 2, 1 };
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    tmp[i, j] = 0;
                }
            }

            for (int i = 1; i < bmp.Width - 1; i++)
            {
                for (int j = 1; j < bmp.Height - 1; j++)
                {
                    int rsum = 0;
                    for (int k = 0; k < 9; k++)
                    {
                        int jj = k / 3, ii = k % 3;
                        Color c = bmp.GetPixel(i - 1 + ii, j - 1 + jj);
                        rsum += c.R * dir[k];
                    }
                    tmp[i, j] = rsum / 16;
                }
            }
            // 拉普拉斯算子
            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);
            int[,] tmp2 = new int[bmp.Width, bmp.Height];
            int[] dir2 = { -1, -1, -1, -1, 8, -1, -1, -1, -1 };
            for (int i = 1; i < bmp.Width - 1; i++)
            {
                for (int j = 1; j < bmp.Height - 1; j++)
                {
                    int sum = 0;
                    for (int k = 0; k < 9; k++)
                    {
                        int jj = k / 3, ii = k % 3;
                        sum += dir2[k] * tmp[i - 1 + ii, j - 1 + jj];
                    }
                    tmp2[i, j] = sum;
                }
            }
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    int newGray = tmp2[i, j];
                    newGray = Math.Max(0, newGray);
                    newGray = Math.Min(255, newGray);
                    bmp_.SetPixel(i, j, Color.FromArgb(newGray, newGray, newGray));
                }
            }
            UpdateImg(ref bmp_);
        }
    }
}
