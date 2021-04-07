using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.entry_conditions
{
	class RSICondition : BaseCondition
	{
		public List<RsiResult> Data;
		private decimal RSI_Oversold;
		private decimal RSI_Overbought;
		public RSICondition(HList<OHLCVPeriod> history, decimal overbought, decimal oversold) : base(history)
		{
			RSI_Oversold = oversold;
			RSI_Overbought = overbought;
		}

		public override SignalType[] GetAllowed(int period)
		{
			ShouldUpdate();

			if (!Data[period].Rsi.HasValue)
				return new SignalType[] { };

			if (Data[period].Rsi.Value >= 80)
			{
				return new SignalType[] { SignalType.Short };
			}
			else if(Data[period].Rsi.Value <= RSI_Overbought)
			{
				return new SignalType[] { SignalType.Long };
			}
			else
			{
				return new SignalType[] { SignalType.Long, SignalType.Short };
			}
		}

		public override void UpdateData()
		{
			Data = Indicator.GetRsi(History).ToList();
		}
	}
}
