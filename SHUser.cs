using System;
using Fleck;
using WSServer;
namespace SecretCompanion
{
 
	public class SHUser : User
	{
		private string _faction;
		private string _secretRole;
		private bool _alive;
		private bool _isChancellor;

		public SHUser(string name, string faction, string secretRole) : base(name)
		{
			_faction = faction;
			_secretRole = secretRole;
			_alive = true;
			_isChancellor = false;
		}

		public bool Alive
		{
			get { return _alive;}
			set { _alive = value; }
		}

		public bool IsChancellor
		{
			get { return _isChancellor; }
			set { _isChancellor = value; }
		}
		public string Faction
		{
			get { return _faction;}
			set
			{
				if (value == "L" || value == "F")
				{
					_faction = value;
				}
				else
				{
					throw new ArgumentException("Invalid Faction");
				}
			}
		}

		public string SecretRole
		{
			get { return _secretRole; }
			set
			{
				if (value == "L" || value == "F" || value == "H")
				{
					_secretRole = value;
				}
				else
				{
					throw new ArgumentException("Invalid secret role");
				}
			}
		}
	}
}
