// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Sprites;
using osu.Game.Rulesets.Judgements;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Beatmaps.Timing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Mods
{
    public abstract class ModClearCondition : ModFailCondition, IApplicableToBeatmap
    {
        public override string Name => "Clear Condition";
        public override string Acronym => "CC";
        public override IconUsage? Icon => FontAwesome.Solid.Filter;
        public override ModType Type => ModType.DifficultyIncrease;
        public override string Description => "Satisfy the conditions or die trying.";
        public override double ScoreMultiplier => 1;
        public override bool RequiresConfiguration => true;

        public override Type[] IncompatibleMods => base.IncompatibleMods.Append(typeof(ModPerfect)).Append(typeof(ModSuddenDeath)).ToArray();

        [SettingSource("Checking interval", "When conditions will be checked")]
        public Bindable<ConditionCheckInterval> CheckingInterval { get; } = new Bindable<ConditionCheckInterval>();

        [SettingSource("Minimum accuracy", "Fail map if your accuracy goes under this value")]
        public BindableNumber<double> MinimumAccuracy { get; } = new BindableDouble
        {
            MinValue = 0,
            MaxValue = 100,
            Default = 0,
            Precision = 0.01,
        };

        [SettingSource("Minimum health", "Fail map if your health goes under this value")]
        public BindableNumber<double> MinimumHealth { get; } = new BindableDouble
        {
            MinValue = 0,
            MaxValue = 100,
            Default = 0,
            Precision = 0.1,
        };

        [SettingSource("Enable judgements condition", "Enable the maximum imperfect judgements condition")]
        public virtual Bindable<bool> EnableImperfectJudgementCondition { get; } = new BindableBool(false);

        [SettingSource("Maximum imperfect judgements", "Fail map if the number of imperfect judgements goes above this value")]
        public virtual BindableNumber<int> MaximumImperfectJudgements { get; } = new BindableInt
        {
            MinValue = 0,
            MaxValue = 100,
            Precision = 1,
        };

        private readonly List<HitObject> hitObjectsBeforeBreaks = new List<HitObject>();
        private HitObject lastHitObjectInBeatmap;
        private readonly List<JudgementResult> flawedJudgements = new List<JudgementResult>();
        private double baseScore;
        private double maxBaseScore;
        private double accuracy => maxBaseScore > 0 ? baseScore / maxBaseScore : 1;

        public void ApplyToBeatmap(IBeatmap beatmap)
        {
            switch (CheckingInterval.Value)
            {
                case ConditionCheckInterval.AtEnd:
                    lastHitObjectInBeatmap = beatmap.HitObjects.LastOrDefault();
                    break;

                case ConditionCheckInterval.AtBreak:
                    for (int i = 0; i < beatmap.HitObjects.Count - 1; i++)
                    {
                        double inBetweenTime = (beatmap.HitObjects.ElementAtOrDefault(i).GetEndTime() + beatmap.HitObjects.ElementAtOrDefault(i + 1).StartTime) / 2;
                        foreach (BreakPeriod breakPeriod in beatmap.Breaks)
                        {
                            if (breakPeriod.Contains(inBetweenTime))
                                hitObjectsBeforeBreaks.Add(beatmap.HitObjects.ElementAtOrDefault(i));
                        }
                    }
                    break;
            }
        }

        public ScoreRank AdjustRank(ScoreRank rank, double accuracy) => rank;

        protected override bool FailCondition(HealthProcessor healthProcessor, JudgementResult result)
        {
            incrementInternalScoresFromJudgementResult(result);

            if (EnableImperfectJudgementCondition.Value && judgementIsFlawed(result))
                flawedJudgements.Add(result);

            switch (CheckingInterval.Value)
            {
                case ConditionCheckInterval.AtBreak:
                    if (!hitObjectsBeforeBreaks.Contains(result.HitObject)) return false;
                    break;

                case ConditionCheckInterval.AtEnd:
                    if (result.HitObject != lastHitObjectInBeatmap)
                        return false;
                    break;
            }

            return healthProcessor.Health.Value * 100 < MinimumHealth.Value
                || accuracy * 100 < MinimumAccuracy.Value
                || ((flawedJudgements.Count > MaximumImperfectJudgements.Value) && EnableImperfectJudgementCondition.Value);
        }

        private bool judgementIsFlawed(JudgementResult judgement)
        {
            return !(judgement.Type == judgement.Judgement.MaxResult || judgement.Type == HitResult.LargeTickHit) && judgement.Type.AffectsCombo();
        }

        private void incrementInternalScoresFromJudgementResult(JudgementResult result)
        {
            if (!result.Type.IsScorable() || result.Type.IsBonus())
                return;
            baseScore += result.Type.IsHit() ? result.Judgement.NumericResultFor(result) : 0;
            maxBaseScore += result.Judgement.MaxNumericResult;
        }

        public enum ConditionCheckInterval
        {
            Continuously,
            AtBreak,
            AtEnd, // doesn't make sense to be compatible with `ModEasyWithExtraLives` if you're checking the last hitobject
        }
    }
}
