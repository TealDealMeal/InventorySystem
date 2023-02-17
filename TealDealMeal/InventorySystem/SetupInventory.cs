#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDK3.Components;

public static class SetupInventory {
    public static void Setup() {
        Inventory inventory = Object.FindObjectOfType<Inventory>();
        if (inventory != null) {
            byte holsterCount = (byte)inventory.transform.GetChild(0).childCount;
            inventory.holsterCount = holsterCount;
            inventory.holsters = new Holster[holsterCount];

            for (byte i = 0; i < holsterCount; i++) {
                Transform holster = inventory.transform.GetChild(0).GetChild(i);
                inventory.holsters[i] = holster.GetComponent<Holster>();
                inventory.holsters[i].inventory = holster.root.GetComponent<Inventory>();
                inventory.holsters[i].meshRenderer = holster.GetChild(0).GetComponent<MeshRenderer>();
            }

            VRC_Pickup[] pickupArray = Object.FindObjectsOfType<VRC_Pickup>();
            VRC_Pickup[] pickupArray2 = new VRC_Pickup[pickupArray.Length];

            ushort pickupCount = 0;
            for (ushort i = 0; i < pickupArray.Length; i++) {
                VRC_Pickup _pickup = pickupArray[i];
                if (((1 << _pickup.gameObject.layer) & inventory.holsterableItemLayers) == 0) continue;

                pickupArray2[pickupCount] = _pickup;
                pickupCount++;
            }

            //if (pickupCount == inventory.pickupCount) return; // Only update these arrays if the amount of pick ups differs
            //Debug.Log("[Inventory] Pickup list updated! Before: " + inventory.pickupCount + " / After: " + pickupCount);

            pickupArray = new VRC_Pickup[pickupCount];
            for (ushort i = 0; i < pickupCount; i++)
                pickupArray[i] = pickupArray2[i];

            inventory.pickupArray = pickupArray;
            inventory.pickupCount = pickupCount;

            inventory.pickupArrayPlayerId = new int[pickupCount];
            inventory.pickupArrayHolstered = new bool[pickupCount];

            bool usesObjectSync = inventory.respawnItemsOnDisconnect == true || inventory.respawnItemsOnRespawn == true;
            if (usesObjectSync) {
                inventory.pickupArraySync = new VRCObjectSync[pickupCount];
                inventory.pickupArrayRespawnPos = new Vector3[pickupCount];
            } else {
                inventory.pickupArraySync = null;
                inventory.pickupArrayRespawnPos = null;
            }

            inventory.pickupArrayRigidbody = new Rigidbody[pickupCount];
            inventory.pickupArrayUseGravity = new bool[pickupCount];
            inventory.pickupArrayIsKinematic = new bool[pickupCount];

            for (ushort i = 0; i < pickupCount; i++) {
                if (usesObjectSync) {
                    VRCObjectSync objectSync = inventory.pickupArray[i].GetComponent<VRCObjectSync>();
                    if (objectSync != null)
                        inventory.pickupArraySync[i] = objectSync;
                    else
                        inventory.pickupArrayRespawnPos[i] = inventory.pickupArray[i].transform.position;
                }

                inventory.pickupArrayRigidbody[i] = inventory.pickupArray[i].GetComponent<Rigidbody>();
                inventory.pickupArrayUseGravity[i] = inventory.pickupArrayRigidbody[i].useGravity;
                inventory.pickupArrayIsKinematic[i] = inventory.pickupArrayRigidbody[i].isKinematic;
            }
        }
    }
}

[InitializeOnLoad]
class PlayModeStateChanged {
    static PlayModeStateChanged() {
        EditorApplication.playModeStateChanged += LogPlayModeState;
    }

    private static void LogPlayModeState(PlayModeStateChange state) {
        if (state == PlayModeStateChange.ExitingEditMode)
            SetupInventory.Setup();
    }
}

class BuildRequested : IVRCSDKBuildRequestedCallback {
    public int callbackOrder { get { return 0; } }

    public bool OnBuildRequested(VRCSDKRequestedBuildType type) {
        SetupInventory.Setup();

        return true;
    }
}
#endif