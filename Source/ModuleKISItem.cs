﻿// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSPDev.DebugUtils;
using KSPDev.GUIUtils;
using KSPDev.GUIUtils.TypeFormatters;
using KSPDev.KSPInterfaces;
using KSPDev.LogUtils;
using KSPDev.ProcessingUtils;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using UnityEngine;

namespace KIS {

// Next localization ID: #kisLOC_06015.
public class ModuleKISItem : PartModule,
    // KSP interfaces.
    IModuleInfo,
    // KSPDEV interfaces.
    IsPartDeathListener, IsPackable, IHasDebugAdjustables,
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
      description: "The info string to show in the editor to state that the item can be equipped"
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
  [Debug.KISDebugAdjustableAttribute("Sound: item moved")]
  public string moveSndPath = "KIS/Sounds/itemMove";

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Key action")]
  public string shortcutKeyAction = "drop";

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Usable from EVA")]
  public bool usableFromEva;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Usable from container")]
  public bool usableFromContainer;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Usable from pod")]
  public bool usableFromPod;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Usable from editor")]
  public bool usableFromEditor;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Vessel auto-rename")]
  public bool vesselAutoRename;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Use menu name")]
  public string useName = "use";

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Stackable")]
  public bool stackable;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Equipable")]
  public bool equipable;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Equip mode")]
  public string equipMode = "model";

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Equip slot")]
  public string equipSlot = "";

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Equip skill")]
  public string equipSkill = "";

  //FIXME: deprecate?
  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Equip remove helmet")]
  public bool equipRemoveHelmet;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Equip bone")]
  public string equipBoneName = "";

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Equip position (metres)")]
  public Vector3 equipPos = new Vector3(0f, 0f, 0f);

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Equip direction (euler degrees)")]
  public Vector3 equipDir = new Vector3(0f, 0f, 0f);

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Volume override")]
  public float volumeOverride;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Carriable")]
  public bool carriable;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Part attach")]
  public ItemAttachMode allowPartAttach = ItemAttachMode.AllowedWithKisTool;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Static attach")]
  public ItemAttachMode allowStaticAttach = ItemAttachMode.Disabled;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Static attach break force")]
  public float staticAttachBreakForce = 10;

  [KSPField(isPersistant = true)]
  public bool staticAttached;
  #endregion

  FixedJoint staticAttachJoint;

  #region IHasDebugAdjustables implementation
  List<KIS_Item> dbgEquippedItems;

  /// <summary>Logs all the part's model objects.</summary>
  [Debug.KISDebugAdjustable("Dump active kerbal's model hierarchy")]
  public void ShowHirerachyDbgAction() {
    if (FlightGlobals.ActiveVessel.isEVA) {
      var p = FlightGlobals.ActiveVessel.rootPart;
      DebugEx.Warning("Objects hierarchy in: {0}", p);
      DebugGui.DumpHierarchy(p.transform, p.transform);
    } else {
      DebugEx.Warning("The active vessel is not EVA!");
    }
  }

  /// <inheritdoc/>
  public virtual void OnBeforeDebugAdjustablesUpdate() {
    dbgEquippedItems = FlightGlobals.Vessels
        .Where(v => v.isEVA)
        .Select(v => v.rootPart)
        .SelectMany(p => p.Modules.OfType<ModuleKISInventory>())
        .SelectMany(inv => inv.items.Values)
        .Where(item => item.carried || item.equipped)
        .ToList();
    dbgEquippedItems.ForEach(item => item.Unequip());
  }

  /// <inheritdoc/>
  public virtual void OnDebugAdjustablesUpdated() {
    dbgEquippedItems.ForEach(item => item.Equip());
    dbgEquippedItems = null;
  }
  #endregion

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
        HostedDebugLog.Warning(
            this, "Item {0} has been destroyed. Drop it from inventory of {1}",
            item.availablePart.title, inventory.part);
        AsyncCall.CallOnEndOfFrame(inventory, () => inventory.DeleteItem(item.slot));
      }
    }
  }
  #endregion

  #region IKISInventoryItem candidates
  public virtual void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom) {
  }

  public virtual void OnDragToPart(KIS_Item item, Part destPart) {
  }

  public virtual void OnDragToInventory(KIS_Item item, ModuleKISInventory destInventory,
                                        int destSlot) {
  }

  /// <summary>Called when an item equips.</summary>
  /// <remarks>
  /// Note, that the module that gets the callback is not be the actual module of the equipped
  /// part (e.g. when the equip mode is "model"). It's a prefab module. Use <paramref name="item"/>
  /// to reach to the actual part/module of the item. Note, that in some equipping modes there may
  /// be no live part for the item.
  /// </remarks>
  /// <param name="item">The item the action is executed for.</param>
  public virtual void OnEquip(KIS_Item item) {
  }

  /// <summary>Called when the item unequips.</summary>
  /// <remarks>
  /// Note, that the module that gets the callback is not be the actual module of the equipped
  /// part (e.g. when the equip mode is "model"). It's a prefab module. Use <paramref name="item"/>
  /// to reach to the actual part/module of the item. Note, that in some equipping modes there may
  /// be no live part for the item.
  /// </remarks>
  /// <param name="item">The item the action is executed for.</param>
  public virtual void OnUnEquip(KIS_Item item) {
  }
  #endregion

  #region IsPackable implementation
  /// <inheritdoc/>
  public virtual void OnPartPack() {
  }

  /// <inheritdoc/>
  public virtual void OnPartUnpack() {
    if (allowStaticAttach == ItemAttachMode.Disabled) {
      return;
    }
    if (staticAttached) {
      HostedDebugLog.Warning(this, "Re-attach static object (OnPartUnpack)");
      GroundAttach();
    }
  }
  #endregion

  public void OnKISAction(Dictionary<string, object> eventData) {
    if (allowStaticAttach == ItemAttachMode.Disabled) {
      return;
    }
    var action = eventData["action"].ToString();
    var tgtPart = eventData["targetPart"] as Part;
    //FIXME: use enum values 
    if (action == KIS_Shared.MessageAction.Store.ToString()
        || action == KIS_Shared.MessageAction.DropEnd.ToString()
        || action == KIS_Shared.MessageAction.AttachStart.ToString()) {
      GroundDetach();
    }
    if (action == KIS_Shared.MessageAction.AttachEnd.ToString() && tgtPart == null) {
      GroundAttach();
    }
  }

  public void GroundAttach() {
    staticAttached = true;
    StartCoroutine(WaitAndStaticAttach());
  }

  public void GroundDetach() {
    if (staticAttached) {
      HostedDebugLog.Warning(this, "Removing static rigidbody and fixed joint on: {0}", part);
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
        equipable && !carriable
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

    HostedDebugLog.Warning(this, "Create fixed joint attached to the world");
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
      HostedDebugLog.Warning(this, "A static joint has just been broken! Force: {0}", breakForce);
    } else {
      HostedDebugLog.Warning(this, "A fixed joint has just been broken! Force: {0}", breakForce);
    }
    GroundDetach();
  }
  #endregion
}

}  // namespace
