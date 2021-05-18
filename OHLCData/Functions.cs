using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot
{
	public static class Functions
	{
		public static string GetRandomString(int length)
		{
			Debug.Assert(length > 0);

			string characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			string output = "";
			Random rand = new Random();

			while (output.Length < length)
			{
				char random_char = characters[rand.Next(0, characters.Length)];
				output += random_char;
			}

			return output;
		}

		public static long LongRandom(long min, long max, Random rand)
		{
			long result = rand.Next((Int32)(min >> 32), (Int32)(max >> 32));
			result = (result << 32);
			result = result | (long)rand.Next((Int32)min, (Int32)max);
			return result;
		}

		public static void CreateTimer(TimeSpan delay, Action function)
		{
			Task.Run(() =>
			{
				int ms = Convert.ToInt32(delay.TotalMilliseconds);
				System.Threading.Thread.Sleep(ms);

				function();
			});
		}
	}
}
