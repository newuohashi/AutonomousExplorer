using System;
using System.Collections.Generic;
using System.Linq;
using AutonomousExplorer.Domain.Common;
using NUnit.Framework;

namespace AutonomousExplorer.Tests.EditMode.Tasks
{
    [TestFixture, Category("AE.T001")]
    public sealed class T001BaseTests
    {
        [Test]
        public void CharacterGuidNormalizationPreservesDictionaryIdentity()
        {
            var original = EntityRef.Character("E84410C2-8B16-4B4F-AB9E-95708237AD01");
            var restored = EntityRef.Character("e84410c28b164b4fab9e95708237ad01");
            var lookup = new Dictionary<EntityRef, string> { [original] = "saved character" };

            Assert.That(restored.Id, Is.EqualTo("e84410c28b164b4fab9e95708237ad01"));
            Assert.That(lookup[restored], Is.EqualTo("saved character"));
            Assert.That(original.CompareTo(restored), Is.Zero);
            Assert.That(EntityRef.Character("e84410c28b164b4fab9e95708237ad02"), Is.Not.EqualTo(original));
        }

        [Test]
        public void EntityKindsAndNumericOrderingRemainDistinct()
        {
            var enemy = EntityRef.Enemy(2);
            var item = EntityRef.Item(2);
            var environment = EntityRef.Environment(2);
            var lookup = new HashSet<EntityRef> { enemy, item, environment };

            Assert.That(lookup.Count, Is.EqualTo(3));
            Assert.That(enemy.Id, Is.EqualTo("00000000000000000002"));
            Assert.That(EntityRef.Enemy(10).CompareTo(enemy), Is.Positive);
            Assert.That(enemy.CompareTo(item), Is.Negative);
            Assert.That(item.CompareTo(environment), Is.Negative);
            Assert.That(EntityRef.Enemy(long.MaxValue).Id, Is.EqualTo("09223372036854775807"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not-a-guid")]
        [TestCase("00000000000000000000000000000000")]
        public void InvalidCharacterIdsAreRejected(string id)
        {
            Assert.Throws<ArgumentException>(() => EntityRef.Character(id));
        }

        [Test]
        public void NegativeSequenceIdsAndDefaultReferencesCannotBecomeValidEntities()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EntityRef.Enemy(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => EntityRef.Item(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => EntityRef.Environment(-1));
            Assert.That(default(EntityRef).IsValid, Is.False);
            Assert.That(new HashSet<EntityRef> { default, default }.Count, Is.EqualTo(1));
        }

        [TestCase(0.99, 1.01, 1, 0, 1)]
        [TestCase(1, 0, 1, 1, 0)]
        [TestCase(-0.01, -1, 1, -1, -1)]
        [TestCase(-1.01, -0.99, 1, -2, -1)]
        [TestCase(1.99, -0.01, 2, 0, -1)]
        [TestCase(-2, 2, 2, -1, 1)]
        public void CellMembershipUsesFloorAndHalfOpenBoundaries(double x, double z, double size, int expectedX, int expectedZ)
        {
            Assert.That(new WorldPosition(x, z).ToCell(size), Is.EqualTo(new CellCoord(expectedX, expectedZ)));
        }

        [Test]
        public void MovementInterpolationCrossesTheBoundaryBeforeArrival()
        {
            var from = new WorldPosition(0.5, -0.5);
            var to = new WorldPosition(1.5, -0.5);
            Assert.That(WorldPosition.Lerp(from, to, 0.49).ToCell(), Is.EqualTo(new CellCoord(0, -1)));
            Assert.That(WorldPosition.Lerp(from, to, 0.5).ToCell(), Is.EqualTo(new CellCoord(1, -1)));
            Assert.That(WorldPosition.Lerp(from, to, 0), Is.EqualTo(from));
            Assert.That(WorldPosition.Lerp(from, to, 1), Is.EqualTo(to));
            Assert.That(from.ToCell(), Is.EqualTo(new CellCoord(0, -1)));
        }

        [Test]
        public void DistanceIsSymmetricAndAvoidsSquaringOverflow()
        {
            var from = new WorldPosition(-1, -2);
            var to = new WorldPosition(2, 2);
            Assert.That(WorldPosition.Distance(from, to), Is.EqualTo(5));
            Assert.That(WorldPosition.Distance(to, from), Is.EqualTo(5));
            Assert.That(WorldPosition.Distance(from, from), Is.Zero);
            Assert.That(WorldPosition.Distance(default, new WorldPosition(3e150, 4e150)) / 1e150, Is.EqualTo(5).Within(1e-12));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void NonFiniteCoordinatesAndInterpolationAreRejected(double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorldPosition(value, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorldPosition(0, value));
            Assert.Throws<ArgumentOutOfRangeException>(() => WorldPosition.Lerp(default, default, value));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidCellSizeCannotProduceACell(double size)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => default(WorldPosition).ToCell(size));
        }

        [Test]
        public void OutOfRangePositionsDoNotWrapToAnotherCell()
        {
            Assert.Throws<OverflowException>(() => new WorldPosition((double)int.MaxValue + 1, 0).ToCell());
            Assert.Throws<OverflowException>(() => new WorldPosition(0, (double)int.MinValue - 1).ToCell());
            Assert.That(new WorldPosition(int.MinValue, int.MaxValue).ToCell(), Is.EqualTo(new CellCoord(int.MinValue, int.MaxValue)));
        }

        [Test]
        public void NeighborsUseNorthEastSouthWestAndCoordinatesRemainHashable()
        {
            var cell = new CellCoord(-2, 3);
            Assert.That(cell.Neighbors4(), Is.EqualTo(new[]
            {
                new CellCoord(-2, 4), new CellCoord(-1, 3),
                new CellCoord(-2, 2), new CellCoord(-3, 3)
            }));
            Assert.That(new HashSet<CellCoord>(cell.Neighbors4()).Count, Is.EqualTo(4));
            Assert.That(new Dictionary<CellCoord, int> { [cell] = 7 }[new CellCoord(-2, 3)], Is.EqualTo(7));
            Assert.Throws<OverflowException>(() => new CellCoord(0, int.MaxValue).Neighbors4().ToArray());
        }

        [Test]
        public void CellOrderIsXThenZWithoutSubtractionOverflow()
        {
            var cells = new[] { new CellCoord(1, -100), new CellCoord(0, 2), new CellCoord(0, -1) };
            Array.Sort(cells);
            Assert.That(cells, Is.EqualTo(new[] { new CellCoord(0, -1), new CellCoord(0, 2), new CellCoord(1, -100) }));
            Assert.That(new CellCoord(int.MinValue, 0).CompareTo(new CellCoord(int.MaxValue, 0)), Is.Negative);
        }

        [TestCase(OperationErrorCode.NoSpace)]
        [TestCase(OperationErrorCode.InvalidTarget)]
        [TestCase(OperationErrorCode.AlreadyHandled)]
        [TestCase(OperationErrorCode.NotFound)]
        [TestCase(OperationErrorCode.InvalidOrigin)]
        [TestCase(OperationErrorCode.Reserved)]
        [TestCase(OperationErrorCode.InvalidPhase)]
        [TestCase(OperationErrorCode.Unavailable)]
        [TestCase(OperationErrorCode.FullHP)]
        public void OrdinaryFailuresPreserveTheirReasonWithoutChangingCallerState(OperationErrorCode code)
        {
            // These are caller-owned values, not substitute implementations of later services.
            var item = EntityRef.Item(7);
            var source = new List<EntityRef> { item };
            var destination = new List<EntityRef>();
            var result = OperationResult.Failed(code, "Operation rejected", item);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(code));
            Assert.That(result.RelatedEntity, Is.EqualTo(item));
            Assert.That(source, Is.EqualTo(new[] { item }));
            Assert.That(destination, Is.Empty);
        }

        [Test]
        public void ResultDefaultsAndFactoriesCannotConfuseSuccessWithFailure()
        {
            Assert.That(default(OperationResult).Success, Is.False);
            Assert.That(default(OperationResult).ErrorCode, Is.EqualTo(OperationErrorCode.Unspecified));
            Assert.That(OperationResult.Succeeded().Success, Is.True);
            Assert.That(OperationResult.Succeeded().ErrorCode, Is.EqualTo(OperationErrorCode.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => OperationResult.Failed(OperationErrorCode.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => OperationResult.Failed((OperationErrorCode)999));
            Assert.Throws<ArgumentException>(() => OperationResult.Failed(OperationErrorCode.InvalidTarget, relatedEntity: default(EntityRef)));
        }

        [Test]
        public void DomainAssemblyHasNoUnityOrOuterLayerReferences()
        {
            var references = typeof(EntityRef).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
            Assert.That(references.Any(name => name.StartsWith("Unity", StringComparison.Ordinal)), Is.False);
            Assert.That(references.Any(name => name.StartsWith("AutonomousExplorer.", StringComparison.Ordinal)), Is.False);
        }
    }
}
