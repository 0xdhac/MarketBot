using Binance.Net.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot
{
	public static partial class Converter
	{
		public static GenericOrderStatus ToOrderStatus(Binance.Net.Enums.OrderStatus status)
		{
			switch (status)
			{
				case Binance.Net.Enums.OrderStatus.New:
					return GenericOrderStatus.New;
				case Binance.Net.Enums.OrderStatus.Rejected:
					return GenericOrderStatus.Rejected;
				case Binance.Net.Enums.OrderStatus.Canceled:
					return GenericOrderStatus.Canceled;
				case Binance.Net.Enums.OrderStatus.PartiallyFilled:
					return GenericOrderStatus.PartiallyFilled;
				case Binance.Net.Enums.OrderStatus.Filled:
					return GenericOrderStatus.Filled;
				default:
					return GenericOrderStatus.Other;
			}
		}

		public static GenericOrderSide ToOrderSide(Binance.Net.Enums.OrderSide side)
		{
			switch (side)
			{
				case Binance.Net.Enums.OrderSide.Buy:
					return GenericOrderSide.Buy;
				case Binance.Net.Enums.OrderSide.Sell:
					return GenericOrderSide.Sell;
				default:
					throw new ArgumentException($"Impossible order side: {side}");
			}
		}

		public static OrderSide FromOrderSide(GenericOrderSide side)
		{
			switch (side)
			{
				case GenericOrderSide.Buy:
					return OrderSide.Buy;
				case GenericOrderSide.Sell:
					return OrderSide.Sell;
				default:
					throw new ArgumentException($"Impossible order side: {side}");
			}
		}

		public static GenericOrderType ToOrderType(Binance.Net.Enums.OrderType type)
		{
			switch (type)
			{
				case OrderType.Limit:
					return GenericOrderType.Limit;
				case OrderType.Market:
					return GenericOrderType.Market;
				case OrderType.StopLossLimit:
					return GenericOrderType.Limit;
				case OrderType.LimitMaker:
					return GenericOrderType.Limit;
				default:
					throw new NotSupportedException();
			}
		}
	}
}
