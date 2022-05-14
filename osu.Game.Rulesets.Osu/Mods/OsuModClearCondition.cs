// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Mods;
using System.Linq;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModClearCondition : ModClearCondition
    {
        public override int imperfectJudgementCount { get { return hitEvents.Where(hitEvent => !(hitEvent.Result is HitResult.Great) && hitEvent.Result.AffectsCombo() && !hitEvent.Result.IsHit()).Count(); } }
    }
}
