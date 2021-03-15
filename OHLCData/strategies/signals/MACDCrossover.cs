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
		private CMF CMF;

		//Testing
		private EMA MacdShort;
		private EMA MacdLong;
		CustomList<decimal> MacdEval = new CustomList<decimal>();
		CustomList<decimal> Signal = new CustomList<decimal>();

		private int ATR_Length;
		private int Trend_Length;
		private int Short_Macd_Length;
		private int Long_Macd_Length;
		private int Signal_Length;

		public MACDCrossover(SymbolData data, int trend_len, int short_len, int long_len, int signal_len, int atr_len) : base(data) 
		{
			Trend_Length = trend_len;
			Short_Macd_Length = short_len;
			Long_Macd_Length = long_len;
			Signal_Length = signal_len;
			ATR_Length = atr_len;

			ApplyIndicators();
			MacdEval.OnAdd += CalculateSignal;
			MacdLong.IndicatorData.OnAdd += CalculateMacd;
			FullCalcMacdEval();
		}

		public override void ApplyIndicators()
		{
			TrendLine = (EMA)DataSource.RequireIndicator("EMA",
				new KeyValuePair<string, object>("Length", Trend_Length));

			MacdShort = (EMA)DataSource.RequireIndicator("EMA",
				new KeyValuePair<string, object>("Length", Short_Macd_Length));

			MacdLong = (EMA)DataSource.RequireIndicator("EMA",
				new KeyValuePair<string, object>("Length", Long_Macd_Length));

			CMF = (CMF)DataSource.RequireIndicator("CMF", new KeyValuePair<string, object>("Length", 20));
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

			
			if (DataSource.Data[new_period].Low > TrendLine[new_period].Item2 &&
				MacdEval[new_period] - Signal[new_period] < 0 &&
				MacdEval[old_period] - Signal[old_period] > 0 &&
				MacdEval[new_period] < 0)
			{
				return SignalType.Long;
			}

			if (DataSource.Data[new_period].High < TrendLine[new_period].Item2 &&
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
