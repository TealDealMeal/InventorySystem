using UnityEngine;
using UdonSharp;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Holster : UdonSharpBehaviour {
    [HideInInspector] public Inventory inventory;
    [HideInInspector] public MeshRenderer meshRenderer;

    bool holdingItem;
    VRCPlayerApi localPlayer;

    ushort itemId;
    GameObject itemObject;
    VRC_Pickup itemPickup;

    bool handCollision;
    bool rightHandCollision;
    bool leftHandCollision;

    void Start() {
        localPlayer = Networking.LocalPlayer;
    }

    void LateUpdate() {
        if (itemPickup != null) {
            if (holdingItem) {
                if (itemPickup.IsHeld) {
                    _ReleaseItem();
                    return;
                }

            } else if (!itemPickup.IsHeld) {
                if (Networking.GetOwner(itemPickup.gameObject) != localPlayer) { // Item was stolen
                    _UnassignItem();

                    return;
                }

                _HoldItem();
            }
        }
    }

    // Insert items
    void OnTriggerEnter(Collider other) {
        if (other.gameObject.layer == inventory.handLayer) {
            bool usingLeftHand = false;

            if (other.gameObject == inventory.rightHand)
                rightHandCollision = true;
            else {
                leftHandCollision = true;
                usingLeftHand = true;
            }

            handCollision = true;

            if (holdingItem && itemPickup != null && inventory.handCollidersAccessHolsters)
                itemPickup.pickupable = true;
            else if (!holdingItem && itemPickup == null && inventory.handCollidersPlaceItems) {
                VRC_Pickup _pickup = localPlayer.GetPickupInHand(!usingLeftHand ? VRC_Pickup.PickupHand.Right : VRC_Pickup.PickupHand.Left);

                if (_pickup != null && ((1 << _pickup.gameObject.layer) & inventory.holsterableItemLayers) != 0) {
                    for (byte i = 0; i < inventory.holsterCount; i++)
                        if (inventory.holsters[i].itemObject == _pickup.gameObject)
                            inventory.holsters[i]._UnassignItem(); // Maybe we want to put it in the other slot?

                    if (!usingLeftHand)
                        localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.2f, 0.1f, 67);
                    else
                        localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.2f, 0.1f, 67);

                    _AssignItem(_pickup.gameObject);
                }
            }

            return;
        }

        if (holdingItem || inventory.handCollidersPlaceItems || itemPickup != null) return;

        GameObject _itemObject = other.gameObject;

        if (((1 << other.gameObject.layer) & inventory.holsterableItemLayers) == 0) return;

        VRC_Pickup rightHand = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
        VRC_Pickup leftHand = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);

        bool isRightHand = rightHand != null && rightHand.gameObject == _itemObject ? true : false;
        bool isLeftHand = leftHand != null && leftHand.gameObject == _itemObject ? true : false;

        if (isRightHand || isLeftHand) {
            for (byte i = 0; i < inventory.holsterCount; i++)
                if (inventory.holsters[i].itemObject == _itemObject)
                    inventory.holsters[i]._UnassignItem(); // Maybe we want to put it in the other slot?

            if (isRightHand)
                localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.2f, 0.1f, 67);
            else
                localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.2f, 0.1f, 67);

            _AssignItem(_itemObject);
        }
    }

    public void _AssignItem(GameObject _itemObject) {
        if (ushort.TryParse(_itemObject.name, out ushort pickupId)) {
            itemId = pickupId;
            itemObject = _itemObject;
            itemPickup = inventory.pickupArray[itemId];

            meshRenderer.material = inventory.filledHolsterMaterial;
        }
    }

    public void _HoldItem(bool dontRegister = false) {
        if (itemObject == null) return;

        holdingItem = true;
        if (!dontRegister)
            inventory._SetItemState(itemId, true);

        Transform _transform = itemObject.transform;
        _transform.parent = transform;
        _transform.localPosition = Vector3.zero;
        _transform.localRotation = Quaternion.identity;

        SendCustomEventDelayedSeconds(nameof(_EnableItemPickup), inventory.holsterDelay);
    }

    public void _EnableItemPickup() {
        if (itemPickup == null || inventory.handCollidersAccessHolsters && !handCollision) return;

        itemPickup.pickupable = true;
    }

    public void _ForceHolster(GameObject _itemObject, bool dontRegister = false) {
        if (!Networking.IsOwner(_itemObject))
            Networking.SetOwner(localPlayer, _itemObject);

        _AssignItem(_itemObject);
        _HoldItem(dontRegister);
    }

    // Remove items
    void OnTriggerExit(Collider other) {
        if (other.gameObject.layer == inventory.handLayer) {
            bool usingLeftHand = false;

            if (other.gameObject == inventory.rightHand)
                rightHandCollision = false;
            else {
                leftHandCollision = false;
                usingLeftHand = true;
            }

            handCollision = rightHandCollision == true || leftHandCollision == true;

            if (holdingItem && itemPickup != null && inventory.handCollidersAccessHolsters && !handCollision)
                itemPickup.pickupable = false;
            else if (!holdingItem && inventory.handCollidersPlaceItems && !inventory.keepItemsInHolster) {
                VRC_Pickup _pickup = localPlayer.GetPickupInHand(!usingLeftHand ? VRC_Pickup.PickupHand.Right : VRC_Pickup.PickupHand.Left);

                if (_pickup == itemPickup)
                    _UnassignItem();
            }

            return;
        }

        if (inventory.keepItemsInHolster || inventory.handCollidersPlaceItems && handCollision || itemObject == null || other.gameObject != itemObject) return;

        VRC_Pickup rightHand = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
        VRC_Pickup leftHand = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);

        // Make sure we hold the item too, otherwise it drops out of our holster
        if (rightHand != itemPickup && leftHand != itemPickup) return;

        _UnassignItem();
    }

    public void _UnassignItem() {
        itemId = ushort.MaxValue;
        itemObject = null;
        itemPickup = null;

        meshRenderer.material = inventory.emptyHolsterMaterial;
    }

    public void _ReleaseItem(bool dontRegister = false) {
        if (itemObject == null) return;

        itemPickup.pickupable = true;
        if (!dontRegister)
            inventory._SetItemState(itemId, false);

        itemObject.transform.SetParent(null);

        holdingItem = false;
    }

    public void _ForceDrop(bool dontRegister = false) {
        _ReleaseItem(dontRegister);
        _UnassignItem();
    }

    public override bool OnOwnershipRequest(VRCPlayerApi requester, VRCPlayerApi newOwner) {
        return false;
    }
}