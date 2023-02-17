# Udon Inventory System

## Easy to use and fully works out of the box Inventory System for VRChat Worlds!


### Features:
- Create and customize any amount of holsters, which are automatically registered and attached to the player
- All objects with a VRC_Pickup script are detected before the scene loads and registered, no scripts, code or manually set up arrays required (works out of the box)
- Fully synced (at least from my testing)
- Works on both VR and non-VR, as well as standalone Quest
- A bunch of settings, pick the ones that fit best for your project:

![grafik](https://user-images.githubusercontent.com/97361953/219655853-3585b7d7-a400-469a-a3d9-ea62882d41fa.png)


### Requirements:
- UdonSharp


### Setup:
Place the Inventory prefab into your scene, right click it in the hierarchy and click "Unpack Prefab"

- Set up 2 new layers (or 1 if you don't wish to use HandColliders):

![grafik](https://user-images.githubusercontent.com/97361953/219653395-3891cf55-1056-4fa6-a173-4407e58879df.png)

- Open your "Project Settings" -> "Physics" and set up the "Layer Collision Matrix" as shown:

(Holster / Pickup, HandCollider / Holster)

![grafik](https://user-images.githubusercontent.com/97361953/219654347-19cf6c3b-054c-413a-b351-5bd16abc55ae.png)

(Optional: If you wish for the holster to detect VRC_Pickups that don't use the Pickup layer, you must add them here)

- That's it! You should now be able to drop any pickupable object into your holsters!


### Current Issues:
- Due to the limited networking tools currently, it's possible to desync if multiple people try to holster simultaneously (I hope UDON2 provides a way to send variables inside networked events, this should be able to fix the issue)
