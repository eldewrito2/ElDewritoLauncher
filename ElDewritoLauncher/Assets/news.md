# ElDewrito 0.7 - April 20 2024

It's been 6 years since the release of 0.6 and a lot has changed, too much to list in this news post.
Here is just a **small** selection of some of the changes that we've made.

### GAMEPLAY
- Added **Mod support** complete with browser and automatic download when joining a server
- Added Playable elites with assassination blades & sprint animations
- Added new playable characters through mods with support for assigning them via game types
- Added new unarmed combat mode
- Added player emotes with support for adding new ones through mods
- Added support for Campaign & Firefight
- Added a !kill chat command
- Added !endround vote chat command
- Added third person camera & sprint game type settings (player trait)
- Fixed assassination desync & hold timer
- Fixed assassinations not respecting invincibility
- Fixed Territories game type
- Fixed slow-walk when charging the spartan laser & fp shotgun animations
- Weapon balance improvements for the needler, shotgun, and magnum
- Backported "Projectile hit registration" fixes from MCC
- Reduced the default sprint speed to the original Halo Online values (if enabled)
- Restored cut armor abilities from Halo Online (equipment)
- Restored cut post-match podium from Halo Online (optional)
- Restored cut falling, jump, and soft-land animations from Halo Online (optional)


### CUSTOMIZATION
- Restore H3 armor customization system
- Added new player customization system with support for mods
- Added player nameplates
- Added many new player emblems
- Added RGB color changing for emblems
- Fixed hologram not using player customization
- Player emblems now also display in the lobby and on armor in game
  
### ENGINE
- Generally improved performance
- Updated everything to use tag names, and named every tag
- Improved map load times
- Reimplemented large parts of the engine to support Reach maps
- Increased havok memory limit
- Increased render visibility limit allowing more objects and instances to be rendered
- Fixed havok mopp crashes with ported H3 maps
- Fixed multiple crashes in the effect system
- Fixed crashes with flying camera
- Fixed holes in s3d_reactor map collision geometry

### RENDER
- Reduced frame times and stutter
- Added support for Nvidia RTX GPUs and newer AMD GPUs
- Reimplemented the screen fx system from Reach
- Increased the vertex buffer limits for Reach maps
- Recompiled shaders (fixes a whole range of issues)
- Fixed two_lobe_phong (red tinge when viewed at certain angles)
- Restored H3 gamma
- Added SSAO settings option
- Added level-of-detail and decorator distance sliders
- Added dynamic shadow resolution option
- Fixed dynamic shadows
- Fixed patchy fog (fog no longer rotates with the camera)
- Fixed object lightmap sampling from instanced geometry
- Fixed dynamic lights rendering on decorators
- Fixed object terrain bitmap sampling (caused incorrect tinting)
- Fixed lens flare flickering
- Fixed bloom/brightness issues with some graphics cards

### NETWORK
- Added new stats and ranking systems with hooks to allow servers hosts to make their own
- Fixed issues with some people not being able to connect to servers
- Fixed many VoIP issues
- Fixed Player join/leave message spam at the start of a game
- Fixed Client vehicle weapon magnetism desync (tank shells now actually hit)
- Added breakable surface sync (breakable glass)

### UI & HUD
- Added main menu mods
- Added GPU acceleration to the Web UI (CEF)
- Added mouse support for menu navigation
- Added new menu for mod selection in the lobby
- Added player service records
- Added support for ultrawide aspect ratios
- Added centered crosshair option for third person
- Added network ping to scoreboard
- Added consistent keyboard/controller support to all screens
- Added persistent console command history
- Added rematch option to voting
- Fixed scoreboard getting stuck when the player is out of lives
- Upscaled halox UI
- Restored halox menu blur
- Restored Campaign & Firefight lobbies
- Restored H3 roster with rank & emblems
- Restored the H3 matchmaking globe background for the server browser
- Restored name-over-player speaking indicator for VoIP & other HUD fixes
- Restored disconnected players on the scoreboard

### AUDIO
- Added new sound cache/streaming to free up memory for mods and speed up map load times
- Fixed loop permutations (previously only a single permutation would play in a loop)
- Fixed left/right engine permutations (previously only one side would play)
- Fixed doppler effect
- Fixed audio cutoff (too many sounds being played at once would not allow any new sounds)
- Fixed delayed spawn beeps
- Fixed lipsync

### FORGE
- Added new global forge palette with support for adding new objects with mods
- Added Undo/Redo capability to forge
- Added new selection renderer to make it easier to see selected objects
- Added devices machines that can be activated through different power channels
- Added climbable surfaces
- Added selectable physics materials for ReForge objects
- Added team based pre-match cameras
- Added a podium placement object
- Added a team barrier property to ReForge objects (prevents players passing through)
- Added RGB color changing for supported objects with CC
- Added many new ReForge materials & improved existing ones
- Added scalable rocks & vegetation to the palette
- Improved weather effect visuals
- Restored the H3 budget bar