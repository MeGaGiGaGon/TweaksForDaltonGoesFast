# TweaksForDaltonGoesFast

A collection of tweaks for DaltonGoesFast. Main body also writen by DaltonGoesFast.

## Player Trust System

Blocks untrusted players from interacting with any type of stage progression. Only players trusted are allowed to use Portals, Teleporters, Lunar Seers, or Simulacrum Assessment/cell vent. Each blocking option is toggleable with commands 

     TFD.TogglePortalBlocking     - Untrusted players cannot interact with Portals, such as bazaar, celestial, or arena
     TFD.ToggleSeerBlocking       - Untrusted players cannot interact with Lunar Seers in the Bazaar Between Time (cannot dream of a stage)
     TFD.ToggleTeleporterBlocking - Untrusted players cannot trigger a teleporter event
     TFD.ToggleSafeWardBlocking   - Untrusted players cannot progress Simulacrum


## Trusted Players

Trust System also allows you to add your friends to a comma separated config list

Players added to the config using these commands will work mid-run, those added manually by editing the config will not

Using the commands is recommended since they maintain the comma delimited format of the config

     TFD.TrustPlayer       - Add player to trusted list, ex.: TFD.TrustPlayer DaltonGoesFast
     TFD.TrustedPlayerList - Show list of trusted players
     TFD.UntrustPlayer     - Remove a trusted player the same way you can add one

## Expanded Interactable Spawning

### ***Outdated but kept for legacy reasons, DebugToolKit now can spawn all of these and more***

     TFD.SpawnTeleporter          - Normal stage teleporter
     TFD.SpawnLunarTeleporter     - Stage 5 (sky meadows) teleporter
     TFD.spawnPortalArena         - Null Portal - OG void fields portal found in lunar bazaar
     TFD.spawnPortalArtifactworld - Takes you to bulwark ambry, no artifact will be active unless previously set
     TFD.spawnPortalGoldshores    - Gold portal that takes you to Aureleonite
     TFD.spawnPortalMS            - Celestial Portal - Obliteration Portal
     TFD.spawnPortalShop          - Blue Portal - Lunar Bazaar
     TFD.spawnPortalInfiniteTower - Simulacrum progression portal
     TFD.spawnDeepVoidPortal      - Portal to Voidling fight
     TFD.spawnPortalVoid          - Portal to the Void Locus
     TFD.spawnScavBackpack        - Scav Bag that drops items (10)
     TFD.spawnScavLunarBackpack   - Scav Bag that drops lunar coins (10)
     TFD.spawnShrineCleanse       - Cleansing Pool to remove lunar items

## Simulacrum Item Droplet Relocation

At the end of each wave, item droplets are spawned on each player's position, instead of around assessment focus

This prevents accidental item stealing when the focus is too close to a wall, or there are too many players
  

## Old Moon Stage Arena Fix

On the outdated stage `moon` (old commencement) you can now escape the arena after Mithrix dies 

**Note: This function requires all players to have the mod installed**

# Links

If you want to see this mod or others showcased please check out [DaltonGoesFast on YouTube](https://www.youtube.com/@DaltonGoesFast) 

If you want to join his games you can find him [here on Twitch](https://www.twitch.tv/daltongoesfast)

Dalton has also recently made [this video](https://youtu.be/_gfBhDtz5CE) reviewing the mod

Feel free to complain to me about my bad code or report bugs at `gigagon` on discord

# Changelog:

V 1.0.6

- Fixed IL hook for simulacrum drop rewards breaking

- Removed dependancy on R2API

V 1.0.5

- Tried to fix breaking all other mod commands by removing the attribute that should make commands work

V 1.0.4

- Added a null check for interactableObject that will hopefully fix an issue

V 1.0.3

- Still can't figure out how to fix the R2API dependancy, so oh well too bad

V 1.0.2

- Updated mod markdown and switched to pure markdown instead of HTML

- Updated mod dependancies

- Added config option for the message show when a non-trusted player uses a blocked interactable

V 1.0.1

- Fixed incompatibility with ImprovedCommandEssence

V 1.0.0

- Release