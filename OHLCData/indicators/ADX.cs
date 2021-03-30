using MarketBot.interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.indicators
{
	class ADX : Indicator
	{
		public ADX(SymbolData data, int length) : base(data, length){}

		public override DataRow Calculate(int period)
		{
			int length = (int)Inputs[0];

			if(period == 0)
			{
				return Data.Rows.Add(false, 0, 0, 0, 0, 0, 0, 0, 0);
			}

			decimal truerange = TR.GetTR(Source.Data.Periods, period);

			decimal upmove = Source[period].High - Source[period - 1].High;
			decimal downmove = Source[period].Low - Source[period - 1].Low;

			decimal plus_dm = Source[period].High - Source[period - 1].High > Source[period - 1].Low - Source[period].Low ? Math.Max(Source[period].High - Source[period - 1].High, 0) : 0;
			decimal minus_dm = Source[period - 1].Low - Source[period].Low > Source[period].High - Source[period - 1].High ? Math.Max(Source[period - 1].Low - Source[period].Low, 0) : 0;
			decimal str = (decimal)Data.Rows[period - 1]["str"] - ((decimal)Data.Rows[period - 1]["str"] / length) + truerange;

			decimal sdmplus = (decimal)Data.Rows[period - 1]["sdmplus"] - ((decimal)Data.Rows[period - 1]["sdmplus"] / length) + plus_dm;
			decimal sdmminus = (decimal)Data.Rows[period - 1]["sdmminus"] - ((decimal)Data.Rows[period - 1]["sdmminus"] / length) + minus_dm;

			decimal diplus = (str == 0)?0:(sdmplus / str) * 100;
			decimal diminus = (str == 0)?0:(sdmminus / str) * 100;

			decimal dx = ((diplus + diminus) == 0)?0:Math.Abs((diplus - diminus) / (diplus + diminus)) * 100;
			
			if(period >= length)
			{
				decimal adx = 0;
				decimal sum = 0;
				if(period == length)
				{
					sum = dx;
					for(int i = 1; i < length; i++)
					{
						sum += (decimal)Data.Rows[period - i]["dx"];
					}
				}
				else
				{
					sum = ((decimal)Data.Rows[period - 1]["sum"] - (decimal)Data.Rows[period - length]["dx"] + dx);
				}

				adx = sum / length;
				return Data.Rows.Add(true, str, sdmplus, sdmminus, diplus, diminus, dx, sum, adx);
			}
			else
			{
				return Data.Rows.Add(false, str, sdmplus, sdmminus, diplus, diminus, dx, 0, 0);
			}	
		}

		public override string GetName()
		{
			return "Average Directional Index";
		}

		public override void BuildDataTable()
		{
			Data.Columns.Add("calculated", typeof(bool));
			Data.Columns.Add("str", typeof(decimal));
			Data.Columns.Add("sdmplus", typeof(decimal));
			Data.Columns.Add("sdmminus", typeof(decimal));
			Data.Columns.Add("diplus", typeof(decimal));
			Data.Columns.Add("diminus", typeof(decimal));
			Data.Columns.Add("dx", typeof(decimal));
			Data.Columns.Add("sum", typeof(decimal));
			Data.Columns.Add("value", typeof(decimal));
		}
	}
}
