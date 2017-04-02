using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Fleck;
using Newtonsoft.Json;
namespace WSServer
{
	public class WSServer
	{
		private WebSocketServer _ws;
		private ConcurrentDictionary<string, GameContainer> _activeGames;

		public WSServer(string address, int port)
		{
			// create a new instance of the server on the desired address and port number
			_ws = new WebSocketServer("ws://" + address + ":" + port);
			_activeGames = new ConcurrentDictionary<string, GameContainer>();

			// start the server and wire in all of our functions to be called
			// each function matches up with an action a client can take
			_ws.Start(client =>
			{
				client.OnOpen = () => onConnect(client);
				client.OnClose = () => onDisconnect(client);
				client.OnError = exeption => onError(client, exeption);
				client.OnMessage = message => onMessage(client, message);
			});
		}

		// function is called when a client disconects from the server
		public void onDisconnect(IWebSocketConnection client)
		{
			// find user remove them and probably notify the game for pausing
			if (client.ConnectionInfo.Cookies.ContainsKey("gameCode"))
			{
				_activeGames[client.ConnectionInfo.Cookies["gameCode"]].removePlayer(client.ConnectionInfo.Cookies["name"]);
			}
		}

		// function is called when a client connects to the server
		public void onConnect(IWebSocketConnection client)
		{
			// check to see if room key code exists and add them back to the game
			Console.WriteLine("client Connected");
			if (client.ConnectionInfo.Cookies.ContainsKey("GameCode"))
			{
				Console.WriteLine(client.ConnectionInfo.Cookies["GameCode"]);
			}
		}

		// called when there is an erorr with the server
		public void onError(IWebSocketConnection client, Exception ex)
		{
			Console.WriteLine(ex.Message);
		}

		//called when data is sent to the server, the bulk of the code for the server will go here
		public void onMessage(IWebSocketConnection client, string message)
		{
			// our server will communicate using json formatted strings. Take that string and turn it into a dictionary
			// for ease of use.
			var messageObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(message);

			Console.WriteLine("data recieved...");
			//Console.WriteLine(messageObject.Keys);
			foreach (var pair in messageObject)
			{
				Console.WriteLine(pair);
			}
			// handle new games being created
			if (messageObject.ContainsKey("gameName"))
			{
				var code = createNewGame(messageObject["gameName"]);
				if (code != "")
				{
					// since this is a new game, we create a player that will represent the board
					_activeGames[code].addPlayer("board", client);

					// we need to tell the board what the code is so we have to send it back
					// for this we will use an anonymous object then let the json library package it up for us
					var codeMessage = new { code = 0, gameCode = code };
					client.Send(JsonConvert.SerializeObject(codeMessage));
				}

				// couldn't find the game to be created
				else
				{
					var badGameName = new { badGame = "1" };
					client.Send(JsonConvert.SerializeObject(badGameName));
				}

			}

			else if ( messageObject.ContainsKey("gameCode") )
			{
				// check to make sure the game code actually exists
				if (_activeGames.ContainsKey(messageObject["gameCode"]))
				{
					// here we have a new user joining the game, they must have a name
					if (messageObject.ContainsKey("name"))
					{

						var added = _activeGames[messageObject["gameCode"]].addPlayer(messageObject["name"], client);

						var newPlayerMsg = new { code = "0", added };

						// if this fails it's either there are too many players or the name isn't right
						client.Send(JsonConvert.SerializeObject(newPlayerMsg));
					}
					else
					{
						// if we aren't setting a name then we are just passing information
						var game = _activeGames[messageObject["gameCode"]];
						game.provideMessage(message, client);
					}
				}
				else
				{
					// tell the client they have the wrong game code
					var badGameCodeMsg = new { badCode = "1" };
					client.Send(JsonConvert.SerializeObject(badGameCodeMsg));
				}
			}

		}

		// generates a 5 character key to be used to identify other active games
		public string generateKey()
		{
			string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			Random rnd = new Random();
			string code = "";

			while (_activeGames.Keys.Contains(code) || code == "")
			{
				code = "";
				for (int i = 0; i < 5; i++)
				{
					code += chars[rnd.Next(0, chars.Length - 1)];
				}
			}

			return code;
		}

		public string createNewGame(string name)
		{
			IGame g = null;
			g = GameFactory.createGame(name);

			if (g != null)
			{
				string gameCode = generateKey();
				var newGame = new GameContainer(gameCode, g);
				_activeGames.TryAdd(gameCode, newGame);
				return gameCode;

			}
			return "";
		}
	}
}
