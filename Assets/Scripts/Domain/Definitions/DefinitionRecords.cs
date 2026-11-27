using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutonomousExplorer.Domain.Definitions
{
    internal static class DefinitionCopy
    {
        public static IReadOnlyList<T> List<T>(IEnumerable<T> values) =>
            new ReadOnlyCollection<T>((values ?? Enumerable.Empty<T>()).ToArray());
    }

    public abstract class Definition
    {
        protected Definition(string id) { Id = id; }
        public string Id { get; }
    }

    public sealed class CombatStats
    {
        public CombatStats(int maxHP, int attack, int defense, double combatSpeed, double moveSpeed, int vision)
        { MaxHP = maxHP; Attack = attack; Defense = defense; CombatSpeed = combatSpeed; MoveSpeed = moveSpeed; Vision = vision; }
        public int MaxHP { get; }
        public int Attack { get; }
        public int Defense { get; }
        public double CombatSpeed { get; }
        public double MoveSpeed { get; }
        public int Vision { get; }
    }

    public sealed class JobDefinition : Definition
    {
        public JobDefinition(string id, CombatStats stats) : base(id) { Stats = stats; }
        public CombatStats Stats { get; }
    }

    public sealed class EnemyDefinition : Definition
    {
        public EnemyDefinition(string id, CombatStats stats) : base(id) { Stats = stats; }
        public CombatStats Stats { get; }
    }

    public sealed class ItemDefinition : Definition
    {
        public ItemDefinition(string id, string effectActionId, int effectAmount, bool consumable, int capacity,
            long recoveryValue, IEnumerable<string> toolTags) : base(id)
        { EffectActionId = effectActionId; EffectAmount = effectAmount; Consumable = consumable;
          Capacity = capacity; RecoveryValue = recoveryValue; ToolTags = DefinitionCopy.List(toolTags); }
        public string EffectActionId { get; }
        public int EffectAmount { get; }
        public bool Consumable { get; }
        public int Capacity { get; }
        public long RecoveryValue { get; }
        public IReadOnlyList<string> ToolTags { get; }
    }

    public enum ItemUsePhase { Preparation, Exploration, Battle }

    /// <summary>Null duration means unconfigured; zero is a configured immediate effect.</summary>
    public sealed class PhaseUseDefinition
    {
        public PhaseUseDefinition(ItemUsePhase phase, double? durationSeconds)
        { Phase = phase; DurationSeconds = durationSeconds; }
        public ItemUsePhase Phase { get; }
        public double? DurationSeconds { get; }
    }

    public sealed class ActionDefinition : Definition
    {
        public ActionDefinition(string id, string providerId, string executorId, string requirementId,
            double? durationSeconds, IEnumerable<PhaseUseDefinition> uses) : base(id)
        { ProviderId = providerId; ExecutorId = executorId; RequirementId = requirementId;
          DurationSeconds = durationSeconds; Uses = DefinitionCopy.List(uses); }
        public string ProviderId { get; }
        public string ExecutorId { get; }
        public string RequirementId { get; }
        public double? DurationSeconds { get; }
        public IReadOnlyList<PhaseUseDefinition> Uses { get; }
        public bool CanUse(ItemUsePhase phase) => Uses.Any(x => x != null && x.Phase == phase);
        public PhaseUseDefinition GetUse(ItemUsePhase phase) => Uses.Single(x => x.Phase == phase);
    }

    public sealed class RequirementDefinition : Definition
    {
        public RequirementDefinition(string id, IEnumerable<string> anyJobIds, IEnumerable<string> anyToolTags,
            IEnumerable<string> preconditionIds) : base(id)
        { AnyJobIds = DefinitionCopy.List(anyJobIds); AnyToolTags = DefinitionCopy.List(anyToolTags);
          PreconditionIds = DefinitionCopy.List(preconditionIds); }
        public IReadOnlyList<string> AnyJobIds { get; }
        public IReadOnlyList<string> AnyToolTags { get; }
        public IReadOnlyList<string> PreconditionIds { get; }
    }

    public readonly struct CurvePoint
    {
        public CurvePoint(double x, double y) { X = x; Y = y; }
        public double X { get; }
        public double Y { get; }
    }

    public sealed class CurveDefinition : Definition
    {
        public CurveDefinition(string id, IEnumerable<CurvePoint> points) : base(id) { Points = DefinitionCopy.List(points); }
        public IReadOnlyList<CurvePoint> Points { get; }
    }

    public sealed class ConsiderationDefinition
    {
        public ConsiderationDefinition(string metricId, string curveId, double weight)
        { MetricId = metricId; CurveId = curveId; Weight = weight; }
        public string MetricId { get; }
        public string CurveId { get; }
        public double Weight { get; }
    }

    public sealed class ActionUtilityDefinition
    {
        public ActionUtilityDefinition(string actionId, double multiplier, IEnumerable<ConsiderationDefinition> considerations)
        { ActionId = actionId; Multiplier = multiplier; Considerations = DefinitionCopy.List(considerations); }
        public string ActionId { get; }
        public double Multiplier { get; }
        public IReadOnlyList<ConsiderationDefinition> Considerations { get; }
    }

    public sealed class ProfileDefinition : Definition
    {
        public ProfileDefinition(string id, IEnumerable<ActionUtilityDefinition> actions) : base(id) { Actions = DefinitionCopy.List(actions); }
        public IReadOnlyList<ActionUtilityDefinition> Actions { get; }
    }

    public sealed class WeightedDefinition
    {
        public WeightedDefinition(string definitionId, double weight) { DefinitionId = definitionId; Weight = weight; }
        public string DefinitionId { get; }
        public double Weight { get; }
    }

    public sealed class ItemQuantity
    {
        public ItemQuantity(string itemId, int count) { ItemId = itemId; Count = count; }
        public string ItemId { get; }
        public int Count { get; }
    }

    public sealed class MapDefinition : Definition
    {
        public MapDefinition(string id, int width, int height, int doorCount, int containerCount, int enemyCount,
            int maxAttempts, int safeRadius, double cellWidth) : base(id)
        { Width = width; Height = height; DoorCount = doorCount; ContainerCount = containerCount;
          EnemyCount = enemyCount; MaxAttempts = maxAttempts; SafeRadius = safeRadius; CellWidth = cellWidth; }
        public int Width { get; }
        public int Height { get; }
        public int DoorCount { get; }
        public int ContainerCount { get; }
        public int EnemyCount { get; }
        public int MaxAttempts { get; }
        public int SafeRadius { get; }
        public double CellWidth { get; }
    }

    public sealed class GenerationRuleDefinition : Definition
    {
        public GenerationRuleDefinition(string id, string mapId, string enemyId, int initialCharacterCount,
            int initialSeed, long initialBalance, int personalCapacity, int sharedCapacity, int containerCapacity,
            int returnContainerCapacity, IEnumerable<WeightedDefinition> jobs, IEnumerable<WeightedDefinition> profiles,
            IEnumerable<ItemQuantity> initialSupply, IEnumerable<ItemQuantity> containerContents) : base(id)
        { MapId = mapId; EnemyId = enemyId; InitialCharacterCount = initialCharacterCount; InitialSeed = initialSeed;
          InitialBalance = initialBalance; PersonalCapacity = personalCapacity; SharedCapacity = sharedCapacity;
          ContainerCapacity = containerCapacity; ReturnContainerCapacity = returnContainerCapacity;
          Jobs = DefinitionCopy.List(jobs); Profiles = DefinitionCopy.List(profiles);
          InitialSupply = DefinitionCopy.List(initialSupply); ContainerContents = DefinitionCopy.List(containerContents); }
        public string MapId { get; }
        public string EnemyId { get; }
        public int InitialCharacterCount { get; }
        public int InitialSeed { get; }
        public long InitialBalance { get; }
        public int PersonalCapacity { get; }
        public int SharedCapacity { get; }
        public int ContainerCapacity { get; }
        public int ReturnContainerCapacity { get; }
        public IReadOnlyList<WeightedDefinition> Jobs { get; }
        public IReadOnlyList<WeightedDefinition> Profiles { get; }
        public IReadOnlyList<ItemQuantity> InitialSupply { get; }
        public IReadOnlyList<ItemQuantity> ContainerContents { get; }
    }

    public sealed class ShopEntryDefinition : Definition
    {
        public ShopEntryDefinition(string itemId, long? purchasePrice, long sellPrice) : base(itemId)
        { PurchasePrice = purchasePrice; SellPrice = sellPrice; }
        public long? PurchasePrice { get; }
        public long SellPrice { get; }
    }
}
