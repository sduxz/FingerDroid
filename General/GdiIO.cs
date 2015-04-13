using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using Android.Graphics;
using System.IO;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Provider;
using Java.Nio;
//using System.Drawing.Imaging;
//using SystemPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace SourceAFIS.General
{
    public static class GdiIO
    {
        public static byte[,] GetPixels(Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            /*BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, SystemPixelFormat.Format24bppRgb);

            byte[] bytes = new byte[height * data.Stride];
            try
            {
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            byte[,] result = new byte[height, width];
            for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    int offset = (height - 1 - y) * data.Stride + x * 3;
                    result[y, x] = (byte)((bytes[offset + 0] + bytes[offset + 1] + bytes[offset + 2]) / 3);
                }
            return result;  */


            ////MemoryStream baos = new MemoryStream ();
            ////bmp.Compress (Bitmap.CompressFormat.Png, 100, baos);

            //IntBuffer ibuffer = IntBuffer.Allocate (550 * 550);
            //bmp.CopyPixelsToBuffer (ibuffer);

            //int[] flats = new int[ibuffer.Capacity()];
            //ibuffer.Get (flats, 0, flats.Length);
            //int[] flats = ibuffer.ToArray<int> ();
            //ibuffer.Clear ();
            int[] flat = new int[width*height];
            bmp.GetPixels(flat,0,width,0,0,width,height);

            ////byte[] flat = baos.ToArray ();

            //Console.WriteLine ("---------------test----------------");
            //Console.WriteLine ("width= {0} ,height= {1}", width,height);
            //Console.WriteLine ("byte[] flat.length = {0}", flat.Length);
            ////Console.WriteLine ("{0}", baos.Length);
            //Console.WriteLine ();
            byte[,] pixels = new byte[height, width];
            for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                    pixels [y, x] = ARGBtoGray8(flat [(height - y - 1) * width + x]);

            return pixels;
        }

        public static Bitmap GetBitmap(byte[,] pixels)
        {
            int width = pixels.GetLength(1);
            int height = pixels.GetLength(0);
            //Bitmap bmp = new Bitmap(width, height, SystemPixelFormat.Format24bppRgb);
            //BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, SystemPixelFormat.Format24bppRgb);
            
            int[] colors = new int[height * width];//new byte[height * data.Stride];
            for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    int offset = (height - 1 - y) * width + x;//data.Stride + x * 3;
					int pixels1 = (int)pixels[y,x];
					colors [offset] = (int)(0xff000000 + pixels1 + (pixels1 << 8) + (pixels1 << 16));
                    //bytes[offset + 1] = pixels[y, x];
                    //bytes[offset + 2] = pixels[y, x];
                }
			return Bitmap.CreateBitmap (colors, width, height, Bitmap.Config.Argb8888);

            /*try
            {
                Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            return bmp; */
        }

        public static byte[,] Load(string filename)
        {
            //using (Image fromFile = Bitmap.FromFile(filename))
            //{
            using (Bitmap bmp = BitmapFactory.DecodeFile(filename))//new Bitmap(fromFile))
                {
                    return GetPixels(bmp);
                }
           // }
        }

        public static byte ARGBtoGray8(int col)
        {
			byte gray =(byte)(Android.Graphics.Color.GetRedComponent (col) * 0.3 + Android.Graphics.Color.GetGreenComponent (col) * 0.59 + Android.Graphics.Color.GetBlueComponent (col) * 0.11); //Android.Graphics.Color.GetGreenComponent (col);
            return gray;
        }
    }
}
