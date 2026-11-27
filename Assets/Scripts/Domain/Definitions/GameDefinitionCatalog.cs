using System.Collections.Generic;
using System.Linq;

namespace AutonomousExplorer.Domain.Definitions
{
    /// <summary>An immutable snapshot. Invalid source rows are retained for aggregate validation.</summary>
    public sealed class GameDefinitionCatalog
    {
        public GameDefinitionCatalog(IEnumerable<JobDefinition> jobs, IEnumerable<EnemyDefinition> enemies,
            IEnumerable<ItemDefinition> items, IEnumerable<ActionDefinition> actions,
            IEnumerable<RequirementDefinition> requirements, IEnumerable<ProfileDefinition> profiles,
            IEnumerable<CurveDefinition> curves, IEnumerable<MapDefinition> maps,
            IEnumerable<GenerationRuleDefinition> generationRules, IEnumerable<ShopEntryDefinition> shopEntries,
            IEnumerable<string> toolTags)
        { Jobs = DefinitionCopy.List(jobs); Enemies = DefinitionCopy.List(enemies); Items = DefinitionCopy.List(items);
          Actions = DefinitionCopy.List(actions); Requirements = DefinitionCopy.List(requirements);
          Profiles = DefinitionCopy.List(profiles); Curves = DefinitionCopy.List(curves); Maps = DefinitionCopy.List(maps);
          GenerationRules = DefinitionCopy.List(generationRules); ShopEntries = DefinitionCopy.List(shopEntries);
          ToolTags = DefinitionCopy.List(toolTags); }
        public IReadOnlyList<JobDefinition> Jobs { get; }
        public IReadOnlyList<EnemyDefinition> Enemies { get; }
        public IReadOnlyList<ItemDefinition> Items { get; }
        public IReadOnlyList<ActionDefinition> Actions { get; }
        public IReadOnlyList<RequirementDefinition> Requirements { get; }
        public IReadOnlyList<ProfileDefinition> Profiles { get; }
        public IReadOnlyList<CurveDefinition> Curves { get; }
        public IReadOnlyList<MapDefinition> Maps { get; }
        public IReadOnlyList<GenerationRuleDefinition> GenerationRules { get; }
        public IReadOnlyList<ShopEntryDefinition> ShopEntries { get; }
        public IReadOnlyList<string> ToolTags { get; }
        public JobDefinition GetJob(string id) => Jobs.Single(x => x.Id == id);
        public EnemyDefinition GetEnemy(string id) => Enemies.Single(x => x.Id == id);
        public ItemDefinition GetItem(string id) => Items.Single(x => x.Id == id);
        public ActionDefinition GetAction(string id) => Actions.Single(x => x.Id == id);
        public ProfileDefinition GetProfile(string id) => Profiles.Single(x => x.Id == id);
        public GenerationRuleDefinition GetGenerationRule(string id) => GenerationRules.Single(x => x.Id == id);
        public ShopEntryDefinition GetShopEntry(string itemId) => ShopEntries.Single(x => x.Id == itemId);
        public MapDefinition GetMap(string id) => Maps.Single(x => x.Id == id);
        public RequirementDefinition GetRequirement(string id) => Requirements.Single(x => x.Id == id);
        public CurveDefinition GetCurve(string id) => Curves.Single(x => x.Id == id);
    }
}
