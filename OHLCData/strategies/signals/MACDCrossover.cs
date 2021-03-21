using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.signals
{
	public class MACDCrossover : Strategy
	{
		private EMA TrendLine;

		//Testing
		private EMA MacdShort;
		private EMA MacdLong;
		HList<decimal> MacdEval = new HList<decimal>();
		HList<decimal> Signal = new HList<decimal>();

		private int Trend_Length;
		private int Short_Macd_Length;
		private int Long_Macd_Length;
		private int Signal_Length;

		public MACDCrossover(SymbolData data, int trend_len, int short_len, int long_len, int signal_len) : 
			base(data, $"{{0:{{name:\"EMA\",params:\"{trend_len}\"}},1:{{name:\"EMA\",params:\"{short_len}\"}},2:{{name:\"EMA\",params:\"{long_len}\"}}}}") 
		{
			Trend_Length = trend_len;
			Short_Macd_Length = short_len;
			Long_Macd_Length = long_len;
			Signal_Length = signal_len;

			TrendLine = (EMA)FindIndicator("EMA", Trend_Length);
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
			int size = MacdLong.DataSource.Count;

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
			for(int i = 0; i < MacdLong.DataSource.Count; i++)
			{
				MacdEval.Add(MacdShort[i].Item2 - MacdLong[i].Item2);
			}
		}

		public override SignalType StrategyConditions(int old_period, int new_period)
		{
			if (new_period < 300)
				return SignalType.None;

			//Console.WriteLine($"{MacdShort.Equals(MacdLong)}");



			if (Source.Data[new_period].Low > TrendLine[new_period].Item2 &&
				MacdEval[new_period] - Signal[new_period] > 0 &&
				MacdEval[old_period] - Signal[old_period] < 0 &&
				MacdEval[new_period] < 0)
			{
				return SignalType.Long;
			}

			if (Source.Data[new_period].High < TrendLine[new_period].Item2 &&
				MacdEval[new_period] - Signal[new_period] < 0 &&
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
