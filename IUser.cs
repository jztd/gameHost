using System;
using Fleck;
namespace WSServer
{
	public interface IUser
	{
		
		void send(string message);
		IWebSocketConnection context { get; set;}
		string name { get; set; }

	}
}
