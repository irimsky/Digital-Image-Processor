using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DIP
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 迭代阈值分割所用求阈值子函数
        /// </summary>
        /// <returns></returns>
        private int ITS_th(ref int[] grayCount)
        {
            int th;
            int l = 0, r = 255, prel = 0, prer = 255;
            while (true)
            {
                prel = l;
                prer = r;
                th = (l + r) / 2;
                int Asum = 0, Acnt = 0, Bsum = 0, Bcnt = 0;
                for (int i = 0; i < 256; i++)
                {
                    if (i >= th)
                    {
                        Bsum += i * grayCount[i];
                        Bcnt += grayCount[i];
                    }
                    else
                    {
                        Asum += i * grayCount[i];
                        Acnt += grayCount[i];
                    }
                }
                l = Asum / Acnt;
                r = Bsum / Bcnt;
                if (l == prel && r == prer)
                    break;
            }
            th = (l + r) / 2;
            return th;
        }

        /// <summary>
        /// 迭代阈值分割
        /// </summary>
        private void IterativeThresholdSegmentation()
        {
            int[] grayCount = new int[256];
            for (int i = 0; i < 256; i++)
                grayCount[i] = 0;
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    int x = bmp.GetPixel(i, j).R;
                    grayCount[x] += 1;
                }
            }

            int th = ITS_th(ref grayCount);

            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);

            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    int x = bmp.GetPixel(i, j).R;
                    if (x >= th)
                    {
                        bmp_.SetPixel(i, j, Color.White);
                    }
                    else
                    {
                        bmp_.SetPixel(i, j, Color.Black);
                    }
                }
            }
            UpdateImg(ref bmp_);
        }

        /// <summary>
        /// 灰度-局部灰度均值散布图法（使用迭代阈值分割法找阈值）
        /// </summary>
        private void LocalGrayAverage()
        {
            int[] grayCount = new int[256];
            int[] aveGrayCount = new int[256];
            for (int i = 0; i < 256; i++)
                grayCount[i] = 0;

            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    int x = bmp.GetPixel(i, j).R;
                    grayCount[x] += 1;
                    if (i > 0 && j > 0 && i < bmp.Width - 1 && j < bmp.Height - 1)
                    {
                        int sum =
                            bmp.GetPixel(i - 1, j - 1).R + bmp.GetPixel(i - 1, j).R + bmp.GetPixel(i - 1, j + 1).R
                            + bmp.GetPixel(i, j - 1).R + bmp.GetPixel(i, j).R + bmp.GetPixel(i, j + 1).R
                            + bmp.GetPixel(i + 1, j - 1).R + bmp.GetPixel(i + 1, j).R + bmp.GetPixel(i + 1, j + 1).R;
                        sum /= 9;
                        if (x == sum)
                        {
                            aveGrayCount[sum]++;
                        }
                    }
                    else aveGrayCount[x]++;
                }
            }
            int th = ITS_th(ref aveGrayCount);

            Bitmap bmp_ = new Bitmap(bmp.Width, bmp.Height);

            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    int x = bmp.GetPixel(i, j).R;
                    if (x >= th)
                    {
                        bmp_.SetPixel(i, j, Color.White);
                    }
                    else
                    {
                        bmp_.SetPixel(i, j, Color.Black);
                    }
                }
            }
            UpdateImg(ref bmp_);
        }

        /// <summary>
        /// 轮廓提取法
        /// </summary>
        private void EdgeExtraction()
        {
            Bitmap bmp_ = new Bitmap(bmp);
            int[,] tmp = new int[bmp.Width, bmp.Height];
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    tmp[i, j] = bmp.GetPixel(i, j).R;
                    if (tmp[i, j] > 128)
                        tmp[i, j] = 255;
                    else tmp[i, j] = 0;
                }
            }

            for (int i = 1; i < bmp.Width - 1; i++)
            {
                for (int j = 1; j < bmp.Height - 1; j++)
                {
                    if (tmp[i, j] == 255)
                        continue;
                    bool flag = false;
                    for (int ii = -1; !flag && ii <= 1; ii++)
                    {
                        for (int jj = -1; jj <= 1; jj++)
                        {
                            if (tmp[i + ii, j + jj] == 255)
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                    if (!flag)
                    {
                        bmp_.SetPixel(i, j, Color.White);
                    }
                    else
                    {
                        bmp_.SetPixel(i, j, Color.Black);
                    }

                }
            }

            UpdateImg(ref bmp_);
        }


        public class Block{
            public int perimeter, size;
            public int up, bottom, left, right;

            public Block(int p, int s, int u, int b, int l, int r)
            {
                perimeter = p;
                size = s;
                up = u;
                bottom = b;
                left = l;
                right = r;
            }
        }
        
        class BinaryMeasurer
        {
            public List<Block> arr;
            int[,] flag, gray;
            int mk = 1, width, height;
            int[,] dirs = { {-1, 0}, {-1, 1}, {0, 1}, { 1, 1 }, { 1, 0 }, { 1, -1 }
                    , { 0, -1 }, {-1, -1 } };
            public BinaryMeasurer(ref Bitmap bmp)
            {
                width = bmp.Width;
                height = bmp.Height;
                flag = new int[bmp.Width, bmp.Height];
                gray = new int[bmp.Width, bmp.Height];
                arr = new List<Block>();
                for (int i = 0; i < bmp.Width; i++)
                {
                    for (int j = 0; j < bmp.Height; j++)
                    {
                        flag[i, j] = 0;
                        gray[i, j] = bmp.GetPixel(i, j).R;
                    }
                }
            }

            //洪泛法求面积
            public void CalSize()
            {
                for(int i=0;i<width;i++)
                {
                    for(int j=0;j<height;j++)
                    {
                        if(flag[i, j]==0 && gray[i,j]==0)
                        {
                            Block block = new Block(0, 0, 0, height, width, 0);
                            arr.Add(block);
                            FloodFill(i, j, mk++);
                        }
                    }
                }
            }

            //洪泛函数,用栈模拟递归防止栈溢出
            void FloodFill(int xi, int yi, int mkk)
            {
                int m = mkk - 1;
                Stack<int> stack = new Stack<int>();
                stack.Push(xi * width + yi);
                while(stack.Count()!=0)
                {
                    int x = stack.Peek() / width, y = stack.Peek() % width;
                    stack.Pop();
                    flag[x, y] = mkk;
                    arr[m].size += 1;
                    arr[m].up = Math.Max(arr[m].up, y);
                    arr[m].bottom = Math.Min(arr[m].bottom, y);
                    arr[m].left = Math.Min(arr[m].left, x);
                    arr[m].right = Math.Max(arr[m].right, x);
                    int xx = x - 1, yy = y;
                    if (xx >= 0 && xx < width && yy >= 0 && yy < height)
                    {
                        if (flag[xx, yy] == 0 && gray[xx, yy] == 0)
                        {
                            stack.Push(xx * width + yy);
                        }
                    }
                    xx = x + 1; yy = y;
                    if (xx >= 0 && xx < width && yy >= 0 && yy < height)
                    {
                        if (flag[xx, yy] == 0 && gray[xx, yy] == 0)
                        {
                            stack.Push(xx * width + yy);
                        }
                    }
                    xx = x; yy = y - 1;
                    if (xx >= 0 && xx < width && yy >= 0 && yy < height)
                    {
                        if (flag[xx, yy] == 0 && gray[xx, yy] == 0)
                        {
                            stack.Push(xx * width + yy);
                        }
                    }
                    xx = x; yy = y + 1;
                    if (xx >= 0 && xx < width && yy >= 0 && yy < height)
                    {
                        if (flag[xx, yy] == 0 && gray[xx, yy] == 0)
                        {
                            stack.Push(xx * width + yy);
                        }
                    }
                }
                
            }

            //沿边缘计算周长
            public void CalPerimeter()
            {
                for(int k=1;k<mk;k++)
                {
                    bool f = false;
                    for(int i=arr[k-1].left; !f && i<=arr[k-1].right;i++)
                    {
                        for(int j=arr[k-1].bottom;j<=arr[k-1].up;j++)
                        {
                            if(flag[i, j] == k)
                            {
                                arr[k - 1].perimeter = 1;
                                int kk = 1;
                                for(;;kk=(kk+1)%8)
                                {
                                    int xx = i + dirs[kk, 0], yy = j + dirs[kk, 1];
                                    if (xx >= 0 && xx < width && yy >= 0 && yy < height)
                                    {
                                        if (flag[xx, yy] == k)
                                        {
                                            LeftHand(xx, yy, i, j, k);
                                            break;
                                        }
                                    }
                                }
                               
                                f = true;
                                break;
                            }
                        }

                    }
                }
            }

            //左手搜索法
            void LeftHand(int x, int y, int stx, int sty, int k)
            {
                int prex = stx, prey = sty;
                int pred = JudgeDir(x, y, stx, sty);
                while (x != stx || y != sty) {
                    flag[x, y] = 0;
                    int d = pred - 2;
                    if (d < 0) d += 8;
                    arr[k - 1].perimeter += 1;
                    int xx=x, yy=y;
                    for (int dd = 0; dd < 8; dd++)
                    {
                        int kk = (d + dd)%8;
                        xx = x + dirs[kk, 0]; yy = y + dirs[kk,1];
                        if (xx == stx && yy == sty)
                            return;
                        if (xx >= 0 && xx < width && yy>=0 && yy<height)
                        {
                            if(flag[xx, yy]==k)
                            {
                                pred = kk;
                                break;
                            }
                        }
                    }
                    if (xx == x && yy == y) break;
                    x = xx; y = yy;
                }
            }

            // 左手搜索法所用判断方向函数
            private int JudgeDir(int x, int y, int prex, int prey)
            {
                if (x == prex)
                {
                    if (y == prey - 1) return 2;
                    else if (y == prey + 1) return 6;
                }
                else if (y == prey)
                {
                    if (x == prex - 1) return 0;
                    else if (x == prex + 1) return 4;
                }
                else if (x == prex - 1)
                {
                    if (y == prey - 1) return 1;
                    else if (y == prey + 1) return 7;
                }
                else if (x == prex + 1)
                {
                    if (y == prey - 1) return 3;
                    else if (y == prey + 1) return 5;
                }
                return -1;
            }
        }


        /// <summary>
        /// 测量二值化图像
        /// </summary>
        private void MeasureBinary()
        {
            System.Drawing.Image oriImg = bmp;
            BinaryMeasurer measure = new BinaryMeasurer(ref bmp);
            measure.CalSize();
            measure.CalPerimeter();
            FlagWindow fw = new FlagWindow(oriImg, measure.arr);
            fw.Show();
        }


    }
}

