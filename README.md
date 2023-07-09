# Inventory System

## Easy to use and fully works out of the box Inventory System for VRChat Worlds!

### Features:
- Fully synced (at least from my testing)
- Works on both VR and non-VR, as well as standalone Quest
- Create and customize any amount of holsters, which are automatically registered and attached to the player (you can change the mesh and collider to fit your preference)
- All objects with the predetermined layers (i.e. Pickup) and VRC_Pickup script are automatically detected before the scene loads and registered, no scripts, code or manually set up arrays required (this also means you can also set up pick ups that cannot be holstered)
- Hand collider system, only interact with holsters and it's contents if you physically touch them (VR only)
- Plays a short haptic feedback when placing items into a holster
- A bunch of settings, pick the ones that fit best for your project:

![grafik](https://user-images.githubusercontent.com/97361953/219671944-0434132b-8203-4214-b838-bb523b6d8996.png)

---

### Requirements:
- UdonSharp
- [NetworkEventCaller](https://github.com/Miner28/NetworkedEventCaller)

---

### Setup:
Place the Inventory prefab into your scene, right click it in the hierarchy and click "Unpack Prefab"

---

- Set up these 2 new layers (or 1 if you don't wish to use HandColliders):

![grafik](https://user-images.githubusercontent.com/97361953/219653395-3891cf55-1056-4fa6-a173-4407e58879df.png)

---

- Change the layer of both hands to use "HandCollider" (don't change the layer of children)

![grafik](https://user-images.githubusercontent.com/97361953/219677799-5bd8b31c-86b4-4d6c-a769-0c41f36a2ff9.png)

---

- Change the layer of your holsters to use "Holster" (don't change the layer of children)
- You can also add any amount of holsters you wish here

![grafik](https://user-images.githubusercontent.com/97361953/219678562-8e5b8491-3eaf-49e7-8d16-afefa29e7639.png)

---

- Open your "Project Settings" -> "Physics" and set up the "Layer Collision Matrix" as shown:

(Holster / Pickup, HandCollider / Holster)

![grafik](https://user-images.githubusercontent.com/97361953/219654347-19cf6c3b-054c-413a-b351-5bd16abc55ae.png)

(Optional: If you wish for the holster to detect VRC_Pickups that don't use the Pickup layer, you must add them here and on the "Holsterable Item Layers")

![grafik](https://user-images.githubusercontent.com/97361953/219671789-145db780-71a3-4558-a9a1-fb4fb50ee0f0.png)

---

- That's it! You should now be able to drop any pickupable object into your holsters!

---

### Performance

- Untested, though the code shouldn't be too demanding to run at all, my concern would be bandwidth at high pick up count (prob. somewhere over 250) as we need to sync an array of player ids for every item
- Feedback appreciated!

---

### Current Issues:
- For some reason, mesh renderers seem to interrupt the pick up raycast, this becomes an issue when playing in non-VR and the holster mesh is bigger than the item itself (VRChat bug)
- Due to the limited networking tools currently, it's possible to desync if multiple people try to holster simultaneously (I hope UDON2 provides a way to send variables inside networked events, this should be able to fix the issue)
- If "Disable Items When Holstered" is turned on, items in the holster will no longer sync position, this is fine until the player disconnects, resetting the items back to where they were last synced
