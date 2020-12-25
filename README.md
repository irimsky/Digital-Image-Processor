数字图像处理大作业

使用C# 从底层像素级别实现数字图像处理课上所学的部分算法（尽量不使用封装好的现成的库）



**GitHub源码**：[https://github.com/irimsky/Digital-Image-Processor](https://github.com/irimsky/Digital-Image-Processor)



注：

- 本项目将使用.NET中 [System.Drawing.Bitmap](https://docs.microsoft.com/zh-cn/dotnet/api/system.drawing.bitmap?view=dotnet-plat-ext-3.1) 类来作为位图的数据结构






# 位图文件操作



## 读取位图头文件

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

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201224233902950.png?x-oss-process=image/resize,p_70)





# 几何变换

## 旋转-反变换公式法

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
    UpdateImgimg(ref bmp_);
}
```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201224234157202.png?x-oss-process=image/resize,p_70)



## 缩小-局部均值缩小法

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
    UpdateImgimg(ref bmp_);
}

```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201224234315638.png?x-oss-process=image/resize,p_70)



## 放大-双线性插值放大算法

放大必定会导致图像空穴的产生，需要使用**插值**填补，所以这里采用效果比较好的**双线性插值法**

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
    UpdateImgimg(ref bmp_);
}
```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201224234512126.png?x-oss-process=image/resize,p_70)



## 错切

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
    UpdateImgimg(ref bmp_);
}
```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201224234732105.png?x-oss-process=image/resize,p_70)



# 灰度变换

## 灰度化

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
    UpdateImgimg(ref bmp_);
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



