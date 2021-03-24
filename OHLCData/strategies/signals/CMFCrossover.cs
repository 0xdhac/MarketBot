using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.indicators;

namespace MarketBot.strategies.signals
{
	public class CMFCrossover : EntrySignaler
	{
		public CMF Cmf;

		public HList<decimal> Signal = new HList<decimal>();

		private int Cmf_Length;
		private int Signal_Length;

		public CMFCrossover(SymbolData data, int cmf_len, int signal_len) : 
			base(data, $"{{\"indicators\":[{{\"name\":\"CMF\", \"inputs\":[{cmf_len}]}}]}}")
		{
			Cmf_Length = cmf_len;
			Signal_Length = signal_len;

			Cmf = (CMF)FindIndicator("CMF", Cmf_Length);

			FullCalcSignal();
			Cmf.IndicatorData.OnAdd += CalculateSignal;
		}

		private void CalculateSignal(object sender, EventArgs e)
		{
			int size = Cmf.Source.Data.Periods.Count;

			if ((size - 1) - (Signal_Length + Cmf_Length) < 0)
			{
				Signal.Add(0);
			}
			else
			{
				if (size - 1 == (Signal_Length + Cmf_Length))
				{
					decimal sum = 0;
					for (int i = 0; i < Signal_Length; i++)
					{
						sum += Cmf[size - 1 - i];
					}
					Signal.Add(SMA.GetSMA(sum, Signal_Length));
				}
				else
				{
					Signal.Add(EMA.GetEMA(Cmf[size - 1], Signal_Length, Signal[size - 2]));
				}
			}
		}

		private void FullCalcSignal()
		{
			for (int i = 0; i < Cmf.Source.Data.Periods.Count; i++)
			{
				if (i - (Cmf_Length + Signal_Length) < 0) //i - 1 - 20 < 0
				{
					Signal.Add(0);
				}
				else
				{
					if (i == Cmf_Length + Signal_Length)
					{
						decimal sum = 0;
						for(int j = 0; j < Signal_Length; j++)
						{
							sum += Cmf[i - j];
						}

						Signal.Add(SMA.GetSMA(sum, Signal_Length));
					}
					else
					{
						Signal.Add(EMA.GetEMA(Cmf[i], Signal_Length, Signal[i - 1]));
					}
				}
			}
		}

		public override SignalType StrategyConditions(int old_period, int new_period)
		{
			//Console.WriteLine($"{Cmf.IndicatorData[new_period].Item2} {Signal[new_period]}");
			if (Cmf[new_period] < Signal[new_period] &&
				Cmf[old_period] > Signal[old_period] &&
				Cmf[new_period] < 0) // AFTER A FEW TESTS, IT WAS UNANIMOUSLY BETTER FOR THE SIGNAL TO BE ON THE OPPOSITE SIDE OF THE ZERO LINE
			{
				return SignalType.Long;
			}
			

			if (Cmf[new_period] > Signal[new_period] &&
				Cmf[old_period] < Signal[old_period] &&
				Cmf[new_period] > 0)
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
