using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;
using System.Data;

namespace MarketBot.indicators
{
	public class RSI : Indicator
	{
		public static int Default = 14;

		public decimal this[int index]
		{
			get => Value<decimal>("value", index);
		}

		public RSI(SymbolData data, int length) : base(data, length)
		{
			if(length <= 0)
			{
				throw new ArgumentException("Invalid parameter value", "length");
			}
		}

		public override void BuildDataTable()
		{
			Data.Columns.Add("calculated", typeof(bool));
			Data.Columns.Add("avg_gain", typeof(decimal));
			Data.Columns.Add("avg_loss", typeof(decimal));
			Data.Columns.Add("value", typeof(decimal));
		}

		public override DataRow Calculate(int period)
		{
			if(period - (int)Inputs[0] < 0)
			{
				return Data.Rows.Add(false, 0, 0, 0);
			}

			decimal avg_gain = 0;
			decimal avg_loss = 0;
			if(Value<bool>("calculated", period - 1) == false)
			{
				decimal sum_gains = 0;
				decimal sum_losses = 0;
				for (int i = 0; i < (int)Inputs[0]; i++)
				{
					decimal change = Source[period - i].Close - Source[period - i - 1].Close;

					if(change > 0)
					{
						sum_gains += change;
					}
					else if(change < 0)
					{
						sum_losses += Math.Abs(change);
					}
				}

				avg_gain = sum_gains / (int)Inputs[0];
				avg_loss = sum_losses / (int)Inputs[0];
			}
			else
			{
				decimal previous_average_gain = Value<decimal>("avg_gain", period - 1);
				decimal previous_average_loss = Value<decimal>("avg_loss", period - 1);
				decimal current_gain = 0;
				decimal current_loss = 0;

				decimal change = Source[period].Close - Source[period - 1].Close;

				if(change > 0)
				{
					current_gain = change;
				}
				else if(change < 0)
				{
					current_loss = Math.Abs(change);
				}

				avg_gain = ((previous_average_gain * ((int)Inputs[0] - 1)) + current_gain) / (decimal)(int)Inputs[0];
				avg_loss = ((previous_average_loss * ((int)Inputs[0] - 1)) + current_loss) / (decimal)(int)Inputs[0];
			}

			decimal rs = avg_gain / avg_loss;
			decimal rsi = 100 - (100 / (1 + rs));

			return Data.Rows.Add(true, avg_gain, avg_loss, rsi);
		}

		public override string GetName()
		{
			return "Relative Strength Index";
		}
	}
}
