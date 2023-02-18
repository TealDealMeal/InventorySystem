using UnityEngine;
using UdonSharp;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
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
                    ReleaseItem();
                    return;
                }

            } else if (!itemPickup.IsHeld) {
                if (Networking.GetOwner(itemPickup.gameObject) != localPlayer) { // Item was stolen
                    UnassignItem();

                    return;
                }

                HoldItem();
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
                            inventory.holsters[i].UnassignItem(); // Maybe we want to put it in the other slot?

                    if (!usingLeftHand)
                        localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.2f, 0.1f, 67);
                    else
                        localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.2f, 0.1f, 67);

                    AssignItem(_pickup.gameObject);
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
                    inventory.holsters[i].UnassignItem(); // Maybe we want to put it in the other slot?

            if (isRightHand)
                localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.2f, 0.1f, 67);
            else
                localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.2f, 0.1f, 67);

            AssignItem(_itemObject);
        }
    }

    public void AssignItem(GameObject _itemObject) {
        if (ushort.TryParse(_itemObject.name, out ushort pickupId)) {
            itemId = pickupId;
            itemObject = _itemObject;
            itemPickup = inventory.pickupArray[itemId];

            meshRenderer.material = inventory.filledHolsterMaterial;
        }
    }

    public void HoldItem(bool dontRegister = false) {
        if (itemObject == null) return;

        holdingItem = true;
        if (!dontRegister)
            inventory.RegisterItemHolstered(itemId, true);

        Transform _transform = itemObject.transform;
        _transform.parent = transform;
        _transform.localPosition = Vector3.zero;
        _transform.localRotation = Quaternion.identity;

        SendCustomEventDelayedSeconds(nameof(EnableItemPickup), inventory.holsterDelay);
    }

    public void EnableItemPickup() {
        if (itemPickup == null || inventory.handCollidersAccessHolsters && !handCollision) return;

        itemPickup.pickupable = true;
    }

    public void ForceHolster(GameObject _itemObject, bool dontRegister = false) {
        if (!Networking.IsOwner(_itemObject))
            Networking.SetOwner(Networking.LocalPlayer, _itemObject);

        AssignItem(_itemObject);
        HoldItem(dontRegister);
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
                    UnassignItem();
            }

            return;
        }

        if (inventory.keepItemsInHolster || inventory.handCollidersPlaceItems && handCollision || itemObject == null || other.gameObject != itemObject) return;

        VRC_Pickup rightHand = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
        VRC_Pickup leftHand = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);

        // Make sure we hold the item too, otherwise it drops out of our holster
        if (rightHand != itemPickup && leftHand != itemPickup) return;

        UnassignItem();
    }

    public void UnassignItem() {
        itemId = ushort.MaxValue;
        itemObject = null;
        itemPickup = null;

        meshRenderer.material = inventory.emptyHolsterMaterial;
    }

    public void ReleaseItem(bool dontRegister = false) {
        if (itemObject == null) return;

        itemPickup.pickupable = true;
        if (!dontRegister)
            inventory.RegisterItemHolstered(itemId, false);

        itemObject.transform.SetParent(null);

        holdingItem = false;
    }

    public void ForceDrop(bool dontRegister = false) {
        ReleaseItem(dontRegister);
        UnassignItem();
    }
}