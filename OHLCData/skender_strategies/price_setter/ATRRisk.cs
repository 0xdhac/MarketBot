using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.price_setter
{
	class ATRRisk : PriceSetter
	{
		public List<AtrResult> Data;
		public List<PivotPointsResult> Pivots;
		private bool UsePivot;

		public ATRRisk(HList<OHLCVPeriod> history, bool use_pivot) : base(history)
		{
			UsePivot = use_pivot;
		}

		public override decimal GetPrice(int period, SignalType signal)
		{
			ShouldUpdate();

			switch(signal)
			{
				case SignalType.Long:
					if(!UsePivot || History[period].Close < Pivots[period].S1.Value)
						return History[period].Low - Data[period].Atr.Value;
					else
						return Pivots[period].S1.Value - Data[period].Atr.Value;
				case SignalType.Short:
					if(!UsePivot || History[period].Close > Pivots[period].R1.Value)
						return History[period].High + Data[period].Atr.Value;
					else
						return Pivots[period].R1.Value + Data[period].Atr.Value;
			}

			throw new ArgumentException($"Invalid signal type for GetPrice: {signal}");
		}

		public override void UpdateData()
		{
			Data = Indicator.GetAtr(History).ToList();

			if (UsePivot)
				Pivots = Indicator.GetPivotPoints(History, PeriodSize.Day).ToList();
		}
	}
}
