using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot.UserStream;
using Binance.Net.Objects.Spot;
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

				using (var socket = new BinanceSocketClient(new BinanceSocketClientOptions() { AutoReconnect = true, ReconnectInterval = TimeSpan.FromSeconds(5) }))
				{
					var result = socket.Spot.SubscribeToUserDataUpdatesAsync(key, OrderUpdate, OcoOrderUpdate, PositionUpdate, BalanceUpdate);

					//socket.
				}
			}
		}

		private static void OrderUpdate(BinanceStreamOrderUpdate update)
		{
			List<Position> ToRemove = new List<Position>();

			// Not in positions list
			foreach (var pos in Position.Positions)
			{
				if(pos.Symbol == update.Symbol) // If it matches the symbol
				{
					if (pos.Real == true) // If it's a real money position
					{
						OrderSide signal_direction = (pos.Type == SignalType.Long) ? OrderSide.Buy : OrderSide.Sell;

						if(update.Side == signal_direction) // If the order type matches the signal direction
						{
							if(update.Status == OrderStatus.PartiallyFilled) // If it only fills partially
							{
								pos.Filled += update.LastQuantityFilled;
							}
							if (update.Status == OrderStatus.Filled)
							{
								pos.Status			= PositionStatus.Filled;
								if (new Regex($"^{update.Symbol}").IsMatch(update.CommissionAsset))
								{
									pos.Commission += update.Commission; // Update the fee/commission
								}
								pos.Filled			+= update.LastQuantityFilled;
								pos.Quantity		= ExchangeTasks.GetStepSizeAdjustedQuantity(Exchanges.Binance, update.Symbol, pos.Filled - pos.Commission);

								BinanceOrders.PlaceOcoOrder(pos, 0, pos.Quantity);
							}
							else if (update.Status == OrderStatus.Rejected || update.Status == OrderStatus.Expired)
							{
								RealtimeBot.Wallets[Exchanges.Binance].Available += (pos.Quantity - update.QuantityFilled) * pos.Entry;
								Console.WriteLine($"[{pos.Symbol}] Order {update.Status}");
								ToRemove.Add(pos); // Remove expired/rejected buys
							}
							else if(update.Status == OrderStatus.New)
							{
								// Update wallet balance
								RealtimeBot.Wallets[Exchanges.Binance].Available -= (update.Quantity * update.Price);

								pos.OrderId = update.OrderId;
								Task.Run(() => 
								{
									string setting = Program.GetConfigSetting("CANCEL_BUY_ORDER_AFTER_X_SECONDS");
									bool success = int.TryParse(setting, out int seconds);

									if(!success)
									{
										Console.WriteLine($"Error: Config setting 'CANCEL_BUY_ORDER_AFTER_X_SECONDS' not found. Defaulted to 10 seconds.");
										Program.LogError($"Config setting 'CANCEL_BUY_ORDER_AFTER_X_SECONDS' not found.");
										seconds = 10;
									}

									System.Threading.Thread.Sleep(seconds * 1000);

									if(pos != null)
									{
										if(pos.Status == PositionStatus.Ordered)
										{
											BinanceOrders.CancelAnyOrders(pos.Symbol, OrderType.Limit, signal_direction);
										}
									}
								});
							}
							else if(update.Status == OrderStatus.Canceled)
							{
								// Update wallet balance, increase based on quantity that was unfilled during the order
								RealtimeBot.Wallets[Exchanges.Binance].Available += (pos.Quantity - update.QuantityFilled) * pos.Entry;

								if(pos.Filled > 0)
								{
									pos.Status = PositionStatus.Filled;
									pos.Quantity = ExchangeTasks.GetStepSizeAdjustedQuantity(Exchanges.Binance, update.Symbol, pos.Filled - pos.Commission);

									BinanceOrders.PlaceOcoOrder(pos, 0, pos.Quantity);
								}
							}
						}
						else // if an asset tries to sell when the entry was to buy
						{
							if (update.Status == OrderStatus.Expired && update.Price > pos.Entry)
							{
								//Program.Print($"market selling after x seconds {update.Symbol} {update.Status}");
								Task.Run(() =>
								{
									string setting = Program.GetConfigSetting("CANCEL_AND_MARKET_SELL_ORDER_AFTER_X_SECONDS");
									bool success = int.TryParse(setting, out int seconds);

									if (!success)
									{
										Console.WriteLine($"Error: Config setting 'CANCEL_AND_MARKET_SELL_ORDER_AFTER_X_SECONDS' not found. Defaulted to 5 seconds.");
										Program.LogError($"Config setting 'CANCEL_AND_MARKET_SELL_ORDER_AFTER_X_SECONDS' not found.");
										seconds = 5;
									}

									System.Threading.Thread.Sleep(seconds * 1000);

									if (pos != null)
									{
										if (pos.Status == PositionStatus.Oco)
										{
											BinanceOrders.CancelAnyOrders(pos.Symbol, OrderType.StopLossLimit, signal_direction + 1 % 2);
											BinanceOrders.PlaceOrder(pos, signal_direction + 1 % 2, OrderType.Market, 0, pos.Filled, -2010); // Ignore rejected order in the case that the stop loss limit was a success
										}
									}
								});
							}
							else if(update.Status == OrderStatus.PartiallyFilled)
							{
								pos.Filled -= update.LastQuantityFilled;
							}
							else if(update.Status == OrderStatus.Filled)
							{
								//Console.WriteLine($"{pos.Symbol} Sold: {update.LastQuantityFilled}");
								pos.Filled -= update.LastQuantityFilled;
								if(++RealtimeBot.Trades[pos.Exchange] % RealtimeBot.ResetBetAmountEvery == 0)
								{
									ExchangeTasks.GetWallet(pos.Exchange, RealtimeBot.OnWalletLoaded); // Reset bet size
								}

								if((update.Price > pos.Entry && signal_direction == OrderSide.Buy) || (update.Price < pos.Entry && signal_direction == OrderSide.Sell))
								{
									RealtimeBot.Wins[pos.Exchange]++;
								}
								else
								{
									RealtimeBot.Losses[pos.Exchange]++;
								}

								int losses = RealtimeBot.Losses[pos.Exchange] == 0 ? 1 : RealtimeBot.Losses[pos.Exchange];
								int wins = RealtimeBot.Wins[pos.Exchange];
								Console.WriteLine($"[{pos.Symbol}] Winrate: {string.Format("{0:0.00}", (decimal)wins / (decimal)(wins + losses))}");
								ToRemove.Add(pos); // Remove filled sells
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
			
		}

		public static void PositionUpdate(BinanceStreamPositionsUpdate update)
		{

		}

		public static void BalanceUpdate(BinanceStreamBalanceUpdate update)
		{
			Console.WriteLine($"{update.Asset} balance update. {update.BalanceDelta}");
		}
	}
}
