using System;
using System.Collections.Generic;
namespace WSServer
{
	public interface IGame
	{
		bool gameFull(); // if enough players connected or not
		List<Tuple<string,string>> receiveMessage(string userName, string message); // have data sent to the game and it sends its response
														// first string is the player name second is the data
		IUser createUser(string name); // creates a user for the game
		void setBoard(IUser user); // each game needs a display board
	}
}
