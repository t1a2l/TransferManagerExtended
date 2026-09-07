using HarmonyLib;
using ColossalFramework;
using UnityEngine;
using TransferManagerCore.Settings;
using SleepyCommon;

namespace TransferManagerCore
{
    [HarmonyPatch]
    public class CargoTruckAIPatch : VehicleAIPatch
    {
        // --------------------------------------------------------------------
        // Try and find a customer for this resource
        [HarmonyPatch(typeof(CargoTruckAI), "SimulationStep")]
        [HarmonyPostfix]
        public static void SimulationStep(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
        {
            if (ModSettings.GetSettings().CargoTruckAI)
            {
                // Request new locations for trucks already out in the city
                if (UnityEngine.Random.Range(0, 10) == 0 &&
                    (data.m_flags & Vehicle.Flags.TransferToTarget) != 0 &&
                    (data.m_flags & Vehicle.Flags.WaitingTarget) == 0 &&
                    (data.m_flags & Vehicle.Flags.Arriving) == 0 &&
                    (data.m_flags & Vehicle.Flags.WaitingPath) == 0 &&
                    data.m_targetBuilding == 0 &&
                    !ShouldReturnToSource(vehicleID, ref data) &&
                    data.m_cargoParent == 0 &&
                    (data.m_flags & Vehicle.Flags.Spawned) != 0 &&
                    (data.m_flags & Vehicle.Flags.GoingBack) != 0 &&
                    data.m_transferSize > 2000)
                {
                    //CDebug.Log($"Adding Offer - Vehicle: {vehicleID} Flags: {data.m_flags} Material: {(CustomTransferReason.Reason)data.m_transferType} TransferSize: {data.m_transferSize} Parent: {data.m_cargoParent} Source: {data.m_sourceBuilding} Target: {data.m_targetBuilding}");

                    TransferManager.TransferOffer offer = default;
                    offer.Vehicle = vehicleID;
                    offer.Priority = 7;
                    offer.Position = GetCargoVehicleOfferPosition(vehicleID, ref data);
                    offer.Amount = 1;
                    offer.Active = true;
                    Singleton<TransferManager>.instance.AddOutgoingOffer((TransferManager.TransferReason)data.m_transferType, offer);

                    data.m_flags &= ~Vehicle.Flags.GoingBack;
                    data.m_flags |= Vehicle.Flags.WaitingTarget;
                }
            }
        }

        [HarmonyPatch(typeof(CargoTruckAI), "GetLocalizedStatus")]
        [HarmonyPrefix]
        public static bool GetLocalizedStatus(ushort vehicleID, ref Vehicle data, ref InstanceID target, ref string __result)
        {
            if (!TransferManagerMod.IsIndustriesMeetsSunsetHarborRunning)
            {
                return true;
            }

            if ((data.m_flags & Vehicle.Flags.TransferToTarget) != 0)
            {
                ushort targetBuilding = data.m_targetBuilding;
                if ((data.m_flags & Vehicle.Flags.GoingBack) != 0)
                {
                    target = InstanceID.Empty;
                    return true;
                }
                if ((data.m_flags & Vehicle.Flags.WaitingTarget) != 0)
                {
                    target = InstanceID.Empty;
                    return true;
                }
                if (targetBuilding != 0)
                {
                    Building.Flags flags = Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetBuilding].m_flags;
                    if (data.m_transferType >= 153 && data.m_transferType <= 193)
                    {
                        CustomTransferReason.Reason transferReason = (CustomTransferReason.Reason)data.m_transferType;
                        if ((data.m_flags & Vehicle.Flags.Exporting) != 0 || (flags & Building.Flags.IncomingOutgoing) != 0)
                        {
                            target = InstanceID.Empty;
                            __result = "Exporting " + transferReason.ToString();
                            return false;
                        }
                        if ((data.m_flags & Vehicle.Flags.Importing) != 0)
                        {
                            target = InstanceID.Empty;
                            target.Building = targetBuilding;
                            __result = "Importing " + transferReason.ToString() + " to";
                            return false;
                        }
                        target = InstanceID.Empty;
                        target.Building = targetBuilding;
                        __result = "Delivering " + transferReason.ToString() + " to";
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return true;
        }

    }
}