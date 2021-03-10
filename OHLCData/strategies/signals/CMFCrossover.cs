using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.indicators;

namespace MarketBot.strategies.signals
{
	public class CMFCrossover : Strategy
	{
		private EMA TrendLine;

		//Testing
		private CMF CmfShort;
		CustomList<decimal> Signal = new CustomList<decimal>();

		private int Trend_Length;
		private int Short_Cmf_Length;
		private int Signal_Length;

		public CMFCrossover(SymbolData data, int trend_len, int short_len, int signal_len) : base(data)
		{
			Trend_Length = trend_len;
			Short_Cmf_Length = short_len;
			Signal_Length = signal_len;

			ApplyIndicators();
			FullCalcSignal();
			CmfShort.IndicatorData.OnAdd += CalculateSignal;
		}

		public override void ApplyIndicators()
		{
			TrendLine = (EMA)DataSource.RequireIndicator("EMA",
				new KeyValuePair<string, object>("Length", Trend_Length));


			CmfShort = (CMF)DataSource.RequireIndicator("CMF",
				new KeyValuePair<string, object>("Length", Short_Cmf_Length));
		}

		private void CalculateSignal(object sender, EventArgs e)
		{
			int size = CmfShort.DataSource.Count;

			if ((size - 1) - (Signal_Length + Short_Cmf_Length) < 0)
			{
				Signal.Add(0);
			}
			else
			{
				if (size - 1 == (Signal_Length + Short_Cmf_Length))
				{
					List<decimal> range = CmfShort.IndicatorData.GetRange(size - 1 - Signal_Length, Signal_Length).ConvertAll(s => s.Item2);
					Signal.Add(SMA.GetSMA(range.Sum(), Signal_Length));
				}
				else
				{
					Signal.Add(EMA.GetEMA(CmfShort[size - 1].Item2, Signal_Length, Signal[size - 2]));
				}
			}
		}

		private void FullCalcSignal()
		{
			for (int i = 0; i < CmfShort.DataSource.Count; i++)
			{
				if ((i - 1) - (Signal_Length + Short_Cmf_Length) < 0)
				{
					Signal.Add(0);
				}
				else
				{
					if (i - 1 == (Signal_Length + Short_Cmf_Length))
					{
						List<decimal> range = CmfShort.IndicatorData.GetRange(i - 1 - Signal_Length, Signal_Length).ConvertAll(s => s.Item2);
						Signal.Add(SMA.GetSMA(range.Sum(), Signal_Length));
					}
					else
					{
						Signal.Add(EMA.GetEMA(CmfShort[i - 1].Item2, Signal_Length, Signal[i - 2]));
					}
				}
			}
		}

		public override SignalType StrategyConditions(int new_period, int old_period)
		{
			if (new_period < 300)
				return SignalType.None;

			/*
			if (DataSource.Data[new_period].Low > TrendLine[new_period].Item2 &&
				CmfShort[new_period].Item2 > 0 &&
				CmfShort[old_period].Item2 < 0)
			{
				return SignalType.Long;
			}


			if (DataSource.Data[new_period].High < TrendLine[new_period].Item2 &&
				CmfShort[new_period].Item2 < 0 &&
				CmfShort[old_period].Item2 > 0)
			{
				return SignalType.Short;
			}
			*/

			//Console.WriteLine($"{new_period} {CmfEval[new_period]} {Signal[new_period]}");
			if (DataSource.Data[new_period].Low > TrendLine[new_period].Item2 &&
				CmfShort[new_period].Item2 > Signal[new_period] &&
				CmfShort[old_period].Item2 < Signal[old_period] &&
				CmfShort[new_period].Item2 < 0)
			{
				return SignalType.Long;
			}
			

			if (DataSource.Data[new_period].High < TrendLine[new_period].Item2 &&
				CmfShort[new_period].Item2 < Signal[new_period] &&
				CmfShort[old_period].Item2 > Signal[old_period] &&
				CmfShort[new_period].Item2 > 0)
			{
				return SignalType.Short;
			}

			return SignalType.None;

		}

		public override string GetName()
		{
			return "Chaikin Money Flow Crossover";
		}
	}
}
