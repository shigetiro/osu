using osu.Framework.Localisation;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Space.Objects;
using osu.Game.Rulesets.Space.UI;

namespace osu.Game.Rulesets.Space.Mods
{
    public class SpaceModMirror : ModMirror, IApplicableToHitObject
    {
        public override LocalisableString Description => "Reflects the playfield horizontally.";

        public void ApplyToHitObject(osu.Game.Rulesets.Objects.HitObject hitObject)
        {
            var spaceObject = (SpaceHitObject)hitObject;
            spaceObject.X = SpacePlayfield.BASE_SIZE - spaceObject.X;
            spaceObject.oX = spaceObject.X / (SpacePlayfield.BASE_SIZE / 3f) - 0.5f;
        }
    }
}
