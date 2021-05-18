using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.exit_strategy
{
	class OCOExit : ExitStrategy
	{
		public decimal? Profit;
		public decimal? Risk;
		Func<int, SignalType, decimal> RiskSetter;
		Func<int, SignalType, decimal> ProfitSetter;

		public OCOExit(
			HList<OHLCVPeriod> history, 
			Func<int, SignalType, decimal> risk_setter, 
			Func<int, SignalType, decimal> profit_setter) : 
			base(history)
		{
			ProfitSetter = profit_setter;
			RiskSetter = risk_setter;
		}

		public override void Update(int period, SignalType entry_type)
		{
			decimal entry = History[period].Close;

			Risk = RiskSetter(period, entry_type);
			Profit = ProfitSetter(period, entry_type);
		}

		public override bool ShouldExit(int period, SignalType entry_signal, out decimal price)
		{
			switch (entry_signal)
			{
				case SignalType.Long:
					if (History[period].High >= Profit.Value)
					{
						price = Profit.Value;
						return true;
					}						

					if (History[period].Low <= Risk.Value)
					{
						price = Risk.Value;
						return true;
					}						

					break;
				case SignalType.Short:
					if (History[period].Low <= Profit.Value)
					{
						price = Profit.Value;
						return true;
					}

					if (History[period].High >= Risk.Value)
					{
						price = Risk.Value;
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
