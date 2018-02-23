﻿// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSPDev.GUIUtils;
using KSPDev.KSPInterfaces;
using KSPDev.LogUtils;
using KSPDev.ProcessingUtils;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KIS {

// Next localization ID: #kisLOC_06015.
public class ModuleKISItem : PartModule,
    // KSP interfaces.
    IModuleInfo,
    // KSPDEV interfaces.
    IsPartDeathListener, IsPackable,
    // KSPDEV sugar interfaces.
    IKSPDevModuleInfo {

  #region Localizable GUI strings.
  static readonly Message ModuleTitleInfo = new Message(
      "#kisLOC_06000",
      defaultTemplate: "KIS Item",
      description: "The title of the module to present in the editor details window.");

  static readonly Message<string> EquippableInfo = new Message<string>(
      "#kisLOC_06001",
      defaultTemplate: "Equips on: <<1>>",
      description: "The info string to show in the editor to state that the item can be equipped"
      + " on the kerbal at the designated equip slot."
      + "\nArgument <<1>> is a the slot name.");

  static readonly Message<string> CarriableInfo = new Message<string>(
      "#kisLOC_06002",
      defaultTemplate: "Carried on: <<1>>",
      description: "The info string to show in the editor to state that the item can be carried"
      + " by the kerbal at the designated equip slot."
      + "\nArgument <<1>> is a the slot name.");
  
  static readonly Message EqupSlot_LeftHand = new Message(
      "#kisLOC_06003",
      defaultTemplate: "left hand",
      description: "The name for the left hand equip slot.");

  static readonly Message EqupSlot_RightHand = new Message(
      "#kisLOC_06004",
      defaultTemplate: "right hand",
      description: "The name for the right hand equip slot.");

  static readonly Message EqupSlot_Jetpack = new Message(
      "#kisLOC_06005",
      defaultTemplate: "jetpack",
      description: "The name for the jetpack equip slot.");

  static readonly Message EqupSlot_Eyes = new Message(
      "#kisLOC_06006",
      defaultTemplate: "eyes",
      description: "The name for the eye equip slot.");

  static readonly Message EqupSlot_Helmet = new Message(
      "#kisLOC_06007",
      defaultTemplate: "helmet",
      description: "The name for the helmet equip slot.");

  static readonly Dictionary<string, Message> EquipSlotsLookup = new Dictionary<string, Message> {
      { "leftHand", EqupSlot_LeftHand },
      { "rightHand", EqupSlot_RightHand },
      { "jetpack", EqupSlot_Jetpack },
      { "eyes", EqupSlot_Eyes },
      { "helmet", EqupSlot_Helmet },
  };

  static readonly Message AttachesToPartsWithoutToolsInfo = new Message(
      "#kisLOC_06008",
      defaultTemplate: "Attaches to the parts without a tool",
      description: "The info string to show in the editor to state that the item can be attached"
      + " to another part without a need of any attach tool.");

  static readonly Message DoesntAttachToPartsInfo = new Message(
      "#kisLOC_06009",
      defaultTemplate: "<color=orange>Cannot be attached to the parts</color>",
      description: "The info string to show in the editor to state that the item CANNOT be attached"
      + " to another part.");

  static readonly Message AttachesToSurfaceWithoutToolsInfo = new Message(
      "#kisLOC_06010",
      defaultTemplate: "Attaches to the surface without a tool",
      description: "The info string to show in the editor to state that the item can be attached"
      + " to the surface without a need of any attach tool.");

  static readonly Message AttachToSurfaceNeedsToolInfo = new Message(
      "#kisLOC_06011",
      defaultTemplate: "The tool is need to attach to the surface",
      description: "The info string to show in the editor to state that the item can be attached"
      + " to the surface, but the appropriate tool will be needed.");

  static readonly Message<ForceType> SurfaceAttachStrengthInfo = new Message<ForceType>(
      "#kisLOC_06012",
      defaultTemplate: "Surface attach strength: <<1>>",
      description: "The info string to show in the editor to specify with what force the part will"
      + " be attached to the surface (if such attachment is allowed).");

  static readonly Message CanBeCarriedInfo = new Message(
      "#kisLOC_06013",
      defaultTemplate: "Carrried by the kerbal",
      description: "The info string to show in the editor to state that the item can be carried"
      + " by the kerbal. I.e. it attaches on the kerbal's model and doesn't take space in the"
      + " personal inventory.");

  static readonly Message CanBeEquippedInfo = new Message(
      "#kisLOC_06014",
      defaultTemplate: "Equippable item",
      description: "The info string to show in the editor to state that the item can be eqipped"
      + " by the kerbal. I.e. it attaches on the kerbal's model and reacts to the 'use' hotkey.");
  #endregion

  /// <summary>Specifies how item can be attached.</summary>
  public enum ItemAttachMode {
    /// <summary>Not initialized. Special value.</summary>
    Unknown = -1,
    /// <summary>The item cannot be attached.</summary>
    Disabled = 0,
    /// <summary>The item can be attached with bare hands.</summary>
    /// <remarks>EVA skill is not checked. Anyone can attach such items.</remarks>
    AllowedAlways = 1,
    /// <summary>The item can be attached only if a KIS attach tool is equipped.</summary>
    /// <remarks>The tool may apply extra limitations on the attach action. E.g. wrenches cannot
    /// attach to stack nodes.</remarks>
    AllowedWithKisTool = 2
  }

  #region Part's config fields
  [KSPField]
  public string moveSndPath = "KIS/Sounds/itemMove";
  [KSPField]
  public string shortcutKeyAction = "drop";
  [KSPField]
  public bool usableFromEva;
  [KSPField]
  public bool usableFromContainer;
  [KSPField]
  public bool usableFromPod;
  [KSPField]
  public bool usableFromEditor;
  [KSPField]
  public bool vesselAutoRename;
  [KSPField]
  public string useName = "use";
  [KSPField]
  public bool stackable;
  [KSPField]
  public bool equipable;
  [KSPField]
  public string equipMode = "model";
  [KSPField]
  public string equipSlot = "";
  [KSPField]
  public string equipSkill = "";
  [KSPField]
  public bool equipRemoveHelmet = false;
  [KSPField]
  public string equipMeshName = "helmet";
  [KSPField]
  public string equipBoneName = "bn_helmet01";
  [KSPField]
  public Vector3 equipPos = new Vector3(0f, 0f, 0f);
  [KSPField]
  public Vector3 equipDir = new Vector3(0f, 0f, 0f);
  [KSPField]
  public float volumeOverride;
  [KSPField]
  public bool carriable;
  [KSPField]
  public ItemAttachMode allowPartAttach = ItemAttachMode.AllowedWithKisTool;
  [KSPField]
  public ItemAttachMode allowStaticAttach = ItemAttachMode.Disabled;
  [KSPField]
  public bool useExternalPartAttach;
  // For KAS
  [KSPField]
  public bool useExternalStaticAttach;
  // For KAS
  [KSPField]
  public float staticAttachBreakForce = 10;
  [KSPField(isPersistant = true)]
  public bool staticAttached;
  #endregion

  FixedJoint staticAttachJoint;

  #region IModuleInfo implementation
  /// <inheritdoc/>
  public virtual string GetModuleTitle() {
    return ModuleTitleInfo;
  }

  /// <inheritdoc/>
  public virtual Callback<Rect> GetDrawModulePanelCallback() {
    return null;
  }

  /// <inheritdoc/>
  public virtual string GetPrimaryField() {
    return string.Join("\n", GetPrimaryFieldInfo().Where(x => x != null).ToArray());
  }

  /// <inheritdoc/>
  public override string GetInfo() {
    return
        string.Join("\n", GetParamInfo().Where(x => x != null).ToArray())
        + "\n\n"
        + string.Join("\n", GetPropInfo().Where(x => x != null).ToArray());
  }
  #endregion

  #region IsPartDeathListener implementation
  /// <inheritdoc/>
  public virtual void OnPartDie() {
    if (vessel.isEVA) {
      var inventory = vessel.rootPart.GetComponent<ModuleKISInventory>();
      var item = inventory.items.Values.FirstOrDefault(i => i.equipped && i.equippedPart == part);
      if (item != null) {
        DebugEx.Info("Item {0} has been destroyed. Drop it from inventory of {1}",
                     item.availablePart.title, inventory.part);
        AsyncCall.CallOnEndOfFrame(inventory, () => inventory.DeleteItem(item.slot));
      }
    }
  }
  #endregion

  #region IInventoryItem candidate
  public virtual void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom) {
  }

  // TODO(ihsoft): Deprecate it. Too expensive.
  public virtual void OnItemUpdate(KIS_Item item) {
  }

  // TODO(ihsoft): Deprecate it. Too expensive.
  public virtual void OnItemGUI(KIS_Item item) {
  }

  public virtual void OnDragToPart(KIS_Item item, Part destPart) {
  }

  public virtual void OnDragToInventory(KIS_Item item, ModuleKISInventory destInventory,
                                        int destSlot) {
  }

  public virtual void OnEquip(KIS_Item item) {
  }

  public virtual void OnUnEquip(KIS_Item item) {
  }
  #endregion

  #region IsPackable implementation
  /// <inheritdoc/>
  public virtual void OnPartPack() {
  }

  /// <inheritdoc/>
  public virtual void OnPartUnpack() {
    if (allowStaticAttach == ItemAttachMode.Disabled || useExternalStaticAttach) {
      return;
    }
    if (staticAttached) {
      DebugEx.Fine("Re-attach static object (OnPartUnpack)");
      GroundAttach();
    }
  }
  #endregion

  public void OnKISAction(Dictionary<string, object> eventData) {
    if (allowStaticAttach == ItemAttachMode.Disabled || useExternalStaticAttach) {
      return;
    }
    var action = eventData["action"].ToString();
    var tgtPart = eventData["targetPart"] as Part;
    //FIXME: use enum values 
    if (action == KIS_Shared.MessageAction.Store.ToString()
        || action == KIS_Shared.MessageAction.DropEnd.ToString()
        || action == KIS_Shared.MessageAction.AttachStart.ToString()) {
      GroundDetach();
      var modulePickup = KISAddonPickup.instance.GetActivePickupNearest(this.transform.position);
      if (modulePickup) {
        KIS_Shared.PlaySoundAtPoint(modulePickup.detachStaticSndPath, this.transform.position);
      }
    }
    if (action == KIS_Shared.MessageAction.AttachEnd.ToString() && tgtPart == null) {
      GroundAttach();
      var modulePickup = KISAddonPickup.instance.GetActivePickupNearest(this.transform.position);
      if (modulePickup) {
        KIS_Shared.PlaySoundAtPoint(modulePickup.attachStaticSndPath, this.transform.position);
      }
    }
  }

  public void GroundAttach() {
    staticAttached = true;
    StartCoroutine(WaitAndStaticAttach());
  }

  public void GroundDetach() {
    if (staticAttached) {
      DebugEx.Fine("Removing static rigidbody and fixed joint on: {0}", part);
      if (staticAttachJoint) {
        Destroy(staticAttachJoint);
      }
      staticAttachJoint = null;
      staticAttached = false;
    }
  }

  /// <summary>
  /// Returns a localized message that describes how the item can be equipped/carried. 
  /// </summary>
  /// <returns>The localized string.</returns>
  public string GetEqipSlotString() {
    var slotName = EquipSlotsLookup.ContainsKey(equipSlot)
        ? EquipSlotsLookup[equipSlot].Format()
        : equipSlot;
    if (equipable) {
      return EquippableInfo.Format(slotName);
    }
    if (carriable) {
      return CarriableInfo.Format(slotName);
    }
    return null;
  }

  #region Inheritable & customization methods
  /// <summary>Returns parameterized info strings.</summary>
  /// <remarks>
  /// These strings have a value that can change from part to part. E.g. "resource1: XXX".
  /// </remarks>
  /// <returns>
  /// The list with the localized strings. There can be <c>null</c> values, they will be safely
  /// ignored when making the editor info string.
  /// </returns>
  protected virtual IEnumerable<string> GetParamInfo() {
    var slotName = EquipSlotsLookup.ContainsKey(equipSlot)
        ? EquipSlotsLookup[equipSlot].Format()
        : equipSlot;
    return new[] {
        equipable
            ? EquippableInfo.Format(slotName) : null,
        carriable
            ? CarriableInfo.Format(slotName) : null,
        allowStaticAttach == ItemAttachMode.AllowedAlways
        || allowStaticAttach == ItemAttachMode.AllowedWithKisTool
            ? SurfaceAttachStrengthInfo.Format(staticAttachBreakForce) : null,
    };
  }

  /// <summary>Returns property info strings.</summary>
  /// <remarks>
  /// These strings reflect the boolean settings on the part. E.g. "has something", "can do this",
  /// "cannot do this", "not usable for that", etc.
  /// </remarks>
  /// <returns>
  /// The list with the localized strings. There can be <c>null</c> values, they will be safely
  /// ignored when making the editor info string.
  /// </returns>
  protected virtual IEnumerable<string> GetPropInfo() {
    return new[] {
        allowStaticAttach == ItemAttachMode.AllowedWithKisTool
            ? AttachToSurfaceNeedsToolInfo.Format() : null,
    };
  }

  /// <summary>Returns info strings to display in the primary details screen.</summary>
  /// <remarks>Limit this list to the bare minimum as this view doesn't assume much space.</remarks>
  /// <returns>
  /// The list with the localized strings. There can be <c>null</c> values, they will be safely
  /// ignored when making the editor info string.
  /// </returns>
  protected virtual IEnumerable<string> GetPrimaryFieldInfo() {
    return new[] {
        carriable ? CanBeCarriedInfo.Format() : null,
        equipable ? CanBeEquippedInfo.Format() : null,
        allowPartAttach == ItemAttachMode.Disabled
            ? DoesntAttachToPartsInfo.Format() : null,
        allowPartAttach == ItemAttachMode.AllowedAlways
            ? AttachesToPartsWithoutToolsInfo.Format() : null,
        allowStaticAttach == ItemAttachMode.AllowedAlways
            ? AttachesToSurfaceWithoutToolsInfo.Format() : null,
    };
  }
  #endregion

  #region Local utility methods
  IEnumerator WaitAndStaticAttach() {
    // Wait for part to become active in case of it came from inventory.
    while (!part.started && part.State != PartStates.DEAD) {
      yield return new WaitForFixedUpdate();
    }
    part.vessel.Landed = true;

    DebugEx.Fine("Create fixed joint attached to the world");
    if (staticAttachJoint) {
      Destroy(staticAttachJoint);
    }
    staticAttachJoint = part.gameObject.AddComponent<FixedJoint>();
    staticAttachJoint.breakForce = staticAttachBreakForce;
    staticAttachJoint.breakTorque = staticAttachBreakForce;
  }

  // Resets item state when joint is broken.
  // A callback from MonoBehaviour.
  void OnJointBreak(float breakForce) {
    if (staticAttached) {
      DebugEx.Warning("A static joint has just been broken! Force: {0}", breakForce);
    } else {
      DebugEx.Warning("A fixed joint has just been broken! Force: {0}", breakForce);
    }
    GroundDetach();
  }
  #endregion
}

}  // namespace
