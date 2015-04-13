using System;

namespace TestAlgorithm
{
	public static class ShowImage
	{

		public static byte[,] floatToByte(float[,] floats)
		{
			int width = floats.GetLength(1);
			int height = floats.GetLength(0);
			byte[,] dis = new byte[height,width];
			for(int i = 0;i < width;i++)
				for(int j= 0;j <height;j++)
					dis[j,i] = (byte)(((floats[j,i] + 1)/2)*255);
			return dis;
		}
	}
}

