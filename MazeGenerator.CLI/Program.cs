using MazeGenerator;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;

namespace MazeGenerator.CLI
{
	internal static class Program
	{
		private static void Main()
		{
			var seed = Random.Shared.Next(-10000, 10000);
			ushort w = 35, h = 70;

			Console.Clear();

			while (true)
			{
				while (true)
				{
					Console.Write($"""
		Parameters are:
			Seed:   {seed}
			Width:  {w}
			Height: {h}

		Generate the maze (y/n; i to import from file; r to generate random mazes)? 
		""");

					if (Console.ReadLine() is string input)
					{
						switch (input)
						{
							case "y":
								goto generate;
							case "i":
							{
								Console.WriteLine("Enter the filename:");
								var file = Console.ReadLine() ?? "";

								using var stream = File.OpenRead(file);
								var maze = Maze.Import(stream);

								seed = maze.Seed;
								w = maze.Width;
								h = maze.Height;
								Console.Clear();

								Render(maze, new(ushort.MaxValue, ushort.MaxValue), [], new(ushort.MaxValue, ushort.MaxValue), []);
								continue;
							}
							case "r":
								RandomUntilStopped();
								continue;
						}
					}

					seed = InputParseable<int>("Enter the new seed:");
					w = InputParseable<ushort>("Enter the new width:");
					h = InputParseable<ushort>("Enter the new height:");
				}

			generate:

				Console.Clear();
				var generated = MazeGenerationAlgorithm.Backtracking(w, h, seed, (a, b, c, d, e) => RenderDebug(a, b, c, d, e));

				Render(generated, new(ushort.MaxValue, ushort.MaxValue), [], new(ushort.MaxValue, ushort.MaxValue), []);

				Console.Write($"""
Next:
'e' to export to '{seed}_{w}x{h}.maze'
'q' to exit
'r' to try again
(e/q/r): 
""");

				switch (Console.ReadLine())
				{
					case "e":
					{
						using var stream = File.OpenWrite($"{seed}_{w}x{h}.maze");
						generated.Export(stream);
						break;
					}
					case "q":
						return;
				}
			}
		}

