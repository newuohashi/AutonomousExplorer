using System;
using System.Globalization;

namespace AutonomousExplorer.Domain.Common
{
    public enum EntityKind
    {
        None = 0,
        Character = 1,
        Enemy = 2,
        Item = 3,
        Environment = 4
    }

    /// <summary>A persistent, kind-qualified identity, independent of Unity objects.</summary>
    public readonly struct EntityRef : IEquatable<EntityRef>, IComparable<EntityRef>
    {
        private readonly string normalizedId;

        private EntityRef(EntityKind kind, string normalizedId)
        {
            Kind = kind;
            this.normalizedId = normalizedId;
        }

        public EntityKind Kind { get; }
        public string Id => normalizedId ?? string.Empty;
        public bool IsValid => Kind != EntityKind.None;

        public static EntityRef Character(string instanceId)
        {
            if (!Guid.TryParse(instanceId, out var id) || id == Guid.Empty)
                throw new ArgumentException("A non-empty character GUID is required.", nameof(instanceId));

            return new EntityRef(EntityKind.Character, id.ToString("N"));
        }

        public static EntityRef Enemy(long id) => FromSequence(EntityKind.Enemy, id);
        public static EntityRef Item(long id) => FromSequence(EntityKind.Item, id);
        public static EntityRef Environment(long id) => FromSequence(EntityKind.Environment, id);

        private static EntityRef FromSequence(EntityKind kind, long id)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id), "Sequence IDs must be non-negative.");

            return new EntityRef(kind, id.ToString("D20", CultureInfo.InvariantCulture));
        }

        public int CompareTo(EntityRef other)
        {
            var kindOrder = Kind.CompareTo(other.Kind);
            return kindOrder != 0 ? kindOrder : string.CompareOrdinal(Id, other.Id);
        }

        public bool Equals(EntityRef other) => Kind == other.Kind && string.Equals(Id, other.Id, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is EntityRef other && Equals(other);
        public override int GetHashCode() => unchecked(((int)Kind * 397) ^ StringComparer.Ordinal.GetHashCode(Id));
        public override string ToString() => $"{Kind}:{Id}";
        public static bool operator ==(EntityRef left, EntityRef right) => left.Equals(right);
        public static bool operator !=(EntityRef left, EntityRef right) => !left.Equals(right);
    }
}
