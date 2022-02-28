using BepInEx;
using BepInEx.Configuration;
using DuckSurvivorTweaks.CoreModules;
using DuckSurvivorTweaks.SurvivorTweaks;
using EntityStates.Treebot.Weapon;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using EntityStates;
using System.Collections.Generic;
using DuckSurvivorTweaks.Skills;

using System.Security;
using System.Security.Permissions;
using System.Runtime.CompilerServices;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace DuckSurvivorTweaks
{
    [BepInDependency("com.bepis.r2api")]

    [BepInDependency("com.Borbo.HuntressBuffULTIMATE", BepInDependency.DependencyFlags.SoftDependency)]

    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.johnedwa.RTAutoSprintEx", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Withor.AcridBiteLunge", BepInDependency.DependencyFlags.SoftDependency)]

    [BepInPlugin(
        "com.Borbo.DuckSurvivorTweaks",
        "DuckSurvivorTweaks",
        "2.7.0")]

    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(LoadoutAPI), 
        nameof(RecalculateStatsAPI), nameof(PrefabAPI))]

    public class Main : BaseUnityPlugin
    {
        public static AssetBundle iconBundle = Tools.LoadAssetBundle(Properties.Resources.dsticons);
        public static string iconsPath = "Assets/DSTicons/";
        public static string TokenName = "SURVIVORTWEAKS_";

        public static bool isScepterLoaded = Tools.isLoaded("com.DestroyedClone.AncientScepter");
        public static bool autosprintLoaded = Tools.isLoaded("com.johnedwa.RTAutoSprintEx");
        public static bool acridLungeLoaded = Tools.isLoaded("Withor.AcridBiteLunge");
        public static ConfigFile CustomConfigFile { get; set; }

        void Awake()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + "\\DuckSurvivorTweaks.cfg", true);


            InitializeCoreModules();
            InitializeSkills();
            InitializeTweaks();
            new ContentPacks().Initialize();
        }

        void InitializeCoreModules()
        {
            var CoreModuleTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(CoreModule)));

            foreach (var coreModuleType in CoreModuleTypes)
            {
                CoreModule coreModule = (CoreModule)Activator.CreateInstance(coreModuleType);

                coreModule.Init();
            }
        }

        void InitializeTweaks()
        {
            var TweakTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(SurvivorTweakModule)));

            foreach (var tweakType in TweakTypes)
            {
                SurvivorTweakModule module = (SurvivorTweakModule)Activator.CreateInstance(tweakType);

                string name = module.survivorName == "" ? module.bodyName : module.survivorName;
                bool isEnabled = CustomConfigFile.Bind<bool>("Survivor Tweaks", 
                    $"Enable Tweaks For: {module.survivorName}", true, 
                    $"Should DuckSurvivorTweaks change {module.survivorName}?").Value;
                if (isEnabled)
                {
                    module.Init();
                }
                //TweakStatusDictionary.Add(module.ToString(), isEnabled);
            }
        }


        #region skills
        public static List<Type> entityStates = new List<Type>();
        public static List<SkillBase> Skills = new List<SkillBase>();
        public static List<ScepterSkillBase> ScepterSkills = new List<ScepterSkillBase>();
        public static Dictionary<SkillBase, bool> SkillStatusDictionary = new Dictionary<SkillBase, bool>();

        private void InitializeSkills()
        {
            var SkillTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(SkillBase)));

            foreach (var skillType in SkillTypes)
            {
                SkillBase skill = (SkillBase)System.Activator.CreateInstance(skillType);

                if (ValidateSkill(skill))
                {
                    skill.Init(CustomConfigFile);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void InitializeScepterSkills()
        {
            var SkillTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ScepterSkillBase)));

            foreach (var skillType in SkillTypes)
            {
                ScepterSkillBase skill = (ScepterSkillBase)System.Activator.CreateInstance(skillType);

                if (ValidateScepterSkill(skill))
                {
                    skill.Init(CustomConfigFile);
                }
            }
        }

        bool ValidateSkill(SkillBase item)
        {
            var forceUnlock = true;

            if (forceUnlock)
            {
                Skills.Add(item);
            }
            SkillStatusDictionary.Add(item, forceUnlock);

            return forceUnlock;
        }

        bool ValidateScepterSkill(ScepterSkillBase item)
        {
            var forceUnlock = isScepterLoaded;

            if (forceUnlock)
            {
                ScepterSkills.Add(item);
            }

            return forceUnlock;
        }
        #endregion
    }
}