## 拓展压缩的线性灰度变换

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
    UpdateImgimg(ref bmp_);
    HistForm histForm = new HistForm(bmp);
    histForm.Show();
}
```

![变换后的图片与变换前（左前，右后）后的直方图](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/DIP/linegray3.png)





## 直方图均衡化

**直方图均衡化**是将原图像通过某种变换，得到一幅灰度直方图为均匀分布的新图像的方法。

设图像均衡化处理后，图像的直方图是平直的，即各灰度级具有相同的出现频数(大体相同)，那么由于灰度级具有**均匀**的概率分布，图像看起来就更清晰了。

**步骤**：

1. 计算原图的灰度直方图

设$f、g$分别为原图与处理后的图像，$N$为图像总体像素个数，统计$h(i)$为灰度 $i$ 的像素在原图中的个数。$0\le i\le255$



2. 由原图直方图计算灰度分布概率

原图的灰度分布概率 $hs(i) = h(i)/N$



3. 计算图像各个灰度级的累积分布概率

各灰度级的累计分布 $h_{p}(i) = \sum_{k=0}^{i}hs(k)$ 



4. 进行直方图均衡化计算，得到新图像的灰度值

$g(i,j)=255 * h_{p}(k)$

```csharp
/// <summary>
/// 将灰度图像的灰度直方图均衡化
/// </summary>
private void Equalization()
{
    int[] grayValue = new int[256];
    Array.Clear(grayValue, 0, 256);
    // 统计灰度分布
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
    
    // 计算累积分布概率
    for (int i = 0; i < 256; i++)
    {
        cnt += grayValue[i];
        hp[i] = (int)Math.Round(cnt * 255.0 / sum);
    }
    
	// 得到新图
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
    // 展示灰度直方图
    HistForm histForm = new HistForm(bmp);
    histForm.Show();
}
```

![均衡化前（左）后（右）](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201224235743773.png?x-oss-process=image/resize,p_70)



# 噪声抑制



## 添加噪声

### 高斯噪声

高斯噪声又称正态噪声。噪声位置是一定的，即每一点都有噪声，但噪声的幅值是随机的。

高斯随机变量的PDF为：$$p(z)=\frac{1}{\sqrt{2\pi\sigma}}e^{-(z-\mu)^{2}/2}$$

其中z表示灰度值，$\mu$表示z的平均值或期望值，$\sigma$表示z的标准差

![高斯噪声概率密度函数](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201223114740.png?x-oss-process=image/resize,p_60)



```csharp
/// <summary>
/// 为高斯噪声生成随机种子
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
```

![加噪声前后（k=16)](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225000201191.png)



### 椒盐噪声

椒盐噪声又称脉冲噪声。噪声的幅值基本相同，但噪声出现的位置是随机的。

双极均匀分布噪声的PDF为：

$$p(z)=\begin{cases} P_{a} & \ {z=a}\\ P_{a} & \ {z=b}\\ P_{a} & \ {其他} \end{cases}$$

![椒盐噪声概率密度函数](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201223114807.png?x-oss-process=image/resize,p_60)



```csharp
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
```



![image-20201225000352845](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225000352845.png)

## 滤波去噪

### 均值滤波

**均值滤波**是指在图像上，对待处理的像素给定一个模板，该模板包括了其周围的邻近像素。将模板中的全体像素的均值来替代原来的像素值的方法。

设$f(i，j)$为给定的含有噪声的图像，经过简单邻域平均处理后为$ g(i，j)$，在数学上可表现为： 

$$g(x,y)=\frac{1}{M}\sum_{(i,j)\in s}f(i,j)$$  

式中S是所取邻域中的各邻近像素的坐标，M是邻域中包含的邻近像素的个数.



**步骤**

1. 取得图像大小、数据区，并把数据区复制到缓冲区中；
2. 循环取得各点像素值；取得该点周围8像素值的平均值
3. 把缓冲区中改动的数据复制到原数据区中。

![image-20201223124513189](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201223124513.png?x-oss-process=image/resize,p_60)



其主要特点是：**算法简单，计算速度快，但会造成图像一定程度上的模糊**。



```csharp
/// <summary>
/// 均值滤波
/// </summary>
private void EvenFilter()
{
    Bitmap bmp_ = new Bitmap(bmp);
    for (int i = 1; i < bmp.Width - 1; i++)
    {
        for (int j = 1; j < bmp.Height - 1; j++)
        {
            int rsum = 0, gsum = 0, bsum = 0;
            for (int ii = -1; ii <= 1; ii++)
            {
                for(int jj = -1; jj <= 1; jj++)
                {
                    int x = i + ii, y = j + jj;
                	Color c = bmp.GetPixel(x, y);
                	rsum += c.R; gsum += c.G; bsum += c.B;
                }
            }
            bmp_.SetPixel(i, j, Color.FromArgb(rsum / 9, gsum / 9, bsum / 9));
        }
    }
    UpdateImg(ref bmp_);
}
```

![对高斯噪声](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225001613045.png)

![对椒盐噪声](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225000759207.png)

### 中值滤波

N×N中值滤波器，计算灰度图像f中以像素$f(i，j)$为中心的N×N屏蔽窗口(N=3，5，7…)内灰度的中值为$u$，作$(i，j)=u$ 处理，$N$由用户给定。
例如做3×3的模板，对9个数排序，取第5个数替代原来的像素值。

**步骤**：

1. 取得图像大小、数据区，并把数据区复制到缓冲区中；
2. 循环取得各点像素值；
    （1） 对以该点像素为中心的N×N屏蔽窗口包括的各点像素值进行排序，得到中间值。
    （2）把该点像素值置为中间值；
3. 把缓冲区中改动的数据复制到原数据区中。

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201223125722.png?x-oss-process=image/resize,p_50)

```csharp
/// <summary>
/// 中值滤波
/// </summary>
private void MidFilter()
{
    Bitmap bmp_ = new Bitmap(bmp);
    for (int i = 1; i < bmp.Width - 1; i++)
    {
        for (int j = 1; j < bmp.Height - 1; j++)
        {
            ArrayList rarr = new ArrayList(),
            garr = new ArrayList(),
            barr = new ArrayList();
            for (int ii = -1; ii <= 1; d++)
            {
                for(int jj=-1;jj<=1;jj++)
                {
                    int x = i + ii, y = j + jj;
                	Color c = bmp.GetPixel(x, y);
                	rarr.Add(c.R);
                	garr.Add(c.G);
                	barr.Add(c.B);
                }
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
```

![对高斯噪声](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225001839431.png)

![对椒盐噪声](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225000849004.png)



从以上结果可以看出：

- 对于**椒盐噪声**，**中值滤波**效果优于**均值滤波**。因为中值滤波的原理是取合理的邻近像素值来替代噪声点。噪声的均值不为0，所以均值滤波不能很好地去除噪声点。
- 对于**高斯噪声**，**均值滤波**效果优于**中值滤波**。因为高斯噪声是分布在每点像素上的。因为图像中的每点都是污染点，所中值滤波选不到合适的干净点。而且正态分布的均值为0，所以根据统计数学，均值可以消除噪声。



## 边界保持平滑滤波器

前面的均值和中值滤波处理结果可知，经过平滑（特别是均值）滤波处理之后，图像就会**变得模糊**。

简单的采用中值或均值，都会降低**边界**的灰度显著性，导致图像的模糊。 因此引入**边界保持类**的平滑滤波。

在进行平滑处理时，首先判别当前像素是否为边界上的点：

- 如果是，则不进行处理

- 如果不是，则进行平滑处理

### 灰度最小方差的均值滤波器

**灰度最小方差的均值滤波器**又称选择掩模滤波器。

其取5×5的模板窗口，以中心像素为基准点，制作4个五边形、4个六边形、一个边长为3的正方形共9个形状的屏幕窗口，分别计算每个窗口内的平均值及方差。

由于含有尖锐边缘的区域，方差必定比平缓区域大，因此**采用方差最小的屏蔽窗口进行平均化**。这种方法在完成滤波操作的同时，又不破坏区域边界的细节。



![9个模板](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201223132306.png?x-oss-process=image/resize,p_40)



灰度最小方差的均值滤波器的**特点**：在去噪能力上弱于传统的均值、中值滤波，但在**保留图像边缘和细节**能力方面要强于前者。



**步骤**：

1. 以$f(x,y)$为中心，上图所示的9个模板中的原有像素的灰度分布方差。
2. 找出方差值最小的模板位置。
3. 将所选择出的模板中像素的灰度平均值代替$f(x,y)$
4. 对图像中所有处于滤波范围内的像素点均进行相同处理。



```csharp
/// <summary>
/// 灰度最小方差均值滤波器
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
```

![对高斯噪声](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225002049733.png)

![对椒盐噪声](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225001000852.png)

### K近邻平滑滤波器

K近邻(KNN)平滑滤波器的**核心**是：在一个与待处理像素邻近的范围内，寻找出其中像素值与之最接近的**K个邻点**(是指灰度上最邻近)，将该K个邻点的均值（或中值）替代原像素值。



若待处理像素是非噪声点，则通过选择像素值与之相近的邻点，可保证在进行平滑处理时，基本上是同一区域的像素值的计算。可以**保持图像清晰度**。
若待处理点是噪声点，因噪声本身的孤立性，则通过邻点的平滑处理，可对其进行抑制



**步骤**：

1. 以待处理像素为中心，作一个 $m*m$ 的作用模板;
2. 在模板中，选择$K$个与待处理像素的灰度差为最小的像素;
3. 将这$K$个像素的灰度均值（中值）替换掉原来的像素值.



```csharp
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
```

![对高斯噪声](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225002148949.png)

![对椒盐噪声](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225001058248.png)



## 二值图像去噪

### 黑白点噪声滤波

**方法：**消去二值图像$f(i，j)$上的黑白的噪声，当$f(i，j)$周围的8个像素的平均值为$a$时，若$|f(i，j)-a|$的值在127.5以上，则对$f(i，j)$的黑白进行翻转，若不到127.5则$f(i，j)$不变。

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225003327835.png?x-oss-process=image/resize,p_80)



```csharp
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
```



### 消除孤立黑点

**方法：**

- 4点邻域的情况下，若黑像素$f(i，j)$的上下左右4个像素全为白(0)，则$f(i，j)$也取为0。

- 8点邻域的情况下，若黑像素$f(i，j)$的周围8个像素全为白(0)，则$f(i，j)$也取为0。



![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225003557489.png?x-oss-process=image/resize,p_80)



```csharp
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
```





# 锐化与边缘检测

噪声和边缘都是高频段，滤波后不清晰，因此我们需要引入<u>**锐化**</u>使图像的物体边缘变得清晰，目标区域清楚，鲜明。其作用就是**边缘部分灰度反差增强**



## 双向梯度锐化

**梯度锐化的一般思路**：由梯度的计算可知，在图像灰度变化较大的边沿区域其梯度值大，在灰度变化平缓的区域梯度值较小，而在灰度均匀的区域其梯度值为零。所以加强梯度值大的像素灰度，或者降低梯度值小的像素灰度，以此达到**突出细节实现锐化**的效果。



梯度锐化常用的方法有:

- 直接以梯度值代替；
- 辅以门限判断；
- 给边缘规定一个特定的灰度级;
- 给背景规定灰度级;
- 根据梯度二值化图像



我们这里选择"根据梯度二值化".

$d_{x}=f(i,j)-f(i-1,j)$

$d_{y}=f(i,j)-f(i,j-1)$

梯度$G(i, j) = \sqrt{d_{x}^{2} + d_{y}^{2}}$

设取阈值为$T$，若$G(i,j)>=T$，则$g(i,j)=255$；否则 $g(i,j)=0$



**步骤**：

1. 获得原图像的首地址，及图像的高和宽；
2. 开辟一块内存缓冲区，并初始化为255；
3. 计算图像的像素的梯度；
4. 将结果保存在内存缓冲区比较像素的梯度是否大于30，是则将灰度值置为255，否则置0；
5. 将内存中的数据复制到原图像的数据区。 

```csharp
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
```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225003832320.png?x-oss-process=image/resize,p_80)





## 边缘检测

前面的锐化处理结果对于人工设计制造的具有矩形特征物体的边缘的提取很有效。但是，对于<u>不规则形状</u>的边缘提取，则存在**信息的缺损**。

所以希望提出对任何方向上的边缘信息均敏感的锐化算法。因为这类锐化方法要求对边缘的方向没有选择，所以称为**无方向的锐化算法**。



### Roberts算子

$d_{x}=f(i+1,j+1)-f(i,j)$

$d_{y}=f(i+1,j)-f(i,j+1)$

梯度$G(i, j) = \sqrt{d_{x}^{2} + d_{y}^{2}}$

Roberts算子如下：

![Roberts算子](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201224163457.png)



```csharp
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
```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225004044372.png?x-oss-process=image/resize,p_80)



### Sobel算子

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201224171633.png)

梯度$G(i, j) = \sqrt{d_{x}^{2} + d_{y}^{2}}$

![Sobel算子](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201224163848.png)

```csharp
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
```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225004241726.png?x-oss-process=image/resize,p_80)





Roberts与Sobel对比：

| 算子    | 特点                                                         |
| ------- | :----------------------------------------------------------- |
| Roberts | 实现简单，对具有陡峭的低噪声图像效果比较好。但锁提取的边缘比较粗，边缘定位不准确，且对噪声敏感。 |
| Sobel   | 对灰度渐变和噪声较多的图像效果比较好，且边缘定位准确。       |



**问题：**

以上两种算子属于一阶微分算子。当遇到**"尖顶型灰度变化"**时，很难将其识别出。比如下图的渐变细节，一阶微分则较难识别。

![渐变的细节](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201224174206.png?x-oss-process=image/resize,p_30)

但采用二阶微分能够更加获得更丰富的景物细节。



### Laplacian算子

二阶微分算子的**原理**如下：

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201224175124.png?x-oss-process=image/resize,p_40)



写成模板系数形式形式即为**Laplacian算子**：

![image-20201224175249641](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201224175249.png?x-oss-process=image/resize,p_60)



拉普拉斯**对噪声敏感**，会产生<u>双边效果</u>。不能检测出边的方向。故通常不直接用于边的检测，只起辅助的角色，检测一个像素是在边的亮的一边还是暗的一边利用零跨越，确定边的位置。



```csharp
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
            // 实现锐化效果则需要原图加上该点
            // gg += bmp.GetPixel(i, j).R;
            gg = Math.Min(gg, 255);
            gg = Math.Max(gg, 0);
            bmp_.SetPixel(i, j, Color.FromArgb(gg, gg, gg));
        }
    }

    UpdateImg(ref bmp_);
}
```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225004652028.png?x-oss-process=image/resize,p_80)



以Sobel与Laplacian为例,对比**一阶微分算子**与**二阶微分算子**：

| 算子      | 特点                                                         |
| --------- | ------------------------------------------------------------ |
| Sobel     | 获得的边界是比较粗略的边界，反映的边界信息较少，但是所反映的边界比较清晰 |
| Laplacian | 获得的边界是比较细致的边界。反映的边界信息包括了许多的细节信息，但是所反映的边界不是太清晰 |



### Wallis算子

考虑到人的视觉特性中包含一个对数环节，因此在锐化时，加入**对数处理**的方法来改进。

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201224180501.png)

注意：

- 为了防止对0取对数，计算时实际上是用 $log(f(i,j)+1)$

- 因为对数值很小$log(256)=5.45$，所以计算时用 $46*log(f(i,j)+1)$



**特点：**Wallis算法基于Laplacian算子考虑了人眼视觉特性，因此，与其他算法相比，可以对**暗区的细节**进行比较好的锐化。



```csharp
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

            gg = Math.Min(gg, 255);
            gg = Math.Max(gg, 0);
            bmp_.SetPixel(i, j, Color.FromArgb(gg, gg, gg));
        }
    }
    UpdateImg(ref bmp_);
}
```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225004756771.png?x-oss-process=image/resize,p_80)



### LoG算子

**问题：**

在较大噪声的场合，由于微分运算会起到放大噪声的作用，因此，梯度算子和拉普拉斯算子**对噪声比较敏感**。

因此 **LoG算子**（Laplacian of Gauss）先对图像进行高斯平滑滤波以抑制噪声，然后再使用**Laplacian算子**进行求微分。

```csharp
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
```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225004902692.png)





# 图像分割和二值图像测量

图像分割将图像分为一些有意义的子区域，通过某种方法，使得画面场景被分为“目标物”及“非目标物”两类，即将图像的像素变换为黑、白两种。然后可以对这些区域进行描述，相当于提取出某些目标区域图像的特征。



## 图像分割方法

### 基于图像灰度分布的阈值方法——迭代阈值图像分割

一种简单的图像分割原理：选定一个阈值$T$，灰度大于或等于 $T$ 的像素点置为白，否则置为黑。

这里选择**迭代阈值图像分割**方法来确定阈值。



**计算方法：**

1.  设定阈值$T$，初始为127；

2. 通过初始阈值$T$，把图像的平均灰度值分成两组 $R1$ 和 $R2$；
3. 计算着两组平均灰度值$μ1$和$μ2$；
4. 重新选择阈值$T$，新的T定义为：$T=(μ1+μ2)/2$;
5. 循环做第二步到第四步，一直到 $T$ 的值不再发生改变，那么我们就获得了所需要的阈值。 



```csharp
/// <summary>
/// 迭代阈值分割所用求阈值子函数
/// </summary>
/// <param name="grayCount">灰度统计</param>
/// <returns>求得的阈值</returns>
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
    // 统计灰度
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
    
	// 根据阈值分割图像
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
```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225011148693.png?x-oss-process=image/resize,p_70)



### 基于图像灰度空间分布的阈值方法——灰度-局部灰度均值散布图法

前面所讲的阈值方法是单一阈值。即对整幅图像采用一个被确定的阈值进行分割处理。

但是通常图像之所以可以呈现景物的概念，是因为<u>像素与像素之间存在着一定的相关性</u>，如果在确定阈值时，除了当前像素本身的灰度值外，再**考虑其与邻近像素之间的关系**，就可以获得更科学的判别分割。



这里选用**灰度-局部灰度均值散布图法**



**原理：**如果某个像素与其周围领域中的均值偏差较大，则说明该点是边界上的点或者是噪声点。

**步骤：**

1. 以图像灰度为横轴，局部灰度均值（如3*3模板下的均值）为纵轴，构造一个图像分布的散布图

- 对于对角线上的点分布，对应于目标或者背景内部的点
- 对于离开对角线的点，则对应于区域边界上的点

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201224212741.png?x-oss-process=image/resize,p_60)

2. 统计对角线上的灰度
3. 用任意灰度阈值的评价方法（这里仍然用迭代阈值分割）求出对角线上像素灰度的阈值
4. 用该阈值分割图像



```csharp
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
```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225011318157.png?x-oss-process=image/resize,p_70)



![车牌号识别](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225014134806.png?x-oss-process=image/resize,p_60)





### 边缘检测法——轮廓提取法

图像边缘是图像局部特性不连续性的反映，它标志着一个区域的终结和另一个区域的开始。



**原理：**

掏空内部点：如果原图中有一点为黑，且它的8个相邻点皆为黑，则将该点删除（意思就是把该点置为背景白色，而轮廓即边始终是黑色）。



**步骤：**

1. 获取原图像像素的首地址，及图像的高和宽。
2. 开辟一块内存缓冲区，将原图像素保存在内存中。
3. 将像素点的8邻域像素读入组中，如果8个邻域像素都和中心点相同，在内存缓冲区中将该像素点置白，否则保持不变。 
4. 重复执行(3)，对每一个像素进行处理。
 5. 将内存中的数据复制到原图像中



```csharp
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
```

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225011459394.png?x-oss-process=image/resize,p_70)





## 二值图像的测量

对于二值图像中的一个黑色连通块

面积：**黑色像素的个数**

周长：**黑色像素轮廓的个数**

![](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/20201224214348.png?x-oss-process=image/resize,p_50)



1. 求面积使用**洪泛法**（深度优先搜索）

   **步骤：**

   遍历图像中的像素点，遇到一个**未打上标记的**黑色像素点便开始以下算法：

- 将该像素点打上标记，统计量加1
- 对该像素点的**未被打上标记**的邻近像素点继续执行此操作，直到无法继续
- 此时统计量的值即为该连通块的面积。将统计量置为0，标记加1



2. 求周长可以使用**轮廓跟踪法**

![轮廓跟踪法步骤](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201224233407124.png)



```csharp
//连通块类
public class Block
{
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

// 测量二值图类
class BinaryMeasurer
{
    public List<Block> arr;
    int[,] flag, gray;
    int mk = 1, width, height;
    // 顺时针的八个方向的坐标变化
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

    //轮廓跟踪法
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

    // 判断方向下标函数
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
    // 展示结果
    FlagWindow fw = new FlagWindow(oriImg, measure.arr);
    fw.Show();
}
```

![测量结果](https://irimskyblog.oss-cn-beijing.aliyuncs.com/content/image-20201225012746438.png)
