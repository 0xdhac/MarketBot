using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.exit_strategy
{
	public class ChoppinessExit : ExitStrategy
	{
		List<ChopResult> Data;
		public ChoppinessExit(HList<OHLCVPeriod> history) : base(history) 
		{
			
		}

		public override bool ShouldExit(int period, SignalType signal, out decimal price)
		{
			ShouldUpdate();

			if (!Data[period].Chop.HasValue)
			{
				price = History[period].Close;
				return true;
			}

			if (Data[period].Chop.Value > (decimal)61.8)
			{
				price = History[period].Close;
				return true;
			}

			price = default;
			return false;
		}

		public override void Update(int period, SignalType signal)
		{
			
		}

		public override void UpdateData()
		{
			Data = Indicator.GetChop(History).ToList();
		}
	}
}
