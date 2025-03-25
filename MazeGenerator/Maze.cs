using CommunityToolkit.HighPerformance;
using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MazeGenerator
{
	public readonly struct Maze
	{
		public readonly Point Start { get; }
		public readonly Point End { get; }
		public readonly ushort Width { get; }
		public readonly ushort Height { get; }
		public readonly int Seed { get; }

		public readonly Point[] Solution { get; }
		public readonly Wall[,] Walls { get; }

		// Walls is a 2D array of booleans, where true means there is a wall and false means there is no wall.
		// It is one cell shorter in each dimension than the maze itself, as it only takes into account the walls between cells.
		public Maze(ushort width, ushort height, int seed, Point start, Point end, Wall[,] walls, Point[] solution)
		{
			Width = width;
			Height = height;
			Walls = walls;
			Start = start;
			End = end;
			Seed = seed;
			Solution = solution;
		}

		public readonly void Export(Stream stream)
		{
			Span<byte> buffer = stackalloc byte[sizeof(int)];

			WriteUint16LittleEndian(buffer, stream, Width);
			WriteUint16LittleEndian(buffer, stream, Height);
			WriteInt32LittleEndian(buffer, stream, Seed);
		}

		private static void WriteUint16LittleEndian(Span<byte> buffer, Stream stream, ushort value)
		{
			BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
			stream.Write(buffer[..sizeof(ushort)]);
		}
		private static void WriteInt32LittleEndian(Span<byte> buffer, Stream stream, int value)
		{
			BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
			stream.Write(buffer[..sizeof(int)]);
		}
		private static ushort ReadUint16LittleEndian(Span<byte> buffer, Stream stream)
		{
			stream.ReadExactly(buffer[..sizeof(ushort)]);
			return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
		}
		private static int ReadInt32LittleEndian(Span<byte> buffer, Stream stream)
		{
			stream.ReadExactly(buffer[..sizeof(int)]);
			return BinaryPrimitives.ReadInt32LittleEndian(buffer);
		}

		public static Maze Import(Stream stream)
		{
			Span<byte> buffer = stackalloc byte[sizeof(int)];

			var width = ReadUint16LittleEndian(buffer, stream);
			var height = ReadUint16LittleEndian(buffer, stream);
			var seed = ReadInt32LittleEndian(buffer, stream);

			return MazeGenerationAlgorithm.Backtracking(width, height, seed);
		}
	}

	[Flags]
	[SuppressMessage("Roslynator", "RCS1135:Declare enum member with zero value (when enum has FlagsAttribute)")]
	[SuppressMessage("Roslynator", "RCS1191:Declare enum value as combination of names")]
	public enum Wall : byte
	{
		Right = 0b00000001,
		Bottom = 0b00000010,
		All = 0b00000011,
		None = 0b00001000,
	}

	[Flags]
	public enum Direction : byte
	{
		None,
		Left = 1,
		Right = 2,
		Horizontal = Left | Right,
		Up = 4,
		Down = 8,
		Vertical = Up | Down,
		Any = Horizontal | Vertical
	}

	internal static class DirectionExtensions
	{
		public static Direction Invert(this Direction direction)
		{
			if ((direction & Direction.Horizontal) == Direction.Horizontal)
			{
				return Direction.Vertical;
			}

			if ((direction & Direction.Vertical) == Direction.Vertical)
			{
				return Direction.Horizontal;
			}

			if (direction == Direction.Any)
			{
				return Direction.None;
			}

			if (direction == Direction.None)
			{
				return Direction.Any;
			}

			return direction switch
			{
				Direction.Up => Direction.Down,
				Direction.Down => Direction.Up,
				Direction.Left => Direction.Right,
				Direction.Right => Direction.Left,
				_ => throw new UnreachableException()
			};
		}

		public static Direction[] ToOptions(this Direction direction)
		{
			return [.. ToOptionsEnumerable(direction)];

			static IEnumerable<Direction> ToOptionsEnumerable(Direction d)
			{
				if ((d & Direction.Right) == Direction.Right)
				{
					yield return Direction.Right;
				}
				if ((d & Direction.Left) == Direction.Left)
				{
					yield return Direction.Left;
				}
				if ((d & Direction.Up) == Direction.Up)
				{
					yield return Direction.Up;
				}
				if ((d & Direction.Down) == Direction.Down)
				{
					yield return Direction.Down;
				}
			}
		}

		public static Point ToPoint(this Direction d, Point relativeTo)
		{
			var (xDiff, yDiff) = d.ToPoint();

			return new((ushort)(relativeTo.X + xDiff), (ushort)(relativeTo.Y + yDiff));
		}

		public static (sbyte, sbyte) ToPoint(this Direction d)
		{
			sbyte x = 0, y = 0;

			if ((d & Direction.Right) == Direction.Right)
			{
				x++;
			}
			if ((d & Direction.Left) == Direction.Left)
			{
				x--;
			}
			if ((d & Direction.Up) == Direction.Up)
			{
				y--;
			}
			if ((d & Direction.Down) == Direction.Down)
			{
				y++;
			}

			return (x, y);
		}
	}
}
