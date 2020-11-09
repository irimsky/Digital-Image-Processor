数字图像处理大作业

使用C# 从底层像素级别实现数字图像处理课上所学的部分算法（不使用封装好的现成的库）



**GitHub源码**：[https://github.com/irimsky/DIP_Exp](https://github.com/irimsky/DIP_Exp)



注：

- 本项目使用.NET中 [System.Drawing.Bitmap](https://docs.microsoft.com/zh-cn/dotnet/api/system.drawing.bitmap?view=dotnet-plat-ext-3.1) 类实现位图
- Bitmap的API的尺寸、坐标默认以（宽，高）顺序，本人习惯使用（高，宽），所以代码中有涉及Bitmap类的“宽高”顺序有些许不同



# 读取位图头文件

位图文件格式分析参考：[https://blog.csdn.net/guanchanghui/article/details/1172092](https://blog.csdn.net/guanchanghui/article/details/1172092)

```csharp
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
```



![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/Snipaste_2020-10-17_20-06-12.png?x-oss-process=image/resize,p_70)



# 旋转-反变换公式法

课上讲了三种旋转方法

1. **直角坐标系的旋转**，需要插值
2. **极坐标系的旋转**，需要变换+插值
3. **反变换公式**，无需插值



这里采用第三种方法。



- 基本原理：从新图像的像素点坐标反过来求其所对应的原图像的像素点的坐标。

  ![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/rotate.png?x-oss-process=image/resize,p_50)

  其中$x^{'}、y^{'}$为新图像中的坐标，$x、y$为原图像中的坐标

  

- 步骤：

  先确定画布大小→确定新图像坐标→计算出对应的原图像坐标。

  这样可将**原图像坐标**的像素值对应到**新图像**中。



```csharp
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
    // 先确定画布大小
    MaxXY(h-1, w-1, angle, ref range);
   
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
```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/rotate2.png?x-oss-process=image/resize,p_75)



# 缩小-局部均值缩小法

等间隔采样缩小法虽然简单，然而对于没有采样到的像素点的信息无法反映到新图中，因此会有失真。为解决这个问题，引入基于局部均值的图像缩小法。



步骤：

1. 计算新图像的大小，计算采样间隔$Δi=1/k1，Δj=1/k2$。k1、k2是缩小幅度
2. 对新图像的像素$g(i, j)$，计算其在原图像中对应的子块$f^{(i,j)}$： 

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/minimize1.png)

3.  $g(i, j) = f^{(i,j)}的均值$

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/minimize2%20(2).png?x-oss-process=image/resize,p_50)

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/minimize3.png?x-oss-process=image/resize,p_50)



```csharp
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

```

![缩小前](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/mini.png?x-oss-process=image/resize,p_50)

![缩小后](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/mini2.png?x-oss-process=image/resize,p_50)



# 放大-双线性插值放大算法

放大必定会导致图像空穴的产生，需要使用**插值**填补，这里采用效果比较好的**双线性插值法**

双线性插值法假设：

1. 首先灰度级在纵向方向上是线性变化的  

2. 然后假定灰度级在横向方向上也是线性变化的。



步骤：

1. 先按照基于$G(i,j)=F(i*c1,\ j*c2)$，确定每一个原图像的像素在新图像中对应的子块。

   ![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/max1.png)

   

2. 对新图像中每一个子块，仅对其一个像素进行填充。在每个子块中选取一个填充像素的方法如下：

   对右下角的子块，选取子块中右下角的像素；

   对末列、非末行子块，选取子块中的右上角像素；

   对末行、非末列子块，选取子块中的左下角像素；

   对剩余的子块，选取子块中的左上角像素。

   ![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/max2.png?x-oss-process=image/resize,p_40)

3. 通过双线性插值方法计算剩余像素的值。

   对所有填充像素所在列中的其他像素的值，可以根据该像素的上方与下方的已填充的像素值，采用双线性插值方法计算得到。 

   ![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/max3.png?x-oss-process=image/crop,y_3/resize,p_60)

   ![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/max4.png?x-oss-process=image/resize,p_60)

   

   对剩余像素的值，可以利用该像素的左方与右方的已填充像素的值，通过线性插值方法计算得到。

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/max5.png?x-oss-process=image/resize,p_60)

​										      ![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/max6.png?x-oss-process=image/crop,y_3/resize,p_60)

​			



```csharp
/// <summary>
/// 放大
/// </summary>
/// <param name="k1">高扩大幅度</param>
/// <param name="k2">宽扩大幅度</param>
private void maximize(double k1, double k2)
{
    int nw = (int)Math.Round(bmp.Width * k2),
    nh = (int)Math.Round(bmp.Height * k1);
    Bitmap bmp_ = new Bitmap(nw, nh);
	
    ArrayList widx = new ArrayList(), hidx = new ArrayList(); //从原图得到像素的列号与行号

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
```



![放大前](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/max7.png?x-oss-process=image/resize,p_50)

![放大后](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/max8.png?x-oss-process=image/resize,p_50)



