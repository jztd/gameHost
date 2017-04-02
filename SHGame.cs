//TODO:
// do not allow usernames to be the same as other users
// finish checkGameOver // it still needs to send the message to the clients
// need to pretify the board layout
// need a better button solution for players
// powers test and approved: policy peek, special investigation
// powers that need tweaks. special election needs a board update to show what's going on...
// board needs a lot more notifications and information updates
// client side veto is done, need server side

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Fleck;
using Newtonsoft.Json;
using System.Linq;
using WSServer;

namespace SecretCompanion
{
	public class SHGame : IGame
	{
		
		// user information and communication
		private List<SHUser> _userList;
		private SHUser _board;
		private object _lock = new object();

		//faction setup variables
		private int _libsNeeded;
		private int _facNeeded;
		private bool _hitlerIssued;
		private int _startingPlayers;
		private int _players;
		private int _playersReady;

		private Board _gameBoard;

		private int _votesCounted;
		private List<playerVote> _votesCast;

		private SHUser _nominatedPresident;
		private SHUser _nominatedChancellor;

		private string _lastPresident;
		private string _lastChancellor;

		private int _playerTurn;

		public SHGame()
		{
			_userList = new List<SHUser>();
			_board = null;
			_gameBoard = new Board();
			_libsNeeded = -1;
			_facNeeded = -1;
			_hitlerIssued = false;
			_players = 0;
			_votesCounted = 0;
			_lastPresident = "";
			_lastChancellor = "";
			_playerTurn = 0;
			_votesCast = new List<playerVote>();
			_playersReady = 0;
		}

		// all message processing happnes here
		// messages from the board are coded as double digits starting 00
		// messages from the player are coded as tripple digits starting 000
		public List<Tuple<string,string>> receiveMessage(string userName, string message)
		{
			var returnData = new List<Tuple<string, string>>();
			// messageObject will hold a dictionary of the objects sent
			var messageObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(message);

			string messageCode = messageObject["code"];

			// getting players from the board
			if (messageCode == "00")
			{
				// get data from the board client first is players
				if (messageObject.ContainsKey("players"))
				{
					// got players
					_players = int.Parse(messageObject["players"]);
					// now call the setup function based on the players
					gameSetup(_players);
				}
			}

			// set name of a player
			else if (messageCode == "001")
			{
				if (messageObject.ContainsKey("ready"))
				{
					_playersReady++;
				}
				if (_players == _playersReady)
				{
					returnData.AddRange(sendPlayerInformation());
					returnData.AddRange(notifyNextPlayer());
				}
			}

			// reconnect a player
			else if (messageCode == "002")
			{
				// we are reconnecting a user so we have to find the user with the proper name and
				// set this context as the context for that user
			}

			// received vote from player
			else if (messageCode == "000")
			{
				var voter = _userList.Where(x => x.name == userName).Single();
				recieveVote(voter, messageObject["vote"]);
			}

			//chancellor selection
			else if (messageCode == "003")
			{
				// player that is president sent their chancellor selection, time to initiate vote
				var name = messageObject["name"];

				initiateVote(userName, name);
			}

			//elected president has sent policy selection to be sent to elected chancellor
			else if (messageCode == "004")
			{
				receivePoliciesFromPresident(messageObject["policies"]);
			}

			// elected chancellor is sending their policy in;
			else if (messageCode == "005")
			{
				recievePolicyFromChancellor(messageObject["policy"]);
			}

			// elected president sent us their choice for who to investigate
			else if (messageCode == "006")
			{
				recievePlayerInvestigationChoice(messageObject["name"]);
			}

			// elected president sent us their choice for next president
			else if (messageCode == "007")
			{
				recieveSpecialElecitionNomination(messageObject["name"]);
			}

			// elected president sent us their kill order
			else if (messageCode == "008")
			{
				recievePlayerKillOrder(messageObject["name"]);
			}

			return returnData;
		}


