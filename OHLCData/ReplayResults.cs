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
		public decimal Profitability { get; set; }
		public OHLCVInterval Interval { get; set; }
		public decimal Periods { get; set; }
		public decimal ProfitRiskRatio { get; set; }
		public decimal Trades { get; set; }
		public decimal EndTotal { get; set; }

		public List<string> EntryStrategies { get; set; } = new List<string>();
		public List<string> ExitStrategies { get; set; } = new List<string>();
	}
}
