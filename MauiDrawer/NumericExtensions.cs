using System;
namespace MauiDrawer
{
	public static class NumericExtensions
	{
		public static double Clamp(this double val, double min, double max)
		{
			if (val < min)
			{
				return min;
			}

			if (val > max)
			{
				return max;
			}

			return val;
		}
	}
}

