using System;
using System.Collections.Generic;
using System.Linq;
using AutonomousExplorer.Domain.Definitions;
using AutonomousExplorer.Infrastructure.Definitions;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace AutonomousExplorer.Tests.EditMode.Definitions
{
    [TestFixture, Category("AE.T002")]
    public sealed class T002DefinitionsTests
    {
        private GameDefinitionAsset asset;
        private readonly DefinitionAssetMapper mapper = new DefinitionAssetMapper();

        [SetUp] public void SetUp() { asset = PrototypeDefinitionFixture.Create(); }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(asset); }

        [Test]
        public void AdoptedCatalogSurvivesUnitySerializationAndPassesStartupGate()
        {
            var copy = ScriptableObject.CreateInstance<GameDefinitionAsset>();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(asset), copy);
                Assert.That(mapper.TryMap(copy, PrototypeDefinitionFixture.Registrations(), out var catalog, out var errors),
                    Is.True, string.Join("\n", errors));
                Assert.That(catalog.GetMap("InitialMap").Width, Is.EqualTo(21));
                Assert.That(catalog.GetMap("InitialMap").Height, Is.EqualTo(21));
                Assert.That(catalog.GetJob("Engineer").Stats.MaxHP, Is.EqualTo(100));
                Assert.That(catalog.GetJob("Breacher").Stats.MoveSpeed, Is.EqualTo(2));
                Assert.That(catalog.GetItem("HealingPotion").EffectAmount, Is.EqualTo(30));
                Assert.That(catalog.GetItem("BuffPotion").EffectAmount, Is.EqualTo(4));
                Assert.That(catalog.GetShopEntry("Material").PurchasePrice, Is.Null);
                Assert.That(catalog.GetShopEntry("HealingPotion").PurchasePrice, Is.EqualTo(10));
                Assert.That(catalog.GetGenerationRule("Initial").InitialSupply.Sum(x => x.Count), Is.EqualTo(7));
                Assert.That(catalog.GetGenerationRule("Initial").Jobs.Select(x => x.Weight), Is.EqualTo(new[] { 1d, 1d }));
                Assert.That(catalog.GetGenerationRule("Initial").Profiles.Select(x => x.Weight), Is.EqualTo(new[] { 1d, 1d }));
            }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
        }

        [TestCase("Heal", ItemUsePhase.Preparation, true, 0)]
        [TestCase("Heal", ItemUsePhase.Exploration, true, 1)]
        [TestCase("Heal", ItemUsePhase.Battle, true, 0)]
        [TestCase("Buff", ItemUsePhase.Preparation, false, 0)]
        [TestCase("Buff", ItemUsePhase.Exploration, false, 0)]
        [TestCase("Buff", ItemUsePhase.Battle, true, 0)]
        public void AllAdoptedMedicinePhasesAndTimesMapExactly(string id, ItemUsePhase phase, bool allowed, double duration)
        {
            var action = mapper.Map(asset).GetAction(id);
            Assert.That(action.CanUse(phase), Is.EqualTo(allowed));
            if (allowed) Assert.That(action.GetUse(phase).DurationSeconds, Is.EqualTo(duration));
        }

        [TestCase(true, 20)] [TestCase(false, 20)]
        [TestCase(true, 5)] [TestCase(false, 5)]
        [TestCase(true, 0)] [TestCase(false, 0)]
        public void InvalidSingleMapEdgeIsReportedWithoutRepair(bool width, int value)
        {
            if (width) asset.Maps[0].Width = value; else asset.Maps[0].Height = value;
            var raw = mapper.Map(asset).GetMap("InitialMap");
            Assert.That(width ? raw.Width : raw.Height, Is.EqualTo(value));
            AssertError("InitialMap", width ? "Width" : "Height", value.ToString());
        }

        [Test]
        public void OddRectangularMapIsAcceptedAndNeverCoercedToSquare()
        {
            asset.Maps[0].Height = 23;
            Assert.That(mapper.TryMap(asset, PrototypeDefinitionFixture.Registrations(), out var catalog, out _), Is.True);
            Assert.That(catalog.GetMap("InitialMap").Height, Is.EqualTo(23));
        }

        [TestCase("Heal", 0)] [TestCase("Heal", 1)] [TestCase("Heal", 2)] [TestCase("Buff", 0)]
        public void UnconfiguredZeroIsRejectedForEveryMedicineTime(string id, int index)
        {
            asset.Actions.Single(x => x.Id == id).Uses[index].DurationSeconds.IsConfigured = false;
            AssertError(id, "DurationSeconds", "<unset>");
        }

        [TestCase("Heal", 0, double.NaN)] [TestCase("Heal", 1, double.PositiveInfinity)]
        [TestCase("Heal", 2, -0.1)] [TestCase("Buff", 0, double.NegativeInfinity)]
        [TestCase("Buff", 0, -1)]
        public void InvalidMedicineTimesCannotPassStartup(string id, int index, double value)
        {
            asset.Actions.Single(x => x.Id == id).Uses[index].DurationSeconds.Value = value;
            AssertError(id, "DurationSeconds");
        }

        [TestCase("Heal")] [TestCase("Buff")]
        public void MissingMedicinePhaseEntriesAreRejected(string id)
        {
            asset.Actions.Single(x => x.Id == id).Uses = Array.Empty<PhaseUseRecord>();
            AssertError(id, "Uses");
        }

        [Test]
        public void BuffOutsideBattleAndDuplicatePhasesAreRejected()
        {
            var buff = asset.Actions.Single(x => x.Id == "Buff");
            buff.Uses = new[] { buff.Uses[0], new PhaseUseRecord { Phase = ItemUsePhase.Preparation, DurationSeconds = PrototypeDefinitionFixture.Seconds(0) } };
            AssertError("Buff", "Uses");
            buff.Uses = new[] { buff.Uses[0], buff.Uses[0] };
            AssertError("Buff", "Uses.Phase");
        }

        [TestCase("Job")] [TestCase("Profile")] [TestCase("Map")] [TestCase("Enemy")]
        [TestCase("Item")] [TestCase("Action")] [TestCase("ToolTag")]
        [TestCase("Requirement")] [TestCase("Curve")]
        public void UnknownCatalogReferencesIncludeTheirOwningDefinition(string kind)
        {
            switch (kind)
            {
                case "Job": asset.GenerationRules[0].Jobs[0].DefinitionId = "Missing"; break;
                case "Profile": asset.GenerationRules[0].Profiles[0].DefinitionId = "Missing"; break;
                case "Map": asset.GenerationRules[0].MapId = "Missing"; break;
                case "Enemy": asset.GenerationRules[0].EnemyId = "Missing"; break;
                case "Item": asset.GenerationRules[0].InitialSupply[0].ItemId = "Missing"; break;
                case "Action": asset.Items[0].EffectActionId = "Missing"; break;
                case "ToolTag": asset.Requirements[1].AnyToolTags[0] = "Missing"; break;
                case "Requirement": asset.Actions[0].RequirementId = "Missing"; break;
                case "Curve": asset.Profiles[0].Actions[0].Considerations[0].CurveId = "Missing"; break;
            }
            var errors = Errors();
            Assert.That(errors.Any(x => x.Value == "Missing" && !string.IsNullOrEmpty(x.DefinitionId)), Is.True);
        }

        [TestCase("ProviderId")] [TestCase("ExecutorId")] [TestCase("PreconditionIds")] [TestCase("MetricId")]
        public void RealImplementationRegistrationSetsMustCloseReferences(string field)
        {
            var original = PrototypeDefinitionFixture.Registrations();
            var registrations = new DefinitionRegistrations(
                field == "ProviderId" ? Array.Empty<string>() : original.Providers,
                field == "ExecutorId" ? Array.Empty<string>() : original.Executors,
                field == "PreconditionIds" ? Array.Empty<string>() : original.Preconditions,
                field == "MetricId" ? Array.Empty<string>() : original.Metrics);
            Assert.That(mapper.TryMap(asset, registrations, out var catalog, out var errors), Is.False);
            Assert.That(catalog, Is.Null);
            Assert.That(errors.Any(x => x.Field == field), Is.True);
        }

        [TestCase("Job")] [TestCase("Action")] [TestCase("Profile")] [TestCase("Item")] [TestCase("ToolTag")]
        public void DuplicateDefinitionsAreKeptForValidationInsteadOfOverwriting(string kind)
        {
            switch (kind)
            {
                case "Job": asset.Jobs = new[] { asset.Jobs[0], asset.Jobs[0] }; break;
                case "Action": asset.Actions = asset.Actions.Concat(new[] { asset.Actions[0] }).ToArray(); break;
                case "Profile": asset.Profiles = asset.Profiles.Concat(new[] { asset.Profiles[0] }).ToArray(); break;
                case "Item": asset.Items = asset.Items.Concat(new[] { asset.Items[0] }).ToArray(); break;
                case "ToolTag": asset.ToolTags = new[] { "Lockpick", "Lockpick", "BreachTool" }; break;
            }
            Assert.That(Errors().Any(x => x.Reason.Contains("duplicate")), Is.True);
        }

        [TestCase(0)] [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidLotteryWeightsAreRejected(double weight)
        {
            foreach (var job in asset.GenerationRules[0].Jobs) job.Weight = weight;
            AssertError("Initial", "Jobs.Weight");
        }

        [TestCase(0)] [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidProfileWeightsAreRejected(double weight)
        {
            foreach (var consideration in asset.Profiles[0].Actions[0].Considerations) consideration.Weight = weight;
            AssertError("Cautious", "Weight");
        }

        [TestCase("empty")] [TestCase("duplicateX")] [TestCase("unordered")] [TestCase("endpoint")]
        [TestCase("NaN")] [TestCase("Infinity")] [TestCase("outsideY")] [TestCase("null")]
        public void InvalidCurvesAreRejected(string kind)
        {
            var curve = asset.Curves[0];
            switch (kind)
            {
                case "empty": curve.Points = Array.Empty<CurvePointRecord>(); break;
                case "duplicateX": curve.Points = new[] { curve.Points[0], curve.Points[0], curve.Points[1] }; break;
                case "unordered": curve.Points = new[] { curve.Points[1], curve.Points[0] }; break;
                case "endpoint": curve.Points[0].X = 0.1; break;
                case "NaN": curve.Points[0].Y = double.NaN; break;
                case "Infinity": curve.Points[1].X = double.PositiveInfinity; break;
                case "outsideY": curve.Points[1].Y = 1.1; break;
                case "null": curve.Points[0] = null; break;
            }
            AssertError("Linear", "Points");
        }

        [Test]
        public void SnapshotCannotBeChangedThroughAuthoringArraysOrReturnedCollections()
        {
            var catalog = mapper.Map(asset);
            asset.Jobs[0].Stats.MaxHP = 1;
            asset.Items[2].ToolTags[0] = "Changed";
            asset.Profiles[0].Actions[0].Considerations[0].Weight = 99;
            asset.GenerationRules[0].InitialSupply[0].Count = 99;
            asset.Actions.Single(x => x.Id == "Heal").Uses[0].DurationSeconds.Value = 9;
            asset.Maps[0].Width = 7;
            UnityEngine.Object.DestroyImmediate(asset);
            asset = null;
            Assert.That(catalog.GetJob("Engineer").Stats.MaxHP, Is.EqualTo(100));
            Assert.That(catalog.GetItem("Lockpick").ToolTags[0], Is.EqualTo("Lockpick"));
            Assert.That(catalog.GetProfile("Cautious").Actions[0].Considerations[0].Weight, Is.EqualTo(1));
            Assert.That(catalog.GetGenerationRule("Initial").InitialSupply[0].Count, Is.EqualTo(4));
            Assert.That(catalog.GetAction("Heal").GetUse(ItemUsePhase.Preparation).DurationSeconds, Is.EqualTo(0));
            Assert.That(catalog.GetMap("InitialMap").Width, Is.EqualTo(21));
            Assert.Throws<NotSupportedException>(() => ((IList<JobDefinition>)catalog.Jobs).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<string>)catalog.GetItem("Lockpick").ToolTags)[0] = "Changed");
        }

        [Test]
        public void EmptyAssetAndNullRowsProduceErrorsInsteadOfPrototypeDefaults()
        {
            var empty = ScriptableObject.CreateInstance<GameDefinitionAsset>();
            try
            {
                Assert.That(mapper.TryMap(empty, PrototypeDefinitionFixture.Registrations(), out var catalog, out var errors), Is.False);
                Assert.That(catalog, Is.Null);
                Assert.That(errors.Any(x => x.Field == "Maps"), Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(empty); }
            asset.Items = new ItemRecord[] { null };
            Assert.That(Errors().Any(x => x.Field == "Items"), Is.True);
            Assert.That(mapper.TryMap(null, PrototypeDefinitionFixture.Registrations(), out _, out _), Is.False);
            Assert.That(mapper.TryMap(asset, null, out _, out _), Is.False);
        }

        [Test]
        public void ProfileConfigurationKeepsPersonalitySeparateFromJobPermissions()
        {
            var catalog = mapper.Map(asset);
            var cautious = catalog.GetProfile("Cautious").Actions.Single(x => x.ActionId == "Loot");
            var collector = catalog.GetProfile("Collector").Actions.Single(x => x.ActionId == "Loot");
            Assert.That(cautious.Considerations.Select(x => x.Weight), Is.EqualTo(new[] { 1d, 1d, 3d }));
            Assert.That(collector.Considerations.Select(x => x.Weight), Is.EqualTo(new[] { 3d, 1d, 1d }));
            Assert.That(catalog.Requirements.Single(x => x.Id == "Unlock").AnyJobIds, Is.EqualTo(new[] { "Engineer" }));
            Assert.That(catalog.Requirements.Single(x => x.Id == "Break").AnyToolTags, Is.EqualTo(new[] { "BreachTool" }));
        }

        [Test]
        public void PriceValidationUsesExactLongAndRejectsArbitrage()
        {
            asset.ShopEntries[0].PurchasePrice = long.MaxValue - 1;
            asset.ShopEntries[0].SellPrice = long.MaxValue;
            AssertError("HealingPotion", "PurchasePrice");
            asset.ShopEntries[0].PurchasePrice = long.MaxValue;
            Assert.That(mapper.TryMap(asset, PrototypeDefinitionFixture.Registrations(), out _, out var errors), Is.True);
            Assert.That(errors, Is.Empty);
            Assert.That(mapper.Map(asset).GetShopEntry("HealingPotion").SellPrice, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void NumericAndReferenceErrorsAreAggregatedBeforeStarting()
        {
            asset.Jobs[0].Stats.MaxHP = 0;
            asset.Enemies[0].Stats.MoveSpeed = double.NaN;
            asset.GenerationRules[0].SharedCapacity = -1;
            asset.Maps[0].Width = 20;
            asset.ShopEntries[0].SellPrice = -1;
            var errors = Errors();
            Assert.That(errors.Select(x => x.Field), Does.Contain("MaxHP").And.Contain("MoveSpeed").And.Contain("SharedCapacity").And.Contain("Width").And.Contain("SellPrice"));
        }

        private IReadOnlyList<DefinitionError> Errors()
        {
            Assert.That(mapper.TryMap(asset, PrototypeDefinitionFixture.Registrations(), out var catalog, out var errors), Is.False);
            Assert.That(catalog, Is.Null);
            return errors;
        }

        private void AssertError(string id, string fieldPart, string value = null) =>
            Assert.That(Errors().Any(x => x.DefinitionId == id && x.Field.Contains(fieldPart) && (value == null || x.Value == value)), Is.True,
                $"Expected {id}.{fieldPart}={value}");
    }
}
