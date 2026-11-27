using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AutonomousExplorer.Domain.Definitions
{
    public sealed class DefinitionError
    {
        public DefinitionError(string definitionId, string field, object value, string reason)
        { DefinitionId = definitionId; Field = field; Value = value == null ? "<unset>" : Convert.ToString(value, CultureInfo.InvariantCulture); Reason = reason; }
        public string DefinitionId { get; }
        public string Field { get; }
        public string Value { get; }
        public string Reason { get; }
        public override string ToString() => $"{DefinitionId}.{Field}={Value}: {Reason}";
    }

    /// <summary>IDs of real implementations supplied by the composition root, never fabricated by the catalog.</summary>
    public sealed class DefinitionRegistrations
    {
        public DefinitionRegistrations(IEnumerable<string> providers, IEnumerable<string> executors,
            IEnumerable<string> preconditions, IEnumerable<string> metrics)
        { Providers = DefinitionCopy.List(providers); Executors = DefinitionCopy.List(executors);
          Preconditions = DefinitionCopy.List(preconditions); Metrics = DefinitionCopy.List(metrics); }
        public IReadOnlyList<string> Providers { get; }
        public IReadOnlyList<string> Executors { get; }
        public IReadOnlyList<string> Preconditions { get; }
        public IReadOnlyList<string> Metrics { get; }
    }

    public sealed class DefinitionValidator
    {
        public IReadOnlyList<DefinitionError> Validate(GameDefinitionCatalog catalog, DefinitionRegistrations registrations)
        {
            var audit = new Audit();
            if (catalog == null) { audit.Error("Catalog", "Value", null, "Catalog is required."); return audit.Errors; }
            if (registrations == null) { audit.Error("Registrations", "Value", null, "Implementation registrations are required."); return audit.Errors; }
            var jobs = audit.Ids(catalog.Jobs, "Jobs");
            var enemies = audit.Ids(catalog.Enemies, "Enemies");
            var items = audit.Ids(catalog.Items, "Items");
            var actions = audit.Ids(catalog.Actions, "Actions");
            var requirements = audit.Ids(catalog.Requirements, "Requirements");
            var profiles = audit.Ids(catalog.Profiles, "Profiles");
            var curves = audit.Ids(catalog.Curves, "Curves");
            var maps = audit.Ids(catalog.Maps, "Maps");
            audit.Ids(catalog.GenerationRules, "GenerationRules");
            audit.Ids(catalog.ShopEntries, "ShopEntries");
            var tags = audit.StringIds(catalog.ToolTags, "ToolTags");
            var providers = audit.StringIds(registrations.Providers, "Providers");
            var executors = audit.StringIds(registrations.Executors, "Executors");
            var preconditions = audit.StringIds(registrations.Preconditions, "Preconditions");
            var metrics = audit.StringIds(registrations.Metrics, "Metrics");

            foreach (var job in catalog.Jobs.Where(x => x != null)) audit.Stats(job.Id, job.Stats);
            foreach (var enemy in catalog.Enemies.Where(x => x != null)) audit.Stats(enemy.Id, enemy.Stats);
            foreach (var item in catalog.Items.Where(x => x != null))
            {
                audit.Number(item.Id, "Capacity", item.Capacity, v => v == 1);
                audit.Number(item.Id, "RecoveryValue", item.RecoveryValue, v => v >= 0);
                audit.Number(item.Id, "EffectAmount", item.EffectAmount, v => v >= 0);
                if (!string.IsNullOrEmpty(item.EffectActionId)) audit.Ref(item.Id, "EffectActionId", item.EffectActionId, actions);
                foreach (var tag in item.ToolTags) audit.Ref(item.Id, "ToolTags", tag, tags);
            }
            foreach (var requirement in catalog.Requirements.Where(x => x != null))
            {
                foreach (var job in requirement.AnyJobIds) audit.Ref(requirement.Id, "AnyJobIds", job, jobs);
                foreach (var tag in requirement.AnyToolTags) audit.Ref(requirement.Id, "AnyToolTags", tag, tags);
                foreach (var condition in requirement.PreconditionIds) audit.Ref(requirement.Id, "PreconditionIds", condition, preconditions);
            }
            foreach (var action in catalog.Actions.Where(x => x != null))
            {
                audit.Ref(action.Id, "ProviderId", action.ProviderId, providers);
                audit.Ref(action.Id, "ExecutorId", action.ExecutorId, executors);
                audit.Ref(action.Id, "RequirementId", action.RequirementId, requirements);
                // Only fixed-duration actions require this field; movement/transfer/combat timings belong to their services.
                if (action.DurationSeconds.HasValue || action.Id == "Unlock" || action.Id == "Break")
                    audit.Duration(action.Id, "DurationSeconds", action.DurationSeconds);
                var seen = new HashSet<ItemUsePhase>();
                foreach (var use in action.Uses)
                {
                    if (use == null) { audit.Error(action.Id, "Uses", null, "Null phase entry."); continue; }
                    if (!Enum.IsDefined(typeof(ItemUsePhase), use.Phase) || !seen.Add(use.Phase))
                        audit.Error(action.Id, "Uses.Phase", use.Phase, "Unknown or duplicate phase.");
                    audit.Duration(action.Id, $"Uses.{use.Phase}.DurationSeconds", use.DurationSeconds);
                }
                // G3-149: required phase entries distinguish missing configuration from an immediate effect.
                if (action.Id == "Heal")
                    foreach (ItemUsePhase phase in Enum.GetValues(typeof(ItemUsePhase)))
                        if (!seen.Contains(phase)) audit.Error(action.Id, $"Uses.{phase}", null, "Required phase is missing.");
                if (action.Id == "Buff" && (!seen.SetEquals(new[] { ItemUsePhase.Battle })))
                    audit.Error(action.Id, "Uses", string.Join(",", seen), "Buff is available only in Battle.");
            }
            foreach (var curve in catalog.Curves.Where(x => x != null))
            {
                if (curve.Points.Count < 2 || curve.Points[0].X != 0 || curve.Points[curve.Points.Count - 1].X != 1)
                    audit.Error(curve.Id, "Points", curve.Points.Count, "Curve must cover [0,1] with at least two points.");
                double previous = -1;
                foreach (var point in curve.Points)
                {
                    audit.Number(curve.Id, "Points.X", point.X, x => x >= 0 && x <= 1 && x > previous);
                    audit.Number(curve.Id, "Points.Y", point.Y, y => y >= 0 && y <= 1);
                    previous = point.X;
                }
            }
            foreach (var profile in catalog.Profiles.Where(x => x != null))
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var rule in profile.Actions)
                {
                    if (rule == null) { audit.Error(profile.Id, "Actions", null, "Null action rule."); continue; }
                    audit.Ref(profile.Id, "ActionId", rule.ActionId, actions);
                    if (!seen.Add(rule.ActionId ?? "")) audit.Error(profile.Id, "ActionId", rule.ActionId, "Duplicate action rule.");
                    audit.Number(profile.Id, $"{rule.ActionId}.Multiplier", rule.Multiplier, v => v >= 0 && v <= 2);
                    double sum = 0;
                    foreach (var consideration in rule.Considerations)
                    {
                        if (consideration == null) { audit.Error(profile.Id, "Considerations", null, "Null consideration."); continue; }
                        audit.Ref(profile.Id, "MetricId", consideration.MetricId, metrics);
                        audit.Ref(profile.Id, "CurveId", consideration.CurveId, curves);
                        audit.Number(profile.Id, $"{rule.ActionId}.Weight", consideration.Weight, v => v >= 0);
                        sum += consideration.Weight;
                    }
                    audit.Number(profile.Id, $"{rule.ActionId}.WeightSum", sum, v => v > 0);
                }
                foreach (var actionId in actions)
                    if (!seen.Contains(actionId)) audit.Error(profile.Id, "Actions", actionId, "Action evaluation rule is missing.");
            }
            foreach (var map in catalog.Maps.Where(x => x != null))
            {
                audit.Number(map.Id, "Width", map.Width, v => v >= 7 && v % 2 == 1);
                audit.Number(map.Id, "Height", map.Height, v => v >= 7 && v % 2 == 1);
                audit.Number(map.Id, "DoorCount", map.DoorCount, v => v >= 0);
                audit.Number(map.Id, "ContainerCount", map.ContainerCount, v => v >= 0);
                audit.Number(map.Id, "EnemyCount", map.EnemyCount, v => v >= 0);
                audit.Number(map.Id, "MaxAttempts", map.MaxAttempts, v => v > 0);
                audit.Number(map.Id, "SafeRadius", map.SafeRadius, v => v >= 0);
                audit.Number(map.Id, "CellWidth", map.CellWidth, v => v > 0);
            }
            foreach (var rule in catalog.GenerationRules.Where(x => x != null))
            {
                audit.Ref(rule.Id, "MapId", rule.MapId, maps);
                audit.Ref(rule.Id, "EnemyId", rule.EnemyId, enemies);
                audit.Number(rule.Id, "InitialCharacterCount", rule.InitialCharacterCount, v => v > 0);
                audit.Number(rule.Id, "InitialBalance", rule.InitialBalance, v => v >= 0);
                audit.Number(rule.Id, "PersonalCapacity", rule.PersonalCapacity, v => v >= 0);
                audit.Number(rule.Id, "SharedCapacity", rule.SharedCapacity, v => v >= 0);
                audit.Number(rule.Id, "ContainerCapacity", rule.ContainerCapacity, v => v >= 0);
                audit.Number(rule.Id, "ReturnContainerCapacity", rule.ReturnContainerCapacity, v => v >= 0);
                audit.Weights(rule.Id, "Jobs", rule.Jobs, jobs);
                audit.Weights(rule.Id, "Profiles", rule.Profiles, profiles);
                audit.Quantities(rule.Id, "InitialSupply", rule.InitialSupply, items);
                audit.Quantities(rule.Id, "ContainerContents", rule.ContainerContents, items);
            }
            foreach (var entry in catalog.ShopEntries.Where(x => x != null))
            {
                audit.Ref(entry.Id, "ItemId", entry.Id, items);
                if (entry.SellPrice < 0) audit.Error(entry.Id, "SellPrice", entry.SellPrice, "Price must be nonnegative.");
                if (entry.PurchasePrice.HasValue && (entry.PurchasePrice.Value < 0 || entry.PurchasePrice.Value < entry.SellPrice))
                    audit.Error(entry.Id, "PurchasePrice", entry.PurchasePrice, "Purchase price must be nonnegative and at least sell price.");
            }
            return audit.Errors.AsReadOnly();
        }

        private sealed class Audit
        {
            public List<DefinitionError> Errors { get; } = new List<DefinitionError>();
            public void Error(string id, string field, object value, string reason) => Errors.Add(new DefinitionError(id, field, value, reason));
            public void Number(string id, string field, double value, Func<double, bool> valid)
            { if (double.IsNaN(value) || double.IsInfinity(value) || !valid(value)) Error(id, field, value, "Invalid numeric value."); }
            public void Duration(string id, string field, double? value)
            { if (!value.HasValue) Error(id, field, null, "Duration is unconfigured."); else Number(id, field, value.Value, v => v >= 0); }
            public void Ref(string id, string field, string value, HashSet<string> ids)
            { if (string.IsNullOrWhiteSpace(value) || !ids.Contains(value)) Error(id, field, value, "Unresolved ID."); }
            public HashSet<string> StringIds(IEnumerable<string> values, string field)
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var value in values)
                    if (string.IsNullOrWhiteSpace(value) || !ids.Add(value)) Error(value ?? field, field, value, "Missing or duplicate ID.");
                return ids;
            }
            public HashSet<string> Ids<T>(IEnumerable<T> values, string field) where T : Definition
            {
                var rows = values.ToArray();
                if (rows.Length == 0) Error("Catalog", field, 0, "Required definition table is empty.");
                return StringIds(rows.Select(x => x?.Id), field);
            }
            public void Stats(string id, CombatStats stats)
            {
                if (stats == null) { Error(id, "Stats", null, "Stats are required."); return; }
                Number(id, "MaxHP", stats.MaxHP, v => v > 0); Number(id, "Attack", stats.Attack, v => v >= 0);
                Number(id, "Defense", stats.Defense, v => v >= 0); Number(id, "CombatSpeed", stats.CombatSpeed, v => v > 0);
                Number(id, "MoveSpeed", stats.MoveSpeed, v => v > 0); Number(id, "Vision", stats.Vision, v => v >= 0);
            }
            public void Weights(string id, string field, IEnumerable<WeightedDefinition> rows, HashSet<string> ids)
            {
                double sum = 0;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var row in rows)
                {
                    if (row == null) { Error(id, field, null, "Null weighted entry."); continue; }
                    Ref(id, field, row.DefinitionId, ids);
                    if (!seen.Add(row.DefinitionId ?? "")) Error(id, field, row.DefinitionId, "Duplicate weighted entry.");
                    Number(id, field + ".Weight", row.Weight, v => v >= 0); sum += row.Weight;
                }
                Number(id, field + ".WeightSum", sum, v => v > 0);
            }
            public void Quantities(string id, string field, IEnumerable<ItemQuantity> rows, HashSet<string> ids)
            {
                foreach (var row in rows)
                {
                    if (row == null) { Error(id, field, null, "Null item quantity."); continue; }
                    Ref(id, field, row.ItemId, ids); Number(id, field + ".Count", row.Count, v => v >= 0);
                }
            }
        }
    }
}
