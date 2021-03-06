using DuckSurvivorTweaks.Skills;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static DuckSurvivorTweaks.CoreModules.StatHooks;

namespace DuckSurvivorTweaks.CoreModules
{
    class Assets : CoreModule
    {
        public static List<ArtifactDef> artifactDefs = new List<ArtifactDef>();
        public static List<BuffDef> buffDefs = new List<BuffDef>();
        public static List<EffectDef> effectDefs = new List<EffectDef>();
        public static List<SkillFamily> skillFamilies = new List<SkillFamily>();
        public static List<SkillDef> skillDefs = new List<SkillDef>();
        public static List<GameObject> projectilePrefabs = new List<GameObject>();
        public static List<GameObject> networkedObjectPrefabs = new List<GameObject>();

        public static List<ItemDef> itemDefs = new List<ItemDef>();
        public static List<EquipmentDef> equipDefs = new List<EquipmentDef>();

        public static string executeKeywordToken = "DUCK_EXECUTION_KEYWORD";
        public static string shredKeywordToken = "DUCK_SHRED_KEYWORD";

        public override void Init()
        {
            AddExecutionDebuff();
            AddShredDebuff();
            AddCooldownBuff();
            AddAspdPenaltyDebuff();

            On.RoR2.CharacterBody.RecalculateStats += RecalcStats_Stats;

            BorboGetStatCoefficients += BuffStats;
            IL.RoR2.HealthComponent.TakeDamage += AddExecutionThreshold;
            On.RoR2.HealthComponent.GetHealthBarValues += DisplayExecutionThreshold;

            LanguageAPI.Add(executeKeywordToken, 
                $"<style=cKeywordName>Finisher</style>" +
                $"<style=cSub>Enemies targeted by this skill can be " +
                $"<style=cIsHealth>instantly killed</style> if below " +
                $"<style=cIsHealth>{Tools.ConvertDecimal(survivorExecuteThreshold)} health</style>.</style>");

            LanguageAPI.Add(shredKeywordToken, $"<style=cKeywordName>Shred</style>" +
                $"<style=cSub>Apply a stacking debuff that increases ALL damage taken by {shredArmorReduction}% per stack. Critical Strikes apply more Shred.</style>");
        }

        public static BuffDef banditShredDebuff;
        public static int shredArmorReduction = 15;
        private void AddShredDebuff()
        {
            banditShredDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                banditShredDebuff.buffColor = Color.red;
                banditShredDebuff.canStack = true;
                banditShredDebuff.isDebuff = true;
                banditShredDebuff.name = "BanditShredDebuff";
                banditShredDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffCrippleIcon");
            }
            buffDefs.Add(banditShredDebuff);
        }

