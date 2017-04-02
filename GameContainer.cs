using System;
using System.Collections.Concurrent;
using SecretCompanion;
using Fleck;
using System.Linq;
namespace WSServer
{
	/// <summary>
	/// handles adding and removing players from a game. as well as sending data to the game and sending data from the game to a player
	/// 
	/// </summary>
	public class GameContainer
	{
		private IGame _game;
		private ConcurrentDictionary<string, IUser> _users;

		// server is responisble for creating game object
		public GameContainer(string gameCode, IGame game)
		{
			_game = game;
			_users = new ConcurrentDictionary<string, IUser>();
		}

		// add a player to an active game
		public bool addPlayer(string name, IWebSocketConnection context)
		{
			if (!_game.gameFull())
			{
				// if there is not a uniqe name reject
				if (_users.ContainsKey(name))
				{
					return false;
				}
				else
				{
					// try to create user and reject if something goes wrong
					var u = _game.createUser(name);
					u.context = context;

					if (u != null)
					{
						_users.TryAdd(name, u);
						return true;
					}

					return false;
				}
			}
			return false;
		}

		public bool removePlayer(string name)
		{
			IUser toRemove = null;
			_users.TryRemove(name, out toRemove);
			if (toRemove == null)
			{
				return false;
			}
			return true;
		}

		// gets data, sends the data to the game for processing, game will send data back to be sent to a user
		public void provideMessage(string data, IWebSocketConnection context)
		{
			// first find the username in the list
			IUser sender = _users.Values.Where(x => x.context.ConnectionInfo.Id == context.ConnectionInfo.Id).Single();
			var clientData = _game.receiveMessage(sender.name, data);

			// games might need to contact multiple clients per message so loop through the list and send the approriate data
			foreach (var msg in clientData)
			{
				string clientName = msg.Item1;
				string cData = msg.Item2;

				IUser client = _users.Values.Where(x => x.name == clientName).Single();
				client.send(cData);
			}

		}
	}
}
