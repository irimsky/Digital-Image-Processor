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
    }
}