		static void RandomUntilStopped()
		{
			using var logFile = File.Open("failing-mazes.csv", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
			logFile.Seek(0, SeekOrigin.End);
			using var logWriter = new StreamWriter(logFile, Encoding.UTF8);
			Console.TreatControlCAsInput = true;
			Console.CursorVisible = false;
			while (!Console.KeyAvailable || Console.ReadKey() is not { Key: ConsoleKey.C, Modifiers: ConsoleModifiers.Control })
			{
				ushort width = (ushort)Random.Shared.Next(2, 150);
				ushort height = (ushort)Random.Shared.Next(2, 150);
				int seed = Random.Shared.Next();

				Console.WriteLine($"""
					Width:  {width}
					Height: {height}
					Seed:   {seed}
					""");

				try
				{
					Render(MazeGenerationAlgorithm.Backtracking(width, height, seed, (a, b, c, d, e) => RenderSometimes(a, b, c, d, e)), new(ushort.MaxValue, ushort.MaxValue), [], new(ushort.MaxValue, ushort.MaxValue), []);
				} catch (Exception ex)
				{
					logWriter.WriteLine($"{width}\t{height}\t{seed}\t{ex.Message.Replace('\n', ' ')}");
				}
			}
			Console.Clear();
			Console.TreatControlCAsInput = false;
			Console.CursorVisible = true;
		}

		static int offset;
		[Conditional("DEBUG")]
		static void RenderSometimes(Maze generated, Point position, IReadOnlyCollection<Point> nextOptions, Point lastPosition, IReadOnlyCollection<Point> allInteractedPoints)
		{
			if (offset++ % 20 == 0)
			{
				Render(generated, position, nextOptions, lastPosition, allInteractedPoints);
			}
		}

		[Conditional("DEBUG")]
		static void RenderDebug(Maze generated, Point position, IReadOnlyCollection<Point> nextOptions, Point lastPosition, IReadOnlyCollection<Point> allInteractedPoints)
		{
			Console.ReadKey();
			Render(generated, position, nextOptions, lastPosition, allInteractedPoints);
		}

		static void Render(Maze generated, Point position, IReadOnlyCollection<Point> nextOptions, Point lastPosition, IReadOnlyCollection<Point> allInteractedPoints)
		{
			StringBuilder result = new();
			result.Append(ANSI.RESET_ALL);
			result.Append('_');
			for (var i = 0; i < generated.Width; i++)
			{
				if (i != generated.Start.X)
				{
					result.Append("__");
				} else
				{
					result.Append("  ");
				}
			}
			result.AppendLine();

			for (var y = 0; y < generated.Height; y++)
			{
				result.Append('|');
				for (var x = 0; x < generated.Width; x++)
				{
					var isCurrent = position.X == x && position.Y == y;

					var isNextOption = !isCurrent && nextOptions.Any(v => v.X == x && v.Y == y);

					var isLastPosition = !isNextOption && lastPosition.X == x && lastPosition.Y == y;

					if (isCurrent)
					{
						result.Append(ANSI.FOREGROUND_GREEN);
						result.Append(ANSI.BACKGROUND_YELLOW);
					} else if (isNextOption)
					{
						result.Append(ANSI.BACKGROUND_BLUE);
					} else if (isLastPosition)
					{
						result.Append(ANSI.BACKGROUND_WHITE);
						result.Append(ANSI.FOREGROUND_BLACK);
					}
					if (allInteractedPoints.Any(v => v.X == x && v.Y == y))
					{
						if (isCurrent)
						{
							result.Append(ANSI.FOREGROUND_RED);
						} else if (isNextOption)
						{
							result.Append(ANSI.FOREGROUND_MAGENTA);
						} else if (!isLastPosition)
						{
							result.Append(ANSI.FOREGROUND_BLUE);
						}
					}
					if (generated.Solution.Any(v => v.X == x && v.Y == y) && !isCurrent && !isNextOption && !isLastPosition)
					{
						result.Append(ANSI.BACKGROUND_GRAY);
						result.Append(ANSI.FOREGROUND_CYAN);
					}

					const char empty = ' ';
					const char bottom = '_';
					const char side = '|';

					if (y == generated.End.Y && x == generated.End.X)
					{
						switch (generated.Walls[x, y])
						{
							case Wall.All:
							case Wall.Right:
								result.Append([empty, side]);
								break;
							default:
								result.Append([empty, empty]);
								break;
						}
					} else
					{
						switch (generated.Walls[x, y])
						{
							case Wall.All:
								result.Append([bottom, side]);
								break;
							case Wall.Right:
								result.Append([empty, side]);
								break;
							case Wall.Bottom:
								result.Append([bottom, bottom]);
								break;
							default:
								result.Append(new string(empty, 2));
								break;
						}
					}

					result.Append(ANSI.RESET_ALL);
				}
				result.AppendLine();
			}

			Console.SetCursorPosition(0, 0);
			Console.Write(result.ToString());
		}

		static T InputParseable<T>(string message) where T : IParsable<T>
		{
			T? result;
			while (true)
			{
				Console.Write(message + " ");

				if (Console.ReadLine() is string value && T.TryParse(value, null, out result))
					break;

				Console.WriteLine("Invalid input.");
			}
			return result;
		}
	}

	public static class ANSI
	{
		// Foreground colors
		public const string FOREGROUND_RED = "\u001b[31m";
		public const string FOREGROUND_GREEN_LIGHT = "\u001b[01;32m";
		public const string FOREGROUND_GREEN = "\u001b[32m";
		public const string FOREGROUND_YELLOW = "\u001b[33m";
		public const string FOREGROUND_BLUE = "\u001b[34m";
		public const string FOREGROUND_MAGENTA = "\u001b[35m";
		public const string FOREGROUND_CYAN = "\u001b[36m";
		public const string FOREGROUND_WHITE = "\u001b[37m";
		public const string FOREGROUND_BLACK = "\u001b[30m";

		// Text effects
		public const string BLINK_SLOW = "\u001b[5m";
		public const string BLINK_RAPID = "\u001b[6m";

		// Text styles
		// public const string NORMAL = "\u033b[5m"; // Fix this
		public const string BOLD = "\u001b[1m";
		public const string UNDERLINE = "\u001b[4m";
		public const string ITALIC = "\u001b[3m";
		public const string STRIKETHROUGH = "\u001b[9m";

		// Background colors
		public const string BACKGROUND_RED = "\u001b[41m";
		public const string BACKGROUND_GREEN = "\u001b[42m";
		public const string BACKGROUND_YELLOW = "\u001b[43m";
		public const string BACKGROUND_BLUE = "\u001b[44m";
		public const string BACKGROUND_MAGENTA = "\u001b[45m";
		public const string BACKGROUND_CYAN = "\u001b[46m";
		public const string BACKGROUND_WHITE = "\u001b[47m";
		public const string BACKGROUND_GRAY = "\u001B[100m";
		public const string BACKGROUND_BLACK = "\u001b[40m";

		// Reset foreground color
		public const string FOREGROUND_RESET = "\u001b[39m";

		// Reset background color
		public const string BACKGROUND_RESET = "\u001b[49m";

		/// <summary>
		/// ANSI escape code to reset the console color to its default value.
		/// </summary>
		public const string RESET_ALL = "\u001b[0m";
	}
}