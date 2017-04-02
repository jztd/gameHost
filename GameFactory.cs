using System;
using SecretCompanion;
using WSServer;
using Fleck;
namespace WSServer
{
	public static class GameFactory
	{
		public static IGame createGame(string gameName)
		{
			if (gameName == "secretHitler")
			{
				return new SHGame();
			}
			return null;
		}
	}
}