		public void gameSetup(int players)
		{
			_startingPlayers = players;
			if (players == 5)
			{
				_facNeeded = 2;
				_libsNeeded = 3;
			}
			else if (players == 6)
			{
				_facNeeded = 2;
				_libsNeeded = 4;
			}
			else if (players == 7)
			{
				_facNeeded = 3;
				_libsNeeded = 4;
			}
			else if (players == 8)
			{
				_facNeeded = 3;
				_libsNeeded = 5;
			}
			else if (players == 9)
			{
				_facNeeded = 4;
				_libsNeeded = 5;
			}
			else
			{
				_facNeeded = 4;
				_libsNeeded = 6;
			}
		}

		// returns a list of strings, first one being the faction second one being the secret role
		public List<string> getFaction()
		{
			lock (_lock)
			{
				string faction = "";
				string secretRole = "";

				Random rnd = new Random();
				double total = _libsNeeded + _facNeeded;

				// if we need more players assigned
				if (total > 0)
				{
					double perc = _libsNeeded / total;
					double random = rnd.NextDouble();

					// if we need both fascists and liberals
					if (_libsNeeded != 0 && _facNeeded != 0)
					{
						if (random > perc)
						{
							// fascist

							faction = "fascist";
							if (!_hitlerIssued)
							{
								if (_facNeeded == 1)
								{
									secretRole = "hitler";
									_hitlerIssued = true;
								}
								else
								{
									var coin = rnd.NextDouble();
									if (coin > .5)
									{
										//hitler
										secretRole = "hitler";
										_hitlerIssued = true;
									}
									else
									{
										secretRole = "fascist";
									}
								}
							}
							else
							{
								secretRole = "fascist";
							}
							_facNeeded--;
						}
						else
						{
							//liberal
							_libsNeeded--;
							faction = "liberal";
							secretRole = "liberal";
						}
					}

					else
					{
						// we only need one type of player 
						if (_facNeeded == 0)
						{
							// generate a liberal
							faction = "liberal";
							secretRole = "liberal";
							_libsNeeded--;
						}
						else
						{
							// generate a fascist
							faction = "fascist";
							if (!_hitlerIssued)
							{
								if (_facNeeded == 1)
								{
									// create hitler
									secretRole = "hitler";
									_hitlerIssued = true;
								}
								else
								{
									var pe = rnd.NextDouble();
									if (pe > .5)
									{
										secretRole = "hitler";
										_hitlerIssued = true;
									}
									else
									{
										secretRole = "fascist";
									}
								}
							}
							else
							{
								secretRole = "fascist";
							}
							_facNeeded--;
						}
					}
				}

				var temp = new List<string>();
				temp.Add(faction);
				temp.Add(secretRole);
				return temp;
			}
		} // getFaction

		public List<Tuple<string,string>> sendPlayerInformation()
		{
			// gather all of our players into a json object and send it to the _board
			List<Tuple<string, string>> returnData = new List<Tuple<string, string>>();
			List<object> playerList = new List<object>();
			foreach (var e in _userList)
			{
			
				var temp = new
				{
					name = e.name,
					party = e.Faction,
					secretRole = e.SecretRole
				};
				playerList.Add(temp);


				//notify the player of their stuff
				var UserInformationObj = new
				{
					code = "7",
					faction = e.Faction,
					secretRole = e.SecretRole
				};
				var playerData = JsonConvert.SerializeObject(UserInformationObj);
				returnData.Add(new Tuple<string,string>(e.name, playerData));
			}

			var json = JsonConvert.SerializeObject(
				new
				{
					code = "0",
					players = playerList
				});
			//tell board who's who
			returnData.Add(new Tuple<string, string>("board", json));
			return returnData;

		} // sendPlayerInformation

