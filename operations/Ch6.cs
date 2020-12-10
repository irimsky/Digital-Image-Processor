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
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
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
            UpdateImg(ref bmp_);
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
            for (int i = 0; i < NP; i++)
            {
                int r = rand.Next(0, bmp.Height), c = rand.Next(0, bmp.Width);
                double prob = rand.NextDouble();
                if (prob > pa)
                {
                    bmp_.SetPixel(c, r, Color.FromArgb(255, 255, 255));
                }
                else
                {
                    bmp_.SetPixel(c, r, Color.FromArgb(0, 0, 0));
                }
            }
            UpdateImg(ref bmp_);
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
            for (int i = 1; i < bmp.Width - 1; i++)
            {
                for (int j = 1; j < bmp.Height - 1; j++)
                {
                    Color now = bmp.GetPixel(i, j);
                    int rsum = now.R, gsum = now.G, bsum = now.B;
                    for (int d = 0; d < 8; d++)
                    {
                        int xx = i + dir[d, 0], yy = j + dir[d, 1];
                        Color c = bmp.GetPixel(xx, yy);
                        rsum += c.R; gsum += c.G; bsum += c.B;
                    }
                    bmp_.SetPixel(i, j, Color.FromArgb(rsum / 9, gsum / 9, bsum / 9));
                }
            }
            UpdateImg(ref bmp_);
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
            UpdateImg(ref bmp_);
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
                    if (tmp > p)
                        bmp_.SetPixel(i, j, Color.FromArgb(255, 255, 255));
                    else
                        bmp_.SetPixel(i, j, Color.FromArgb(0, 0, 0));

                }
            }
            UpdateImg(ref bmp_);
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
                    if (Math.Abs(now.R - sum) > 127.5)
                        bmp_.SetPixel(i, j, Color.FromArgb(255 - now.R, 255 - now.R, 255 - now.R));
                }
            }
            UpdateImg(ref bmp_);
        }

        /// <summary>
        /// 二值图像消除孤立点（四连通）
        /// </summary>
        private void BinIsoRemove()
        {
            int[,] dir = new int[,] { { -1, 0 }, { 0, -1 }, { 0, 1 }, { 1, 0 } };
            Bitmap bmp_ = new Bitmap(bmp);
            for (int i = 1; i < bmp.Width - 1; i++)
            {
                for (int j = 1; j < bmp.Height - 1; j++)
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
            UpdateImg(ref bmp_);
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
                        for (int dd = 0; dd < dir[d].Length; dd += 2)
                        {
                            Color c = bmp.GetPixel(i + dir[d][dd / 2, 0], j + dir[d][dd / 2, 1]);
                            arr.Add(c.R);
                        }
                        avg = arr.Average();
                        foreach (var r in arr)
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
            UpdateImg(ref bmp_);


        }

        /// <summary>
        /// KNN中值平滑滤波
        /// </summary>
        /// <param name="m">模板大小（奇数)</param>
        /// <param name="K">K</param>
        private void KNNFilter(int m, int K)
        {
            if (m >= bmp.Width / 2 || K > m * m || m % 2 != 1)
                return;
            Bitmap bmp_ = new Bitmap(bmp);
            int kernel = m / 2;
            List<Tuple<int, int>> sort_list = new List<Tuple<int, int>>(m * m);
            for (int i = kernel; i < bmp.Width - kernel; i++)
            {
                for (int j = kernel; j < bmp.Height - kernel; j++)
                {
                    Color now = bmp.GetPixel(i, j);
                    sort_list.Clear();
                    for (int ii = i - kernel; ii <= i + kernel; ii++)
                    {
                        for (int jj = j - kernel; jj <= j + kernel; jj++)
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
            UpdateImg(ref bmp_);
        }

    }
}
