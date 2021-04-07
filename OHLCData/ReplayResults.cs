using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot
{
	public class ReplayResults
	{
		public string Symbol { get; set; }
		public decimal? Profitability { get; set; }
		public OHLCVInterval Interval { get; set; }
		public decimal Periods { get; set; }
		public decimal ProfitRiskRatio { get; set; }
		public decimal Trades { get; set; }
		public decimal StartTotal { get; set; }
		public decimal EndTotal { get; set; }
		public TimeSpan TimeInLosingTrades { get; set; }
		public TimeSpan TimeInWinningTrades { get; set; }
		public TimeSpan TimeInTrades { get; set; }
		public int Losses { get; set; }
		public int Wins { get; set; }

		public decimal Fitness 
		{ 
			get
			{
				return Profitability.Value * (1 / ((decimal)TimeInTrades.TotalDays / 7 / Trades));
			}
		}

		public TimeSpan AvgWinTime
		{
			get
			{
				return TimeSpan.FromHours((TimeInWinningTrades.TotalHours / Wins));
			}
		}

		public TimeSpan AvgLossTime
		{
			get
			{
				return TimeSpan.FromHours((TimeInLosingTrades.TotalHours / Losses));
			}
		}

		public List<string> EntryStrategies { get; set; } = new List<string>();
		public List<string> ExitStrategies { get; set; } = new List<string>();
	}
}
