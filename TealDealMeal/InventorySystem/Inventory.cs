using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.SDK3.Components;
using static VRC.SDKBase.VRCPlayerApi;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class Inventory : UdonSharpBehaviour {
    [Tooltip("Determines by layer which pickup can be holstered.")]
    public LayerMask holsterableItemLayers = 1 << 13;

    [Header("Inventory Behaviour")]
    [Tooltip("If turned off, items will stay on the player when respawned.")]
    public bool dropItemsOnRespawn = true;

    [Tooltip("Items will respawn when the owner respawns, if turned off, they will be dropped instead.")]
    public bool respawnItemsOnRespawn = false;

    [Tooltip("Items will respawn when the owner disconnects, if turned off, they will be dropped instead.")]
    public bool respawnItemsOnDisconnect = false;

    [Header("Inventory Rotation")]
    [Tooltip("Lock the inventory rotation in place, automatically disabled for non-VR players.")]
    public bool lockInventoryRotation = true;

    [Tooltip("How fast the inventory rotates.")]
    public float rotationSpeed = 1.5f;

    [Tooltip("How far the inventory rotates.")]
    public byte maxRotation = 72;

    [Header("Holster Behaviour")]
    [Tooltip("Turns off items for other players, pretty sure this reduces bandwidth, as items will no longer be synced while turned off.")]
    public bool disableItemsWhenHolstered = true;

    [Tooltip("When attempting to drop an item, the item will return to the holster. The target holster may be modified by placing the item into another empty holster.")]
    public bool keepItemsInHolster = false;

    [Header("Hand Colliders")]
    [Tooltip("Items within the holsters will only become interactable, if the hand collider touches the holster. This will help with VRChat's notoriously bad pick up detection, automatically disabled for non-VR players.")]
    public bool handCollidersAccessHolsters = true;
    
    [Tooltip("You can only place items in the holster if your hand collider touches them, automatically disabled for non-VR players.")]
    public bool handCollidersPlaceItems = true;

    [Tooltip("Displays the hand colliders, doesn't work for non-VR players.")]
    public bool debugHandColliders = false;

    [Header("Misc")]
    [Tooltip("Delay before being able to interact with the holster again, should prevent syncing inconsistencies.")]
    public float holsterDelay = 0.2f;

    public Material emptyHolsterMaterial;
    public Material filledHolsterMaterial;

    VRCPlayerApi localPlayer;
    Transform holsterParent;
    bool lateJoiner = true;

    [HideInInspector] public int handLayer;
    [HideInInspector] public bool trackHands;
    [HideInInspector] public GameObject rightHand;
    TrackingDataType headTracking;
    TrackingDataType rightHandTracking;
    TrackingDataType leftHandTracking;

    bool usedInventory;
    ushort lastPickupId;
    bool lastPickupHolsterState;

    bool[] localPickupArrayHolstered;
    ushort localPickupHolsteredCount;

    [HideInInspector] public Holster[] holsters;
    [HideInInspector] public byte holsterCount;

    [HideInInspector, UdonSynced] public ushort pickupId;
    [HideInInspector, UdonSynced] public int pickupPlayerId;
    [HideInInspector, UdonSynced] public bool pickupHolstered;
    [HideInInspector, UdonSynced] public ushort pickupHolsteredCount;

    [HideInInspector] public VRC_Pickup[] pickupArray;
    [HideInInspector] public ushort pickupCount;
    [HideInInspector, UdonSynced] public int[] pickupArrayPlayerId;
    [HideInInspector, UdonSynced] public bool[] pickupArrayHolstered;

    [HideInInspector] public VRCObjectSync[] pickupArraySync;
    [HideInInspector] public Vector3[] pickupArrayRespawnPos; // Fallback, in case VRCObjectSync doesn't exist
    [HideInInspector] public Rigidbody[] pickupArrayRigidbody;
    [HideInInspector] public Collider[] pickupArrayCollider;
    [HideInInspector] public bool[] pickupArrayUseGravity;
    [HideInInspector] public bool[] pickupArrayIsKinematic;

    void Start() {
        localPlayer = Networking.LocalPlayer;
        holsterParent = transform.GetChild(0);

        headTracking = TrackingDataType.Head;
        handLayer = transform.GetChild(1).gameObject.layer;

        localPickupArrayHolstered = new bool[pickupCount];

        for (ushort i = 0; i < pickupCount; i++)
            pickupArray[i].name = i.ToString();

        if (!localPlayer.IsUserInVR()) {
            lockInventoryRotation = false;
            handCollidersAccessHolsters = false;
            handCollidersPlaceItems = false;
        }

        if (handCollidersAccessHolsters || handCollidersPlaceItems) {
            trackHands = true;
            rightHand = transform.GetChild(1).gameObject;

            rightHand.SetActive(true);
            transform.GetChild(2).gameObject.SetActive(true);

            rightHandTracking = TrackingDataType.RightHand;
            leftHandTracking = TrackingDataType.LeftHand;

            if (debugHandColliders) {
                transform.GetChild(1).GetChild(0).gameObject.SetActive(true);
                transform.GetChild(2).GetChild(0).gameObject.SetActive(true);
            }
        }

        if (localPlayer.isMaster) {
            lateJoiner = false;
            RequestSerialization();
        }
    }

    void LateUpdate() {
        if (trackHands) {
            TrackingData rightHandData = localPlayer.GetTrackingData(rightHandTracking);
            transform.GetChild(1).SetPositionAndRotation(rightHandData.position, rightHandData.rotation);

            TrackingData leftHandData = localPlayer.GetTrackingData(leftHandTracking);
            transform.GetChild(2).SetPositionAndRotation(leftHandData.position, leftHandData.rotation);
        }

        TrackingData headData = localPlayer.GetTrackingData(headTracking);

        holsterParent.position = headData.position;

        float inventoryRotation = headData.rotation.eulerAngles.y;
        float angle = inventoryRotation;

        if (!lockInventoryRotation) {
            angle = Mathf.LerpAngle(holsterParent.eulerAngles.y, inventoryRotation, rotationSpeed * Time.deltaTime);
            if (angle - maxRotation < inventoryRotation && angle + maxRotation > inventoryRotation) return;
        }

        holsterParent.eulerAngles = new Vector3(0, angle, 0);
    }

    public void RegisterItemHolstered(ushort _pickupId, bool _pickupWasHolstered) {
        if (pickupArrayHolstered[_pickupId] == _pickupWasHolstered) return;

        if (!Networking.IsOwner(gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

        usedInventory = true;

        pickupId = _pickupId;
        pickupPlayerId = localPlayer.playerId;
        pickupHolstered = _pickupWasHolstered;

        pickupArrayPlayerId[_pickupId] = _pickupWasHolstered ? pickupPlayerId : 0;

        pickupArrayHolstered[_pickupId] = _pickupWasHolstered;
        pickupHolsteredCount = (ushort)Mathf.Max(_pickupWasHolstered ? pickupHolsteredCount + 1 : pickupHolsteredCount - 1, 0);

        RequestSerialization();
        OnDeserialization();
    }

    public override void OnDeserialization() {
        if (lateJoiner) {
            for (ushort i = 0; i < pickupCount; i++) {
                if (!pickupArrayHolstered[i]) continue;

                SetItemHolstered(i, true);
            }

            lateJoiner = false;
            return;
        }

        if (pickupId < pickupCount && (pickupId != lastPickupId || pickupHolstered != lastPickupHolsterState)) {
            lastPickupId = pickupId;
            lastPickupHolsterState = pickupHolstered;

            SetItemHolstered(pickupId, pickupHolstered);
        }

        if (pickupHolsteredCount != localPickupHolsteredCount) { // Desync or player respawn
            for (ushort i = 0; i < pickupCount; i++) {
                bool state = pickupArrayHolstered[i];
                if (localPickupArrayHolstered[i] == state) continue;

                SetItemHolstered(i, state);
            }
        }
    }

    public void SetItemHolstered(ushort _pickupId, bool _pickupWasHolstered, bool _respawnItems = false) {
        localPickupArrayHolstered[_pickupId] = _pickupWasHolstered;
        localPickupHolsteredCount = (ushort)Mathf.Max(_pickupWasHolstered ? localPickupHolsteredCount + 1 : localPickupHolsteredCount - 1, 0);

        VRC_Pickup _pickup = pickupArray[_pickupId];
        Rigidbody _rigidbody = pickupArrayRigidbody[_pickupId];

        if (_pickupWasHolstered) {
            _pickup.pickupable = false;

            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;

            if (disableItemsWhenHolstered && localPlayer != Networking.GetOwner(_pickup.gameObject))
                _pickup.gameObject.SetActive(false);
        } else {
            _pickup.pickupable = true;

            _rigidbody.useGravity = pickupArrayUseGravity[_pickupId];
            _rigidbody.isKinematic = pickupArrayIsKinematic[_pickupId];

            if (disableItemsWhenHolstered)
                _pickup.gameObject.SetActive(true);
        }

        if (_respawnItems) {
            VRCObjectSync objectSync = pickupArraySync[_pickupId];

            if (objectSync != null)
                objectSync.Respawn();
            else
                _pickup.transform.position = pickupArrayRespawnPos[_pickupId];
        }
    }

    public override void OnPlayerRespawn(VRCPlayerApi player) {
        // No idea why this method is only called locally only

        if (dropItemsOnRespawn || respawnItemsOnRespawn)
            DropInventory();
    }

    public void DropInventory() {
        if (!usedInventory) return; // Prevent people from spamming respawn
        usedInventory = false;

        if (!Networking.IsOwner(gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

        for (byte i = 0; i < holsterCount; i++)
            holsters[i].ForceDrop(true);

        for (ushort i = 0; i < pickupCount; i++) {
            if (pickupArrayPlayerId[i] != localPlayer.playerId) continue;

            pickupArrayPlayerId[i] = 0;
            if (pickupArrayHolstered[i]) {
                pickupArrayHolstered[i] = false;
                pickupHolsteredCount = (ushort)Mathf.Max(pickupHolsteredCount - 1, 0);
                SetItemHolstered(i, false, respawnItemsOnRespawn);
            }
        }

        RequestSerialization();
    }

    public override void OnPlayerLeft(VRCPlayerApi player) {
        for (ushort i = 0; i < pickupCount; i++) {
            if (pickupArrayPlayerId[i] != player.playerId) continue;

            pickupArrayPlayerId[i] = 0;
            if (pickupArrayHolstered[i]) {
                pickupArrayHolstered[i] = false;
                pickupHolsteredCount = (ushort)Mathf.Max(pickupHolsteredCount - 1, 0);
                SetItemHolstered(i, false, respawnItemsOnDisconnect);
            }
        }

        RequestSerialization();
    }
}