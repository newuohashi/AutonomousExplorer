using System;
using System.Collections.Generic;

namespace AutonomousExplorer.Domain.Common
{
    /// <summary>A grid address; contains no cell or actor references.</summary>
    public readonly struct CellCoord : IEquatable<CellCoord>, IComparable<CellCoord>
    {
        public CellCoord(int x, int z)
        {
            X = x;
            Z = z;
        }

        public int X { get; }
        public int Z { get; }

        /// <summary>North (+Z), east (+X), south (-Z), west (-X), in that order.</summary>
        public IEnumerable<CellCoord> Neighbors4()
        {
            yield return new CellCoord(X, checked(Z + 1));
            yield return new CellCoord(checked(X + 1), Z);
            yield return new CellCoord(X, checked(Z - 1));
            yield return new CellCoord(checked(X - 1), Z);
        }

        public int CompareTo(CellCoord other)
        {
            var xOrder = X.CompareTo(other.X);
            return xOrder != 0 ? xOrder : Z.CompareTo(other.Z);
        }

        public bool Equals(CellCoord other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is CellCoord other && Equals(other);
        public override int GetHashCode() => unchecked((X * 397) ^ Z);
        public override string ToString() => $"({X}, {Z})";
        public static bool operator ==(CellCoord left, CellCoord right) => left.Equals(right);
        public static bool operator !=(CellCoord left, CellCoord right) => !left.Equals(right);
    }
}
