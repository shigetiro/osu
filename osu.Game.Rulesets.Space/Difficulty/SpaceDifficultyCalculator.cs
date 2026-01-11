
using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Space.Difficulty.Preprocessing;
using osu.Game.Rulesets.Space.Difficulty.Skills;
using osu.Game.Rulesets.Space.Objects;
using osu.Game.Rulesets.Space.Difficulty;

namespace osu.Game.Rulesets.Space
{
    public class SpaceDifficultyCalculator : DifficultyCalculator
    {
        public SpaceDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new SpaceDifficultyAttributes(mods, 0);

            var ratingCalculator = new SpaceRatingCalculator(mods);

            double aimRating = ratingCalculator.ComputeRating(skills[0].DifficultyValue());
            double readingRating = ratingCalculator.ComputeRating(skills[1].DifficultyValue());

            double baseRating = Math.Pow(
                Math.Pow(aimRating, 1.1) +
                Math.Pow(readingRating, 1.1),
                1.0 / 1.1
            );

            double starRating = baseRating;

            var attributes = new SpaceDifficultyAttributes(mods, starRating)
            {
                AimDifficulty = aimRating,
                ReadingDifficulty = readingRating
            };

            return attributes;
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, double clockRate)
        {
            var sortedObjects = beatmap.HitObjects.OfType<SpaceHitObject>().OrderBy(h => h.StartTime).ToList();
            var difficultyObjects = new List<DifficultyHitObject>();

            for (int i = 1; i < sortedObjects.Count; i++)
            {
                var lastLast = i > 1 ? sortedObjects[i - 2] : null;
                var last = sortedObjects[i - 1];
                var current = sortedObjects[i];

                var difficultyObject = new SpaceDifficultyHitObject(current, last, lastLast, clockRate, difficultyObjects, difficultyObjects.Count);
                difficultyObjects.Add(difficultyObject);
            }

            return difficultyObjects;
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            return
            [
                new Aim(mods),
                new Reading(mods),
            ];
        }
    }
}
