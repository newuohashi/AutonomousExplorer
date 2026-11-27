using System.Linq;
using AutonomousExplorer.Domain.Definitions;
using AutonomousExplorer.Infrastructure.Definitions;
using UnityEngine;

namespace AutonomousExplorer.Tests.EditMode.Definitions
{
    /// <summary>Explicit adopted G3 values for tests only; production assets are authored by the user.</summary>
    internal static class PrototypeDefinitionFixture
    {
        public static readonly string[] ActionIds =
            { "Explore", "Loot", "Deposit", "Unlock", "Break", "Heal", "Buff", "Request", "Approve", "Refuse", "Attack" };

        public static DefinitionRegistrations Registrations() => new DefinitionRegistrations(
            new[] { "Terrain", "Container", "Door", "Item", "Request", "Battle" },
            new[] { "Move", "Transfer", "Door", "Use", "Request", "Respond", "Attack" },
            new[] { "CanAct", "TargetExists", "Reachable", "Resources", "Capacity", "Reservation", "Phase" },
            ActionIds.Select(x => "Benefit." + x).Concat(new[] { "P", "S" }));

        public static ConfiguredSeconds Seconds(double value) => new ConfiguredSeconds { IsConfigured = true, Value = value };

        public static GameDefinitionAsset Create()
        {
            var asset = ScriptableObject.CreateInstance<GameDefinitionAsset>();
            asset.Jobs = new[] {
                new JobRecord { Id = "Engineer", Stats = Stats(100, 12, 3, 10, 2.5, 6) },
                new JobRecord { Id = "Breacher", Stats = Stats(140, 16, 5, 8, 2, 6) }
            };
            asset.Enemies = new[] { new JobRecord { Id = "Enemy", Stats = Stats(40, 7, 1, 8, 2, 4) } };
            asset.ToolTags = new[] { "Lockpick", "BreachTool" };
            asset.Items = new[] {
                Item("HealingPotion", "Heal", 30, true, 5), Item("BuffPotion", "Buff", 4, true, 4),
                Item("Lockpick", null, 0, false, 3, "Lockpick"),
                Item("BreachTool", null, 0, false, 3, "BreachTool"), Item("Material", null, 0, false, 10)
            };
            asset.Requirements = new[] {
                new RequirementRecord { Id = "Basic", PreconditionIds = new[] { "CanAct", "TargetExists", "Reachable", "Resources", "Capacity", "Reservation", "Phase" } },
                new RequirementRecord { Id = "Unlock", AnyJobIds = new[] { "Engineer" }, AnyToolTags = new[] { "Lockpick" }, PreconditionIds = new[] { "CanAct", "TargetExists", "Reachable", "Phase" } },
                new RequirementRecord { Id = "Break", AnyJobIds = new[] { "Breacher" }, AnyToolTags = new[] { "BreachTool" }, PreconditionIds = new[] { "CanAct", "TargetExists", "Reachable", "Phase" } }
            };
            asset.Actions = new[] {
                Action("Explore", "Terrain", "Move"), Action("Loot", "Container", "Transfer"), Action("Deposit", "Container", "Transfer"),
                Action("Unlock", "Door", "Door", "Unlock", 3), Action("Break", "Door", "Door", "Break", 5),
                Action("Heal", "Item", "Use"), Action("Buff", "Item", "Use"), Action("Request", "Request", "Request"),
                Action("Approve", "Request", "Respond"), Action("Refuse", "Request", "Respond"), Action("Attack", "Battle", "Attack")
            };
            asset.Actions.Single(x => x.Id == "Heal").Uses = new[] {
                Use(ItemUsePhase.Preparation, 0), Use(ItemUsePhase.Exploration, 1), Use(ItemUsePhase.Battle, 0)
            };
            asset.Actions.Single(x => x.Id == "Buff").Uses = new[] { Use(ItemUsePhase.Battle, 0) };
            asset.Curves = new[] { new CurveRecord { Id = "Linear", Points = new[] {
                new CurvePointRecord { X = 0, Y = 0 }, new CurvePointRecord { X = 1, Y = 1 }
            } } };
            asset.Profiles = new[] { Profile("Cautious"), Profile("Collector") };
            asset.Maps = new[] { new MapRecord { Id = "InitialMap", Width = 21, Height = 21,
                DoorCount = 6, ContainerCount = 4, EnemyCount = 6, MaxAttempts = 20, SafeRadius = 2, CellWidth = 1 } };
            asset.GenerationRules = new[] { new GenerationRecord {
                Id = "Initial", MapId = "InitialMap", EnemyId = "Enemy", InitialCharacterCount = 2, InitialSeed = 12345,
                InitialBalance = 0, PersonalCapacity = 2, SharedCapacity = 6, ContainerCapacity = 12, ReturnContainerCapacity = 12,
                Jobs = new[] { Weighted("Engineer"), Weighted("Breacher") }, Profiles = new[] { Weighted("Cautious"), Weighted("Collector") },
                InitialSupply = new[] { Quantity("HealingPotion", 4), Quantity("BuffPotion", 2), Quantity("Lockpick", 1) },
                ContainerContents = new[] { Quantity("HealingPotion", 1), Quantity("BuffPotion", 1), Quantity("Material", 2) }
            } };
            asset.ShopEntries = new[] { Shop("HealingPotion", 10, 5), Shop("BuffPotion", 8, 4), Shop("Lockpick", 6, 3),
                Shop("BreachTool", 6, 3), new ShopRecord { ItemId = "Material", Purchasable = false, SellPrice = 10 } };
            return asset;
        }

        private static StatsRecord Stats(int hp, int attack, int defense, double combat, double move, int vision) =>
            new StatsRecord { MaxHP = hp, Attack = attack, Defense = defense, CombatSpeed = combat, MoveSpeed = move, Vision = vision };
        private static ItemRecord Item(string id, string action, int amount, bool consumable, long value, params string[] tags) =>
            new ItemRecord { Id = id, EffectActionId = action, EffectAmount = amount, Consumable = consumable, Capacity = 1, RecoveryValue = value, ToolTags = tags };
        private static ActionRecord Action(string id, string provider, string executor, string requirement = "Basic", double? duration = null) =>
            new ActionRecord { Id = id, ProviderId = provider, ExecutorId = executor, RequirementId = requirement,
                DurationSeconds = duration.HasValue ? Seconds(duration.Value) : null };
        private static PhaseUseRecord Use(ItemUsePhase phase, double duration) => new PhaseUseRecord { Phase = phase, DurationSeconds = Seconds(duration) };
        private static WeightedRecord Weighted(string id) => new WeightedRecord { DefinitionId = id, Weight = 1 };
        private static ItemQuantityRecord Quantity(string id, int count) => new ItemQuantityRecord { ItemId = id, Count = count };
        private static ShopRecord Shop(string id, long purchase, long sell) => new ShopRecord { ItemId = id, Purchasable = true, PurchasePrice = purchase, SellPrice = sell };
        private static ProfileRecord Profile(string id) => new ProfileRecord { Id = id, Actions = ActionIds.Select(action =>
            new ActionUtilityRecord { ActionId = action, Multiplier = 1, Considerations = new[] {
                Consideration("Benefit." + action, id == "Collector" && (action == "Loot" || action == "Deposit") ? 3 : 1),
                Consideration("P", 1), Consideration("S", id == "Cautious" ? 3 : 1)
            } }).ToArray() };
        private static ConsiderationRecord Consideration(string metric, double weight) =>
            new ConsiderationRecord { MetricId = metric, CurveId = "Linear", Weight = weight };
    }
}
