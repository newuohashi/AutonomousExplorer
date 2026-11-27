using System;
using System.Collections.Generic;
using System.Linq;
using AutonomousExplorer.Domain.Definitions;

namespace AutonomousExplorer.Infrastructure.Definitions
{
    public sealed class DefinitionAssetMapper
    {
        /// <summary>Copies values without repairs. Validate before exposing this raw snapshot to a session.</summary>
        public GameDefinitionCatalog Map(GameDefinitionAsset asset)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            return new GameDefinitionCatalog(
                Copy(asset.Jobs, x => new JobDefinition(x.Id, Stats(x.Stats))),
                Copy(asset.Enemies, x => new EnemyDefinition(x.Id, Stats(x.Stats))),
                Copy(asset.Items, x => new ItemDefinition(x.Id, x.EffectActionId, x.EffectAmount, x.Consumable,
                    x.Capacity, x.RecoveryValue, x.ToolTags)),
                Copy(asset.Actions, x => new ActionDefinition(x.Id, x.ProviderId, x.ExecutorId, x.RequirementId,
                    Seconds(x.DurationSeconds), Copy(x.Uses, u => new PhaseUseDefinition(u.Phase, Seconds(u.DurationSeconds))))),
                Copy(asset.Requirements, x => new RequirementDefinition(x.Id, x.AnyJobIds, x.AnyToolTags, x.PreconditionIds)),
                Copy(asset.Profiles, x => new ProfileDefinition(x.Id, Copy(x.Actions, a => new ActionUtilityDefinition(
                    a.ActionId, a.Multiplier, Copy(a.Considerations, c => new ConsiderationDefinition(c.MetricId, c.CurveId, c.Weight)))))),
                Copy(asset.Curves, x => new CurveDefinition(x.Id, (x.Points ?? Array.Empty<CurvePointRecord>())
                    .Select(p => p == null ? new CurvePoint(double.NaN, double.NaN) : new CurvePoint(p.X, p.Y)))),
                Copy(asset.Maps, x => new MapDefinition(x.Id, x.Width, x.Height, x.DoorCount, x.ContainerCount,
                    x.EnemyCount, x.MaxAttempts, x.SafeRadius, x.CellWidth)),
                Copy(asset.GenerationRules, x => new GenerationRuleDefinition(x.Id, x.MapId, x.EnemyId,
                    x.InitialCharacterCount, x.InitialSeed, x.InitialBalance, x.PersonalCapacity, x.SharedCapacity,
                    x.ContainerCapacity, x.ReturnContainerCapacity,
                    Copy(x.Jobs, w => new WeightedDefinition(w.DefinitionId, w.Weight)),
                    Copy(x.Profiles, w => new WeightedDefinition(w.DefinitionId, w.Weight)),
                    Quantities(x.InitialSupply), Quantities(x.ContainerContents))),
                Copy(asset.ShopEntries, x => new ShopEntryDefinition(x.ItemId, x.Purchasable ? (long?)x.PurchasePrice : null, x.SellPrice)),
                asset.ToolTags);
        }

        /// <summary>Startup gate: never returns an invalid catalog as usable output.</summary>
        public bool TryMap(GameDefinitionAsset asset, DefinitionRegistrations registrations,
            out GameDefinitionCatalog catalog, out IReadOnlyList<DefinitionError> errors)
        {
            catalog = null;
            if (asset == null)
            {
                errors = Array.AsReadOnly(new[] { new DefinitionError("Catalog", "Asset", null, "Definition asset is required.") });
                return false;
            }
            var snapshot = Map(asset);
            errors = new DefinitionValidator().Validate(snapshot, registrations);
            if (errors.Count != 0) return false;
            catalog = snapshot;
            return true;
        }

        private static IEnumerable<TOut> Copy<TIn, TOut>(TIn[] input, Func<TIn, TOut> convert)
            where TIn : class where TOut : class =>
            (input ?? Array.Empty<TIn>()).Select(x => x == null ? null : convert(x));
        private static double? Seconds(ConfiguredSeconds input) => input != null && input.IsConfigured ? input.Value : (double?)null;
        private static CombatStats Stats(StatsRecord input) => input == null ? null :
            new CombatStats(input.MaxHP, input.Attack, input.Defense, input.CombatSpeed, input.MoveSpeed, input.Vision);
        private static IEnumerable<ItemQuantity> Quantities(ItemQuantityRecord[] input) =>
            Copy(input, x => new ItemQuantity(x.ItemId, x.Count));
    }
}
