// using System.Runtime.InteropServices;
//
// TODO implement this
//
// namespace USharpLibs.Engine2.Math {
// 	[PublicAPI]
// 	[StructLayout(LayoutKind.Sequential, Pack = 0)]
// 	public readonly record struct TriPos {
// 		public int A { get; }
// 		public int B { get; }
// 		public int C { get; } // this can *technically* be auto calculated if we know IsUp. (1 - q - r) for down, (2 - q - r) for up
//
// 		public bool IsUp =>
// 				(A + B + C) switch {
// 						1 => false,
// 						2 => true,
// 						_ => throw new Exception(),
// 				};
//
// 		public TriPos(int a, int b, int c) {
// 			if (a + b + c is not 1 and 2) { }
//
// 			A = a;
// 			B = b;
// 			C = c;
// 		}
//
// 		public void Deconstruct(out int a, out int b, out int c) {
// 			a = A;
// 			b = B;
// 			c = C;
// 		}
//
// 		public TriPos Add(Direction direction, int amount) {
// 			// bool inc = (byte)direction % 2 == 0;
// 			//int h = (inc && IsUp) || (!IsUp))) || (!inc && !IsUp) ? (amount + 1) / 2 : 0;
//
// 			// TODO check if the amount is 1 and it's a neighbor. if so, don't do this math.
//
// 			int h = (amount + (amount + (1 - (A + B + C - 1) - (byte)direction % 2)) % 2) / 2;
// 			// ^^^ UNTESTED. dunno if this'll work. gotta run the math
//
// 			int h = ((-A - B - C - (byte)direction + amount) % 2 + amount) / 2;
// 			// this may also work?
//
// 			// if (inc) {
// 			// 	// if (IsUp) {
// 			// 	// 	// h = amount % 2 == 0 ? amount / 2 : (amount + 1) / 2;
// 			// 	// 	h = (amount + amount % 2) / 2;
// 			// 	// } else {
// 			// 	// 	// h = amount % 2 == 0 ? (amount + 1) / 2 : amount / 2;
// 			// 	// 	h = (amount + (amount + 1) % 2) / 2;
// 			// 	// }
// 			//
// 			// 	h = (amount + (IsUp ? amount : amount + 1) % 2) / 2;
// 			// } else {
// 			// 	// if (IsUp) {
// 			// 	// 	// h = amount % 2 == 0 ? (amount + 1) / 2 : amount / 2;
// 			// 	// 	h = (amount + (amount + 1) % 2) / 2;
// 			// 	// } else {
// 			// 	// 	// h = amount % 2 == 0 ? amount / 2 : (amount + 1) / 2;
// 			// 	// 	h = (amount + (amount % 2)) / 2;
// 			// 	// }
// 			//
// 			// 	h = (amount + (IsUp ? amount + 1 : amount) % 2) / 2;
// 			// }
//
// 			// return direction switch {
// 			// 		Direction.BIncrement => IsUp ? new(A - h, B + amount, C - h) : new(A, B + amount, C),
// 			// 		Direction.CDecrement => IsUp ? new(A, B, C - amount) : new(A + h, B + h, C - amount),
// 			// 		Direction.AIncrement => IsUp ? new(A + amount, B - h, C - h) : new(A + amount, B, C),
// 			// 		Direction.BDecrement => IsUp ? new(A, B - amount, C) : new(A + h, B - amount, C + h),
// 			// 		Direction.CIncrement => IsUp ? new(A - h, B - h, C + amount) : new(A, B, C + amount),
// 			// 		Direction.ADecrement => IsUp ? new(A - amount, B, C) : new(A - amount, B + h, C + h),
// 			// 		_ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
// 			// };
//
// 			return direction switch {
// 					Direction.BIncrement => new(A - h, B + amount, C - h),
// 					Direction.CDecrement => new(A + h, B + h, C - amount),
// 					Direction.AIncrement => new(A + amount, B - h, C - h),
// 					Direction.BDecrement => new(A + h, B - amount, C + h),
// 					Direction.CIncrement => new(A - h, B - h, C + amount),
// 					Direction.ADecrement => new(A - amount, B + h, C + h),
// 					_ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
// 			}; // ^^^ also untested but should also work but better? nvm. no it won't
// 		}
//
// 		public static TriPos operator +(TriPos pos, Direction direction) => pos.Add(direction, 1);
//
// 		public override int GetHashCode() => HashCode.Combine(A, B, C);
// 		public bool Equals(TriPos other) => A == other.A && B == other.B && C == other.C;
// 		public override string ToString() => $"A: {A}, B: {B}, C: {C}";
//
// 		public enum Direction : byte {
// 			BIncrement = 0,
// 			CDecrement = 1,
// 			AIncrement = 2,
// 			BDecrement = 3,
// 			CIncrement = 4,
// 			ADecrement = 5,
// 		}
// 	}
// }