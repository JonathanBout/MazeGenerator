namespace MazeGenerator.Tests
{
	public class RepeatableRandomTests
	{
		[Test]
		[TestCase(0)]
		[TestCase(1)]
		[TestCase(30312)]
		[TestCase(-29229)]
		[TestCase(int.MaxValue)]
		public void TestIfRepeatable(int seed)
		{
			var random = new RepeatableRandom(seed);
			var random2 = new RepeatableRandom(seed);

			for (int i = 0; i < 1000; i++)
			{
				Assert.That(random.Next(), Is.EqualTo(random2.Next()));
			}
		}

		[Test]
		[TestCase(0, 0, 10)]
		[TestCase(1, 9, 10)]
		[TestCase(30312, 0, 100)]
		[TestCase(-29229, -1, 1)]
		public void TestIfRangeIsCorrect(int seed, int min, int max)
		{
			var random = new RepeatableRandom(seed);
			for (int i = 0; i < 1000; i++)
			{
				var value = random.Next(min, max);
				Assert.That(value, Is.GreaterThanOrEqualTo(min));
				Assert.That(value, Is.LessThan(max));
			}
		}

		[Test]
		[TestCase(0)]
		[TestCase(1)]
		[TestCase(30312)]
		[TestCase(-29229)]
		[TestCase(int.MaxValue)]
		public void TestIfIsReallyRandom(int seed)
		{
			var random = new RepeatableRandom(seed);
			HashSet<int> values = new HashSet<int>(1000);

			for (int i = 0; i < 1000; i++)
			{
				Assert.That(values.Add(random.Next()), Is.True);
			}
		}
	}
}
