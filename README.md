# Udon Inventory System

## Easy to use and fully works out of the box Inventory System for VRChat Worlds!


### Features:
- Fully synced (at least from my testing)
- Works on both VR and non-VR, as well as standalone Quest
- Create and customize any amount of holsters, which are automatically registered and attached to the player
- All objects with the predetermined layers (i.e. Pickup) and VRC_Pickup script are automatically detected before the scene loads and registered, no scripts, code or manually set up arrays required (this means you can also set up pick ups that cannot be holstered)
- A bunch of settings, pick the ones that fit best for your project:

![grafik](https://user-images.githubusercontent.com/97361953/219671944-0434132b-8203-4214-b838-bb523b6d8996.png)


### Requirements:
- UdonSharp


### Setup:
Place the Inventory prefab into your scene, right click it in the hierarchy and click "Unpack Prefab"

- Set up 2 new layers (or 1 if you don't wish to use HandColliders):

![grafik](https://user-images.githubusercontent.com/97361953/219653395-3891cf55-1056-4fa6-a173-4407e58879df.png)

- Open your "Project Settings" -> "Physics" and set up the "Layer Collision Matrix" as shown:

(Holster / Pickup, HandCollider / Holster)

![grafik](https://user-images.githubusercontent.com/97361953/219654347-19cf6c3b-054c-413a-b351-5bd16abc55ae.png)

(Optional: If you wish for the holster to detect VRC_Pickups that don't use the Pickup layer, you must add them here and on the "Holsterable Item Layers")

![grafik](https://user-images.githubusercontent.com/97361953/219671789-145db780-71a3-4558-a9a1-fb4fb50ee0f0.png)


- That's it! You should now be able to drop any pickupable object into your holsters!


### Current Issues:
- Due to the limited networking tools currently, it's possible to desync if multiple people try to holster simultaneously (I hope UDON2 provides a way to send variables inside networked events, this should be able to fix the issue)
- If "Disable Items When Holstered" is turned on, items in the holster will no longer sync position, this is fine until the player disconnects, resetting the items back to where they were last picked up
