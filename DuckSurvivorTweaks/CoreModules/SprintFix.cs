using DuckSurvivorTweaks.SurvivorTweaks;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Bindings;

namespace DuckSurvivorTweaks.CoreModules
{
    class SprintFix : CoreModule
    {
        public override void Init()
        {
            return;
            On.RoR2.Skills.SkillDef.OnFixedUpdate += SkillDefFixedUpdate;

            SkillDef acridPoisonSkill = LegacyResourcesAPI.Load<SkillDef>("skilldefs/crocobody/CrocoPassivePoison");
            SkillDef acridBlightSkill = LegacyResourcesAPI.Load<SkillDef>("skilldefs/crocobody/CrocoPassiveBlight");
            acridPoisonSkill.cancelSprintingOnActivation = false;
            acridBlightSkill.cancelSprintingOnActivation = false;
        }

        private void SkillDefFixedUpdate(On.RoR2.Skills.SkillDef.orig_OnFixedUpdate orig, RoR2.Skills.SkillDef self, [NotNull] GenericSkill skillSlot)
        {
            if (skillSlot.stateMachine == null)
            {
                orig(self, skillSlot);
                return;
            }

            skillSlot.RunRecharge(Time.fixedDeltaTime);
            if (skillSlot.stateMachine.state?.GetType() == self.activationState.stateType)
            {
                if (self.canceledFromSprinting && skillSlot.characterBody.isSprinting)
                {
                    skillSlot.stateMachine.SetNextStateToMain();
                }
                else
                {
                    if (self.forceSprintDuringState)
                    {
                        skillSlot.characterBody.isSprinting = true;
                    }
                    else if (self.cancelSprintingOnActivation)
                    {
                        skillSlot.characterBody.isSprinting = false;
                    }
                }
            }
        }
    }
}
