using System;
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

        public MainWindow()
        {
            FileStream fs = new FileStream("E:\\test.bmp", FileMode.Open);
            bmp = new Bitmap(fs);
            bip = bmp.GetHbitmap();
            fs.Close();
            BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bip, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            InitializeComponent();
            foreach(Button i in BtnSp.Children)
            {
                i.Click += Button_Click;
            }
            img.Source = bitmapSource;

            // 
            TextBlock tb = new TextBlock();
            tb.Margin = new Thickness(10, 5, 10, 10);
            tb.FontSize = 16;
            info = tb.Text = getInfo("E:\\test.bmp");
            grid.Children.Add(tb);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string txt = (string)((Button)sender).Content;
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
            }
            else
            {
                status = txt;
                if (txt == "打开图片")
                {
                    Bitmap b2 = new Bitmap(200, 100);

                    for (int i = 0; i < 200; i++)
                        for (int j = 0; j < 50; j++)
                            b2.SetPixel(i, j, Color.Red);
                    BitmapSource bitmapSource = bmp2img(ref b2);
                    img.Source = bitmapSource;
                    status = "原图信息";
                    grid.Children.Clear();
                    TextBlock tb = new TextBlock();
                    tb.Margin = new Thickness(10, 5, 10, 10);
                    tb.FontSize = 16;
                    tb.Text = info;
                    grid.Children.Add(tb);
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
                        tb.Margin = new Thickness(10, 5, 10, 10);
                        tb.FontSize = 16;
                        tb.Text = info;
                        grid.Children.Add(tb);
                    }
                    else if (txt == "旋转")
                    {
                        grid.Children.Clear();
                        Label lb = new Label();
                        lb.Content = "角度:";
                        lb.VerticalAlignment = VerticalAlignment.Center;
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
                        shear(0.5, 0);
                    }
                }
            }
            
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //grid.Children.Clear();
            rotate(45);
            Button btn = new Button();
            btn.Margin = new Thickness(20, 20, 20, 20);
            btn.Content = "添加";
            btn.Height = 20;
            btn.Width = 50;
            grid.Children.Add(btn);
        }
    }
}
