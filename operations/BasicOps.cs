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
    
    class Operation
    {
        int[,] Rdiff, Gdiff, Bdiff, Adiff;
        int Width, Height;
        int Width_, Height_;
        public Operation(ref Bitmap old, ref Bitmap now)
        {
            int w1 = old.Width, w2 = now.Width, h1 = old.Height, h2 = now.Height;
            // 矩阵取最大宽高
            Rdiff = new int[Math.Max(w1, w2), Math.Max(h1, h2)];
            Gdiff = new int[Math.Max(w1, w2), Math.Max(h1, h2)];
            Bdiff = new int[Math.Max(w1, w2), Math.Max(h1, h2)];
            Adiff = new int[Math.Max(w1, w2), Math.Max(h1, h2)];

            // 新图宽度
            Width = w2; Height = h2;
            // 旧图宽高
            Width_ = w1; Height_ = h1;

            for (int i = 0 ;i < w1; i++)
            {
                for(int j=0;j<h1;j++)
                {
                    if(i<w2 && j<h2)
                    {
                        Rdiff[i, j] = now.GetPixel(i, j).R - old.GetPixel(i, j).R;
                        Gdiff[i, j] = now.GetPixel(i, j).G - old.GetPixel(i, j).G;
                        Bdiff[i, j] = now.GetPixel(i, j).B - old.GetPixel(i, j).B;
                        Adiff[i, j] = now.GetPixel(i, j).A - old.GetPixel(i, j).A;
                    }
                    else
                    {
                        Rdiff[i, j] = - old.GetPixel(i, j).R;
                        Gdiff[i, j] = - old.GetPixel(i, j).G;
                        Bdiff[i, j] = - old.GetPixel(i, j).B;
                        Adiff[i, j] = - old.GetPixel(i, j).A;
                    }
                }
            }
            for (int i = 0; i < w2; i++)
            {
                for (int j = 0; j < h2; j++)
                {
                    if (i < w1 && j < h1)
                    {
                        continue;
                    }
                    else
                    {
                        Rdiff[i, j] = now.GetPixel(i, j).R;
                        Gdiff[i, j] = now.GetPixel(i, j).G;
                        Bdiff[i, j] = now.GetPixel(i, j).B;
                        Adiff[i, j] = now.GetPixel(i, j).A;
                    }
                }
            }
        }

        public Bitmap Undo(ref Bitmap now)
        {
            Bitmap old = new Bitmap(Width_, Height_);
            for(int i=0;i<Width_;i++)
            {
                for(int j=0;j<Height_;j++)
                {
                    int r, g, b, a;
                    if (i < Width && j < Height)
                    {
                        Color c = now.GetPixel(i, j);
                        r = c.R - Rdiff[i, j];
                        g = c.G - Gdiff[i, j];
                        b = c.B - Bdiff[i, j];
                        a = c.A - Adiff[i, j];
                    }
                    else
                    {
                        r = 0 - Rdiff[i, j];
                        g = 0 - Gdiff[i, j];
                        b = 0 - Bdiff[i, j];
                        a = 0 - Adiff[i, j];
                    }
                    old.SetPixel(i, j, Color.FromArgb(a, r, g, b));
                }
            }
            return old;
        }

        public Bitmap Redo(ref Bitmap old)
        {
            Bitmap now = new Bitmap(Width, Height);
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    int r, g, b, a;
                    if (i < Width_ && j < Height_)
                    {
                        Color c = old.GetPixel(i, j);
                        r = c.R + Rdiff[i, j];
                        g = c.G + Gdiff[i, j];
                        b = c.B + Bdiff[i, j];
                        a = c.A + Adiff[i, j];
                    }
                    else
                    {
                        r = Rdiff[i, j];
                        g = Gdiff[i, j];
                        b = Bdiff[i, j];
                        a = Adiff[i, j];
                    }
                    now.SetPixel(i, j, Color.FromArgb(a, r, g, b));
                }
            }
            return now;
        }
    }

    public partial class MainWindow : Window
    {
        //操作记录及其指针
        private ArrayList EditOps = new ArrayList();
        int OpPtr = 0;
       
        //引入删除指针操作
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        /// <summary>
        /// 更新bmp
        /// </summary>
        private void UpdateImg(ref Bitmap bmp_, bool isEdit=false)
        {
            if (bmp != null && !isEdit) // 非 撤销/重做 操作，需要删除指针后的操作，并添加新的操作记录。
            {
                op = new Operation(ref bmp, ref bmp_);
                
                while(EditOps.Count > OpPtr)
                {
                    EditOps.RemoveAt(OpPtr);
                }
                EditOps.Add(op);
                redo.IsEnabled = false;
                OpPtr++;
                if(OpPtr>=0)
                    undo.IsEnabled = true;
            }
            DeleteObject(bip);
            bmp = (Bitmap)bmp_.Clone();
            bip = bmp.GetHbitmap();
            IntPtr ip = bmp_.GetHbitmap();
            DeleteObject(ip);
            BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bip, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            img.Source = bitmapSource;
        }

        private void OpenImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "选择位图源文件";
            openFileDialog.Filter = ".bmp|*.bmp";
            openFileDialog.FileName = string.Empty;
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == false)
            {
                return;
            }
            string txtFile = openFileDialog.FileName;
            FileStream fs = new FileStream(txtFile, FileMode.Open);
            Bitmap b2 = new Bitmap(fs);
            fs.Close();
            UpdateImg(ref b2);
            status = "原图信息";
            grid.Children.Clear();
            TextBlock tb = new TextBlock();
            tb.Margin = new Thickness(15, 10, 10, 10);
            tb.FontSize = 14;
            info = getInfo(txtFile);
            tb.Text = info;
            grid.Children.Add(tb);
        }

        private void SaveImage()
        {
            SaveFileDialog Dialog = new SaveFileDialog();
            Dialog.Title = "选择保存位置";
            Dialog.Filter = ".bmp|*.bmp";
            Dialog.FileName = string.Empty;
            Dialog.FilterIndex = 1;
            Dialog.RestoreDirectory = true;
            if (Dialog.ShowDialog() == false)
            {
                return;
            }
            string txtFile = Dialog.FileName;
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)this.img.Source));
            using (FileStream stream = new FileStream(txtFile, FileMode.Create))
            encoder.Save(stream);
        }

    }
}