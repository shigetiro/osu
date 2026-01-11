using System;
using System.Linq;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Space.Mods;

namespace osu.Game.Rulesets.Space.Difficulty
{
    public class SpaceRatingCalculator
    {
        private readonly Mod[] mods;

        public SpaceRatingCalculator(Mod[] mods)
        {
            this.mods = mods;
        }

        public double ComputeRating(double difficultyValue)
        {
            double rating = Math.Sqrt(difficultyValue) * 0.0675;

            if (mods.Any(m => m is ModRelax)) // Assuming Space supports Relax if it existed, or generic Relax
                rating *= 0.9;

            return rating;
        }
    }
}