# 错切

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/shear1.png?x-oss-process=image/resize,p_70)

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/shear2.png?x-oss-process=image/resize,p_70/crop,y_3,x_3)

```csharp
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
```

![错切前](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/mini.png?x-oss-process=image/resize,p_50)

![错切后](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/shear3.png?x-oss-process=image/resize,p_50)





# 灰度化

将彩色图片**灰度化**，只需要将每一个像素的RGB值都设置为一样的即可。

常见的RGB值计算公式为：$Gray(i,j)=[R(i,j)+G(i,j)+B(i,j)]÷3$

但是因为人眼对颜色的感知能力不同，所以有一个比较合理的公式：

$$Gray(i,j)=0299×R(i,j)+0.587×G(i,j)+0.114×B(i,j)$$



```csharp
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
```



我们可以将**灰度直方图**绘制出来：

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DIP
{
    public partial class HistForm : Form
    {
        //利用构造函数实现窗体之间的数据传递
        public HistForm(Bitmap bmp)
        {
            InitializeComponent();

            //把主窗体的图像数据传递给从窗体
            bmpHist = bmp;
            //灰度级计数
            countPixel = new int[256];  //8位可表示256个灰度级
        }

        //图像数据
        private Bitmap bmpHist;
        //灰度等级
        private int[] countPixel;

        /// <summary>
        /// 计算各个灰度级所具有的像素个数
        /// </summary>
        private void HistForm_Load(object sender, EventArgs e)
        {
            int bytes = bmpHist.Width * bmpHist.Height;
            byte[] grayValues = new byte[bytes];
            
            //灰度等级数组清零
            Array.Clear(countPixel, 0, 256);
            //计算各个灰度级的像素个数
            for (int i=0;i<bmpHist.Width;i++)
                for(int j=0;j<bmpHist.Height;j++)
                {
                    byte temp = bmpHist.GetPixel(i, j).G;
                    countPixel[temp]++;
                }
        }


        /// <summary>
        /// 绘制直方图
        /// </summary>
        private void HistForm_Paint(object sender, PaintEventArgs e)
        {
            //获取Graphics对象
            Graphics g = e.Graphics;

            //创建一个宽度为1的黑色钢笔
            Pen curPen = new Pen(Brushes.Black, 1);

            //绘制坐标轴
            g.DrawLine(curPen, 50, 240, 320, 240);//横坐标
            g.DrawLine(curPen, 50, 240, 50, 30);//纵坐标

            //绘制并标识坐标刻度
            g.DrawLine(curPen, 100, 240, 100, 242);
            g.DrawLine(curPen, 150, 240, 150, 242);
            g.DrawLine(curPen, 200, 240, 200, 242);
            g.DrawLine(curPen, 250, 240, 250, 242);
            g.DrawLine(curPen, 300, 240, 300, 242);
            g.DrawString("0", new Font("New Timer", 8), Brushes.Black, new PointF(46, 242));
            g.DrawString("50", new Font("New Timer", 8), Brushes.Black, new PointF(92, 242));
            g.DrawString("100", new Font("New Timer", 8), Brushes.Black, new PointF(139, 242));
            g.DrawString("150", new Font("New Timer", 8), Brushes.Black, new PointF(189, 242));
            g.DrawString("200", new Font("New Timer", 8), Brushes.Black, new PointF(239, 242));
            g.DrawString("250", new Font("New Timer", 8), Brushes.Black, new PointF(289, 242));
            g.DrawLine(curPen, 48, 40, 50, 40);
            g.DrawString("0", new Font("New Timer", 8), Brushes.Black, new PointF(34, 234));

            //绘制直方图
            double temp = 0;
            int bytes = bmpHist.Width * bmpHist.Height;
            for (int i = 0; i < 256; i++)
            { 
                //纵坐标长度
                temp = 800.0 * countPixel[i] / bytes;
                g.DrawLine(curPen, 50 + i, 240, 50 + i, 240 - (int)temp);
            }
            //释放对象
            curPen.Dispose();
        }

    }
}
```



![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/gray.png)



# 拓展压缩的线性灰度变换

由于图像的亮度范围不足或非线性可能会使图像的对比度不理想。

所以采用图像灰度值变换方法，即改变图像像素的灰度值，以改变图像灰度的动态范围，增强图像的对比度。

灰度变换分为线性变换 (正比或反比)和非线性变换。非线性变换有对数的(对数和反对数的)，幂次的(n次幂和n次方根变换) 。下面是一些灰度变换曲线。

![用于图像增强的某些基本灰度变换函数](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/gray2.png?x-oss-process=image/resize,p_70)



为了突出感兴趣目标所在的灰度区间，相对抑制那些不感兴趣的灰度空间，可采用**分段线性变换**

在扩展感兴趣的[a,b]区间的同时，为了保留其他区间的灰度层次，也可以采用其它区间压缩的方法，既有扩有压，变换函数为

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/linegray.png)





![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/linegray2.png)





```csharp
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
```

![变换后的图片与变换前（左前，右后）后的直方图](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/linegray3.png)





# 直方图均衡化



