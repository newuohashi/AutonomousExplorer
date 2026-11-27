using System;

namespace AutonomousExplorer.Domain.Common
{
    /// <summary>A finite logical XZ position or horizontal vector.</summary>
    public readonly struct WorldPosition : IEquatable<WorldPosition>
    {
        public WorldPosition(double x, double z)
        {
            RequireFinite(x, nameof(x));
            RequireFinite(z, nameof(z));
            X = x;
            Z = z;
        }

        public double X { get; }
        public double Z { get; }

        public CellCoord ToCell(double cellSize = 1d)
        {
            RequireFinite(cellSize, nameof(cellSize));
            if (cellSize <= 0d)
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");

            return new CellCoord(ToCellIndex(X / cellSize), ToCellIndex(Z / cellSize));
        }

        private static int ToCellIndex(double coordinate)
        {
            var index = Math.Floor(coordinate);
            if (index < int.MinValue || index > int.MaxValue)
                throw new OverflowException("The position is outside the integer cell coordinate range.");
            return checked((int)index);
        }

        public static double Distance(WorldPosition from, WorldPosition to)
        {
            var dx = Math.Abs(to.X - from.X);
            var dz = Math.Abs(to.Z - from.Z);
            var largest = Math.Max(dx, dz);
            if (largest == 0d || double.IsInfinity(largest))
                return largest;

            var ratio = Math.Min(dx, dz) / largest;
            return largest * Math.Sqrt(1d + ratio * ratio);
        }

        /// <summary>Linear interpolation, with extrapolation when t is outside [0, 1].</summary>
        public static WorldPosition Lerp(WorldPosition from, WorldPosition to, double t)
        {
            RequireFinite(t, nameof(t));
            if (t == 0d) return from;
            if (t == 1d) return to;
            return new WorldPosition((1d - t) * from.X + t * to.X, (1d - t) * from.Z + t * to.Z);
        }

        private static void RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, "A finite value is required.");
        }

        public bool Equals(WorldPosition other) => X.Equals(other.X) && Z.Equals(other.Z);
        public override bool Equals(object obj) => obj is WorldPosition other && Equals(other);
        public override int GetHashCode() => unchecked((X.GetHashCode() * 397) ^ Z.GetHashCode());
        public static bool operator ==(WorldPosition left, WorldPosition right) => left.Equals(right);
        public static bool operator !=(WorldPosition left, WorldPosition right) => !left.Equals(right);
    }
}
