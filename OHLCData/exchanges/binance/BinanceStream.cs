using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot.UserStream;
using MarketBot.tools;

namespace MarketBot.exchanges.binance
{
	static class BinanceStream
	{
		public static void CreateUserStream()
		{
			using (var client = new BinanceClient())
			{
				var key = client.Spot.UserStream.StartUserStream().Data;
				
				using (var socket = new BinanceSocketClient())
				{
					socket.Spot.SubscribeToUserDataUpdatesAsync(key, OrderUpdate, OcoOrderUpdate, PositionUpdate, BalanceUpdate); 
				}

				/*
				var margin_key = client.Margin.UserStream.StartUserStream().Data;

				using (var socket = new BinanceSocketClient())
				{
					
				}
				*/
			}
		}

		private static void OrderUpdate(BinanceStreamOrderUpdate update)
		{
			// Expired Sell sell_side  == pos.Risk -> MARKET SELL
			// PartialFill buy_side == update pos.Commission
			// Fill buy_side        == update pos.Commission, quantity, Place OCO
			// Fill sell_side, pos.Profit == update.Price -> WINS++
			// Fill sell_side, pos.Risk >= update.Price -> LOSSES--
			//Console.WriteLine($"Order update: {update.Symbol} {update.Status} {update.Side} {update.Price} {update.Quantity}");
			List<Position> ToRemove = new List<Position>();
			
			foreach (var position in Position.Positions)
			{
				if(position.Symbol == update.Symbol) // If it matches the symbol
				{
					if(position.Real == true) // If it's a real money position
					{
						OrderSide signal_direction = (position.Type == SignalType.Long) ? OrderSide.Buy : OrderSide.Sell;

						if(update.Side == signal_direction) // If the order type matches the signal direction
						{
							if(update.Status == OrderStatus.PartiallyFilled) // If it only fills partially
							{
								position.Commission += update.Commission; // Update the fee/commission
							}
							if (update.Status == OrderStatus.Filled)
							{
								Console.WriteLine($"[{position.Symbol}] Entry order filled.");
								position.Status = PositionStatus.Filled;
								position.Commission += update.Commission;
								position.Quantity -= position.Commission;
								position.Quantity = ExchangeTasks.GetStepSizeAdjustedQuantity(Exchanges.Binance, update.Symbol, position.Quantity);

								BinanceOrders.PlaceOcoOrder(position, 0);
							}
							else if (update.Status == OrderStatus.Rejected || update.Status == OrderStatus.Expired)
							{
								Console.WriteLine($"[{position.Symbol}] Order {update.Status}");
								ToRemove.Add(position); // Remove expired/rejected buys
							}
						}
						else // if a coin tries to sell when the entry was to buy
						{
							if (update.Status == OrderStatus.Expired && update.Price <= position.Risk)
							{
								Console.WriteLine($"[{position.Symbol}] Exit order expired. Using Market instead of limit.");
								BinanceOrders.PlaceOrder(position, signal_direction + 1 % 2, OrderType.Market, 0);
							}
							else if(update.Status == OrderStatus.Filled)
							{
								if(++RealtimeBot.Trades[position.Exchange] % RealtimeBot.ResetBetAmountEvery == 0)
								{
									ExchangeTasks.GetWallet(position.Exchange, RealtimeBot.OnWalletLoaded); // Reset bet size
								}

								if((update.Price > position.Entry && signal_direction == OrderSide.Buy) || (update.Price < position.Entry && signal_direction == OrderSide.Sell))
								{
									RealtimeBot.Wins[position.Exchange]++;
								}
								else
								{
									RealtimeBot.Losses[position.Exchange]++;
								}

								decimal losses = RealtimeBot.Losses[position.Exchange] == 0 ? 1 : RealtimeBot.Losses[position.Exchange];
								Console.WriteLine($"[{position.Symbol}] Winrate: {string.Format("{0:0.00}", (decimal)RealtimeBot.Wins[position.Exchange] / losses)}");
								ToRemove.Add(position); // Remove filled sells
							}
						}
					}
				}
			}

			foreach(var pos in ToRemove)
			{
				Position.Positions.Remove(pos);
			}
		}

		private static void OcoOrderUpdate(BinanceStreamOrderList update)
		{
			/*
			List<Position> ToRemove = new List<Position>();
			foreach (var pos in Position.Positions)
			{
				if(update.Symbol == pos.Symbol && update.ListOrderStatus == ListOrderStatus.Done)
				{
					ToRemove.Add(pos);
				}
			}

			foreach (var pos in ToRemove)
			{
				Position.Positions.Remove(pos);
			}
			*/

			//Console.WriteLine($"Oco Order Update: {update.Symbol} {update.ListOrderStatus} {update.ListStatusType}");
		}

		public static void PositionUpdate(BinanceStreamPositionsUpdate update)
		{
			//Console.WriteLine($"Position Update: {update.Event}");
			/*
			List<Position> ToRemove = new List<Position>();
			foreach (var pos in Position.Positions)
			{
				if (update.Symbol == pos.Symbol)
				{
					if (update.ListOrderStatus == ListOrderStatus.Done)
					{
						Console.WriteLine($"Removing {update.Symbol} from positions");
						ToRemove.Add(pos);
					}
				}
			}

			foreach (var pos in ToRemove)
			{
				Position.Positions.Remove(pos);
			}
			*/
		}

		public static void BalanceUpdate(BinanceStreamBalanceUpdate update)
		{
			//Console.WriteLine($"Balance Update: {update.Asset} {update.BalanceDelta}");
			//update.
		}
	}
}
