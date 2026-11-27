using System;
using UnityEngine;
using AutonomousExplorer.Domain.Definitions;

namespace AutonomousExplorer.Infrastructure.Definitions
{
    /// <summary>Authoring input only. No runtime instance state and no automatic prototype defaults.</summary>
    [CreateAssetMenu(menuName = "AutonomousExplorer/Game Definition Catalog")]
    public sealed class GameDefinitionAsset : ScriptableObject
    {
        public JobRecord[] Jobs = Array.Empty<JobRecord>();
        public JobRecord[] Enemies = Array.Empty<JobRecord>();
        public ItemRecord[] Items = Array.Empty<ItemRecord>();
        public ActionRecord[] Actions = Array.Empty<ActionRecord>();
        public RequirementRecord[] Requirements = Array.Empty<RequirementRecord>();
        public ProfileRecord[] Profiles = Array.Empty<ProfileRecord>();
        public CurveRecord[] Curves = Array.Empty<CurveRecord>();
        public MapRecord[] Maps = Array.Empty<MapRecord>();
        public GenerationRecord[] GenerationRules = Array.Empty<GenerationRecord>();
        public ShopRecord[] ShopEntries = Array.Empty<ShopRecord>();
        public string[] ToolTags = Array.Empty<string>();
    }

    [Serializable] public sealed class ConfiguredSeconds
    {
        public bool IsConfigured;
        public double Value;
    }

    [Serializable] public sealed class StatsRecord
    {
        public int MaxHP;
        public int Attack;
        public int Defense;
        public double CombatSpeed;
        public double MoveSpeed;
        public int Vision;
    }

    [Serializable] public sealed class JobRecord
    {
        public string Id;
        public StatsRecord Stats;
    }

    [Serializable] public sealed class ItemRecord
    {
        public string Id;
        public string EffectActionId;
        public int EffectAmount;
        public bool Consumable;
        public int Capacity;
        public long RecoveryValue;
        public string[] ToolTags = Array.Empty<string>();
    }

    [Serializable] public sealed class PhaseUseRecord
    {
        public ItemUsePhase Phase;
        public ConfiguredSeconds DurationSeconds;
    }

    [Serializable] public sealed class ActionRecord
    {
        public string Id;
        public string ProviderId;
        public string ExecutorId;
        public string RequirementId;
        public ConfiguredSeconds DurationSeconds;
        public PhaseUseRecord[] Uses = Array.Empty<PhaseUseRecord>();
    }

    [Serializable] public sealed class RequirementRecord
    {
        public string Id;
        public string[] AnyJobIds = Array.Empty<string>();
        public string[] AnyToolTags = Array.Empty<string>();
        public string[] PreconditionIds = Array.Empty<string>();
    }

    [Serializable] public sealed class CurvePointRecord
    {
        public double X;
        public double Y;
    }

    [Serializable] public sealed class CurveRecord
    {
        public string Id;
        public CurvePointRecord[] Points = Array.Empty<CurvePointRecord>();
    }

    [Serializable] public sealed class ConsiderationRecord
    {
        public string MetricId;
        public string CurveId;
        public double Weight;
    }

    [Serializable] public sealed class ActionUtilityRecord
    {
        public string ActionId;
        public double Multiplier;
        public ConsiderationRecord[] Considerations = Array.Empty<ConsiderationRecord>();
    }

    [Serializable] public sealed class ProfileRecord
    {
        public string Id;
        public ActionUtilityRecord[] Actions = Array.Empty<ActionUtilityRecord>();
    }

    [Serializable] public sealed class WeightedRecord
    {
        public string DefinitionId;
        public double Weight;
    }

    [Serializable] public sealed class ItemQuantityRecord
    {
        public string ItemId;
        public int Count;
    }

    [Serializable] public sealed class MapRecord
    {
        public string Id;
        public int Width;
        public int Height;
        public int DoorCount;
        public int ContainerCount;
        public int EnemyCount;
        public int MaxAttempts;
        public int SafeRadius;
        public double CellWidth;
    }

    [Serializable] public sealed class GenerationRecord
    {
        public string Id;
        public string MapId;
        public string EnemyId;
        public int InitialCharacterCount;
        public int InitialSeed;
        public long InitialBalance;
        public int PersonalCapacity;
        public int SharedCapacity;
        public int ContainerCapacity;
        public int ReturnContainerCapacity;
        public WeightedRecord[] Jobs = Array.Empty<WeightedRecord>();
        public WeightedRecord[] Profiles = Array.Empty<WeightedRecord>();
        public ItemQuantityRecord[] InitialSupply = Array.Empty<ItemQuantityRecord>();
        public ItemQuantityRecord[] ContainerContents = Array.Empty<ItemQuantityRecord>();
    }

    [Serializable] public sealed class ShopRecord
    {
        public string ItemId;
        public bool Purchasable;
        public long PurchasePrice;
        public long SellPrice;
    }
}
