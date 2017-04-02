using System;
using System.Collections.Generic;
namespace SecretCompanion
{
	public class Board
	{
		private int _playedFac;
		private int _playedLib;
		private int _electionTracker;
		private PolicyDeck _deck;

		public Board()
		{
			_playedLib = 0;
			_playedFac = 0;
			_electionTracker = 0;
			_deck = new PolicyDeck();
		}

		public int PlayedFac
		{
			get { return _playedFac; }
		}

		public int PlayedLib
		{
			get { return _playedLib; }
		}

		public void PlayFascist()
		{
			_playedFac++;
			_deck.PlayFac();
		}

		public void PlayLiberal()
		{
			_playedLib++;
			_deck.PlayLib();
		}

		// increases the election tracker and if it is an angry populace it returns true
		public bool IncreaseElectionTracker()
		{
			_electionTracker++;
			if (_electionTracker >= 3)
			{
				PlayTopCard();
				_electionTracker = 0;
				return true;
			}
			return false;
		}

		public void ResetElectionTracker()
		{
			_electionTracker = 0;
		}
		public List<string> DrawCards()
		{
			return _deck.GetNextPolicy(3);
		}

		public void PlayTopCard()
		{
			List<string> cardList = _deck.GetNextPolicy(1);
			if (cardList[0] == "F")
			{
				PlayFascist();
			}
			else
			{
				PlayLiberal();
			}
		}

		public List<string> policyPeek()
		{
			return _deck.Top3PolicyPeek();
		}
	}
}
