using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace MarketBot.skender_strategies.exit_strategy
{
	class TrailingStopLossExit : ExitStrategy
	{
		private decimal ProfitLevel;
		private decimal StopLevel;

		private Func<int, SignalType, decimal> UpdateStopCallback;

		public TrailingStopLossExit(HList<OHLCVPeriod> history, Func<int, SignalType, decimal> update_stop_level_callback) : base(history)
		{
			UpdateStopCallback = update_stop_level_callback;
		}

		public override void Update(int period, SignalType signal)
		{
			StopLevel = UpdateStopCallback(period, signal);
			ProfitLevel = History[period].Close + Math.Abs(History[period].Close - StopLevel);
		}

		public override bool ShouldExit(int period, SignalType signal, out decimal price)
		{
			switch(signal)
			{
				case SignalType.Long:
					if (History[period].Close >= ProfitLevel)
						Update(period, signal);
					else if (History[period].Close <= StopLevel)
					{
						price = StopLevel;
						return true;
					}
					break;
				case SignalType.Short:
					if (History[period].Close <= ProfitLevel)
						Update(period, signal);
					else if (History[period].Close >= StopLevel)
					{
						price = StopLevel;
						return true;
					}
					break;	
			}

			price = default;
			return false;
		}

		public override void UpdateData()
		{
			
		}
	}
}
