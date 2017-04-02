using System;
namespace SecretCompanion
{
	public class playerVote
	{
		private string _name;
		private bool _votedYes;
		public playerVote(string name, bool vote)
		{
			_name = name;
			_votedYes = vote;
		}

		public string Name
		{
			get { return _name; }
		}

		public bool VotedYes
		{
			get { return _votedYes; }
		}
	}
}