        private void RecalcStats_Stats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (self.HasBuff(Assets.aspdPenaltyDebuff))
            {
                self.attackSpeed *= (1 - Assets.aspdPenaltyPercent);
            }
            int shredBuffCount = self.GetBuffCount(Assets.banditShredDebuff);
            if (shredBuffCount > 0)
            {
                self.armor -= shredBuffCount * shredArmorReduction;
            }
        }

        public static BuffDef aspdPenaltyDebuff;
        public static float aspdPenaltyPercent = 0.20f;
        private void AddAspdPenaltyDebuff()
        {
            aspdPenaltyDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                aspdPenaltyDebuff.buffColor = Color.red;
                aspdPenaltyDebuff.canStack = false;
                aspdPenaltyDebuff.isDebuff = false;
                aspdPenaltyDebuff.name = "AttackSpeedPenalty";
                aspdPenaltyDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffSlow50Icon");
            }
            buffDefs.Add(aspdPenaltyDebuff);
        }

        private void BuffStats(CharacterBody sender, BorboStatHookEventArgs args)
        {
            if (sender.HasBuff(captainCdrBuff))
            {
                float mult = (1 - captainCdrPercent);
                args.primaryCooldownMultiplier *= mult;
                args.secondaryCooldownMultiplier *= mult;
                args.utilityCooldownMultiplier *= mult;
                args.specialCooldownMultiplier *= mult;
            }
        }

        public static BuffDef captainCdrBuff;
        public static float captainCdrPercent = 0.25f;

        private void AddCooldownBuff()
        {
            captainCdrBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                captainCdrBuff.buffColor = Color.yellow;
                captainCdrBuff.canStack = false;
                captainCdrBuff.isDebuff = false;
                captainCdrBuff.name = "CaptainBeaconCdr";
                captainCdrBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texMovespeedBuffIcon");
            }
            buffDefs.Add(captainCdrBuff);
        }

        public static BuffDef desperadoExecutionDebuff;
        public static BuffDef lightsoutExecutionDebuff;
        public static float survivorExecuteThreshold = 0.15f;
        public static float banditExecutionThreshold = 0.1f;
        public static float harvestExecutionThreshold = 0.2f;

        private void AddExecutionDebuff()
        {
            desperadoExecutionDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                desperadoExecutionDebuff.buffColor = Color.black;
                desperadoExecutionDebuff.canStack = false;
                desperadoExecutionDebuff.isDebuff = true;
                desperadoExecutionDebuff.name = "DesperadoExecutionDebuff";
                desperadoExecutionDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffCrippleIcon");
            }
            buffDefs.Add(desperadoExecutionDebuff);
            lightsoutExecutionDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                lightsoutExecutionDebuff.buffColor = Color.black;
                lightsoutExecutionDebuff.canStack = false;
                lightsoutExecutionDebuff.isDebuff = true;
                lightsoutExecutionDebuff.name = "LightsOutExecutionDebuff";
                lightsoutExecutionDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffCrippleIcon");
            }
            buffDefs.Add(lightsoutExecutionDebuff);
        }

        private void AddExecutionThreshold(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int thresholdPosition = 0;

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(float.NegativeInfinity),
                x => x.MatchStloc(out thresholdPosition)
                );
            
            c.GotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<HealthComponent>("get_isInFrozenState")
                );

            c.Emit(OpCodes.Ldloc, thresholdPosition);
            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((currentThreshold, hc) =>
            {
                float newThreshold = currentThreshold;

                newThreshold = GetExecutionThreshold(currentThreshold, hc);

                return newThreshold;
            });
            c.Emit(OpCodes.Stloc, thresholdPosition);
        }

        static float GetExecutionThreshold(float currentThreshold, HealthComponent healthComponent)
        {
            float newThreshold = currentThreshold;
            CharacterBody body = healthComponent.body;

            if (body != null)
            {
                if (!body.bodyFlags.HasFlag(CharacterBody.BodyFlags.ImmuneToExecutes))
                {
                    bool hasBanditExecutionBuff = body.HasBuff(desperadoExecutionDebuff) || body.HasBuff(lightsoutExecutionDebuff);
                    newThreshold = ModifyExecutionThreshold(newThreshold, survivorExecuteThreshold, hasBanditExecutionBuff);

                    bool hasRexHarvestBuff = body.HasBuff(RoR2Content.Buffs.Fruiting);
                    newThreshold = ModifyExecutionThreshold(newThreshold, survivorExecuteThreshold, hasRexHarvestBuff);
                }
            }

            return newThreshold;
        }

        public static float ModifyExecutionThreshold(float currentThreshold, float newThreshold, bool condition)
        {
            if(condition)
            {
                return Mathf.Max(currentThreshold, newThreshold);
            }
            //else...
            return currentThreshold;
        }

        private HealthComponent.HealthBarValues DisplayExecutionThreshold(On.RoR2.HealthComponent.orig_GetHealthBarValues orig, HealthComponent self)
        {
            HealthComponent.HealthBarValues values = orig(self);

            values.cullFraction = Mathf.Clamp01(GetExecutionThreshold(values.cullFraction, self));

            return values;
        }
    }
}
