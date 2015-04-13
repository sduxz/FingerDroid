/*#define MONO
using System;
using System.Collections.Generic;
using System.Text;

#if !MONO
using System.Windows.Media.Imaging;
#endif
//using System.Windows.Media;
using System.Windows;
using System.IO;
//using System.Drawing;
using Android.Graphics;

namespace SourceAFIS.General
{
    public static class WpfIO
    {
        //#if !MONO
        public static Bitmap GetBitmapSource (byte[,] pixels)
        {
            int width = pixels.GetLength (1);
            int height = pixels.GetLength (0);
            byte[] flat = new byte[width * height];
            for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                    flat [(height - 1 - y) * width + x] = pixels [y, x];
            MemoryStream ms = new MemoryStream (flat);
            Bitmap bm = (Bitmap)Image.FromStream (ms);
            ms.Close ();
            return bm;
            //return BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, flat, width);
        }

        public static byte[,] GetPixels (Bitmap bitmap)
        {
            //FormatConvertedBitmap converted = new FormatConvertedBitmap(bitmap, PixelFormats.Gray8, null, 0.5);
            MemoryStream ms = new MemoryStream ();
            bitmap.Save (ms, System.Drawing.Imaging.ImageFormat.Bmp);
            byte[] flat = ms.ToArray ();
            ms.Close ();
            int width = bitmap.Width;
            int height = bitmap.Height;
            //int width = (int)converted.PixelWidth;
            //int height = (int)converted.PixelHeight;

//...........................................changed by sduxz..............................................
            //System.Diagnostics.Debug.WriteLine("w:{0},h:{1}",width,height);
            //实例化一个矩形窗
            //var cut = new Int32Rect(620, 280, 250, 350);
            //width = cut.Width;
            //height = cut.Height;
//...........................................change end..............................................
            

            //byte[] flat = new byte[width * height];

            //changed by sduxz............................................................................
            //converted.CopyPixels(flat, width, 0);
            //加窗取像素点
            //converted.CopyPixels(cut,flat, width, 0);
            //BitmapSource bmp = BitmapSource.Create(250, 350, 0, 0, PixelFormats.Gray8, null, flat, width);
            //FileStream stream = new FileStream("myfinger1_2.bmp", FileMode.Create);
            //BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            //encoder.Frames.Add(BitmapFrame.Create(bmp));
            //encoder.Save(stream);

            byte[,] pixels = new byte[height, width];
            for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                    pixels [y, x] = flat [(height - y - 1) * width + x];

            return pixels;
        }

        public static Bitmap Load (string filename)
        {
//            BitmapImage image = new BitmapImage ();
//            //BitmapImage属性初始化，以endinit结尾
//            image.BeginInit ();
//            image.CacheOption = BitmapCacheOption.OnLoad;
//            image.UriSource = new Uri (filename, UriKind.RelativeOrAbsolute);
//            image.EndInit ();
//            return image;
            Bitmap bitmap = new Bitmap (filename);
            return bitmap;
        }

        public static void Save (Bitmap image, string filename)
        {
//            PngBitmapEncoder encoder = new PngBitmapEncoder ();
//            encoder.Frames.Add (BitmapFrame.Create (image));
//            using (FileStream stream = new FileStream (filename, FileMode.Create, FileAccess.Write))
//                encoder.Save (stream);
            image.Save (filename, System.Drawing.Imaging.ImageFormat.Png);
        }
        //#endif
    }
}
*/