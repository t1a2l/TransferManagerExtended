using ColossalFramework;
using ColossalFramework.Globalization;
using HarmonyLib;
using TransferManagerCore;

namespace TransferManagerExtended.Harmony.TransferManager
{
    [HarmonyPatch]
    public static class LocalePatch
    {
        [HarmonyPatch(typeof(Locale), "Get", [typeof(string), typeof(string)], [ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static bool Get(Locale __instance, string id, string key, ref string __result)
        {
            if (SingletonLite<LocaleManager>.exists && !string.IsNullOrEmpty(id) && TransferManagerExtendedMod.IsIndustriesMeetsSunsetHarborRunning)
            {
                if (id == "WAREHOUSEPANEL_RESOURCE")
                {
                    if (int.TryParse(key, out int num))
                    {
                        var name = TransferManagerUtils.GetTransferReasonName(num);
                        if (name != null)
                        {
                            __result = name;
                            return false;
                        }
                    }
                    if (key == "AnimalProducts")
                    {
                        __result = "RedMeat";
                        return false;
                    }
                    if (key == "LuxuryProducts")
                    {
                        __result = "Jewelry";
                        return false;
                    }
                }
                else if (id == "RESOURCEDESCRIPTION")
                {
                    if (int.TryParse(key, out int num))
                    {
                        var name = TransferManagerUtils.GetTransferReasonDescription(num);
                        if (name != null)
                        {
                            __result = name;
                            return false;
                        }
                    }
                    if (key == "Grain")
                    {
                        __result = "Grain is produced by Wheat Fields.";
                        return false;
                    }
                    if (key == "Crops")
                    {
                        __result = "Crops are produced by Wheat Fields.";
                        return false;
                    }
                    if (key == "AnimalProducts")
                    {
                        __result = "RedMeat is produced by Slaughterhouses.";
                        return false;
                    }
                    if (key == "LuxuryProducts")
                    {
                        __result = "Jewelry is produced in a Jewelry Workshop and requires Metals, Glass, Chemical Products and Leather/Cotton.";
                        return false;
                    }
                }
                else if (id == "RESOURCEUNIT_TONS" || id == "RESOURCEUNIT_BARRELS")
                {
                    if (int.TryParse(key, out int num))
                    {
                        var name = TransferManagerUtils.GetTransferReasonName(num);
                        if (name != null)
                        {
                            switch (name)
                            {
                                case "Cows":
                                case "HighlandCows":
                                case "Sheep":
                                case "Pigs":
                                    __result = "heads";
                                    return false;
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
