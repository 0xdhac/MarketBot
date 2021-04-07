using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace MarketBot.skender_strategies.exit_strategy
{
	class EMAExit : ExitStrategy
	{
		public List<EmaResult> Data;
		private int Lookback;
		public EMAExit(HList<OHLCVPeriod> history, int lookback) : base(history)
		{
			Lookback = lookback;
		}

		public override void Update(int period, SignalType entry_type)
		{

		}

		public override bool ShouldExit(int period, SignalType entry_type, out decimal price)
		{
			ShouldUpdate();

			switch (entry_type)
			{
				case SignalType.Long:
					if (History[period].Close < Data[period].Ema)
					{
						price = History[period].Close;
						return true;
					}

					break;
				case SignalType.Short:
					if (History[period].Close > Data[period].Ema)
					{
						price = History[period].Close;
						return true;
					}

					break;
			}

			price = default;
			return false;
		}

		public override void UpdateData()
		{
			Data = Indicator.GetEma(History, Lookback).ToList();
		}
	}
}
