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
	}
}
