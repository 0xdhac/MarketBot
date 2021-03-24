using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using MarketBot.indicators;
using MarketBot.interfaces;

namespace MarketBot.strategies.signals
{
	public class MACDCrossover : EntrySignaler
	{
		//Testing
		private EMA MacdShort;
		private EMA MacdLong;
		HList<decimal> MacdEval = new HList<decimal>();
		HList<decimal> Signal = new HList<decimal>();

		private int Short_Macd_Length;
		private int Long_Macd_Length;
		private int Signal_Length;

		public MACDCrossover(SymbolData data, int short_len, int long_len, int signal_len) :
			base(data, new IIndicator[] {new EMA(data, short_len), new EMA(data, long_len)})
		{
			Short_Macd_Length = short_len;
			Long_Macd_Length = long_len;
			Signal_Length = signal_len;

			MacdShort = (EMA)FindIndicator("EMA", Short_Macd_Length);
			MacdLong = (EMA)FindIndicator("EMA", Long_Macd_Length);

			MacdEval.OnAdd += CalculateSignal;
			MacdLong.IndicatorData.OnAdd += CalculateMacd;
			FullCalcMacdEval();
		}

		private void CalculateSignal(object sender, EventArgs e)
		{
			int size = MacdEval.Count;

			if((size - 1) - (Signal_Length + Long_Macd_Length) < 0)
			{
				Signal.Add(0);
			}
			else
			{
				if(size - 1 == (Signal_Length + Long_Macd_Length))
				{
					Signal.Add(SMA.GetSMA(MacdEval.GetRange(size - 1 - Signal_Length, Signal_Length).Sum(), Signal_Length));
				}
				else
				{
					Signal.Add(EMA.GetEMA(MacdEval[size - 1], Signal_Length, Signal[size - 2]));
				}
			}
		}

		public void CalculateMacd(object sender, EventArgs e)
		{
			int size = MacdLong.Source.Data.Periods.Count;

			if (size > 0)
			{
				MacdEval.Add(MacdShort[size - 1].Item2 - MacdLong[size - 1].Item2);
			}
			else
			{
				MacdEval.Add(0);
			}
		}

		private void FullCalcMacdEval()
		{
			for(int i = 0; i < MacdLong.Source.Data.Periods.Count; i++)
			{
				MacdEval.Add(MacdShort[i].Item2 - MacdLong[i].Item2);
			}
		}

		public override SignalType StrategyConditions(int old_period, int new_period)
		{
			if (MacdEval[new_period] - Signal[new_period] > 0 &&
				MacdEval[old_period] - Signal[old_period] < 0 &&
				MacdEval[new_period] < 0)
			{
				return SignalType.Long;
			}

			if (MacdEval[new_period] - Signal[new_period] < 0 &&
				MacdEval[old_period] - Signal[old_period] > 0 &&
				MacdEval[new_period] > 0)
			{
				return SignalType.Short;
			}
			return SignalType.None;
			
		}

		public override string GetName()
		{
			return "MACD Crossover";
		}
	}
}
