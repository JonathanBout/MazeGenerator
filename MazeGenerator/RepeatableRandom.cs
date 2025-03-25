using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MazeGenerator
{
	public class RepeatableRandom(int seed)
	{
		private readonly Lock _seedLock = new();
		private int _seed = seed;
		private readonly int _initialSeed = seed;

		public void Reset()
		{
			lock (_seedLock)
			{
				_seed = _initialSeed;
			}
		}

		public int Next()
		{
			lock(_seedLock)
			{
				// https://en.wikipedia.org/wiki/Linear_congruential_generator
				// x = (a * x + c) % m
				//	with a = 1664525, c = 1013904223, m = int.MaxValue
				//	gives
				// x = (1664525 * x + 1013904223) % int.MaxValue

				// to prevent overflow, we do the math with long and then cast back to int
				return _seed = (int)unchecked(((1664525L * _seed) + 1013904223L) % int.MaxValue);
			}
		}

		public int Next(int min = 0, int max = int.MaxValue)
		{
			if (min == 0 && max == int.MaxValue)
			{
				return Next();
			}

			var next = int.Abs(Next());
			return min + (next % (max - min));
		}
	}
}