		//notifies next player that it's their turn and tells the board who's next
		private List<Tuple<string,string>> notifyNextPlayer(string nextPlayer = "" )
		{
			List<Tuple<string, string>> returnData = new List<Tuple<string, string>>();
			// first we need to figure out player order.
			var listOfAllUsers = _userList.ToList();

			// get rid of dead players
			foreach (var player in listOfAllUsers)
			{
				if (player.Alive == false)
				{
					listOfAllUsers.Remove(player);
				}
			}

			List<string> listOfNames = new List<string>();
			foreach(var t in listOfAllUsers)
			{
				listOfNames.Add(t.name);
			}
			// remove yourself plus invalid names
			listOfNames.RemoveAll(x => x == _lastPresident || x == _lastChancellor || x == listOfAllUsers[_playerTurn].name);

			object boardMsgObj = null;
			// now tell the board who's turn it is
			if (nextPlayer == "")
			{
				boardMsgObj = new
				{
					code = 1,
					name = listOfAllUsers[_playerTurn].name
				};
			}
			else
			{
				boardMsgObj = new { code = 1, name = nextPlayer };
			}

			// now we need to send the message to the player who's turn it is
			var playerMsgObj = new
			{
				code = 1,
				names = listOfNames
			};

			//send all the data to everyone
			//_board.Context.Send(JsonConvert.SerializeObject(boardMsgObj));
			returnData.Add(new Tuple<string, string>("board", JsonConvert.SerializeObject(boardMsgObj)));

			// if there is no next player then there is no special election
			if (nextPlayer == "")
			{
				_nominatedPresident = listOfAllUsers[_playerTurn];
				returnData.Add(new Tuple<string, string>(_nominatedPresident.name, JsonConvert.SerializeObject(playerMsgObj)));
				//move tracker to next player
				if (_playerTurn == _players - 1)
				{
					_playerTurn = 0;
				}
				else
				{
					_playerTurn++;
				}
			}
			else
			{
				var n = _userList.Where(x => x.name == nextPlayer).Single();
				//n.Context.Send(JsonConvert.SerializeObject(playerMsgObj));
				returnData.Add(new Tuple<string, string>(n.name, JsonConvert.SerializeObject(playerMsgObj)));
				nextPlayer = "";
			}
			return returnData;
		} // notifyNextPlayer()

		private void initiateVote(string presidentName, string chancellorName)
		{
			// what needs to happen here?
			// we need to tell each person who the vote is for
			// so lets build the message object
			var voteMsgObj = new
			{
				code = "10",
				president = presidentName,
				chancellor = chancellorName
			};

			var jsonMsg = JsonConvert.SerializeObject(voteMsgObj);
			_board.context.Send(jsonMsg);
			int voteCount = 0;
			foreach (var user in _userList.ToList())
			{
				if (user.Alive == true)
				{
					Console.WriteLine(++voteCount + " Starting Vote for: " + user.name);
					user.context.Send(jsonMsg);
				}
			}


			var chancellor = _userList.Where(x => x.name == chancellorName).Single();
			_nominatedChancellor = chancellor;
		}

		private void recieveVote(SHUser voter, string vote)
		{
			// player sent a vote...what does it need to do?
			// it needs to add this player to the votes cast
			// it needs to keep track of the votes
			// and needs to check for the last vote
			lock (_lock)
			{
				var returnData = new List<Tuple<string, string>>();
				if (vote == "0")
				{
					playerVote v = new playerVote(voter.name, false);
					_votesCast.Add(v);
				}
				else
				{
					playerVote v = new playerVote(voter.name, true);
					_votesCast.Add(v);
				}

				_votesCounted++;
				Console.WriteLine(_votesCounted + "Recieved Vote from " + voter.name.ToUpper());
				if (_votesCounted == _players)
				{
					// all votes have been cast
					// we need to tell the board who won
					// then we need to figure out 
					// if they won or if they need to go into the policy decisions
					int yesVotes = 0;
					int noVotes = 0;
					_votesCounted = 0;

					foreach (var v in _votesCast)
					{
						if (v.VotedYes == true)
						{
							yesVotes++;
						}

						else
						{
							noVotes++;
						}
					}
					if (yesVotes > noVotes)
					{
						// continue with players turn, with policy decision
						_gameBoard.ResetElectionTracker();
						policySelection();


						_lastChancellor = _nominatedChancellor.name;
						if (_players > 5)
						{
							_lastPresident = _nominatedPresident.name;
						}
						else
						{
							_lastPresident = "";
						}
						var resetTrackerMsg = new { code = "5", reset = true };
						_board.context.Send(JsonConvert.SerializeObject(resetTrackerMsg));


					}
					else
					{
						// hung parlement, increase on gameboard, 
						// if a policy needs to be played, play it
						// call next player
						var libCards = _gameBoard.PlayedLib;
						var facCards = _gameBoard.PlayedFac;

						bool reset = _gameBoard.IncreaseElectionTracker();
						var angryPopulaceMsg = new { code = "5", reset };
						_board.context.Send(JsonConvert.SerializeObject(angryPopulaceMsg));
						checkGameOver();
						notifyNextPlayer();

						if (reset == true)
						{
							// 3 times in a row of angry populace
							var cardPlayed = "";
							if (_gameBoard.PlayedFac > facCards)
							{
								// fac card was played
								cardPlayed = "Fascist";
							}
							else
							{
								cardPlayed = "Liberal";	
							}
							var angryPopulaceCardPlayed = new { code = "3", policy = cardPlayed };
							_board.context.Send(JsonConvert.SerializeObject(angryPopulaceCardPlayed));
						}
					}

					// break the votes up and send them to the board
					var jaList = new List<string>();
					var neinList = new List<string>();
					foreach (var p in _votesCast)
					{
						if (p.VotedYes == true)
						{
							jaList.Add(p.Name);
						}
						else
						{
							neinList.Add(p.Name);
						}
					}

					var voteResults = new { code = "4", ja = jaList, nein = neinList };
					_board.context.Send(JsonConvert.SerializeObject(voteResults));
					_votesCast.Clear();
				}
			}
		} // recieve vote

