using EntityStates;
using EntityStates.Croco;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DuckSurvivorTweaks.SurvivorTweaks
{
    class AcridTweaks : SurvivorTweakModule
    {
        public static bool isLoaded;

        public static float poisonDuration = 8; //10
        public static float blightDuration = 5; //5

        public static float slashDuration = 0.7f; //1.5f

        public static float spitCooldown = 3f; //2
        public static float spitDamageCoeff = 3.6f; //2.4f

        public static float biteForceStrength = 8000f; //0
        public static float biteCooldown = 3f; //2
        public static float biteDamageCoeff = 4.6f; //3.1f

        public static float causticCooldown = 5; //6
        public static float frenziedCooldown = 8; //10
        public static float leapMinY = -0.3f; //0

        public static float epidemicCooldown = 12; //10
        public static float epidemicDamageCoefficient = 2; //1
        public static float epidemicSpreadRange = 100;

        public override string survivorName => "Acrid";

        public override string bodyName => "CrocoBody";

        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            CharacterBody body = bodyObject.GetComponent<CharacterBody>();
            body.baseMoveSpeed = 8;//7

            ChangeVanillaPrimary(primary);
            ChangeVanillaSecondaries(secondary);
            ChangeVanillaUtilities(utility);
            ChangeVanillaSpecials(special);

            IL.RoR2.GlobalEventManager.OnHitEnemy += ChangePoisonDuration;
            LanguageAPI.Add("KEYWORD_POISON", 
                $"<style=cKeywordName>Poisonous</style>" +
                $"<style=cSub>Deal damage equal to <style=cIsDamage>up to 10%</style> of their maximum health over {poisonDuration}s. " +
                $"<i>Poison cannot kill enemies.</i></style>");
        }

        private void ChangeVanillaPrimary(SkillFamily family)
        {
            On.EntityStates.Croco.Slash.OnEnter += ChangeCrocoSlashDuration;
            family.variants[0].skillDef.canceledFromSprinting = false;
        }

        private void ChangeVanillaSecondaries(SkillFamily family)
        {
            //spit
            family.variants[0].skillDef.baseRechargeInterval = spitCooldown;
            LanguageAPI.Add("CROCO_SECONDARY_DESCRIPTION",
                $"<style=cIsHealing>Poisonous</style>. " +
                $"Spit toxic bile for <style=cIsDamage>{Tools.ConvertDecimal(spitDamageCoeff)} damage</style>.");

            //bite
            family.variants[1].skillDef.baseRechargeInterval = biteCooldown;
            On.EntityStates.Croco.Bite.OnEnter += BuffBite;
            LanguageAPI.Add("CROCO_SECONDARY_ALT_DESCRIPTION",
                $"<style=cIsHealing>Poisonous</style>. <style=cIsDamage>Slayer</style>. <style=cIsHealing>Regenerative</style>. " +
                $"Bite an enemy for <style=cIsDamage>{Tools.ConvertDecimal(biteDamageCoeff)} damage</style>.");
        }

        private void BuffBite(On.EntityStates.Croco.Bite.orig_OnEnter orig, EntityStates.Croco.Bite self)
        {
            self.damageCoefficient = biteDamageCoeff;
            orig(self);
            if (!Main.acridLungeLoaded)
            {
                self.characterMotor.velocity = Vector3.zero;
                self.characterMotor.ApplyForce(self.inputBank.aimDirection * biteForceStrength, true, false);
            }
        }

        private void ChangeCrocoSlashDuration(On.EntityStates.Croco.Slash.orig_OnEnter orig, EntityStates.Croco.Slash self)
        {
            self.baseDuration = slashDuration;
            orig(self);
        }

        private void ChangeVanillaUtilities(SkillFamily family)
        {
            //caustic leap
            family.variants[0].skillDef.baseRechargeInterval = causticCooldown;

            //frenzied leap
            family.variants[1].skillDef.baseRechargeInterval = frenziedCooldown;

            /*foreach(SkillFamily.Variant variant in family.variants)
            {
                SkillDef s = variant.skillDef;
                s.interruptPriority = InterruptPriority.Skill;
                s.mustKeyPress = true;
            }*/

            On.EntityStates.Croco.BaseLeap.OnEnter += ChangeLeapStuff;
            On.EntityStates.Croco.BaseLeap.DoImpactAuthority += AddLeapBounce;
        }

        private void AddLeapBounce(On.EntityStates.Croco.BaseLeap.orig_DoImpactAuthority orig, BaseLeap self)
        {
            orig(self);
            self.SmallHop(self.characterMotor, 2f);
        }

        private void ChangeLeapStuff(On.EntityStates.Croco.BaseLeap.orig_OnEnter orig, EntityStates.Croco.BaseLeap self)
        {
            BaseLeap.minimumY = leapMinY;
            orig(self);
        }

        #region specials
        void ChangeVanillaSpecials(SkillFamily family)
        {
            //epidemic
            family.variants[0].skillDef.baseRechargeInterval = epidemicCooldown;
            LanguageAPI.Add("CROCO_SPECIAL_DESCRIPTION", 
                $"<style=cIsHealing>Poisonous</style>. " +
                $"Release a deadly disease that deals <style=cIsDamage>{Tools.ConvertDecimal(epidemicDamageCoefficient)} damage</style>. " +
                $"The disease spreads to up to <style=cIsDamage>20</style> targets.");

            On.EntityStates.Croco.Disease.OnEnter += ChangeDiseaseBehavior;
        }

        private void ChangeDiseaseBehavior(On.EntityStates.Croco.Disease.orig_OnEnter orig, Disease self)
        {
            Disease.damageCoefficient = epidemicDamageCoefficient;
            Disease.bounceRange = epidemicSpreadRange;
            orig(self);
        }
        #endregion

        private void ChangePoisonDuration(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            //poison duration
            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<RoR2.DamageInfo>("damageType"),
                x => x.MatchLdcI4((int)DamageType.PoisonOnHit)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<RoR2.DamageInfo>("procCoefficient")
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, poisonDuration);
            return;
            //blight duration
            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<RoR2.DamageType>(nameof(DamageInfo.damageType)),
                x => x.MatchLdcI4((int)DamageType.BlightOnHit)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<RoR2.DamageInfo>(nameof(DamageInfo.procCoefficient))
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, blightDuration);
        }
    }
}
