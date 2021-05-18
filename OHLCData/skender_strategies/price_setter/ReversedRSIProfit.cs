using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.price_setter
{
	class ReversedRSIProfit : PriceSetter
	{
		public indicators.RSI RSI;
		decimal RSIValue;
		int Lookback;
		public ReversedRSIProfit(HList<OHLCVPeriod> history, int lookback, decimal rsi) : base(history)
		{
			Lookback = lookback;
			RSIValue = rsi;
			RSI = new indicators.RSI(history, lookback);
		}

		public override decimal GetPrice(int period, SignalType signal)
		{
			Debug.Assert(signal == SignalType.Long);

			if(RSI[period] >= RSIValue)
			{
				return History[period].Close;
			}

			decimal rs = (-100 / (RSIValue - 100)) - 1;

			decimal pl = (decimal)RSI.Data.Rows[period - 1]["avg_loss"];
			decimal pg = (decimal)RSI.Data.Rows[period - 1]["avg_gain"];
			decimal u1 = History[period - 1].Close;
			decimal L = Lookback;
			decimal k = (pl * (L - 1)) / L;

			decimal u0 = (rs * k * L) - (pg * (L - 1)) + u1;
			return u0;
		}

		public override void UpdateData()
		{
			throw new NotImplementedException();
		}
	}
}
