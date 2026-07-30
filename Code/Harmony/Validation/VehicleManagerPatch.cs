using ColossalFramework.Math;
using HarmonyLib;
using SleepyCommon;
using UnityEngine;
using static TransferManagerCore.CustomTransferReason;

namespace TransferManagerCore
{
    // ------------------------------------------------------------------------
    [HarmonyPatch]
    public class VehicleManagerPatch
    {
        // IndustriesMeetsSunsetHarborMod - for houseparts or cars trailers we need to set special gateindex so the trailer will show
        [HarmonyPatch(typeof(VehicleManager), "CreateVehicle")]
        [HarmonyPostfix]
        public static void CreateVehicle(VehicleManager __instance, ref ushort vehicle, ref Randomizer r, VehicleInfo info, Vector3 position, TransferManager.TransferReason type, bool transferToSource, bool transferToTarget, ref bool __result)
        {
            if(!TransferManagerExtendedMod.IsIndustriesMeetsSunsetHarborRunning)
            {
                return;
            }

            if (__result && ((int)type == (int)Reason.Cars || (int)type == (int)Reason.HouseParts))
            {
                if ((int)type == (int)Reason.Cars)
                {
                    __instance.m_vehicles.m_buffer[vehicle].m_gateIndex = 1;
                }
                else if ((int)type == (int)Reason.HouseParts)
                {
                    __instance.m_vehicles.m_buffer[vehicle].m_gateIndex = 9;
                }
            }
        }


        // --------------------------------------------------------------------
        // DEBUGGING, check cargo vehicle arrays are still valid
        [HarmonyPatch(typeof(VehicleManager), "ReleaseVehicle")]
        [HarmonyPrefix]
        public static void ReleaseVehicle(ushort vehicle)
        {
            Vehicle vehicleData = VehicleManager.instance.m_vehicles.m_buffer[vehicle];
            Log.Error($"Releasing vehicle: {vehicle} Flags: {vehicleData.m_flags} BlockCounter: {vehicleData.m_blockCounter}");
        }
    }
}