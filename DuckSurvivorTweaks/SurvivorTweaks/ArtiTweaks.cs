using DuckSurvivorTweaks.CoreModules;
using EntityStates.Mage;
using EntityStates.Mage.Weapon;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace DuckSurvivorTweaks.SurvivorTweaks
{
    class ArtiTweaks : SurvivorTweakModule
    {
        public override string survivorName => "Artificer";

        public override string bodyName => "MageBody";

        public static string flamethrowerDesc;
        public static BuffDef jetpackSpeedBoost;
        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            CharacterBody mageBody = bodyObject.GetComponent<CharacterBody>();
            if (mageBody != null)
            {
                float mageDamage = 13; //12
                mageBody.baseDamage = mageDamage;
                mageBody.levelDamage = mageDamage * 0.2f;
            }

            #region Hover
            jetpackSpeedBoost = ScriptableObject.CreateInstance<BuffDef>();
            {
                jetpackSpeedBoost.buffColor = new Color(220, 100, 100);
                jetpackSpeedBoost.canStack = false;
                jetpackSpeedBoost.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texMovespeedBuffIcon");
                jetpackSpeedBoost.isDebuff = false;
                jetpackSpeedBoost.name = "MageJetpackSpeedBoost";
            }
            Assets.buffDefs.Add(jetpackSpeedBoost);

            GetStatCoefficients += JetpackSpeedBoost;

            On.EntityStates.Mage.JetpackOn.OnEnter += (orig, self) =>
            {
                JetpackOn.hoverVelocity = -2f;
                if (self.isAuthority)
                {
                    self.characterBody.AddBuff(jetpackSpeedBoost);
                }
                orig(self);
            };
            On.EntityStates.Mage.JetpackOn.OnExit += (orig, self) =>
            {
                if (self.isAuthority)
                {
                    self.characterBody.RemoveBuff(jetpackSpeedBoost);
                }
                orig(self);
            };

            LanguageAPI.Add("MAGE_PASSIVE_DESCRIPTION",
                "Holding the Jump key causes the Artificer to <style=cIsUtility>hover in the air</style>. Move faster while hovering.");
            #endregion

            #region Nanobomb
            On.EntityStates.Mage.Weapon.BaseThrowBombState.OnEnter += (orig, self) =>
            {
                bool isBomb = self is ThrowNovabomb;
                if (isBomb)
                {
                    self.maxDamageCoefficient = 12f;
                }
                orig(self);
            };

            LanguageAPI.Add("MAGE_SECONDARY_LIGHTNING_DESCRIPTION",
                "<style=cIsDamage>Stunning</style>. Charge up an <style=cIsDamage>exploding</style> nano-bomb that deals <style=cIsDamage>400%-1200%</style> damage.");
            #endregion

            #region Snapfreeze
            SkillDef snapfreeze = utility.variants[0].skillDef;
            snapfreeze.baseRechargeInterval = 8f;

            GameObject iceWallPillarPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/MageIcewallPillarProjectile");
            Collider collider = iceWallPillarPrefab.GetComponentInChildren<Collider>();
            if (collider)
            {
                Debug.Log("shit");
                collider.transform.localScale = Vector3.one * 2.5f;
                ProjectileImpactExplosion pie = iceWallPillarPrefab.GetComponentInChildren<ProjectileImpactExplosion>();
                pie.blastRadius = 4f;
            }

            GameObject iceWallWalkerPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/MageIcewallWalkerProjectile");
            ProjectileMageFirewallWalkerController pmfwc = iceWallWalkerPrefab.GetComponent<ProjectileMageFirewallWalkerController>();
            if (pmfwc)
            {
                //pmfwc.curveToCenter = true;
            }

            ProjectileCharacterController pcc = iceWallWalkerPrefab.GetComponent<ProjectileCharacterController>();
            if (pcc)
            {
                pcc.velocity = 45f;
            }
            #endregion

            #region Flamethrower
            On.EntityStates.Mage.Weapon.Flamethrower.OnEnter += (orig, self) =>
            {
                Flamethrower.baseFlamethrowerDuration = 3;
                Flamethrower.tickFrequency = 7;
                Flamethrower.totalDamageCoefficient = 16.23f;
                Flamethrower.procCoefficientPerTick = 0.8f;

                orig(self);
                float aspd = self.attackSpeedStat;
                float aspdSqrt = Mathf.Sqrt(aspd);

                if (aspd != 0)
                {
                    float damageCoeff = Flamethrower.totalDamageCoefficient * aspdSqrt;
                    float endDuration = Flamethrower.baseFlamethrowerDuration / aspdSqrt;

                    //total ticks increases by aspdSqrt, end duration
                    float totalTicks = Flamethrower.baseFlamethrowerDuration * Flamethrower.tickFrequency * aspdSqrt;

                    //self.flamethrowerDuration = endDuration;
                    //self.tickDamageCoefficient = (damageCoeff / totalTicks);
                    Flamethrower.tickFrequency *= aspdSqrt;
                }
            };
            flamethrowerDesc = "Burn all enemies in front of you for <style=cIsDamage>1700% damage</style>. " +
                "Each hit has a <style=cIsDamage>50% chance to ignite</style>.";
            LanguageAPI.Add("MAGE_SPECIAL_FIRE_DESCRIPTION",
                flamethrowerDesc);
            #endregion
        }

        private void JetpackSpeedBoost(CharacterBody sender, StatHookEventArgs args)
        {

            if (sender.HasBuff(jetpackSpeedBoost))
            {
                args.moveSpeedMultAdd += 0.15f;
            }
        }
    }
}