		private void checkGameOver()
		{
			// how does the game end?
			// hitler elected chancellor with 3 or more fascist policys
			// 5 fascist policys
			// 6 liberal policys
			// kill hitler dun dun duhhhh
			bool gameOver = false;
			string winner = "";

			// first check if hitler is dead or is chancellor
			SHUser hitler = null;
			foreach (var key in _userList)
			{
				var p = key;
				if (p.SecretRole == "hitler")
				{
					hitler = p;
				}
			}
			if (hitler.Alive == false)
			{
				gameOver = true;
				winner = "L";
			}
			else if (hitler.IsChancellor == true && _gameBoard.PlayedFac > 3)
			{
				gameOver = true;
				winner = "F";
			}
			else if (_gameBoard.PlayedFac == 5)
			{
				gameOver = true;
				winner = "F";
			}
			else if (_gameBoard.PlayedLib == 6)
			{
				gameOver = true;
				winner = "L";
			}

			// send proper messages if they need to be sent and kill the game
		}// checkGameOver

		// this function is called when a successful election of a govermnet happens.
		// it draws 3 policy cards, and sends them to the president
		private void policySelection()
		{
			// draw three cards from the deck
			List<string> policies = _gameBoard.DrawCards();

			//create object that sends a code to the president, along with drawn policies
			var policySelectionObj = new
			{
				code = "3",
				policies = policies
			};

			var jsonObj = JsonConvert.SerializeObject(policySelectionObj);
			_nominatedPresident.context.Send(jsonObj);
		}// policyselection

		// takes a string that is in the form xxx=policy1Name&xxx=polic2Name;
		private void receivePoliciesFromPresident(string policies)
		{
			// first tokenize into pieces based on '=';

			var pieces = policies.Split('=');
			if (pieces.Length > 2)
			{
				// we need to split the second set on '&' 
				var secondPieces = pieces[1].Split('&');

				var pol1 = pieces[2];
				var pol2 = secondPieces[0];

				// take pol1 and pol2 and send to chancellor
				var polList = new List<string>();
				polList.Add(pol1);
				polList.Add(pol2);
				bool vetoEnabled = _gameBoard.PlayedFac >= 5;
				var chancellorMessage = new
				{
					code = "2",
					policies = polList,
					veto = vetoEnabled

				};
				var jsonObject = JsonConvert.SerializeObject(chancellorMessage);
				_nominatedChancellor.context.Send(jsonObject);
 			}
		}

