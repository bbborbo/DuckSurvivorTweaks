using EntityStates;
using EntityStates.Huntress;
using EntityStates.Huntress.HuntressWeapon;
using EntityStates.Huntress.Weapon;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DuckSurvivorTweaks.SurvivorTweaks
{
    class HuntressTweaks : SurvivorTweakModule
    {
        public static bool isLoaded;
        public static GameObject arrowRainPrefab;

        public override string survivorName => "Huntress";

        public override string bodyName => "HuntressBody";

        static float glaiveBaseDamage = 3.3f;
        static float glaiveBounceDamage = 1.2f;


        static int arrowRainCooldown = 22;
        static float arrowRainRadius = 12;
        static float arrowRainProcCoeff = 0.3f; //0.2f
        static float arrowRainDamageCoeff = 0.5f; //0.5f
        static float arrowRainHitFrequency = 4f; //3f
        static float arrowRainLifetime = 8f; //6f

        static int ballistaCooldown = 22;
        static float ballistaDamageCoefficient = 12f;

        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            CharacterBody body = bodyObject.GetComponent<CharacterBody>();

            ChangeVanillaPrimary(primary);
            ChangeVanillaSecondaries(secondary);
            ChangeVanillaUtilities(utility);
            ChangeVanillaSpecials(special);
        }

        private void ChangeVanillaPrimary(SkillFamily family)
        {
        }

        private void ChangeVanillaSecondaries(SkillFamily family)
        {
            LanguageAPI.Add("HUNTRESS_SECONDARY_DESCRIPTION", $"Throw a seeking glaive that bounces " +
                $"up to <style=cIsDamage>6</style> times " +
                $"for <style=cIsDamage>{Tools.ConvertDecimal(glaiveBaseDamage)} damage</style>. " +
                $"Damage increases by <style=cIsDamage>{Tools.ConvertDecimal(glaiveBounceDamage - 1)}</style> per bounce.");
            On.EntityStates.Huntress.HuntressWeapon.ThrowGlaive.OnEnter += BuffGlaive;
        }

        private void BuffGlaive(On.EntityStates.Huntress.HuntressWeapon.ThrowGlaive.orig_OnEnter orig, ThrowGlaive self)
        {
            ThrowGlaive.damageCoefficient = glaiveBaseDamage;
            ThrowGlaive.damageCoefficientPerBounce = glaiveBounceDamage;
            orig(self);
        }

        private void ChangeVanillaUtilities(SkillFamily family)
        { 
        }

        void ChangeVanillaSpecials(SkillFamily family)
        {
            arrowRainPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Huntress/HuntressArrowRain.prefab").WaitForCompletion();
            family.variants[0].skillDef.baseRechargeInterval = arrowRainCooldown;
            ArrowRain.arrowRainRadius = arrowRainRadius;
            On.EntityStates.Huntress.ArrowRain.OnEnter += BuffArrowRain;

            arrowRainPrefab.transform.localScale = Vector3.one * 2 * arrowRainRadius;
            ProjectileDotZone arrowRainDotZone = arrowRainPrefab?.GetComponent<ProjectileDotZone>();
            if(arrowRainDotZone != null)
            {
                arrowRainDotZone.damageCoefficient = arrowRainDamageCoeff;
                arrowRainDotZone.resetFrequency = arrowRainHitFrequency;
                LanguageAPI.Add("HUNTRESS_SPECIAL_DESCRIPTION", $"<style=cIsUtility>Teleport</style> into the sky. " +
                    $"Target an area to rain arrows, <style=cIsUtility>slowing</style> all enemies and " +
                    $"dealing <style=cIsDamage>{Tools.ConvertDecimal(2.2f * arrowRainDamageCoeff * arrowRainHitFrequency)} damage per second</style>.");
                arrowRainDotZone.overlapProcCoefficient = arrowRainProcCoeff;
                arrowRainDotZone.lifetime = arrowRainLifetime;
            }

            family.variants[1].skillDef.baseRechargeInterval = ballistaCooldown;
            On.EntityStates.GenericBulletBaseState.OnEnter += BallistaBuff;
            LanguageAPI.Add("HUNTRESS_SPECIAL_ALT1_DESCRIPTION", $"<style=cIsUtility>Teleport</style> backwards into the sky. " +
                $"Fire up to <style=cIsDamage>3</style> energy bolts, " +
                $"dealing <style=cIsDamage>3x{Tools.ConvertDecimal(ballistaDamageCoefficient)} damage</style>.");
        }

        private void BallistaBuff(On.EntityStates.GenericBulletBaseState.orig_OnEnter orig, EntityStates.GenericBulletBaseState self)
        {
            if(self is FireArrowSnipe)
            {
                self.damageCoefficient = ballistaDamageCoefficient;
            }
            orig(self);
        }

        private void BuffArrowRain(On.EntityStates.Huntress.ArrowRain.orig_OnEnter orig, EntityStates.Huntress.ArrowRain self)
        {
            ArrowRain.arrowRainRadius = arrowRainRadius;
            orig(self);
        }
    }
}
