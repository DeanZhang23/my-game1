using System;
using System.Collections.Generic;

namespace Echoes.Simulation
{
    public static class FaceEffects
    {
        public static FaceResolution Resolve(string die0, string die1, string die2)
        {
            string[] faces = { die0, die1, die2 };
            int[] counts = new int[FaceIds.Order.Length];
            for (int i = 0; i < faces.Length; i++)
            {
                int index = FaceIds.IndexOf(faces[i]);
                if (index < 0)
                    throw new ArgumentException("Unknown face: " + faces[i]);
                counts[index]++;
            }

            int skull = FaceIds.IndexOf(FaceIds.Skull);
            int wilds = counts[skull];
            var result = new FaceResolution();
            result.WildTokens = wilds;

            string comboFace = null;
            int tier = 0;
            int used = 0;
            for (int i = 0; i < FaceIds.Order.Length; i++)
            {
                int count = counts[i];
                if (count <= 0)
                    continue;
                if (count >= 3 || count + wilds >= 3)
                {
                    comboFace = FaceIds.Order[i];
                    tier = 2;
                    used = Math.Max(0, 3 - count);
                    break;
                }
            }

            if (tier < 2)
            {
                for (int i = 0; i < FaceIds.Order.Length; i++)
                {
                    int count = counts[i];
                    if (count <= 0)
                        continue;
                    if (count >= 2 || count + wilds >= 2)
                    {
                        comboFace = FaceIds.Order[i];
                        tier = 1;
                        used = Math.Max(0, 2 - count);
                        break;
                    }
                }
            }

            if (tier == 2)
            {
                result.ComboId = ComboIds.Attunement;
                result.Coefficient = CombatFormulas.Attunement;
            }
            else if (tier == 1)
            {
                result.ComboId = ComboIds.Twin;
                result.Coefficient = CombatFormulas.Twin;
            }

            if (comboFace != null)
            {
                result.ComboFace = comboFace;
                result.WildUsed = used;
                result.WildFace = used > 0 ? comboFace : "none";
            }

            if (counts[FaceIds.IndexOf(FaceIds.Sword)] > 0)
            {
                decimal m = Coeff(result, FaceIds.Sword);
                result.AttackMultiplier = 1m + 0.12m * m;
                result.KeenPerFront = counts[FaceIds.IndexOf(FaceIds.Sword)];
            }

            if (counts[FaceIds.IndexOf(FaceIds.Shield)] > 0)
            {
                decimal m = Coeff(result, FaceIds.Shield);
                result.ArmorBonus = 8m * m;
                result.ShieldLifeRatio = 0.15m * m;
            }

            if (counts[FaceIds.IndexOf(FaceIds.Star)] > 0)
            {
                decimal m = Coeff(result, FaceIds.Star);
                result.BackEnergy = (int)decimal.Floor(20m * m);
                result.SkillDamageMultiplier = 1m + 0.10m * m;
            }

            if (counts[FaceIds.IndexOf(FaceIds.Feather)] > 0)
            {
                decimal m = Coeff(result, FaceIds.Feather);
                result.AttackSpeedBonus = 0.15m * m;
                result.CooldownReductionSeconds = 1m * m;
            }

            if (counts[FaceIds.IndexOf(FaceIds.Coin)] > 0)
            {
                decimal m = Coeff(result, FaceIds.Coin);
                result.Gold = CombatFormulas.Gold(6m * m);
            }

            if (counts[FaceIds.IndexOf(FaceIds.Skull)] > 0)
            {
                decimal m = Coeff(result, FaceIds.Skull);
                result.SacrificeFraction = 0.05m * m;
            }

            var effects = new List<FaceEffectSpec>();
            AddEffect(effects, counts, FaceIds.Sword, EffectTargets.AllMelee);
            AddEffect(effects, counts, FaceIds.Shield, EffectTargets.AllMelee);
            AddEffect(effects, counts, FaceIds.Star, EffectTargets.AllRanged);
            AddEffect(effects, counts, FaceIds.Feather, EffectTargets.AllAllies);
            AddEffect(effects, counts, FaceIds.Coin, EffectTargets.Battle);
            AddEffect(effects, counts, FaceIds.Skull, EffectTargets.AllAllies);
            result.Effects = effects.ToArray();
            return result;
        }

        private static void AddEffect(List<FaceEffectSpec> effects, int[] counts, string faceId, string targetKind)
        {
            if (counts[FaceIds.IndexOf(faceId)] <= 0)
                return;
            effects.Add(new FaceEffectSpec
            {
                FaceId = faceId,
                TargetKind = targetKind
            });
        }

        private static decimal Coeff(FaceResolution result, string face)
        {
            if (result.ComboFace == face && result.ComboId != ComboIds.None)
                return result.Coefficient;
            return 1m;
        }
    }
}
