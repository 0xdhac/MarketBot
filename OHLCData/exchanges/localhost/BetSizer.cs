using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.exchanges.localhost
{
	class BetSizer
	{
		public decimal StartingBalance { get; private set; }
		public decimal BetPercent { get; set; }
		public decimal ResizeEvery { get; set; }
		private int ResizeCount = 0;
		public Balance Balance { get; set; } = null;
		public decimal BetAmount { get; private set; }

		public void UpdateBetAmount()
		{
			Debug.Assert(BetPercent != 0 && ResizeEvery != 0 && Balance != null);

			if(ResizeCount++ % ResizeCount == 0)
			{
				//Console.WriteLine($"Resized");
				BetAmount = Balance.Total * BetPercent;
			}
		}
	}
}
