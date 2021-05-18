using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.price_setter
{
	class SuperTrendRisk : PriceSetter
	{
		public List<SuperTrendResult> Data;

		public SuperTrendRisk(HList<OHLCVPeriod> history) : base(history)
		{
			
		}

		public override decimal GetPrice(int period, SignalType signal)
		{
			ShouldUpdate();

			switch (signal)
			{
				case SignalType.Long:
					if (History[period].Close > Data[period].SuperTrend)
						return Data[period].SuperTrend.Value;
					else
						throw new Exception("Invalid supertrend");
				case SignalType.Short:
					if (History[period].Close < Data[period].SuperTrend)
						return Data[period].SuperTrend.Value;
					else
						throw new Exception("Invalid supertrend");
			}

			throw new ArgumentException($"Invalid signal type for GetPrice: {signal}");
		}

		public override void UpdateData()
		{
			Data = Indicator.GetSuperTrend(History).ToList();
		}
	}
}
