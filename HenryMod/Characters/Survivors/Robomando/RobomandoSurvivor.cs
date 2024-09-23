﻿using BepInEx.Configuration;
using HenryMod.Characters.Survivors.Robomando.Content;
using RobomandoMod.Modules;
using RobomandoMod.Modules.Characters;
using RobomandoMod.Survivors.Robomando.Components;
using RobomandoMod.Survivors.Robomando.SkillStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RobomandoMod.Survivors.Robomando
{
    public class RobomandoSurvivor : SurvivorBase<RobomandoSurvivor>
    {
        //used to load the assetbundle for this character. must be unique
        public override string assetBundleName => "robomandobundle"; //if you do not change this, you are giving permission to deprecate the mod

        //the name of the prefab we will create. conventionally ending in "Body". must be unique
        public override string bodyName => "RobomandoBody"; //if you do not change this, you get the point by now

        //name of the ai master for vengeance and goobo. must be unique
        public override string masterName => "RobomandoMonsterMaster"; //if you do not

        //the names of the prefabs you set up in unity that we will use to build your character
        public override string modelPrefabName => "mdlRobomando";
        public override string displayPrefabName => "mdlRobomandoDisplay";

        public const string HENRY_PREFIX = RobomandoPlugin.DEVELOPER_PREFIX + "_ROBOMANDO_";

        public static Transform gunTransform;

        //used when registering your survivor's language tokens
        public override string survivorTokenPrefix => HENRY_PREFIX;
        
        public override BodyInfo bodyInfo => new BodyInfo
        {
            bodyName = bodyName,
            bodyNameToken = HENRY_PREFIX + "NAME",
            subtitleNameToken = HENRY_PREFIX + "SUBTITLE",

            characterPortrait = assetBundle.LoadAsset<Texture>("texRobomandoIcon"),
            bodyColor = Color.white,
            sortPosition = 100,

            crosshair = Asset.LoadCrosshair("Standard"),
            podPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/SurvivorPod"),

            maxHealth = 75f,
            healthRegen = 1.5f,
            armor = 0f,
            moveSpeed = 8.2f,
            jumpCount = 1,
        };

        public override CustomRendererInfo[] customRendererInfos => new CustomRendererInfo[]
        {
            new CustomRendererInfo
                {
                    childName = "Guy",
                },
                new CustomRendererInfo
                {
                    childName = "Gun",
                    material = assetBundle.LoadMaterial("GunMaterial"),
                }
        };

        public override UnlockableDef characterUnlockableDef => RobomandoUnlockables.characterUnlockableDef;
        
        public override ItemDisplaysBase itemDisplays => new RobomandoItemDisplays();

        //set in base classes
        public override AssetBundle assetBundle { get; protected set; }

        public override GameObject bodyPrefab { get; protected set; }
        public override CharacterBody prefabCharacterBody { get; protected set; }
        public override GameObject characterModelObject { get; protected set; }
        public override CharacterModel prefabCharacterModel { get; protected set; }
        public override GameObject displayPrefab { get; protected set; }

        public override void Initialize()
        {
            //uncomment if you have multiple characters
            //ConfigEntry<bool> characterEnabled = Config.CharacterEnableConfig("Survivors", "Robomando");

            //if (!characterEnabled.Value)
            //    return;
            base.Initialize();
        }

        public override void InitializeCharacter()
        {
            //need the character unlockable before you initialize the survivordef
            RobomandoUnlockables.Init();

            base.InitializeCharacter();

            RobomandoConfig.Init();
            RobomandoStates.Init();
            RobomandoTokens.Init();

            RobomandoAssets.Init(assetBundle);
            RobomandoBuffs.Init(assetBundle);

            InitializeEntityStateMachines();
            InitializeSkills();
            InitializeSkins();
            InitializeCharacterMaster();

            AdditionalBodySetup();

            AddHooks();

            gunTransform = characterModelObject.GetComponent<ChildLocator>().FindChild("Gun");
        }

        protected override void InitializeDisplayPrefab()
        {
            displayPrefab = Prefabs.CreateDisplayPrefab(assetBundle, displayPrefabName, bodyPrefab);
            displayPrefab.AddComponent<SoundAnimationEvent>();
        }

        private void AdditionalBodySetup()
        {
            AddHitboxes();
            bodyPrefab.AddComponent<RobomandoWeaponComponent>();
            //bodyPrefab.AddComponent<HuntressTrackerComopnent>();
            //anything else here
        }

        public void AddHitboxes()
        {
            //example of how to create a HitBoxGroup. see summary for more details
            //Prefabs.SetupHitBoxGroup(characterModelObject, "SwordGroup", "SwordHitbox");
        }

        public override void InitializeEntityStateMachines() 
        {
            //clear existing state machines from your cloned body (probably commando)
            //omit all this if you want to just keep theirs
            Prefabs.ClearEntityStateMachines(bodyPrefab);

            //the main "Body" state machine has some special properties
            Prefabs.AddMainEntityStateMachine(bodyPrefab, "Body", typeof(EntityStates.GenericCharacterMain), typeof(EntityStates.SpawnTeleporterState));
            //if you set up a custom main characterstate, set it up here
                //don't forget to register custom entitystates in your RobomandoStates.cs

            Prefabs.AddEntityStateMachine(bodyPrefab, "Weapon");
            //Prefabs.AddEntityStateMachine(bodyPrefab, "Weapon2");
        }

        #region skills
        public override void InitializeSkills()
        {
            //remove the genericskills from the commando body we cloned
            Skills.ClearGenericSkills(bodyPrefab);
            //add our own
            //AddPassiveSkill();
            AddPrimarySkills();
            AddSecondarySkills();
            AddUtiitySkills();
            AddSpecialSkills();
        }

        //skip if you don't have a passive
        //also skip if this is your first look at skills
        private void AddPassiveSkill()
        {
            /*
            //option 1. fake passive icon just to describe functionality we will implement elsewhere
            bodyPrefab.GetComponent<SkillLocator>().passiveSkill = new SkillLocator.PassiveSkill
            {
                enabled = true,
                skillNameToken = HENRY_PREFIX + "PASSIVE_NAME",
                skillDescriptionToken = HENRY_PREFIX + "PASSIVE_DESCRIPTION",
                keywordToken = "KEYWORD_STUNNING",
                icon = assetBundle.LoadAsset<Sprite>("texPassiveIcon"),
            };

            //option 2. a new SkillFamily for a passive, used if you want multiple selectable passives
            GenericSkill passiveGenericSkill = Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, "PassiveSkill");
            SkillDef passiveSkillDef1 = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "RobomandoPassive",
                skillNameToken = HENRY_PREFIX + "PASSIVE_NAME",
                skillDescriptionToken = HENRY_PREFIX + "PASSIVE_DESCRIPTION",
                keywordTokens = new string[] { "KEYWORD_AGILE" },
                skillIcon = assetBundle.LoadAsset<Sprite>("texPassiveIcon"),

                //unless you're somehow activating your passive like a skill, none of the following is needed.
                //but that's just me saying things. the tools are here at your disposal to do whatever you like with

                //activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.Shoot)),
                //activationStateMachineName = "Weapon1",
                //interruptPriority = EntityStates.InterruptPriority.Skill,

                //baseRechargeInterval = 1f,
                //baseMaxStock = 1,

                //rechargeStock = 1,
                //requiredStock = 1,
                //stockToConsume = 1,

                //resetCooldownTimerOnUse = false,
                //fullRestockOnAssign = true,
                //dontAllowPastMaxStocks = false,
                //mustKeyPress = false,
                //beginSkillCooldownOnSkillEnd = false,

                //isCombatSkill = true,
                //canceledFromSprinting = false,
                //cancelSprintingOnActivation = false,
                //forceSprintDuringState = false,
                

            });
            Skills.AddSkillsToFamily(passiveGenericSkill.skillFamily, passiveSkillDef1);
            */
        }

        //if this is your first look at skilldef creation, take a look at Secondary first
        private void AddPrimarySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Primary);

            //the primary skill is created using a constructor for a typical primary
            //it is also a SteppedSkillDef. Custom Skilldefs are very useful for custom behaviors related to casting a skill. see ror2's different skilldefs for reference
            SteppedSkillDef primarySkillDef1 = Skills.CreateSkillDef<SteppedSkillDef>(new SkillDefInfo
                (
                    "RobomandoShoot",
                    HENRY_PREFIX + "PRIMARY_SHOT_NAME",
                    HENRY_PREFIX + "PRIMARY_SHOT_DESCRIPTION",
                    assetBundle.LoadAsset<Sprite>("texShootIcon"),
                    new EntityStates.SerializableEntityStateType(typeof(SkillStates.Shoot)),
                    "Weapon",
                    true
                ));
            //custom Skilldefs can have additional fields that you can set manually
            primarySkillDef1.stepCount = 2;
            primarySkillDef1.stepGraceDuration = 0.5f;

            Skills.AddPrimarySkills(bodyPrefab, primarySkillDef1);
        }

        private void AddSecondarySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Secondary);

            //here is a basic skill def with all fields accounted for
            SkillDef secondarySkillDef1 = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "RobomandoZap",
                skillNameToken = HENRY_PREFIX + "SECONDARY_ZAP_NAME",
                skillDescriptionToken = HENRY_PREFIX + "SECONDARY_ZAP_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texZapIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.Zap)),
                activationStateMachineName = "Weapon",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseRechargeInterval = 2f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = false,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = true,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
                
            });

            Skills.AddSecondarySkills(bodyPrefab, secondarySkillDef1);
        }

        private void AddUtiitySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Utility);

            //here's a skilldef of a typical movement skill.
            SkillDef utilitySkillDef1 = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "RobomandoRoll",
                skillNameToken = HENRY_PREFIX + "UTILITY_ROLL_NAME",
                skillDescriptionToken = HENRY_PREFIX + "UTILITY_ROLL_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texRollIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(Roll)),
                activationStateMachineName = "Body",
                interruptPriority = EntityStates.InterruptPriority.PrioritySkill,

                baseRechargeInterval = 4f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = false,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = false,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = true,
            });

            Skills.AddUtilitySkills(bodyPrefab, utilitySkillDef1);
        }

        private void AddSpecialSkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Special);

            //a basic skill. some fields are omitted and will just have default values
            SkillDef specialSkillDef1 = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "RobomandoHack",
                skillNameToken = HENRY_PREFIX + "SPECIAL_HACK_NAME",
                skillDescriptionToken = HENRY_PREFIX + "SPECIAL_HACK_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texHackIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.Hack)),
                //setting this to the "weapon2" EntityStateMachine allows us to cast this skill at the same time primary, which is set to the "weapon" EntityStateMachine
                activationStateMachineName = "Body", interruptPriority = EntityStates.InterruptPriority.Skill,

                baseMaxStock = 1,
                baseRechargeInterval = 10f,
                beginSkillCooldownOnSkillEnd = true,
                mustKeyPress = false,
            });

            Skills.AddSpecialSkills(bodyPrefab, specialSkillDef1);
        }
        #endregion skills
        
        #region skins
        public override void InitializeSkins()
        {
            ModelSkinController skinController = prefabCharacterModel.gameObject.AddComponent<ModelSkinController>();
            ChildLocator childLocator = prefabCharacterModel.GetComponent<ChildLocator>();

            CharacterModel.RendererInfo[] defaultRendererinfos = prefabCharacterModel.baseRendererInfos;

            List<SkinDef> skins = new List<SkinDef>();

            #region DefaultSkin
            //this creates a SkinDef with all default fields
            SkinDef defaultSkin = Skins.CreateSkinDef("DEFAULT_SKIN",
                assetBundle.LoadAsset<Sprite>("texMainSkin"),
                defaultRendererinfos,
                prefabCharacterModel.gameObject);

            //these are your Mesh Replacements. The order here is based on your CustomRendererInfos from earlier
                //pass in meshes as they are named in your assetbundle
            //currently not needed as with only 1 skin they will simply take the default meshes
                //uncomment this when you have another skin
            //defaultSkin.meshReplacements = Modules.Skins.getMeshReplacements(assetBundle, defaultRendererinfos,
            //    "meshRobomandoSword",
            //    "meshRobomandoGun",
            //    "meshRobomando");

            //add new skindef to our list of skindefs. this is what we'll be passing to the SkinController
            skins.Add(defaultSkin);
            SkinDef commandoSkin = Skins.CreateSkinDef(HENRY_PREFIX + "COMMANDO_SKIN_NAME",
                assetBundle.LoadAsset<Sprite>("texCommandoSkin"),
                defaultRendererinfos,
                prefabCharacterModel.gameObject);
            commandoSkin.rendererInfos[0].defaultMaterial = assetBundle.LoadMaterial("RobomandoCommandoMat");
            //commandoSkin.rendererInfos[1].defaultMaterial = assetBundle.LoadMaterial("RobomandoCommandoMat");
            //commandoSkin.rendererInfos[2].defaultMaterial = assetBundle.LoadMaterial("RobomandoCommandoMat");
            skins.Add(commandoSkin);
            SkinDef BlueSkin = Skins.CreateSkinDef(HENRY_PREFIX + "BLUE_SKIN_NAME",
                assetBundle.LoadAsset<Sprite>("texBlueSkin"),
                defaultRendererinfos,
                prefabCharacterModel.gameObject);
            BlueSkin.rendererInfos[0].defaultMaterial = assetBundle.LoadMaterial("RobomandoBlueMat");
            //BlueSkin.rendererInfos[1].defaultMaterial = assetBundle.LoadMaterial("RobomandoBlueMat");
            //BlueSkin.rendererInfos[2].defaultMaterial = assetBundle.LoadMaterial("RobomandoBlueMat");
            skins.Add(BlueSkin);
            SkinDef GreenSkin = Skins.CreateSkinDef(HENRY_PREFIX + "GREEN_SKIN_NAME",
                assetBundle.LoadAsset<Sprite>("texGreenSkin"),
                defaultRendererinfos,
                prefabCharacterModel.gameObject);
            GreenSkin.rendererInfos[0].defaultMaterial = assetBundle.LoadMaterial("RobomandoGreenMat");
            //GreenSkin.rendererInfos[1].defaultMaterial = assetBundle.LoadMaterial("RobomandoGreenMat");
            //GreenSkin.rendererInfos[2].defaultMaterial = assetBundle.LoadMaterial("RobomandoGreenMat");
            skins.Add(GreenSkin);
            SkinDef masterySkin = Modules.Skins.CreateSkinDef(HENRY_PREFIX + "MASTERY_SKIN_NAME",
                assetBundle.LoadAsset<Sprite>("texProvidenceSkin"),
                defaultRendererinfos,
                prefabCharacterModel.gameObject,
                RobomandoUnlockables.masterySkinUnlockableDef);
            masterySkin.rendererInfos[0].defaultMaterial = assetBundle.LoadMaterial("RobomandoProvidenceMat");
            //masterySkin.rendererInfos[1].defaultMaterial = assetBundle.LoadMaterial("RobomandoProvidenceMat");
            //masterySkin.rendererInfos[2].defaultMaterial = assetBundle.LoadMaterial("RobomandoProvidenceMat");
            skins.Add(masterySkin);
            #endregion

            //uncomment this when you have a mastery skin
            #region MasterySkin

            ////creating a new skindef as we did before
            //

            ////adding the mesh replacements as above. 
            ////if you don't want to replace the mesh (for example, you only want to replace the material), pass in null so the order is preserved
            //masterySkin.meshReplacements = Modules.Skins.getMeshReplacements(assetBundle, defaultRendererinfos,
            //    "meshRobomandoSwordAlt",
            //    null,//no gun mesh replacement. use same gun mesh
            //    "meshRobomandoAlt");

            ////masterySkin has a new set of RendererInfos (based on default rendererinfos)
            ////you can simply access the RendererInfos' materials and set them to the new materials for your skin.
            //masterySkin.rendererInfos[0].defaultMaterial = assetBundle.LoadMaterial("matRobomandoAlt");
            //masterySkin.rendererInfos[1].defaultMaterial = assetBundle.LoadMaterial("matRobomandoAlt");
            //masterySkin.rendererInfos[2].defaultMaterial = assetBundle.LoadMaterial("matRobomandoAlt");

            ////here's a barebones example of using gameobjectactivations that could probably be streamlined or rewritten entirely, truthfully, but it works
            //masterySkin.gameObjectActivations = new SkinDef.GameObjectActivation[]
            //{
            //    new SkinDef.GameObjectActivation
            //    {
            //        gameObject = childLocator.FindChildGameObject("GunModel"),
            //        shouldActivate = false,
            //    }
            //};
            ////simply find an object on your child locator you want to activate/deactivate and set if you want to activate/deacitvate it with this skin

            //skins.Add(masterySkin);

            #endregion

            skinController.skins = skins.ToArray();
        }
        #endregion skins

        //Character Master is what governs the AI of your character when it is not controlled by a player (artifact of vengeance, goobo)
        public override void InitializeCharacterMaster()
        {
            //you must only do one of these. adding duplicate masters breaks the game.

            //if you're lazy or prototyping you can simply copy the AI of a different character to be used
            Modules.Prefabs.CloneDopplegangerMaster(bodyPrefab, masterName, "Merc");

            //how to set up AI in code
            //RobomandoAI.Init(bodyPrefab, masterName);

            //how to load a master set up in unity, can be an empty gameobject with just AISkillDriver components
            //assetBundle.LoadMaster(bodyPrefab, masterName);
        }

        private void AddHooks()
        {
            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            RoR2.GlobalEventManager.onCharacterDeathGlobal += PlayFunnyDeathSounds;
        }

        private void PlayFunnyDeathSounds(DamageReport report)
        {
            //report.victimBody.baseNameToken
            //RoR2.Chat.SendBroadcastChat(new RoR2.Chat.SimpleChatMessage() { baseToken = $"<style=cEvent><color=#307FFF>Victim Name: {report.victimBody.baseNameToken}</color></style>" });
            if (report.victimBody.baseNameToken.Equals("ROB_ROBOMANDO_NAME"))
            {
                Util.PlaySound("LegoDeathSound", report.victimBody.gameObject);
                Util.PlaySound("DeathVoice", report.victimBody.gameObject);
            }
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args)
        {

            if (sender.HasBuff(RobomandoBuffs.armorBuff))
            {
                args.armorAdd += 300;
            }
        }
    }
}