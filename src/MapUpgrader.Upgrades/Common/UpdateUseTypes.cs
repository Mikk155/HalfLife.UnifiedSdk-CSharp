using Utilities.Entities;
using Utilities.Tools.UpgradeTool;
using System.Collections.Immutable;

namespace MapUpgrader.Upgrades.Common
{
    /// <summary>
    /// Update entity's old USE_TYPE systems with our new global one.
    /// </summary>
    internal sealed class UpdateUseTypes : MapUpgrade
    {
        protected override void ApplyCore( MapUpgradeContext context )
        {
            foreach( var entity in context.Map.Entities.OfClass( "trigger_relay" ) )
            {
                if( !entity.ContainsKey( "triggerstate" ) )
                {
                    continue;
                }

                var trigger_type = entity.GetInteger( "triggerstate" ) switch
                {
                    0 => 0, // Off
                    1 => 1, // On
                    3 => 2, // Sven coop's USE_SET
                    4 => 4, // Sven coop's USE_KILL
                    _ => 3 // TOGGLE
                };

                entity.Remove( "triggerstate" );
                entity.SetInteger( "m_UseType", trigger_type );
            }
        }
    }
}
