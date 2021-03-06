using Magic.Schools;
using SpaceShared;
using StardewValley;

namespace Magic.Spells
{
    public class AnalyzeSpell : Spell
    {
        public AnalyzeSpell() : base(SchoolId.Arcane, "analyze")
        {
        }

        public override int getManaCost(Farmer player, int level)
        {
            return 0;
        }

        public override int getMaxCastingLevel()
        {
            return 1;
        }

        public override IActiveEffect onCast(Farmer player, int level, int targetX, int targetY)
        {
            if (Magic.OnAnalyzeCast != null)
                Util.invokeEvent< AnalyzeEventArgs >("OnAnalyzeCast", Magic.OnAnalyzeCast.GetInvocationList(), player, new AnalyzeEventArgs(targetX, targetY));

            return null;
        }
    }
}
