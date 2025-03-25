using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using MazeGenerator;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run();

namespace MazeGenerator.Benchmarks
{
	[MemoryDiagnoser(false)]
	public class MazeBenchmarks
	{
		public record MazeParameter(ushort Width, ushort Height, int Seed)
		{
			public override string ToString() => $"W,H={Width},{Height};S={Seed}";
		}

		public MazeParameter[] MazeParameters { get; } = [
				new MazeParameter(10, 10, 5),
				new MazeParameter(20, 20, 5),
				new MazeParameter(40, 40, 5)
			];

		[Benchmark]
		[ArgumentsSource(nameof(MazeParameters))]
		public Maze GenerateMaze(MazeParameter param)
		{
			return MazeGenerationAlgorithm.Backtracking(param.Width, param.Height, param.Seed);
		}
	}

	public class RandomBenchmarks
	{
		const int OPS = 1_000_000;

		[IterationSetup]
		public void Setup()
		{
			random = new RepeatableRandom(5);
			builtInRandom = new Random(5);
		}

		private RepeatableRandom random = null!;
		private Random builtInRandom = null!;

		[Benchmark(OperationsPerInvoke = OPS)]
		public long Next()
		{
			unchecked
			{
				long sum = 0;
				for (int i = 0; i < OPS; i++)
				{
					sum += random.Next();
				}
				return sum;
			}
		}

		[Benchmark(OperationsPerInvoke = OPS)]
		public long BuiltInNext()
		{
			unchecked
			{
				long sum = 0;
				for (int i = 0; i < OPS; i++)
				{
					sum += builtInRandom.Next();
				}
				return sum;
			}
		}

		[Benchmark(OperationsPerInvoke = OPS)]
		[Arguments(0, 110)]
		[Arguments(100, 110)]
		public long NextMinMax(int min, int max)
		{
			unchecked
			{
				long sum = 0;
				for (int i = 0; i < OPS; i++)
				{
					sum += random.Next(min, max);
				}
				return sum;
			}
		}

		[Benchmark(OperationsPerInvoke = OPS)]
		[Arguments(0, 110)]
		[Arguments(100, 110)]
		public long BuiltInNextMinMax(int min, int max)
		{
			unchecked
			{
				long sum = 0;
				for (int i = 0; i < OPS; i++)
				{
					sum += random.Next(min, max);
				}
				return sum;
			}
		}
	}
}