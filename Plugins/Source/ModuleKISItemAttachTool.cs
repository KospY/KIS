﻿using System;
using System.Linq;
using System.Text;

namespace KIS
{

    public class ModuleKISItemAttachTool : ModuleKISItem
    {
        [KSPField]
        public bool toolPartAttach = true;
        [KSPField]
        public bool toolStaticAttach = false;
        [KSPField]
        public bool toolPartStack = false;
        [KSPField]
        public string attachPartSndPath = "KIS/Sounds/attachPart";
        [KSPField]
        public string detachPartSndPath = "KIS/Sounds/detachPart";
        [KSPField]
        public string attachStaticSndPath = "KIS/Sounds/attachStatic";
        [KSPField]
        public string detachStaticSndPath = "KIS/Sounds/detachStatic";

        private string orgAttachPartSndPath, orgDetachPartSndPath, orgAttachStaticSndPath, orgDetachStaticSndPath;
        private bool orgToolPartAttach, orgToolStaticAttach, orgToolPartStack;

        public override string GetInfo()
        {
            var sb = new StringBuilder();
            if (toolPartStack)
            {
                sb.AppendLine("Allow snap attach on stack node");
            }
            return sb.ToString();
        }

        public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom)
        {
            // Check if grab key is pressed
            if (useFrom == KIS_Item.UseFrom.KeyDown)
            {
                KISAddonPickup.instance.EnableAttachMode();
            }
            if (useFrom == KIS_Item.UseFrom.KeyUp)
            {
                KISAddonPickup.instance.DisableAttachMode();
            }     
        }
        
        public override void OnEquip(KIS_Item item)
        {
            ModuleKISPickup pickupModule = item.inventory.part.GetComponent<ModuleKISPickup>();
            if (pickupModule)
            {
                orgToolPartAttach = pickupModule.allowPartAttach;
                orgToolStaticAttach = pickupModule.allowStaticAttach;
                orgToolPartStack = pickupModule.allowPartStack;
                pickupModule.allowPartAttach = toolPartAttach;
                pickupModule.allowStaticAttach = toolStaticAttach;
                pickupModule.allowPartStack = toolPartStack;

                orgAttachPartSndPath = pickupModule.attachPartSndPath;
                pickupModule.attachPartSndPath = attachPartSndPath;
                orgDetachPartSndPath = pickupModule.detachPartSndPath;
                pickupModule.detachPartSndPath = detachPartSndPath;

                orgAttachStaticSndPath = pickupModule.attachStaticSndPath;
                pickupModule.attachStaticSndPath = attachStaticSndPath;
                orgDetachStaticSndPath = pickupModule.detachStaticSndPath;
                pickupModule.detachStaticSndPath = detachStaticSndPath;
            }
        }

        public override void OnUnEquip(KIS_Item item)
        {
            ModuleKISPickup pickupModule = item.inventory.part.GetComponent<ModuleKISPickup>();
            if (pickupModule)
            {
                pickupModule.allowPartAttach = orgToolPartAttach;
                pickupModule.allowStaticAttach = orgToolStaticAttach;
                pickupModule.allowPartStack = orgToolPartStack;

                pickupModule.attachPartSndPath = orgAttachPartSndPath;
                pickupModule.detachPartSndPath = orgDetachPartSndPath;

                pickupModule.attachStaticSndPath = orgAttachStaticSndPath;
                pickupModule.detachStaticSndPath = orgDetachStaticSndPath;
            }
        }

    }
}