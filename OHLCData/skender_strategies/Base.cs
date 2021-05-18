using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies
{
	public abstract class BaseStrategy
	{
		public HList<OHLCVPeriod> History;
		private int LastCalculatedLength = 0;

		public BaseStrategy(HList<OHLCVPeriod> history) 
		{
			History = history;
		}

		public abstract void UpdateData();

		public void ShouldUpdate()
		{
			if (History.Count != LastCalculatedLength)
			{
				UpdateData();
				LastCalculatedLength = History.Count;
			}
		}

		public abstract SignalType Run(int period);
	}

	public abstract class BaseCondition
	{
		public HList<OHLCVPeriod> History;
		private int LastCalculatedLength = 0;

		public BaseCondition(HList<OHLCVPeriod> history)
		{
			History = history;
		}

		public abstract void UpdateData();

		public void ShouldUpdate()
		{
			if (History.Count != LastCalculatedLength)
			{
				UpdateData();
				LastCalculatedLength = History.Count;
			}
		}

		public abstract SignalType[] GetAllowed(int period);
	}

	public abstract class ExitStrategy
	{
		public HList<OHLCVPeriod> History;
		private int LastCalculatedLength = 0;

		public ExitStrategy(HList<OHLCVPeriod> history)
		{
			History = history;
		}

		public abstract void UpdateData();

		public void ShouldUpdate()
		{
			if (History.Count != LastCalculatedLength)
			{
				UpdateData();
				LastCalculatedLength = History.Count;
			}
		}

		public abstract void Update(int period, SignalType signal);
		public abstract bool ShouldExit(int period, SignalType signal, out decimal price);
	}

	public abstract class PriceSetter
	{
		public HList<OHLCVPeriod> History;
		private int LastCalculatedLength = 0;

		public PriceSetter(HList<OHLCVPeriod> history)
		{
			History = history;
		}

		public abstract void UpdateData();

		public void ShouldUpdate()
		{
			if (History.Count != LastCalculatedLength)
			{
				UpdateData();
				LastCalculatedLength = History.Count;
			}
		}

		public abstract decimal GetPrice(int period, SignalType signal);
		public bool CanSetPrice(int period, SignalType signal)
		{
			try
			{
				GetPrice(period, signal);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
