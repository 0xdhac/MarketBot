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
		public CMF Cmf;
		public EMA EMA;

		public HList<decimal> Signal = new HList<decimal>();

		private int Trend_Length;
		private int Short_Cmf_Length;
		private int Signal_Length;

		public CMFCrossover(SymbolData data, int trend_len, int cmf_len, int signal_len) : 
			base(data, $"{{\"indicators\":[{{\"name\":\"EMA\", \"inputs\":[{trend_len}]}},{{\"name\":\"CMF\", \"inputs\":[{cmf_len}]}}]}}")
		{
			Trend_Length = trend_len;
			Short_Cmf_Length = cmf_len;
			Signal_Length = signal_len;

			Cmf = (CMF)FindIndicator("CMF", Short_Cmf_Length);
			EMA = (EMA)FindIndicator("EMA", Trend_Length);

			FullCalcSignal();
			Cmf.IndicatorData.OnAdd += CalculateSignal;
		}


		private void CalculateSignal(object sender, EventArgs e)
		{
			int size = Cmf.DataSource.Count;

			if ((size - 1) - (Signal_Length + Short_Cmf_Length) < 0)
			{
				Signal.Add(0);
			}
			else
			{
				if (size - 1 == (Signal_Length + Short_Cmf_Length))
				{
					decimal sum = 0;
					for (int i = 0; i < Signal_Length; i++)
					{
						sum += Cmf[size - 1 - i].Item2;
					}
					Signal.Add(SMA.GetSMA(sum, Signal_Length));
				}
				else
				{
					Signal.Add(EMA.GetEMA(Cmf[size - 1].Item2, Signal_Length, Signal[size - 2]));
				}
			}
		}

		private void FullCalcSignal()
		{
			for (int i = 0; i < Cmf.DataSource.Count; i++)
			{
				if (i - (Short_Cmf_Length + Signal_Length) < 0) //i - 1 - 20 < 0
				{
					Signal.Add(0);
				}
				else
				{
					if (i == Short_Cmf_Length + Signal_Length)
					{
						decimal sum = 0;
						for(int j = 0; j < Signal_Length; j++)
						{
							sum += Cmf[i - j].Item2;
						}
						//CmfShort.IndicatorData.GetRange(i - 2 - Signal_Length, Signal_Length).ConvertAll(s => s.Item2);
						Signal.Add(SMA.GetSMA(sum, Signal_Length));
					}
					else
					{
						Signal.Add(EMA.GetEMA(Cmf[i].Item2, Signal_Length, Signal[i - 1]));
					}
				}
			}
		}

		public override SignalType StrategyConditions(int old_period, int new_period)
		{
			if (new_period < 300)
				return SignalType.None;

			if (Source.Data[new_period].Low > EMA[new_period].Item2 &&
				Cmf[new_period].Item2 < Signal[new_period] &&
				Cmf[old_period].Item2 > Signal[old_period] &&
				Cmf[new_period].Item2 < 0) // AFTER A FEW TESTS, IT WAS UNANIMOUSLY BETTER FOR THE SIGNAL TO BE ON THE OPPOSITE SIDE OF THE ZERO LINE
			{
				return SignalType.Long;
			}
			

			if (Source.Data[new_period].High < EMA[new_period].Item2 &&
				Cmf[new_period].Item2 > Signal[new_period] &&
				Cmf[old_period].Item2 < Signal[old_period] &&
				Cmf[new_period].Item2 > 0)
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
