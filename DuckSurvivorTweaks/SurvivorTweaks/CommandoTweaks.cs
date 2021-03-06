using EntityStates.Commando.CommandoWeapon;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DuckSurvivorTweaks.SurvivorTweaks
{
    class CommandoTweaks : SurvivorTweakModule
    {
        public static GameObject phaseRoundPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/FMJ");
        public static float phaseRoundDamageCoeff = 5f; //3
        public static float phaseRoundCooldown = 5f; //3
        public static float phaseRoundDuration = 1f; //0.5

        public static float phaseBlastDamageCoeff = 2.5f; //2f
        public static float phaseBlastCooldown = 5; //3f

        public static int rollStock = 2; //1
        public static float rollCooldown = 4f; //4f
        public static float rollDuration = 0.25f; //0.4f
        public static float slideCooldown = 5f; //4f

        public static float soupDamageCoeff = 1.2f; //1f
        public static float soupCooldown = 8f; //9f

        public override string survivorName => "Commando";

        public override string bodyName => "CommandoBody";

        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            ChangeSecondaries(secondary);

            //roll
            utility.variants[0].skillDef.baseMaxStock = rollStock;
            utility.variants[0].skillDef.baseRechargeInterval = rollCooldown;
            On.EntityStates.Commando.DodgeState.OnEnter += DodgeBuff;
            LanguageAPI.Add("COMMANDO_UTILITY_DESCRIPTION", "<style=cIsUtility>Roll</style> a short distance. Has <style=cIsUtility>2</style> charges.");

            //slide
            utility.variants[1].skillDef.baseRechargeInterval = slideCooldown;

            On.EntityStates.Commando.CommandoWeapon.FireBarrage.OnEnter += SoupBuff;
            special.variants[0].skillDef.baseRechargeInterval = soupCooldown;
            LanguageAPI.Add("COMMANDO_SPECIAL_DESCRIPTION", $"<style=cIsDamage>Stunning</style>. " +
                $"Fire repeatedly for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(soupDamageCoeff)} damage</style> per bullet. " +
                $"The number of shots increases with attack speed.");
        }


        #region primary
        private void ChangeSecondaries(SkillFamily secondary)
        {
            //phase round
            phaseRoundPrefab.transform.localScale *= 2;
            On.EntityStates.GenericProjectileBaseState.OnEnter += PhaseRoundBuff;
            secondary.variants[0].skillDef.baseRechargeInterval = phaseRoundCooldown;
            LanguageAPI.Add("COMMANDO_SECONDARY_DESCRIPTION", 
                $"Fire a <style=cIsDamage>piercing</style> bullet for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(phaseRoundDamageCoeff)} damage </style>. " +
                $"Deals <style=cIsDamage>40%</style> more damage every time it passes through an enemy.");

            //phase blast
            On.EntityStates.GenericBulletBaseState.OnEnter += PhaseBlastBuff;
            secondary.variants[1].skillDef.baseRechargeInterval = phaseBlastCooldown;
            LanguageAPI.Add("COMMANDO_SECONDARY_ALT1_DESCRIPTION",
                $"Fire two close-range blasts that deal " +
                $"<style=cIsDamage>8x{Tools.ConvertDecimal(phaseBlastDamageCoeff)} damage</style> total.");
        }

        private void PhaseRoundBuff(On.EntityStates.GenericProjectileBaseState.orig_OnEnter orig, EntityStates.GenericProjectileBaseState self)
        {
            if(self is FireFMJ)
            {
                self.damageCoefficient = phaseRoundDamageCoeff;
                self.baseDuration = phaseRoundDuration;
            }
            orig(self);
        }
        private void PhaseBlastBuff(On.EntityStates.GenericBulletBaseState.orig_OnEnter orig, EntityStates.GenericBulletBaseState self)
        {
            if(self is FireShotgunBlast)
            {
                self.damageCoefficient = phaseBlastDamageCoeff;
            }
            orig(self);
        }
        #endregion

        private void DodgeBuff(On.EntityStates.Commando.DodgeState.orig_OnEnter orig, EntityStates.Commando.DodgeState self)
        {
            self.duration = rollDuration;
            self.initialSpeedCoefficient = 10f; //5
            self.finalSpeedCoefficient = 3f; //2.5
            orig(self);
        }

        private void SoupBuff(On.EntityStates.Commando.CommandoWeapon.FireBarrage.orig_OnEnter orig, FireBarrage self)
        {
            FireBarrage.damageCoefficient = soupDamageCoeff;
            orig(self);
        }
    }
}
