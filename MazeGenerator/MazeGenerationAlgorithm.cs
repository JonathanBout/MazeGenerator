using CommunityToolkit.HighPerformance;
using System.Diagnostics;

namespace MazeGenerator
{
	public readonly record struct Point(ushort X, ushort Y)
	{
		public override readonly int GetHashCode() => HashCode.Combine(X, Y);
	}

	public delegate void Updated(Maze currentMaze, Point currentPosition, IReadOnlyCollection<Point> nextPositions, Point lastPosition, IReadOnlyCollection<Point> allInteractedCells);

	public static class MazeGenerationAlgorithm
	{
		public static Maze Current(ushort width, ushort height, int seed, Updated? updated = null)
		{
			return Backtracking(width, height, seed, updated);
		}

		public static Maze Backtracking(ushort width, ushort height, int seed, Updated? updated = null)
		{
			var random = new RepeatableRandom(seed);

			var walls = new Wall[width, height];
			var wallsSpan = new Span2D<Wall>(walls);

			wallsSpan.Fill(Wall.All);

			Point startCell = new((ushort)random.Next(0, width), 0);
			Point endCell = new((ushort)random.Next(0, width), (ushort)(height - 1));

			walls[endCell.X, endCell.Y] &= ~Wall.Bottom;

			Stack<Point> trace = new Stack<Point>(width * height / 5 * 4);
			HashSet<Point> allInteractedPoints = new HashSet<Point>(width * height);
			int totalPoints = width * height;
			Direction lastMove = Direction.None;

			Point position = startCell;

			Point[]? solution = null;
			trace.Push(position);
			bool didBacktrack = false;
			while (allInteractedPoints.Count != totalPoints)
			{
				bool wasVisited = allInteractedPoints.Add(position);
				Debug.Assert(didBacktrack != wasVisited);
				if (position == endCell && solution is null)
				{
					solution = [.. trace];
				}

				Direction options = Direction.Any;

				if (position.Y == 0)
				{
					options ^= Direction.Up;
				} else if (position.Y == height - 1)
				{
					options ^= Direction.Down;
				}

				if (position.X == 0)
				{
					options ^= Direction.Left;
				} else if (position.X == width - 1)
				{
					options ^= Direction.Right;
				}

				if ((options & Direction.Up) == Direction.Up && allInteractedPoints.Contains(position with { Y = (ushort)(position.Y - 1) }))
				{
					options &= ~Direction.Up;
				}
				if ((options & Direction.Down) == Direction.Down && allInteractedPoints.Contains(position with { Y = (ushort)(position.Y + 1) }))
				{
					options &= ~Direction.Down;
				}
				if ((options & Direction.Left) == Direction.Left && allInteractedPoints.Contains(position with { X = (ushort)(position.X - 1) }))
				{
					options &= ~Direction.Left;
				}
				if ((options & Direction.Right) == Direction.Right && allInteractedPoints.Contains(position with { X = (ushort)(position.X + 1) }))
				{
					options &= ~Direction.Right;
				}

				if (lastMove != Direction.None)
				{
					var moveToLast = lastMove.Invert();
					options &= ~moveToLast;
				}

#if DEBUG
				if (updated is not null)
				{
					Point[] nextPositions = [.. options.ToOptions().Select(DirectionExtensions.ToPoint)
						.Select(v => new Point((ushort)(position.X + v.Item1), (ushort)(position.Y + v.Item2)))];

					Point last = new(ushort.MaxValue, ushort.MaxValue);
					if (trace.TryPop(out var cur))
					{
						if (trace.TryPeek(out var newLast))
						{
							last = newLast;
						}
						trace.Push(cur);
					}
					updated(new Maze(width, height, seed, startCell, endCell, walls, solution ?? [.. trace]), position, nextPositions, last, allInteractedPoints);
				}
#endif
				if (options == Direction.None)
				{
					if (trace.TryPop(out _) && trace.TryPeek(out var peekedPosition))
					{
						if (peekedPosition.X == position.X)
						{
							if (peekedPosition.Y < position.Y)
							{
								lastMove = Direction.Down;
							} else
							{
								lastMove = Direction.Up;
							}
						} else if (peekedPosition.X < position.X)
						{
							lastMove = Direction.Left;
						} else
						{
							lastMove = Direction.Right;
						}

						position = peekedPosition;

						didBacktrack = true;
						continue;
					}
					throw new Exception($"Maze {width}x{height} with seed {seed} found a bug!");
				}

				didBacktrack = false;

				var availableOptions = options.ToOptions();

				var nextMoveIndex = random.Next(0, availableOptions.Length);

				lastMove = availableOptions[nextMoveIndex];

				var (diffX, diffY) = lastMove.ToPoint();

				Point newPosition = new((ushort)(position.X + diffX), (ushort)(position.Y + diffY));

				if (diffX != 0)
				{
					if (diffX < 0)
					{
						walls[newPosition.X, newPosition.Y] &= ~Wall.Right;
					} else
					{
						walls[position.X, position.Y] &= ~Wall.Right;
					}
				} else
				{
					if (diffY < 0)
					{
						walls[newPosition.X, newPosition.Y] &= ~Wall.Bottom;
					} else
					{
						walls[position.X, position.Y] &= ~Wall.Bottom;
					}
				}

				trace.Push(newPosition);
				position = newPosition;
			}

			return new Maze(width, height, seed, startCell, endCell, walls, solution ?? []);
		}
	}
}
