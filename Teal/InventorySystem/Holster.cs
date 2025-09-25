using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using System;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Holster : UdonSharpBehaviour {
    [NonSerialized] bool holdingItem;
    [NonSerialized] VRCPlayerApi localPlayer;

    [NonSerialized] ushort itemId;
    [NonSerialized] GameObject itemObject;
    [NonSerialized] Transform itemTransform;
    [NonSerialized] VRC_Pickup itemPickup;

    [NonSerialized] bool handCollision;
    [NonSerialized] bool rightHandCollision;
    [NonSerialized] bool leftHandCollision;

    // Automated
    [HideInInspector] public Inventory inventory;
    [HideInInspector] public MeshRenderer meshRenderer;

    void Start() {
        localPlayer = Networking.LocalPlayer;
    }

    public override void PostLateUpdate() {
        if (!itemPickup) return;

        if (holdingItem) {
            if (itemPickup.IsHeld) {
                _ReleaseItem();
                return;
            }

            // We now have to update transforms, it appears VRChat recently broke parenting for VRC Object Sync? (seen in v2)
            itemTransform.localPosition = Vector3.zero;
            itemTransform.localRotation = Quaternion.identity;
        } else if (!itemPickup.IsHeld) {
            if (Networking.GetOwner(itemPickup.gameObject) != localPlayer) { // Item was stolen
                _UnassignItem();

                return;
            }

            _HoldItem();
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

            if (holdingItem && itemPickup && inventory.handCollidersAccessHolsters)
                itemPickup.pickupable = true;
            else if (!holdingItem && !itemPickup && inventory.handCollidersPlaceItems) {
                VRC_Pickup _pickup = localPlayer.GetPickupInHand(!usingLeftHand ? VRC_Pickup.PickupHand.Right : VRC_Pickup.PickupHand.Left);

                if (_pickup && ((1 << _pickup.gameObject.layer) & inventory.holsterableItemLayers) != 0) {
                    for (byte i = 0; i < inventory.holsterCount; i++)
                        if (inventory.holsters[i].itemObject == _pickup.gameObject)
                            inventory.holsters[i]._UnassignItem(); // Maybe we want to put it in the other slot?

                    if (!usingLeftHand)
                        localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.2f, 0.25f, 90);
                    else
                        localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.2f, 0.25f, 90);

                    _AssignItem(_pickup.gameObject);
                }
            }

            return;
        }

        if (holdingItem || inventory.handCollidersPlaceItems || itemPickup) return;

        GameObject _itemObject = other.gameObject;

        if (((1 << other.gameObject.layer) & inventory.holsterableItemLayers) == 0) return;

        VRC_Pickup rightHand = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
        VRC_Pickup leftHand = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);

        bool isRightHand = rightHand && rightHand.gameObject == _itemObject ? true : false;
        bool isLeftHand = leftHand && leftHand.gameObject == _itemObject ? true : false;

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
            itemTransform = _itemObject.transform;
            itemPickup = inventory.pickupArray[itemId];

            meshRenderer.material = inventory.filledHolsterMaterial;
        }
    }

    public void _HoldItem(bool dontRegister = false) {
        if (!itemObject) return;

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
        if (!itemPickup || inventory.handCollidersAccessHolsters && !handCollision) return;

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

            if (holdingItem && itemPickup && inventory.handCollidersAccessHolsters && !handCollision)
                itemPickup.pickupable = false;
            else if (!holdingItem && inventory.handCollidersPlaceItems && !inventory.keepItemsInHolster) {
                VRC_Pickup _pickup = localPlayer.GetPickupInHand(!usingLeftHand ? VRC_Pickup.PickupHand.Right : VRC_Pickup.PickupHand.Left);

                if (_pickup == itemPickup)
                    _UnassignItem();
            }

            return;
        }

        if (inventory.keepItemsInHolster || inventory.handCollidersPlaceItems && handCollision || !itemObject || other.gameObject != itemObject) return;

        VRC_Pickup rightHand = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
        VRC_Pickup leftHand = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);

        // Make sure we hold the item too, otherwise it drops out of our holster
        if (rightHand != itemPickup && leftHand != itemPickup) return;

        _UnassignItem();
    }

    public void _UnassignItem() {
        itemId = ushort.MaxValue;
        itemObject = null;
        itemTransform = null;
        itemPickup = null;

        meshRenderer.material = inventory.emptyHolsterMaterial;
    }

    public void _ReleaseItem(bool dontRegister = false) {
        if (!itemObject) return;

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