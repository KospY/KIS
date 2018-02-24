﻿using KSPDev.ConfigUtils;
using KSPDev.LogUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KIS {

[KSPAddon(KSPAddon.Startup.Instantly, false)]
[PersistentFieldsDatabase("KIS/settings/KISConfig")]
sealed class KISAddonConfig : MonoBehaviour {
  [PersistentField("StackableItemOverride/partName", isCollection = true)]
  public readonly static List<string> stackableList = new List<string>();

  [PersistentField("StackableModule/moduleName", isCollection = true)]
  public readonly static List<string> stackableModules = new List<string>();

  [PersistentField("Global/breathableAtmoPressure")]
  public readonly static float breathableAtmoPressure = 0.5f;

  const string MaleKerbalEva = "kerbalEVA";
  const string FemaleKerbalEva = "kerbalEVAfemale";
  const string RdKerbalEva = "kerbalEVA_RD";

  static ConfigNode nodeSettings;

  class KISConfigLoader: LoadingSystem
  {
    public KISAddonConfig owner;

    public override bool IsReady ()
    {
      return true;
    }

    public override void StartLoad ()
    {
      ConfigAccessor.ReadFieldsInType(owner.GetType(), owner);
      ConfigAccessor.ReadFieldsInType(typeof(ModuleKISInventory), instance: null);

      // Set inventory module for every eva kerbal
      DebugEx.Info("Set KIS config...");
      nodeSettings = GameDatabase.Instance.GetConfigNode("KIS/settings/KISConfig");
      if (nodeSettings == null) {
        DebugEx.Error("KIS settings.cfg not found or invalid !");
      }
    }
  }

  class KISPodInventoryLoader: LoadingSystem
  {
    public bool done;

    int loadedPartCount;
    int loadedPartIndex;

    IEnumerator LoadInventories ()
    {
      // Kerbal parts.
      UpdateEvaPrefab(MaleKerbalEva, nodeSettings);
      UpdateEvaPrefab(FemaleKerbalEva, nodeSettings);

      // Set inventory module for every pod with crew capacity.
      DebugEx.Info("Loading pod inventories...");
      for (loadedPartIndex = 0; loadedPartIndex < loadedPartCount; loadedPartIndex++) {
        AvailablePart avPart = PartLoader.LoadedPartsList[loadedPartIndex];
        if (!(avPart.name == MaleKerbalEva || avPart.name == FemaleKerbalEva
              || avPart.name == RdKerbalEva
              || !avPart.partPrefab || avPart.partPrefab.CrewCapacity < 1)) {
          DebugEx.Fine("Found part with CrewCapacity: {0}", avPart.name);
          AddPodInventories (avPart.partPrefab, avPart.partPrefab.CrewCapacity);
        }
        yield return null;
      }
      done = true;
    }

    public override bool IsReady ()
    {
      return done;
    }

    public override float ProgressFraction ()
    {
      return (float)loadedPartIndex / (float)loadedPartCount;
    }

    public override string ProgressTitle ()
    {
      return "Kerbal Inventory System";
    }

    public override void StartLoad ()
    {
      done = false;
      loadedPartCount = PartLoader.LoadedPartsList.Count;
      StartCoroutine (LoadInventories ());
    }
  }

  public void Awake() {
    List<LoadingSystem> list = LoadingScreen.Instance.loaders;
    if (list != null) {
      for (int i = 0; i < list.Count; i++) {
        if (list[i] is KISConfigLoader) {
          (list[i] as KISConfigLoader).owner = this;
        }
        if (list[i] is KISPodInventoryLoader) {
          (list[i] as KISPodInventoryLoader).done = false;
        }
        if (list[i] is PartLoader) {
          GameObject go = new GameObject();

          var invLoader = go.AddComponent<KISPodInventoryLoader> ();
          // Cause the pod inventory loader to run AFTER the part loader.
          list.Insert (i + 1, invLoader);

          var cfgLoader = go.AddComponent<KISConfigLoader> ();
          cfgLoader.owner = this;
          // Cause the config loader to run BEFORE the part loader this ensures
          // that the KIS configs are loaded after Module Manager has run but
          // before any parts are loaded so KIS aware part modules can add
          // pod inventories as necessary.
          list.Insert (i, cfgLoader);
          break;
        }
      }
    }
  }

  public static void AddPodInventories (Part part, int crewCapacity)
  {
    for (int i = 0; i < crewCapacity; i++) {
      try {
        var moduleInventory =
          part.AddModule(typeof(ModuleKISInventory).Name) as ModuleKISInventory;
        KIS_Shared.AwakePartModule(moduleInventory);
        var baseFields = new BaseFieldList(moduleInventory);
        baseFields.Load(nodeSettings.GetNode("EvaInventory"));
        moduleInventory.podSeat = i;
        moduleInventory.invType = ModuleKISInventory.InventoryType.Pod;
        DebugEx.Fine("Pod inventory module(s) for seat {0} loaded successfully", i);
      } catch {
        DebugEx.Error("Pod inventory module(s) for seat {0} can't be loaded!", i);
      }
    }
  }

  /// <summary>Load config of EVA modules for the requested part name.</summary>
  static void UpdateEvaPrefab(string partName, ConfigNode nodeSettings) {
    var prefab = PartLoader.getPartInfoByName(partName).partPrefab;
    if (LoadModuleConfig(prefab, typeof(ModuleKISInventory),
                         nodeSettings.GetNode("EvaInventory"))) {
      prefab.GetComponent<ModuleKISInventory>().invType = ModuleKISInventory.InventoryType.Eva;
    }
    LoadModuleConfig(prefab, typeof(ModuleKISPickup), nodeSettings.GetNode("EvaPickup"));
  }

  /// <summary>Loads config values for the part's module fro the provided config node.</summary>
  /// <returns><c>true</c> if loaded successfully.</returns>
  static bool LoadModuleConfig(Part p, Type moduleType, ConfigNode node) {
    var module = p.GetComponent(moduleType);
    if (module == null) {
      DebugEx.Warning(
          "Config node for module {0} in part {1} is NULL. Nothing to load!", moduleType, p);
      return false;
    }
    if (node == null) {
      DebugEx.Warning("Cannot find module {0} on part {1}. Config not loaded!", moduleType, p);
      return false;
    }
    var baseFields = new BaseFieldList(module);
    baseFields.Load(node);
    DebugEx.Info("Loaded config for {0} on part {1}", moduleType, p);
    return true;
  }
}

}  // namespace
