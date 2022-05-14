// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Rulesets.Judgements;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Mods
{
    public abstract class ModClearCondition : ModFailCondition, IApplicableToScoreProcessor
    {
        public override string Name => "Clear Condition";
        public override string Acronym => "CC";
        public override IconUsage? Icon => FontAwesome.Solid.Filter;
        public override ModType Type => ModType.DifficultyIncrease;
        public override string Description => "Satisfy the conditions or die trying";
        public override double ScoreMultiplier => 1;
        public override bool RequiresConfiguration => true;

        public override Type[] IncompatibleMods => base.IncompatibleMods.Append(typeof(ModPerfect)).Append(typeof(ModSuddenDeath)).ToArray();

        [SettingSource("Checking Interval", "When conditions will be checked.")]
        public Bindable<CheckOn> Reflection { get; } = new Bindable<CheckOn>();

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

        // suboptimal, player might not want imperfect judgements to have any effect at all
        [SettingSource("Maximum imperfect judgements", "Fail map if the number of imperfect judgements goes above this value")]
        public virtual BindableNumber<int> MaximumImperfectJudgements { get; } = new BindableInt
        {
            MinValue = 0,
            MaxValue = 100,
            Default = 100,
            Precision = 1,
        };

        private BindableNumber<double> currentAccuracy;
        protected IReadOnlyList<HitEvent> hitEvents;
        public abstract int imperfectJudgementCount { get; }

        public void ApplyToScoreProcessor(ScoreProcessor scoreProcessor)
        {
            currentAccuracy = scoreProcessor.Accuracy.GetBoundCopy();
            hitEvents = scoreProcessor.HitEvents;
        }

        public ScoreRank AdjustRank(ScoreRank rank, double accuracy) => rank;
        
        protected override bool FailCondition(HealthProcessor healthProcessor, JudgementResult result)
            => healthProcessor.Health.Value < MinimumHealth.Value/100
               || currentAccuracy.Value < MinimumAccuracy.Value/100
               || imperfectJudgementCount > MaximumImperfectJudgements.Value;

        public enum CheckOn {
            Continuous,
            Break,
            End,
        }
    }
}

/*
https://github.com/ppy/osu/discussions/13120

TODO

_1_
allow player to disable imperfect judgement condition
implement configurable condition check interval

_2_
fail at end unless you have a certain combo or higher
fail if it's no longer possible to beat your highscore (might not be possible/intuitive to impliment)
allow for configuration for all of ruleset's hitresults instead of just imperfect ones
*/