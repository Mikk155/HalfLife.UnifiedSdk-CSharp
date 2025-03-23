using Utilities.Entities;
using Utilities.Tools.UpgradeTool;
using System.Collections.Immutable;

namespace MapUpgrader.Upgrades.Common
{
    /// <summary>
    /// Renames info_player_* entities to be a multiplayer map
    /// </summary>
    internal sealed class UpdateSpawnpoints : MapUpgrade
    {
        private static readonly ImmutableArray<string> ClassNames = ImmutableArray.Create(
            "info_player_deathmatch",
            "info_player_coop"
        );

        protected override void ApplyCore( MapUpgradeContext context )
        {
            foreach( var entity in context.Map.Entities.Where( e => ClassNames.Contains( e.ClassName ) ) )
            {
                entity.ClassName = "info_player_start_mp";
            }
        }
    }
}
