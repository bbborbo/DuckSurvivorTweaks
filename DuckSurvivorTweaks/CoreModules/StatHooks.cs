using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DuckSurvivorTweaks.CoreModules
{
    class StatHooks : CoreModule
    {
        public class BorboStatHookEventArgs : EventArgs
        {
            /// <summary>Added to the direct multiplier to attack speed. ATTACK_SPEED ~ (BASE_ATTACK_SPEED + baseAttackSpeedAdd) * (ATTACK_SPEED_MULT + attackSpeedMultAdd).</summary>
            public float attackSpeedMultAdd = 1f;
            public float attackSpeedDivAdd = 1f;

            /// <summary>Added to the direct multiplier to crit damage. CRIT_DAMAGE ~ DAMAGE * (2 + critDamageAdd) * (critDamageMul).</summary>
            public float critDamageMultAdd = 0f;
            /// <summary>Multiplies the multiplier to crit damage. CRIT_DAMAGE ~ DAMAGE * (2 + critDamageAdd) * (critDamageMul).</summary>
            public float critDamageMultMultAdd = 1f;

            /// <summary>Multipies cooldown scale. COOLDOWN ~ BASE_COOLDOWN * cooldownMultiplier.</summary>
            public float primaryCooldownMultiplier = 1, secondaryCooldownMultiplier = 1, utilityCooldownMultiplier = 1, specialCooldownMultiplier = 1, equipmentCooldownMultiplier = 1;
        }

        public override void Init()
        {
            On.RoR2.CharacterBody.RecalculateStats += RecalcStats_Stats;

            IL.RoR2.HealthComponent.TakeDamage += TakeDamage_CritDamage;
        }

        /// <summary>
        /// Used as the delegate type for the GetStatCoefficients event.
        /// </summary>
        /// <param name="sender">The CharacterBody which RecalculateStats is being called for.</param>
        /// <param name="args">An instance of StatHookEventArgs, passed to each subscriber to this event in turn for modification.</param>
        public delegate void StatHookEventHandler(CharacterBody sender, BorboStatHookEventArgs args);

        /// <summary>
        /// Subscribe to this event to modify one of the stat hooks which TILER2.StatHooks covers (see StatHookEventArgs). Fired during CharacterBody.RecalculateStats.
        /// </summary>
        public static event StatHookEventHandler BorboGetStatCoefficients;

        private void RecalcStats_Stats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);

            BorboStatHookEventArgs statMods = new BorboStatHookEventArgs();
            BorboGetStatCoefficients?.Invoke(self, statMods);

            if(statMods != null)
            {
                SkillLocator skillLocator = self.skillLocator;
                if (skillLocator != null)
                {
                    if (skillLocator.primary != null)
                        skillLocator.primary.cooldownScale *= statMods.primaryCooldownMultiplier;

                    if (skillLocator.secondary != null)
                        skillLocator.secondary.cooldownScale *= statMods.secondaryCooldownMultiplier;

                    if (skillLocator.utility != null)
                        skillLocator.utility.cooldownScale *= statMods.utilityCooldownMultiplier;

                    if (skillLocator.special != null)
                        skillLocator.special.cooldownScale *= statMods.specialCooldownMultiplier;
                }
            }
        }

        private void TakeDamage_CritDamage(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            BorboStatHookEventArgs attackerStatMods = null;
            c.Emit(OpCodes.Ldarg_1); //arg 0 is HC, arg 1 is DI
            c.EmitDelegate<Action<DamageInfo>>((di) =>
            {
                if(di.attacker != null)
                {
                    CharacterBody cb = di.attacker.GetComponent<CharacterBody>();
                    if (cb)
                    {
                        attackerStatMods = new BorboStatHookEventArgs();
                        BorboGetStatCoefficients?.Invoke(cb, attackerStatMods);
                    }
                }
            });

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld("RoR2.DamageInfo", "crit")
                );

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(out _)

                );

            c.EmitDelegate<Func<float, float>>((critDamage) =>
            {
                float critMultiplierAdd = attackerStatMods.critDamageMultAdd;
                float critMultiplierMul = attackerStatMods.critDamageMultMultAdd;

                float finalCritDamageMultiplier = (critDamage + critMultiplierAdd);

                return finalCritDamageMultiplier;
            });
        }
    }
}
