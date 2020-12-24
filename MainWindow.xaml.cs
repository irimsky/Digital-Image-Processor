using Microsoft.Win32;
using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;


namespace DIP
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 当前位图与其指针
        Bitmap bmp = null;
        IntPtr bip;
        // 状态、 原图信息
        string status, info;
        Operation op;
        

        public MainWindow()
        {
            FileStream fs = new FileStream("E:\\test3.bmp", FileMode.Open);
            bmp = new Bitmap(fs);
            bip = bmp.GetHbitmap();
            fs.Close();
            BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bip, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            InitializeComponent();
            
            img.Source = bitmapSource;
            foreach (MenuItem i in MenuBar.Items)
            {
                foreach (var j in i.Items)
                    if(j.GetType() == typeof(MenuItem))
                        ((MenuItem)j).Click += Button_Click;
            }
        
            TextBlock tb = new TextBlock();
            tb.Margin = new Thickness(15, 10, 10, 10);
            tb.FontSize = 14;
            info = tb.Text = getInfo("E:\\test.bmp");
            grid.Children.Add(tb);
        }

        private void Img_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MessageBox.Show("cm");
            img.Width = bmp.Width;
            img.Height = bmp.Height;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string txt = "";
            if (sender.GetType() == typeof(MenuItem))
                txt = (string)((MenuItem)sender).Header;
            else if (sender.GetType() == typeof(Button))
                txt = (string)((Button)sender).Content;

            if(txt == "确认")
            {
                if(status == "旋转")
                {
                    TextBox tb = (TextBox)FindName("angle");
                    try { 
                        double angle = Convert.ToDouble(tb.Text);
                        rotate(angle);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message, "错误");
                    }
                }
                else if(status == "缩小")
                {
                    TextBox tbw = (TextBox)FindName("wscale"), 
                        tbh = (TextBox)FindName("hscale");
                    try
                    {
                        double wscale = Convert.ToDouble(tbw.Text);
                        double hscale = Convert.ToDouble(tbh.Text);
                        if(wscale>1||wscale<=0||hscale>1||hscale<=0)
                        {
                            MessageBox.Show("请输入有效范围的数字");
                            return;
                        }
                        minimize(hscale, wscale);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "错误");
                    }
                }
                else if(status=="放大")
                {
                    TextBox tbw = (TextBox)FindName("wscale"),
                        tbh = (TextBox)FindName("hscale");
                    try
                    {
                        double wscale = Convert.ToDouble(tbw.Text);
                        double hscale = Convert.ToDouble(tbh.Text);
                        if (wscale < 1 || hscale < 1)
                        {
                            MessageBox.Show("请输入有效范围的数字");
                            return;
                        }
                        maximize(hscale, wscale);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "错误");
                    }
                }
                else if(status == "错切")
                {
                    TextBox tbw = (TextBox)FindName("wscale"),
                        tbh = (TextBox)FindName("hscale");
                    try
                    {
                        double wscale = Convert.ToDouble(tbw.Text);
                        double hscale = Convert.ToDouble(tbh.Text);
                        shear(hscale, wscale);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "错误");
                    }
                }
                else if(status=="线性灰度变换")
                {
                    TextBox tba = (TextBox)FindName("rangea"),
                        tbb = (TextBox)FindName("rangeb"),
                        tbc = (TextBox)FindName("rangec"),
                        tbd = (TextBox)FindName("ranged");
                    try
                    {
                        int a = Convert.ToInt32(tba.Text);
                        int b = Convert.ToInt32(tbb.Text);
                        int c = Convert.ToInt32(tbc.Text);
                        int d = Convert.ToInt32(tbd.Text);
                        if (a > b || c > d || d - c <= b - a)
                        {
                            MessageBox.Show("范围错误或被拓展范围错误(拓展后范围要大于原范围)", "错误");
                            return;
                        }
                        LinerGray(a, b, c, d);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message, "错误");
                    }
                }
                else if(status=="高斯噪声")
                {
                    TextBox tba = (TextBox)FindName("tbk");
                    try
                    {
                        int k = Convert.ToInt32(tba.Text);
                        GaussNoise(k);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "错误");
                    }
                    
                }
                else if(status=="椒盐噪声")
                {
                    TextBox tbw = (TextBox)FindName("SNR"),
                        tbh = (TextBox)FindName("pa");
                    try
                    {
                        double SNR = Convert.ToDouble(tbw.Text);
                        double pa = Convert.ToDouble(tbh.Text);
                        if(SNR > 1 || pa > 1 || SNR < 0 || pa < 0)
                        {
                            MessageBox.Show("输入范围错误！");
                            return;
                        }
                        SaltNoise(SNR, pa);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "错误");
                    }
                }
                else if(status=="二值化")
                {
                    TextBox tba = (TextBox)FindName("tbk");
                    try
                    {
                        int k = Convert.ToInt32(tba.Text);
                        Binarize(k);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "错误");
                    }
                }
            }
            else if(txt=="撤销")
            {
                Bitmap bmp_ = ((Operation)EditOps[OpPtr-1]).Undo(ref bmp);
                --OpPtr;
                if (OpPtr == 0)
                    undo.IsEnabled = false;
                else undo.IsEnabled = true;

                if (OpPtr < EditOps.Count)
                    redo.IsEnabled = true;
                else redo.IsEnabled = true;
                UpdateImg(ref bmp_, true);
            }
            else if(txt=="重做")
            {
                Bitmap bmp_ = ((Operation)EditOps[OpPtr]).Redo(ref bmp);
                ++OpPtr;
                if (OpPtr > 0)
                    undo.IsEnabled = true;
                else undo.IsEnabled = true;

                if (OpPtr < EditOps.Count)
                    redo.IsEnabled = true;
                else redo.IsEnabled = false;
                UpdateImg(ref bmp_, true);
            }
            else if(txt=="灰度化")
            {
                if(bmp==null)
                {
                    MessageBox.Show("请打开一张图片!");
                    return;
                }
                grid.Children.Clear();
                Gray(true);
                
            }
            else if (txt == "灰度直方图")
            {
                HistForm histForm = new HistForm(bmp);
                histForm.Show();
            }
            else if (txt == "保存图片")
            {
                SaveImage();
            }
            else
            {
                status = txt;
                if (txt == "打开图片")
                {
                    OpenImage();
                }
                else
                {
                    if(bmp==null)
                    {
                        MessageBox.Show("请打开一张图片！");
                        return;
                    }
                    if (txt == "原图信息")
                    {
                        grid.Children.Clear();
                        TextBlock tb = new TextBlock();
                        tb.Margin = new Thickness(15, 10, 10, 10);
                        tb.FontSize = 14;
                        tb.Text = info;
                        grid.Children.Add(tb);
                    }
                    else if (txt == "旋转")
                    {
                        grid.Children.Clear();
                        Label lb = new Label();
                        lb.Content = "角度:";
                        lb.VerticalAlignment = VerticalAlignment.Center;
                        lb.Margin = new Thickness(10, 0, 0, 0);
                        grid.Children.Add(lb);

                        TextBox tb = new TextBox();
                        tb.Width = 40;
                        tb.Height = 20;
                        tb.Margin = new Thickness(10, 0, 20, 0);
                        if (FindName("angle") != null)
                            grid.UnregisterName("angle");
                        grid.RegisterName("angle", tb);
                        grid.Children.Add(tb);

                        Button btn = new Button();
                        btn.Margin = new Thickness(20, 20, 20, 20);
                        btn.Content = "确认";
                        btn.Height = 20;
                        btn.Width = 50;
                        btn.Click += Button_Click;
                        grid.Children.Add(btn);
                    }
                    else if (txt == "缩小")
                    {
                        grid.Children.Clear();
                        Label lb = new Label();
                        lb.Content = "宽缩小幅度（0-1）:";
                        lb.Margin = new Thickness(10, 0, 0, 0);
                        lb.VerticalAlignment = VerticalAlignment.Center;
                        grid.Children.Add(lb);

                        TextBox tbw = new TextBox();
                        tbw.Width = 40;
                        tbw.Height = 20;
                        tbw.Margin = new Thickness(10, 0, 20, 0);
                        if (FindName("wscale") != null)
                            grid.UnregisterName("wscale");
                        grid.RegisterName("wscale", tbw);
                        grid.Children.Add(tbw);

                        Label lb2 = new Label();
                        lb2.Content = "高缩小幅度（0-1）:";
                        lb2.VerticalAlignment = VerticalAlignment.Center;
                        lb2.Margin = new Thickness(20, 0, 0, 0);
                        grid.Children.Add(lb2);

                        TextBox tbh = new TextBox();
                        tbh.Width = 40;
                        tbh.Height = 20;
                        tbh.Margin = new Thickness(10, 0, 20, 0);
                        if (FindName("hscale") != null)
                            grid.UnregisterName("hscale");
                        grid.RegisterName("hscale", tbh);
                        grid.Children.Add(tbh);

                        Button btn = new Button();
                        btn.Margin = new Thickness(20, 20, 20, 20);
                        btn.Content = "确认";
                        btn.Height = 20;
                        btn.Width = 50;
                        btn.Click += Button_Click;
                        grid.Children.Add(btn);
                    }
                    else if (txt == "放大")
                    {
                        grid.Children.Clear();
                        Label lb = new Label();
                        lb.Margin = new Thickness(10, 0, 0, 0);

                        lb.Content = "宽放大幅度（＞1）:";
                        lb.VerticalAlignment = VerticalAlignment.Center;
                        grid.Children.Add(lb);

                        TextBox tbw = new TextBox();
                        tbw.Width = 40;
                        tbw.Height = 20;
                        tbw.Margin = new Thickness(10, 0, 20, 0);
                        if (FindName("wscale") != null)
                            grid.UnregisterName("wscale");
                        grid.RegisterName("wscale", tbw);
                        grid.Children.Add(tbw);

                        Label lb2 = new Label();
                        lb2.Content = "高放大幅度（＞1）:";
                        lb2.VerticalAlignment = VerticalAlignment.Center;
                        lb2.Margin = new Thickness(20, 0, 0, 0);
                        grid.Children.Add(lb2);

                        TextBox tbh = new TextBox();
                        tbh.Width = 40;
                        tbh.Height = 20;
                        tbh.Margin = new Thickness(10, 0, 20, 0);
                        if (FindName("hscale") != null)
                            grid.UnregisterName("hscale");
                        grid.RegisterName("hscale", tbh);
                        grid.Children.Add(tbh);

                        Button btn = new Button();
                        btn.Margin = new Thickness(20, 20, 20, 20);
                        btn.Content = "确认";
                        btn.Height = 20;
                        btn.Width = 50;
                        btn.Click += Button_Click;
                        grid.Children.Add(btn);
                    }
                    else if (txt == "错切")
                    {
                        grid.Children.Clear();
                        Label lb = new Label();
                        lb.Content = "宽错切幅度:";
                        lb.Margin = new Thickness(10, 0, 0, 0);

                        lb.VerticalAlignment = VerticalAlignment.Center;
                        grid.Children.Add(lb);

                        TextBox tbw = new TextBox();
                        tbw.Width = 40;
                        tbw.Height = 20;
                        tbw.Margin = new Thickness(10, 0, 20, 0);
                        if (FindName("wscale") != null)
                            grid.UnregisterName("wscale");
                        grid.RegisterName("wscale", tbw);
                        grid.Children.Add(tbw);

                        Label lb2 = new Label();
                        lb2.Content = "高错切幅度:";
                        lb2.VerticalAlignment = VerticalAlignment.Center;
                        lb2.Margin = new Thickness(20, 0, 0, 0);
                        grid.Children.Add(lb2);

                        TextBox tbh = new TextBox();
                        tbh.Width = 40;
                        tbh.Height = 20;
                        tbh.Margin = new Thickness(10, 0, 20, 0);
                        if (FindName("hscale") != null)
                            grid.UnregisterName("hscale");
                        grid.RegisterName("hscale", tbh);
                        grid.Children.Add(tbh);

                        Button btn = new Button();
                        btn.Margin = new Thickness(20, 20, 20, 20);
                        btn.Content = "确认";
                        btn.Height = 20;
                        btn.Width = 50;
                        btn.Click += Button_Click;
                        grid.Children.Add(btn);
                    }
                    else if (txt == "线性灰度变换")
                    {
                        grid.Children.Clear();
                        Label lb = new Label();
                        lb.Margin = new Thickness(10, 0, 0, 0);

                        lb.Content = "希望拓展的灰度范围:";
                        lb.VerticalAlignment = VerticalAlignment.Center;
                        grid.Children.Add(lb);

                        TextBox tba = new TextBox();
                        tba.Width = 40;
                        tba.Height = 20;
                        tba.Margin = new Thickness(10, 0, 0, 0);
                        if (FindName("rangea") != null)
                            grid.UnregisterName("rangea");
                        grid.RegisterName("rangea", tba);
                        grid.Children.Add(tba);

                        lb = new Label();
                        lb.Content = "~";
                        lb.Margin = new Thickness(0);
                        lb.VerticalAlignment = VerticalAlignment.Center;
                        grid.Children.Add(lb);

                        TextBox tbb = new TextBox();
                        tbb.Width = 40;
                        tbb.Height = 20;
                        tbb.Margin = new Thickness(0, 0, 0, 0);
                        if (FindName("rangeb") != null)
                            grid.UnregisterName("rangeb");
                        grid.RegisterName("rangeb", tbb);
                        grid.Children.Add(tbb);

                        lb = new Label();
                        lb.Content = "" +
                            "→";
                        lb.VerticalAlignment = VerticalAlignment.Center;
                        grid.Children.Add(lb);

                        TextBox tbc = new TextBox();
                        tbc.Width = 40;
                        tbc.Height = 20;
                        tbc.Margin = new Thickness(10, 0, 0, 0);
                        if (FindName("rangec") != null)
                            grid.UnregisterName("rangec");
                        grid.RegisterName("rangec", tbc);
                        grid.Children.Add(tbc);

                        lb = new Label();
                        lb.Content = "~";
                        lb.Margin = new Thickness(0);
                        lb.VerticalAlignment = VerticalAlignment.Center;
                        grid.Children.Add(lb);

                        TextBox tbd = new TextBox();
                        tbd.Width = 40;
                        tbd.Height = 20;
                        tbd.Margin = new Thickness(0, 0, 0, 0);
                        if (FindName("ranged") != null)
                            grid.UnregisterName("ranged");
                        grid.RegisterName("ranged", tbd);
                        grid.Children.Add(tbd);

                        Button btn = new Button();
                        btn.Margin = new Thickness(20, 20, 20, 20);
                        btn.Content = "确认";
                        btn.Height = 20;
                        btn.Width = 50;
                        btn.Click += Button_Click;
                        grid.Children.Add(btn);
                    }
                    else if (txt == "直方图均衡化")
                    {
                        grid.Children.Clear();
                        Equalization();
                    }
                    else if (txt == "高斯噪声")
                    {
                        grid.Children.Clear();
                        Label lb = new Label();
                        lb.Content = "高斯噪声强度:";
                        lb.Margin = new Thickness(10, 0, 0, 0);

                        lb.VerticalAlignment = VerticalAlignment.Center;
                        grid.Children.Add(lb);

                        TextBox tba = new TextBox();
                        tba.Width = 40;
                        tba.Height = 20;
                        tba.Margin = new Thickness(10, 0, 0, 0);
                        if (FindName("tbk") != null)
                            grid.UnregisterName("tbk");
                        grid.RegisterName("tbk", tba);
                        grid.Children.Add(tba);

                        Button btn = new Button();
                        btn.Margin = new Thickness(20, 20, 20, 20);
                        btn.Content = "确认";
                        btn.Height = 20;
                        btn.Width = 50;
                        btn.Click += Button_Click;
                        grid.Children.Add(btn);
                    }
                    else if (txt == "椒盐噪声")
                    {
                        grid.Children.Clear();
                        Label lb = new Label();
                        lb.Margin = new Thickness(10, 0, 0, 0);

                        lb.Content = "信噪比:";
                        lb.VerticalAlignment = VerticalAlignment.Center;
                        grid.Children.Add(lb);

                        TextBox tbw = new TextBox();
                        tbw.Width = 40;
                        tbw.Height = 20;
                        tbw.Margin = new Thickness(10, 0, 20, 0);
                        if (FindName("SNR") != null)
                            grid.UnregisterName("SNR");
                        grid.RegisterName("SNR", tbw);
                        grid.Children.Add(tbw);

                        Label lb2 = new Label();
                        lb2.Content = "暗点概率:";
                        lb2.VerticalAlignment = VerticalAlignment.Center;
                        lb2.Margin = new Thickness(20, 0, 0, 0);
                        grid.Children.Add(lb2);

                        TextBox tbh = new TextBox();
                        tbh.Width = 40;
                        tbh.Height = 20;
                        tbh.Margin = new Thickness(10, 0, 20, 0);
                        if (FindName("pa") != null)
                            grid.UnregisterName("pa");
                        grid.RegisterName("pa", tbh);
                        grid.Children.Add(tbh);

                        Button btn = new Button();
                        btn.Margin = new Thickness(20, 20, 20, 20);
                        btn.Content = "确认";
                        btn.Height = 20;
                        btn.Width = 50;
                        btn.Click += Button_Click;
                        grid.Children.Add(btn);
                    }
                    else if (txt == "均值滤波")
                    {
                        grid.Children.Clear();
                        EvenFilter();
                    }
                    else if (txt=="中值滤波")
                    {
                        grid.Children.Clear();
                        MidFilter();
                    }
                    else if(txt=="高斯平滑滤波")
                    {
                        grid.Children.Clear();
                        GaussFilter();
                    }
                    else if (txt=="二值化")
                    {
                        grid.Children.Clear();
                        Label lb = new Label();
                        lb.Content = "二值化阈值:";
                        lb.Margin = new Thickness(10, 0, 0, 0);

                        lb.VerticalAlignment = VerticalAlignment.Center;
                        grid.Children.Add(lb);

                        TextBox tba = new TextBox();
                        tba.Width = 40;
                        tba.Height = 20;
                        tba.Margin = new Thickness(10, 0, 0, 0);
                        if (FindName("tbk") != null)
                            grid.UnregisterName("tbk");
                        grid.RegisterName("tbk", tba);
                        grid.Children.Add(tba);

                        Button btn = new Button();
                        btn.Margin = new Thickness(20, 20, 20, 20);
                        btn.Content = "确认";
                        btn.Height = 20;
                        btn.Width = 50;
                        btn.Click += Button_Click;
                        grid.Children.Add(btn);
                    }
                    else if (txt=="二值图像去噪")
                    {
                        grid.Children.Clear();
                        BinaryFilter();
                    }
                    else if (txt== "消除孤立黑像素点")
                    {
                        Gray();
                        grid.Children.Clear();
                        BinIsoRemove();
                        
                    }
                    else if(txt == "选择式掩膜滤波")
                    {
                        Gray();
                        grid.Children.Clear();
                        LSMF();
                    }
                    else if(txt=="KNN平滑滤波")
                    {
                        grid.Children.Clear();
                        KNNFilter(3, 5);
                    }
                    else if(txt== "双向梯度算子")
                    {
                        Gray();
                        grid.Children.Clear();
                        BidirectionalFirstOrderDifferential();
                    }
                    else if(txt == "Roberts算子")
                    {
                        Gray();
                        grid.Children.Clear();
                        Roberts();
                    }
                    else if(txt== "Sobel算子")
                    {
                        Gray();
                        grid.Children.Clear();
                        Sobel();
                    }
                    else if(txt== "Laplacian算子")
                    {
                        Gray();
                        grid.Children.Clear();
                        Laplacian();
                    }
                    else if(txt=="Wallis算子")
                    {
                        Gray();
                        grid.Children.Clear();
                        Wallis();
                    }
                    else if(txt=="LoG算子")
                    {
                        Gray();
                        grid.Children.Clear();
                        LoG();
                    }
                    else if(txt=="迭代阈值分割")
                    {
                        Gray();
                        grid.Children.Clear();
                        IterativeThresholdSegmentation();
                    }
                    else if(txt == "灰度-局部灰度均值散布图")
                    {
                        Gray();
                        grid.Children.Clear();
                        LocalGrayAverage();
                    }
                    else if(txt== "轮廓提取法")
                    {
                        Gray();
                        grid.Children.Clear();
                        EdgeExtraction();
                    }
                    else if(txt=="二值图像测量")
                    {
                        IterativeThresholdSegmentation();
                        MeasureBinary();
                    }
                }
            }
            
            
        }

    }
}
