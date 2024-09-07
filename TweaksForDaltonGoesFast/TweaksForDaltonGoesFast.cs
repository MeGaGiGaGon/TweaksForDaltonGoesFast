using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RoR2.GenericPickupController;

[assembly: HG.Reflection.SearchableAttribute.OptInAttribute]

namespace TweaksForDaltonGoesFast
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class TweaksForDaltonGoesFast : BaseUnityPlugin
    {
        public static TweaksForDaltonGoesFast instance;

        private static Harmony test;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "GiGaGon";
        public const string PluginName = "TweaksForDaltonGoesFast";
        public const string PluginVersion = "1.0.6"; 

        internal class ModConfig
        {
            public static ConfigEntry<bool> blockPortalInteraction;
            public static ConfigEntry<bool> blockSeerInteraction;
            public static ConfigEntry<bool> blockTeleporterInteraction;
            public static ConfigEntry<bool> blockSafeWardInteraction;
            public static ConfigEntry<string> trustedPlayerList;
            public static ConfigEntry<float> simulacrumRewardVectorMultiplyer;
            public static ConfigEntry<string> interactionBlockMessage;

            public static void InitConfig(ConfigFile config)
            {
                blockPortalInteraction =           config.Bind("General", "Block Portal Interaction",          true, "Block non-trusted players from interacting with Portals"                                                    );
                blockSeerInteraction =             config.Bind("General", "Block Seer Interaction",            true, "Block non-trusted players from interacting with Lunar Seers"                                                );
                blockTeleporterInteraction =       config.Bind("General", "Block Teleporter Interaction",      true, "Block non-trusted players from interacting with Teleporters"                                                );
                blockSafeWardInteraction =         config.Bind("General", "Block SafeWard Interaction",        true, "Block non-trusted players from interacting with the Safe Ward"                                              );
                trustedPlayerList =                config.Bind("General", "Trusted Player List",                 "", "Comma seperated list of trusted player ids. Will get copied when installed from modpack code, so watch out.");
                simulacrumRewardVectorMultiplyer = config.Bind("General", "Simulacrum Reward Vector Multiplyer", 1f, "Float to multiply the velocity of Simularcum reward item droplets by.");
                interactionBlockMessage =          config.Bind("General", "Interaction Blocked Message", "Non-trusted player {player_name} tried to activate {interactable_name}, interaction blocked.", "Message to send when an interaction is blocked. The special strings {player_name} and {interactable_name} will be replaced with the relevent details when sent.");
            }
        }

        public void Awake()
        {
            instance = this;

            test = Harmony.CreateAndPatchAll(typeof(MonoBehaviourCallbackHooks));

            ModConfig.InitConfig(Config);

            IL.RoR2.InfiniteTowerWaveController.DropRewards += (il) =>
            {
                ILCursor c = new(il);

                // Patch position vector to player position
                c.GotoNext(
                    x => x.MatchLdloca(6),
                    x => x.MatchLdloc(4),
                    x => x.MatchStfld<CreatePickupInfo>(nameof(CreatePickupInfo.position))
                );
                c.Index += 2;
                c.Emit(OpCodes.Ldloc, 5);
                c.EmitDelegate<Func<Vector3, int, Vector3>>((original_position, index) =>
                {
                    if (index >= PlayerCharacterMasterController.instances.Count) {
                        return original_position;
                    }

                    var player = PlayerCharacterMasterController.instances[index];

                    if (
                        player.master.IsDeadAndOutOfLivesServer()
                        || player.master.GetBody().transform == null
                        || player.master.GetBody().transform.position == null
                    )
                    {
                        return original_position;
                    }

                    return player.master.GetBody().transform.position;
                });

                // Patch velocity vector
                c.GotoNext(
                    x => x.MatchDup(),
                    x => x.MatchLdfld<CreatePickupInfo>(nameof(CreatePickupInfo.position)),
                    x => x.MatchLdloc(2)
                );
                c.Index += 3;
                c.Emit(OpCodes.Ldloc, 5);
                c.EmitDelegate<Func<Vector3, int, Vector3>>((original_velocity, index) =>
                {
                    if (index >= PlayerCharacterMasterController.instances.Count)
                    {
                        Config.Reload();
                        return original_velocity * ModConfig.simulacrumRewardVectorMultiplyer.Value;
                    }

                    var player = PlayerCharacterMasterController.instances[index];

                    if (
                        player.master.IsDeadAndOutOfLivesServer()
                        || player.master.GetBody().transform == null
                        || player.master.GetBody().transform.position == null
                    )
                    {
                        Config.Reload();
                        return original_velocity * ModConfig.simulacrumRewardVectorMultiplyer.Value;
                    }

                    return Vector3.zero;

                });
            };

            [ConCommand(commandName = "TFD.TrustPlayer", flags = ConVarFlags.None, helpText = "Add a player name to the trusted player list.")]
            static void TrustPlayer(ConCommandArgs args)
            {
                ModConfig.trustedPlayerList.Value += $",{args.GetArgString(0)}";
            }

            [ConCommand(commandName = "TFD.TrustedPlayerList", flags = ConVarFlags.None, helpText = "Show the trusted player list.")]
            static void TrustedPlayerList(ConCommandArgs args)
            {
                foreach(string x in ModConfig.trustedPlayerList.Value.Split(',')) 
                {
                    Debug.Log(x);
                }
            }

            [ConCommand(commandName = "TFD.UntrustPlayer", flags = ConVarFlags.None, helpText = "Remove all names from the player list that contain the given ")]
            static void UntrustPlayer(ConCommandArgs args)
            {
                var newList = ModConfig.trustedPlayerList.Value.Split(',').Where(x => !x.Contains(args.GetArgString(0))).ToArray();
                var newString = "";
                foreach (var x in newList)
                {
                    newString += x.ToString();
                }
                ModConfig.trustedPlayerList.Value = newString;
            }

            On.RoR2.Interactor.PerformInteraction += (orig, self, interactableObject) =>
            {
                Config.Reload();
                string[] blacklistNames = new string[] {"Portal", "Seer", "Teleporter", "SafeWard"};

                if (interactableObject == null) { orig(self, interactableObject); return; }
                string[] blacklistFilter = blacklistNames.Where(x => interactableObject.name.Contains(x)).ToArray();

                if (blacklistFilter.Length == 0) { orig(self, interactableObject); return; }

                Config.TryGetEntry("General", $"Block {blacklistFilter[0]} Interaction", out ConfigEntry<bool> releventConfig);

                if (!releventConfig.Value) { orig(self, interactableObject); return; }

                if (self.GetComponent<CharacterBody>() == PlayerCharacterMasterController.instances[0].master.GetBody()) { orig(self, interactableObject); return; }

                string[] splitPlayerList = ModConfig.trustedPlayerList.Value.Split(',').Where(x => x != "").ToArray();

                if (splitPlayerList.Length > 0)
                {

                    foreach (string playerName in splitPlayerList)
                    {

                        if (self.GetComponent<CharacterBody>().GetUserName().Contains(playerName))
                        {
                            orig(self, interactableObject); return;
                        }
                    }
                }
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "<color=#ff0000>{0}</color>", paramTokens = new[] { ModConfig.interactionBlockMessage.Value.Replace("{player_name}", self.GetComponent<CharacterBody>().GetUserName()).Replace("{interactable_name}", blacklistFilter[0]) } });
            };

            [ConCommand(commandName = "TFD.TogglePortalBlocking")]
            static void TogglePortalBlocking(ConCommandArgs args) { ModConfig.blockPortalInteraction.Value = !ModConfig.blockPortalInteraction.Value; instance.Config.Reload(); }

            [ConCommand(commandName = "TFD.ToggleSeerBlocking")]
            static void ToggleSeerBlocking(ConCommandArgs args) { ModConfig.blockSeerInteraction.Value = !ModConfig.blockSeerInteraction.Value; instance.Config.Reload(); }

            [ConCommand(commandName = "TFD.ToggleTeleporterBlocking")]
            static void ToggleTeleporterBlocking(ConCommandArgs args) { ModConfig.blockTeleporterInteraction.Value = !ModConfig.blockTeleporterInteraction.Value; instance.Config.Reload(); }

            [ConCommand(commandName = "TFD.ToggleSafeWardBlocking")]
            static void ToggleSafeWardBlocking(ConCommandArgs args) { ModConfig.blockSafeWardInteraction.Value = !ModConfig.blockSafeWardInteraction.Value; instance.Config.Reload(); }


            On.EntityStates.Missions.BrotherEncounter.EncounterFinished.OnEnter += (orig, self) =>
            {
                self.outer.SetNextState(new BrotherEncounterLowerWalls());
            };

            [ConCommand(commandName = "TFD.SpawnTeleporter")]          static void SpawnTeleporter          (ConCommandArgs args) { NetworkServer.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>( "RoR2/Base/Teleporters/Teleporter1.prefab"                        ).WaitForCompletion(), PlayerCharacterMasterController.instances[0].body.transform.position, Quaternion.identity)); }
            [ConCommand(commandName = "TFD.SpawnLunarTeleporter")]     static void SpawnLunarTeleporter     (ConCommandArgs args) { NetworkServer.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>( "RoR2/Base/Teleporters/LunarTeleporter Variant.prefab"            ).WaitForCompletion(), PlayerCharacterMasterController.instances[0].body.transform.position, Quaternion.identity)); }
            [ConCommand(commandName = "TFD.spawnPortalArena")]         static void SpawnPortalArena         (ConCommandArgs args) { NetworkServer.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>( "RoR2/Base/PortalArena/PortalArena.prefab"                        ).WaitForCompletion(), PlayerCharacterMasterController.instances[0].body.transform.position, Quaternion.identity)); }
            [ConCommand(commandName = "TFD.spawnPortalArtifactworld")] static void SpawnPortalArtifactworld (ConCommandArgs args) { NetworkServer.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>( "RoR2/Base/PortalArtifactworld/PortalArtifactworld.prefab"        ).WaitForCompletion(), PlayerCharacterMasterController.instances[0].body.transform.position, Quaternion.identity)); }
            [ConCommand(commandName = "TFD.spawnPortalGoldshores")]    static void SpawnPortalGoldshores    (ConCommandArgs args) { NetworkServer.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>( "RoR2/Base/PortalGoldshores/PortalGoldshores.prefab"              ).WaitForCompletion(), PlayerCharacterMasterController.instances[0].body.transform.position, Quaternion.identity)); }
            [ConCommand(commandName = "TFD.spawnPortalMS")]            static void SpawnPortalMS            (ConCommandArgs args) { NetworkServer.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>( "RoR2/Base/PortalMS/PortalMS.prefab"                              ).WaitForCompletion(), PlayerCharacterMasterController.instances[0].body.transform.position, Quaternion.identity)); }
            [ConCommand(commandName = "TFD.spawnPortalShop")]          static void SpawnPortalShop          (ConCommandArgs args) { NetworkServer.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>( "RoR2/Base/PortalShop/PortalShop.prefab"                          ).WaitForCompletion(), PlayerCharacterMasterController.instances[0].body.transform.position, Quaternion.identity)); }
            [ConCommand(commandName = "TFD.spawnPortalInfiniteTower")] static void SpawnPortalInfiniteTower (ConCommandArgs args) { NetworkServer.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>( "RoR2/DLC1/GameModes/InfiniteTowerRun/PortalInfiniteTower.prefab" ).WaitForCompletion(), PlayerCharacterMasterController.instances[0].body.transform.position, Quaternion.identity)); }
            [ConCommand(commandName = "TFD.spawnDeepVoidPortal")]      static void SpawnDeepVoidPortal      (ConCommandArgs args) { NetworkServer.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>( "RoR2/DLC1/DeepVoidPortal/DeepVoidPortal.prefab"                  ).WaitForCompletion(), PlayerCharacterMasterController.instances[0].body.transform.position, Quaternion.identity)); }
            [ConCommand(commandName = "TFD.spawnPortalVoid")]          static void SpawnPortalVoid          (ConCommandArgs args) { NetworkServer.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>( "RoR2/DLC1/PortalVoid/PortalVoid.prefab"                          ).WaitForCompletion(), PlayerCharacterMasterController.instances[0].body.transform.position, Quaternion.identity)); }
            [ConCommand(commandName = "TFD.spawnScavBackpack")]        static void SpawnScavBackpack        (ConCommandArgs args) { NetworkServer.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>( "RoR2/Base/Scav/ScavBackpack.prefab"                              ).WaitForCompletion(), PlayerCharacterMasterController.instances[0].body.transform.position, Quaternion.identity)); }
            [ConCommand(commandName = "TFD.spawnScavLunarBackpack")]   static void SpawnScavLunarBackpack   (ConCommandArgs args) { NetworkServer.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>( "RoR2/Base/ScavLunar/ScavLunarBackpack.prefab"                    ).WaitForCompletion(), PlayerCharacterMasterController.instances[0].body.transform.position, Quaternion.identity)); }
            [ConCommand(commandName = "TFD.spawnShrineCleanse")]       static void SpawnShrineCleanse       (ConCommandArgs args) { NetworkServer.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>( "RoR2/Base/ShrineCleanse/ShrineCleanse.prefab"                    ).WaitForCompletion(), PlayerCharacterMasterController.instances[0].body.transform.position, Quaternion.identity)); }
        
            
        }

        private void OnDestroy()
        {
            test.UnpatchSelf();
            //IL.RoR2.InfiniteTowerWaveController.DropRewards -= PatchDropRewards;
            //On.EntityStates.Missions.BrotherEncounter.EncounterFinished.OnEnter -= PatchBrotherWalls;

        }
    }

    public class BrotherEncounterLowerWalls : EntityStates.Missions.BrotherEncounter.BrotherEncounterBaseState
    {
        public override bool shouldEnableArenaWalls => false;

        public override void OnEnter()
        {
            base.OnEnter();
        }
    }
}
