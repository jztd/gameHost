using System;
using Fleck;
namespace WSServer
{
	public class User : IUser
	{
		private IWebSocketConnection _context;
		private string _name;

		public User(string name)
		{
			_name = name;
		}

		public void send(string message)
		{
			_context.Send(message);
		}

		public IWebSocketConnection context
		{
			get { return _context;}
			set { _context = value;}
		}

		public string name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}
	}
}
