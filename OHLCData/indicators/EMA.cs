using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using MarketBot.interfaces;
using System.Data;

namespace MarketBot.indicators
{
	public class EMA : Indicator
	{
		public EMA(SymbolData data, int length) : base(data, length)
		{
		}

		public decimal this[int index]
		{
			get => Value<decimal>("value", index);
		}

		public override DataRow Calculate(int period)
		{
			if (period - (int)Inputs[0] < 0)
			{
				return Data.Rows.Add(false, 0);
			}
			else
			{
				decimal ema_yesterday;
				if(Value<bool>("calculated", period - 1) == false)
				{
					decimal sum = 0;

					for (int i = period; i > period - (int)Inputs[0]; i--)
					{
						sum += Source[i].Close;
					}

					ema_yesterday = SMA.GetSMA(sum, (int)Inputs[0]);
				}
				else
				{
					ema_yesterday = Value<decimal>("value", period - 1);
				}

				decimal weight = (decimal)2.0 / ((int)Inputs[0] + (decimal)1.0);

				decimal ema_today = Source[period].Close * weight + ema_yesterday * (1 - weight);

				return Data.Rows.Add(true, ema_today);
			}
		}

		public static decimal GetEMA(decimal current_value, int length, decimal previous_ema, decimal? alpha = null)
		{
			decimal weight = alpha.HasValue?alpha.Value:(decimal)2.0 / (length + (decimal)1.0);
			return (current_value * weight) + (previous_ema * (1 - weight));
		}

		public override string GetName()
		{
			return "Exponential Moving Average";
		}

		public override void BuildDataTable()
		{
			Data.Columns.Add("calculated", typeof(bool));
			Data.Columns.Add("value", typeof(decimal));
		}
	}
}