		private void recievePolicyFromChancellor(string policyString)
		{
			var policy = policyString.Split('=')[1];
			var boardNotification = new { code = "3", policy = policy };

			if (policy == "Fascist")
			{
				// play fascist policy
				_gameBoard.PlayFascist();
			}
			else
			{
				// play liberal policy
				_gameBoard.PlayLiberal();
			}
			_board.context.Send(JsonConvert.SerializeObject(boardNotification));

			// check if a power should be played yet

			if (!powers())
			{
				notifyNextPlayer();
			}
			
		}

		// if it is time for a player to use a power, this function takes over notifiing players
		// returns false if no powers need to be handled true otherwise
		private bool powers()
		{
			// handle each number of fascist cards one at a time
			if (_gameBoard.PlayedFac == 1)
			{
				if (_startingPlayers > 8)
				{
					sendPlayerNamestoPresident("4");
					return true;
				}
			}
			else if (_gameBoard.PlayedFac == 2)
			{
				if (_startingPlayers > 6)
				{
					sendPlayerNamestoPresident("4");
					return true;
				}
			}
			else if (_gameBoard.PlayedFac == 3)
			{
				if (_startingPlayers > 6)
				{
					sendPlayerNamestoPresident("12");
				}
				else
				{
					sendPolicyPeek();
				}
				return true;
			}
			else if (_gameBoard.PlayedFac > 3)
			{
				sendPlayerNamestoPresident("5");
				return true;
			}
			return false;
		}

		private void recievePlayerKillOrder(string name)
		{
			var playerKilled = _userList.Where(x => x.name == name).Single();

			// make the player dead and reduce the number of players in the game so term limits are 
			// handled correctly..basicly just if it goes down to 5 players
			playerKilled.Alive = false;
			_players--;

			// send message to board so they can update the interface
			var boardKillPlayer = new { code = "11", name };
			_board.context.Send(JsonConvert.SerializeObject(boardKillPlayer));

			// notify the player they are dead
			var playerKillMsg = new { code = "9" };
			playerKilled.context.Send(JsonConvert.SerializeObject(playerKillMsg));

			notifyNextPlayer();
		}
		// sends list of player names to current president

		// takes choice of person to investigate and sends their faction information to the president
		private void recievePlayerInvestigationChoice(string name)
		{
			var personBeingInvestigated = _userList.Where(x => x.name == name).Single();
			SHUser p = personBeingInvestigated;
			if (p != null)
			{
				var playerInformation = new
				{
					code = "8",
					faction = p.Faction,
					name = p.name
				};
				_nominatedPresident.context.Send(JsonConvert.SerializeObject(playerInformation));
			}
			notifyNextPlayer();
		}

		private void sendPolicyPeek()
		{
			var policyPeekMsg = new { code = "11", policies = _gameBoard.policyPeek()};
			_nominatedPresident.context.Send(JsonConvert.SerializeObject(policyPeekMsg));
			notifyNextPlayer();
		}

		// send player names to president
		private void sendPlayerNamestoPresident(string code)
		{
			var playerNames = new List<string>();

			foreach (var pair in _userList)
			{
				if (_nominatedPresident.name != pair.name)
				{
					playerNames.Add(pair.name);
				}
			}

			var playerNamesMsg = new
			{
				code = code,
				playerNames
			};

			_nominatedPresident.context.Send(JsonConvert.SerializeObject(playerNamesMsg));

		}

		// if the game is in a special election this function calls 
		private void recieveSpecialElecitionNomination(string name)
		{
			
			notifyNextPlayer(name);
		}

		public void allowUserInterupt()
		{
			Console.WriteLine("DEBUGGING");
		}

		public IUser createUser(string name)
		{
			
			var data = getFaction();
			var user = new SHUser(name, data[0], data[1]);
			_userList.Add(user);
			var notifyBoardMsg = new { code = "0", connected = true };

			_board.context.Send(JsonConvert.SerializeObject(notifyBoardMsg));
			return user;
		}

		public void setBoard(IUser user)
		{
			_board = user as SHUser;
		}
		public bool gameFull()
		{
			if (_playersReady == _players)
			{
				return true;
			}
			return false;
		}
	} // end of class
}// end of namespace
