using System;
using System.Collections.Generic;
namespace SecretCompanion
{
	public class PolicyDeck
	{
		private int _numFac;
		private int _numLib;
		private int _playedFac;
		private int _playedLib;
		private List<string> _peekedPolicies;
		public PolicyDeck()
		{
			_numFac = 11;
			_numLib = 6;
			_playedFac = 0;
			_playedLib = 0;
			_peekedPolicies = new List<string>();
		}

		public List<string> GetNextPolicy(int num)
		{
			if( (_numLib + _numFac) < num)
			{
				ShuffleDeck();
			}

			Random rnd = new Random();
			List<string> returnedPolicies = new List<string>();
			for (int i = 0; i < num; i++)
			{
				// check to see if we have any pregenerated policies
				if (_peekedPolicies.Count == 0)
				{
					double total = _numLib + _numFac;
					double percFac = _numFac / total;

					double temp = rnd.NextDouble();
					if (temp < percFac)
					{
						returnedPolicies.Add("F");
						_numFac--;
					}
					else
					{
						returnedPolicies.Add("L");
						_numLib--;
					}
				}
				else
				{
					returnedPolicies.Add(_peekedPolicies[0]);
					_peekedPolicies.RemoveAt(0);
				}

			}
			return returnedPolicies;
		}

		private void ShuffleDeck()
		{
			_numLib = 6 - _playedLib;
			_numFac = 11 - _playedFac;
		}

		public void PlayLib()
		{
			_playedLib++;
		}

		public void PlayFac()
		{
			_playedFac++;
		}

		public void ResetDeck()
		{
			_playedFac = 0;
			_playedLib = 0;
			ShuffleDeck();
		}
		public List<string> Top3PolicyPeek()
		{
			_peekedPolicies = GetNextPolicy(3);

			return _peekedPolicies;
		}
	}
}
