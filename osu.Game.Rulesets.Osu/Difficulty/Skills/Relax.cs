// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Relax : OsuStrainSkill
    {
        public readonly bool IncludeSliders;

        public Relax(Mod[] mods, bool includeSliders, double hitWindow)
            : base(mods)
        {
            IncludeSliders = includeSliders;
            this.hitWindow = hitWindow;
        }

        private double currentStrain;
        private readonly double hitWindow;

        private double skillMultiplier => 24.16;
        private double strainDecayBase => 0.15;

        private readonly List<double> sliderStrains = new List<double>();
        private readonly List<OsuDifficultyHitObject> difficultyObjects = new List<OsuDifficultyHitObject>();

        private double strainDecay(double ms) => Math.Pow(strainDecayBase, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) => currentStrain * strainDecay(time - current.Previous(0).StartTime);

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            var osuCurrent = (OsuDifficultyHitObject)current;
            difficultyObjects.Add(osuCurrent);

            currentStrain *= strainDecay(current.DeltaTime);
            currentStrain += RelaxAimEvaluator.EvaluateDifficultyOf(osuCurrent, difficultyObjects.ToArray(), hitWindow, IncludeSliders) * skillMultiplier;

            if (current.BaseObject is Slider)
            {
                sliderStrains.Add(currentStrain);
            }

            return currentStrain;
        }

        public double GetDifficultSliders()
        {
            if (sliderStrains.Count == 0)
                return 0;

            double maxSliderStrain = sliderStrains.Max();
            if (maxSliderStrain == 0)
                return 0;

            return sliderStrains.Sum(strain => 1.0 / (1.0 + Math.Exp(-(strain / maxSliderStrain * 12.0 - 6.0))));
        }

        public new static double DifficultyToPerformance(double difficulty) => Math.Pow(5.0 * Math.Max(1.0, difficulty / 0.0675) - 4.0, 3.0) / 100000.0;
    }
}
