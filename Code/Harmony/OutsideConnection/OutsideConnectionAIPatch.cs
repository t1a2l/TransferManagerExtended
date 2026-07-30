using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using ICities;
using SleepyCommon;
using TransferManagerCore.Settings;
using UnityEngine;

namespace TransferManagerCore
{
    [HarmonyPatch]
    public static class OutsideConnectionAIPatch
    {
        private static bool s_bInAddConnectionOffers = false;

        public static bool IsInAddConnectionOffers
        {
            get { return s_bInAddConnectionOffers; }
        }

        private delegate int DummyTrafficProbabilityDelegate();
        private static readonly DummyTrafficProbabilityDelegate DummyTrafficProbability = AccessTools.MethodDelegate<DummyTrafficProbabilityDelegate>(typeof(OutsideConnectionAI).GetMethod("DummyTrafficProbability", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic), null, false);

        [HarmonyPatch(typeof(OutsideConnectionAI), "AddConnectionOffers")]
        [HarmonyPrefix]
        public static bool AddConnectionOffersPrefix(OutsideConnectionAI __instance, ushort buildingID, ref Building data, int productionRate, int cargoCapacity, int residentCapacity, int touristFactor0, int touristFactor1, int touristFactor2, TransferManager.TransferReason dummyTrafficReason, int dummyTrafficFactor)
        {
            s_bInAddConnectionOffers = true;

            if (!DependencyUtils.IsAdvancedOutsideConnectionsRunning() &&
                OutsideConnectionSettings.HasSettings(buildingID))
            {
                OutsideConnectionSettings settings = OutsideConnectionSettings.GetSettings(buildingID);

                cargoCapacity = settings.m_cargoCapacity;
                residentCapacity = settings.m_residentCapacity;
                touristFactor0 = settings.m_touristFactor0;
                touristFactor1 = settings.m_touristFactor1;
                touristFactor2 = settings.m_touristFactor2;
                dummyTrafficFactor = settings.m_dummyTrafficFactor;
            }

            if (!TransferManagerExtendedMod.IsIndustriesMeetsSunsetHarborRunning)
            {
                return true;
            }

            AddConnectionOffers(__instance, buildingID, ref data, productionRate, cargoCapacity, residentCapacity, touristFactor0, touristFactor1, touristFactor2, dummyTrafficReason, dummyTrafficFactor);

            return false;
        }

        [HarmonyPatch(typeof(OutsideConnectionAI), "AddConnectionOffers")]
        [HarmonyPostfix]
        public static void AddConnectionOffersPostfix(ushort buildingID, ref int cargoCapacity, ref int residentCapacity, ref int touristFactor0, ref int touristFactor1, ref int touristFactor2, ref int dummyTrafficFactor)
        {
            s_bInAddConnectionOffers = false;
        }

        private static bool AddConnectionOffers(OutsideConnectionAI outsideConnectionInstance, ushort buildingID, ref Building data, int productionRate, int cargoCapacity, int residentCapacity, int touristFactor0, int touristFactor1, int touristFactor2, TransferManager.TransferReason dummyTrafficReason, int dummyTrafficFactor)
        {
            SimulationManager instance = Singleton<SimulationManager>.instance;
            TransferManager instance2 = Singleton<TransferManager>.instance;
            DistrictManager instance3 = Singleton<DistrictManager>.instance;
            byte district = instance3.GetDistrict(data.m_position);
            DistrictPolicies.CityPlanning cityPlanningPolicies = instance3.m_districts.m_buffer[district].m_cityPlanningPolicies;
            DistrictPolicies.Services servicePolicies = instance3.m_districts.m_buffer[district].m_servicePolicies;
            if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.BoostConnections) != DistrictPolicies.CityPlanning.None)
            {
                instance3.m_districts.m_buffer[district].m_cityPlanningPoliciesEffect |= DistrictPolicies.CityPlanning.BoostConnections;
                touristFactor0 += (touristFactor0 + 3) / 5;
                touristFactor1 += (touristFactor1 + 3) / 5;
                touristFactor2 += (touristFactor2 + 3) / 5;
            }
            BuildingInfo info = data.Info;
            int num;
            if (info.m_class.m_service == ItemClass.Service.Road)
            {
                num = Singleton<BuildingManager>.instance.m_finalMonumentEffect[5].m_factor;
            }
            else
            {
                ItemClass.SubService subService = info.m_class.m_subService;
                num = ((subService == ItemClass.SubService.PublicTransportShip) ? Singleton<BuildingManager>.instance.m_finalMonumentEffect[8].m_factor : 0);
            }
            if (num != 0)
            {
                touristFactor0 += (touristFactor0 * num + 50) / 100;
                touristFactor1 += (touristFactor1 * num + 50) / 100;
                touristFactor2 += (touristFactor2 * num + 50) / 100;
            }
            cargoCapacity = (cargoCapacity * productionRate + 99) / 100;
            residentCapacity = (residentCapacity * productionRate + 99) / 100;
            touristFactor0 = (touristFactor0 * productionRate + 99) / 100;
            touristFactor1 = (touristFactor1 * productionRate + 99) / 100;
            touristFactor2 = (touristFactor2 * productionRate + 99) / 100;
            dummyTrafficFactor = (dummyTrafficFactor * productionRate + 99) / 100;
            dummyTrafficFactor = (dummyTrafficFactor * DummyTrafficProbability() + 99) / 100;
            int num2 = (cargoCapacity + instance.m_randomizer.Int32(16u)) / 16;
            int num3 = (residentCapacity + instance.m_randomizer.Int32(16u)) / 16;
            if ((data.m_flags & Building.Flags.Outgoing) != Building.Flags.None)
            {
                int num4 = TickPathfindStatus(buildingID, ref data, BuildingAI.PathFindType.LeavingHuman);
                int num5 = TickPathfindStatus(buildingID, ref data, BuildingAI.PathFindType.LeavingCargo);
                int num6 = TickPathfindStatus(buildingID, ref data, BuildingAI.PathFindType.LeavingDummy);
                TransferManager.TransferOffer offer = new()
                {
                    Building = buildingID,
                    Unlimited = true,
                    Position = data.m_position * ((float)instance.m_randomizer.Int32(100, 400) * 0.01f),
                    Active = true
                };
                int num7 = num2;
                if (num7 != 0)
                {
                    if (num7 * num5 + instance.m_randomizer.Int32(256u) >> 8 == 0)
                    {
                        offer.Priority = 0;
                        offer.Amount = 1;
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Ore, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Oil, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Grain, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Logs, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Goods, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Coal, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Petrol, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Food, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Lumber, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.SortedMail, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.IncomingMail, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.FoodProducts, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.BeverageProducts, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.BakedGoods, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.CannedFish, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Furnitures, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.ElectronicProducts, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.IndustrialSteel, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Tupperware, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Toys, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.PrintedProducts, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.TissuePaper, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Cloths, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.PetroleumProducts, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Cars, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Footwear, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.HouseParts, offer);
                        }
                    }
                    else
                    {
                        offer.Priority = 0;
                        offer.Amount = num2;
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.Ore, offer);
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.Oil, offer);
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.Grain, offer);
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.Logs, offer);
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.Goods, offer);
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.Coal, offer);
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.Petrol, offer);
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.Food, offer);
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.Lumber, offer);
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.SortedMail, offer);
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.IncomingMail, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.FoodProducts, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.BeverageProducts, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.BakedGoods, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.CannedFish, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Furnitures, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.ElectronicProducts, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.IndustrialSteel, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Tupperware, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Toys, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.PrintedProducts, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.TissuePaper, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Cloths, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.PetroleumProducts, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Cars, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Footwear, offer);
                        instance2.AddOutgoingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.HouseParts, offer);
                    }
                }
                int num8 = Singleton<ZoneManager>.instance.GetIncomingResidentDemand() * num3 / 100;
                if (num8 > 0)
                {
                    num8 = num8 * num4 + instance.m_randomizer.Int32(256u) >> 8;
                    if (num8 == 0)
                    {
                        offer.Priority = 0;
                        offer.Amount = 1;
                        if (instance.m_randomizer.Int32(2u) == 0)
                        {
                            if (instance.m_randomizer.Int32(16u) == 0)
                            {
                                instance2.AddOutgoingOffer(TransferManager.TransferReason.Single0, offer);
                            }
                            if (instance.m_randomizer.Int32(16u) == 0)
                            {
                                instance2.AddOutgoingOffer(TransferManager.TransferReason.Single1, offer);
                            }
                            if (instance.m_randomizer.Int32(16u) == 0)
                            {
                                instance2.AddOutgoingOffer(TransferManager.TransferReason.Single2, offer);
                            }
                            if (instance.m_randomizer.Int32(16u) == 0)
                            {
                                instance2.AddOutgoingOffer(TransferManager.TransferReason.Single3, offer);
                            }
                        }
                        else
                        {
                            if (instance.m_randomizer.Int32(16u) == 0)
                            {
                                instance2.AddOutgoingOffer(TransferManager.TransferReason.Single0B, offer);
                            }
                            if (instance.m_randomizer.Int32(16u) == 0)
                            {
                                instance2.AddOutgoingOffer(TransferManager.TransferReason.Single1B, offer);
                            }
                            if (instance.m_randomizer.Int32(16u) == 0)
                            {
                                instance2.AddOutgoingOffer(TransferManager.TransferReason.Single2B, offer);
                            }
                            if (instance.m_randomizer.Int32(16u) == 0)
                            {
                                instance2.AddOutgoingOffer(TransferManager.TransferReason.Single3B, offer);
                            }
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Family0, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Family1, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Family2, offer);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Family3, offer);
                        }
                    }
                    else
                    {
                        offer.Priority = 0;
                        offer.Amount = num8;
                        if (instance.m_randomizer.Int32(2u) == 0)
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Single0, offer);
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Single1, offer);
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Single2, offer);
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Single3, offer);
                        }
                        else
                        {
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Single0B, offer);
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Single1B, offer);
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Single2B, offer);
                            instance2.AddOutgoingOffer(TransferManager.TransferReason.Single3B, offer);
                        }
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.Family0, offer);
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.Family1, offer);
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.Family2, offer);
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.Family3, offer);
                    }
                }
                int num9 = (dummyTrafficFactor + instance.m_randomizer.Int32(100u)) / 100;
                if (num9 > 0 && dummyTrafficReason != TransferManager.TransferReason.None)
                {
                    num9 = num9 * num6 + instance.m_randomizer.Int32(256u) >> 8;
                    if (num9 == 0)
                    {
                        offer.Priority = 7;
                        offer.Amount = 1;
                        if (instance.m_randomizer.Int32(4u) == 0)
                        {
                            instance2.AddOutgoingOffer(dummyTrafficReason, offer);
                        }
                    }
                    else
                    {
                        offer.Priority = 7;
                        offer.Amount = num9;
                        instance2.AddOutgoingOffer(dummyTrafficReason, offer);
                    }
                }
                int num10 = Singleton<ImmaterialResourceManager>.instance.CheckActualTourismResource();
                int finalHomeOrWorkCount = (int)Singleton<DistrictManager>.instance.m_districts.m_buffer[0].m_residentialData.m_finalHomeOrWorkCount;
                finalHomeOrWorkCount = (100 * finalHomeOrWorkCount + 20000) / Mathf.Max(finalHomeOrWorkCount + 20000, 20000);
                int num11 = finalHomeOrWorkCount * num10;
                int num12 = (num11 * touristFactor0 + instance.m_randomizer.Int32(160000u)) / 160000;
                int num13 = (num11 * touristFactor1 + instance.m_randomizer.Int32(160000u)) / 160000;
                int num14 = (num11 * touristFactor2 + instance.m_randomizer.Int32(160000u)) / 160000;
                num11 = num12 + num13 + num14;
                if (num11 != 0)
                {
                    if (num11 * num4 + instance.m_randomizer.Int32(256u) >> 8 == 0)
                    {
                        offer.Priority = instance.m_randomizer.Int32(8u);
                        offer.Amount = 1;
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            switch (instance.m_randomizer.Int32(8u))
                            {
                                case 0:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.Shopping, offer);
                                    break;
                                case 1:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingB, offer);
                                    break;
                                case 2:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingC, offer);
                                    break;
                                case 3:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingD, offer);
                                    break;
                                case 4:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingE, offer);
                                    break;
                                case 5:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingF, offer);
                                    break;
                                case 6:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingG, offer);
                                    break;
                                case 7:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingH, offer);
                                    break;
                            }
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            switch (instance.m_randomizer.Int32(8u))
                            {
                                case 0:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.Entertainment, offer);
                                    break;
                                case 1:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.EntertainmentB, offer);
                                    break;
                                case 2:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.EntertainmentC, offer);
                                    break;
                                case 3:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.EntertainmentD, offer);
                                    break;
                                case 4:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.TouristA, offer);
                                    break;
                                case 5:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.TouristB, offer);
                                    break;
                                case 6:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.TouristC, offer);
                                    break;
                                case 7:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.TouristD, offer);
                                    break;
                            }
                        }
                        if (Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.Hotels) && instance.m_randomizer.Int32(16u) == 0)
                        {
                            switch (instance.m_randomizer.Int32(8u))
                            {
                                case 0:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.BusinessA, offer);
                                    break;
                                case 1:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.BusinessB, offer);
                                    break;
                                case 2:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.BusinessC, offer);
                                    break;
                                case 3:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.BusinessD, offer);
                                    break;
                                case 4:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.NatureA, offer);
                                    break;
                                case 5:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.NatureB, offer);
                                    break;
                                case 6:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.NatureC, offer);
                                    break;
                                case 7:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.NatureD, offer);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        offer.Priority = instance.m_randomizer.Int32(8u);
                        int num15 = num12 + num13 + num14;
                        if (info.m_class.m_service == ItemClass.Service.Road && (servicePolicies & DistrictPolicies.Services.TouristTravelCard) != DistrictPolicies.Services.None)
                        {
                            num15 = num15 * 108 / 100;
                        }
                        offer.Amount = num15;
                        switch (instance.m_randomizer.Int32(8u))
                        {
                            case 0:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.Shopping, offer);
                                break;
                            case 1:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingB, offer);
                                break;
                            case 2:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingC, offer);
                                break;
                            case 3:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingD, offer);
                                break;
                            case 4:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingE, offer);
                                break;
                            case 5:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingF, offer);
                                break;
                            case 6:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingG, offer);
                                break;
                            case 7:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.ShoppingH, offer);
                                break;
                        }
                        switch (instance.m_randomizer.Int32(8u))
                        {
                            case 0:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.Entertainment, offer);
                                break;
                            case 1:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.EntertainmentB, offer);
                                break;
                            case 2:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.EntertainmentC, offer);
                                break;
                            case 3:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.EntertainmentD, offer);
                                break;
                            case 4:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.TouristA, offer);
                                break;
                            case 5:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.TouristB, offer);
                                break;
                            case 6:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.TouristC, offer);
                                break;
                            case 7:
                                instance2.AddIncomingOffer(TransferManager.TransferReason.TouristD, offer);
                                break;
                        }
                        if (Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.Hotels))
                        {
                            switch (instance.m_randomizer.Int32(8u))
                            {
                                case 0:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.BusinessA, offer);
                                    break;
                                case 1:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.BusinessB, offer);
                                    break;
                                case 2:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.BusinessC, offer);
                                    break;
                                case 3:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.BusinessD, offer);
                                    break;
                                case 4:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.NatureA, offer);
                                    break;
                                case 5:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.NatureB, offer);
                                    break;
                                case 6:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.NatureC, offer);
                                    break;
                                case 7:
                                    instance2.AddIncomingOffer(TransferManager.TransferReason.NatureD, offer);
                                    break;
                            }
                        }
                    }
                }
            }
            if ((data.m_flags & Building.Flags.Incoming) == 0)
            {
                return false;
            }
            int num16 = TickPathfindStatus(buildingID, ref data, BuildingAI.PathFindType.EnteringHuman);
            int num17 = TickPathfindStatus(buildingID, ref data, BuildingAI.PathFindType.EnteringCargo);
            int num18 = TickPathfindStatus(buildingID, ref data, BuildingAI.PathFindType.EnteringDummy);
            TransferManager.TransferOffer offer2 = new()
            {
                Building = buildingID,
                Unlimited = true,
                Position = data.m_position * ((float)instance.m_randomizer.Int32(100, 400) * 0.01f),
                Active = false
            };
            int num19 = num2;
            if (num19 != 0)
            {
                if (num19 * num17 + instance.m_randomizer.Int32(256u) >> 8 == 0)
                {
                    offer2.Priority = 0;
                    offer2.Amount = 1;
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Ore, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Oil, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Grain, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Logs, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Goods, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Coal, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Petrol, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Food, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Lumber, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.UnsortedMail, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.OutgoingMail, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Glass, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Metals, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Petroleum, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Plastics, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.AnimalProducts, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Flours, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Paper, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.PlanedTimber, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.LuxuryProducts, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Fish, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Anchovy, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Salmon, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Shellfish, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Tuna, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Algae, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Seaweed, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Trout, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Milk, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Pork, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Fruits, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Vegetables, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Cows, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.HighlandCows, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Sheep, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Pigs, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.FoodProducts, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.BeverageProducts, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.BakedGoods, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.CannedFish, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Furnitures, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.ElectronicProducts, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.IndustrialSteel, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Tupperware, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Toys, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.PrintedProducts, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.TissuePaper, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Cloths, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.PetroleumProducts, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Cars, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Footwear, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.HouseParts, offer2);
                    }
                }
                else
                {
                    offer2.Priority = 0;
                    offer2.Amount = num2;
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Ore, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Oil, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Grain, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Logs, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Goods, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Coal, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Petrol, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Food, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Lumber, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.UnsortedMail, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.OutgoingMail, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Glass, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Metals, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Petroleum, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Plastics, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.AnimalProducts, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Flours, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Paper, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.PlanedTimber, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.LuxuryProducts, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Fish, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Anchovy, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Salmon, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Shellfish, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Tuna, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Algae, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Seaweed, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Trout, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Milk, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Pork, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Fruits, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Vegetables, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Cows, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.HighlandCows, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Sheep, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Pigs, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.FoodProducts, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.BeverageProducts, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.BakedGoods, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.CannedFish, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Furnitures, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.ElectronicProducts, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.IndustrialSteel, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Tupperware, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Toys, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.PrintedProducts, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.TissuePaper, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Cloths, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.PetroleumProducts, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Cars, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.Footwear, offer2);
                    instance2.AddIncomingOffer((TransferManager.TransferReason)CustomTransferReason.Reason.HouseParts, offer2);
                }
            }
            int num20 = num3;
            if (num20 > 0)
            {
                if (num20 * num16 + instance.m_randomizer.Int32(256u) >> 8 == 0)
                {
                    offer2.Priority = 0;
                    offer2.Amount = 1;
                    if (instance.m_randomizer.Int32(2u) == 0)
                    {
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddIncomingOffer(TransferManager.TransferReason.Single0, offer2);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddIncomingOffer(TransferManager.TransferReason.Single1, offer2);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddIncomingOffer(TransferManager.TransferReason.Single2, offer2);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddIncomingOffer(TransferManager.TransferReason.Single3, offer2);
                        }
                    }
                    else
                    {
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddIncomingOffer(TransferManager.TransferReason.Single0B, offer2);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddIncomingOffer(TransferManager.TransferReason.Single1B, offer2);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddIncomingOffer(TransferManager.TransferReason.Single2B, offer2);
                        }
                        if (instance.m_randomizer.Int32(16u) == 0)
                        {
                            instance2.AddIncomingOffer(TransferManager.TransferReason.Single3B, offer2);
                        }
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Family0, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Family1, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Family2, offer2);
                    }
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Family3, offer2);
                    }
                }
                else
                {
                    offer2.Priority = 0;
                    offer2.Amount = num3;
                    if (instance.m_randomizer.Int32(2u) == 0)
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Single0, offer2);
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Single1, offer2);
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Single2, offer2);
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Single3, offer2);
                    }
                    else
                    {
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Single0B, offer2);
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Single1B, offer2);
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Single2B, offer2);
                        instance2.AddIncomingOffer(TransferManager.TransferReason.Single3B, offer2);
                    }
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Family0, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Family1, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Family2, offer2);
                    instance2.AddIncomingOffer(TransferManager.TransferReason.Family3, offer2);
                }
            }
            int num21 = (dummyTrafficFactor + instance.m_randomizer.Int32(100u)) / 100;
            if (num21 > 0 && dummyTrafficReason != TransferManager.TransferReason.None)
            {
                num21 = num21 * num18 + instance.m_randomizer.Int32(256u) >> 8;
                if (num21 == 0)
                {
                    offer2.Priority = 7;
                    offer2.Amount = 1;
                    if (instance.m_randomizer.Int32(4u) == 0)
                    {
                        instance2.AddIncomingOffer(dummyTrafficReason, offer2);
                    }
                }
                else
                {
                    offer2.Priority = 7;
                    offer2.Amount = num21;
                    instance2.AddIncomingOffer(dummyTrafficReason, offer2);
                }
            }
            int num22 = Mathf.Max(1, (int)Singleton<DistrictManager>.instance.m_districts.m_buffer[0].m_residentialData.m_finalHomeOrWorkCount);
            int num23 = (num22 + instance.m_randomizer.Int32(10u)) / 10;
            int num24 = (num23 * touristFactor0 + instance.m_randomizer.Int32(16000u)) / 16000;
            int num25 = (num23 * touristFactor1 + instance.m_randomizer.Int32(16000u)) / 16000;
            int num26 = (num23 * touristFactor2 + instance.m_randomizer.Int32(16000u)) / 16000;
            if (num24 != 0)
            {
                num24 = num24 * num16 + instance.m_randomizer.Int32(256u) >> 8;
                if (num24 == 0)
                {
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        offer2.Priority = instance.m_randomizer.Int32(8u);
                        offer2.Amount = 1;
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.LeaveCity0, offer2);
                    }
                }
                else
                {
                    offer2.Priority = instance.m_randomizer.Int32(8u);
                    offer2.Amount = num24;
                    instance2.AddOutgoingOffer(TransferManager.TransferReason.LeaveCity0, offer2);
                }
            }
            if (num25 != 0)
            {
                num25 = num25 * num16 + instance.m_randomizer.Int32(256u) >> 8;
                if (num25 == 0)
                {
                    if (instance.m_randomizer.Int32(16u) == 0)
                    {
                        offer2.Priority = instance.m_randomizer.Int32(8u);
                        offer2.Amount = 1;
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.LeaveCity1, offer2);
                    }
                }
                else
                {
                    offer2.Priority = instance.m_randomizer.Int32(8u);
                    offer2.Amount = num25;
                    instance2.AddOutgoingOffer(TransferManager.TransferReason.LeaveCity1, offer2);
                }
            }
            if (num26 == 0)
            {
                return false;
            }
            num26 = num26 * num16 + instance.m_randomizer.Int32(256u) >> 8;
            if (num26 == 0)
            {
                if (instance.m_randomizer.Int32(16u) == 0)
                {
                    offer2.Priority = instance.m_randomizer.Int32(8u);
                    offer2.Amount = 1;
                    instance2.AddOutgoingOffer(TransferManager.TransferReason.LeaveCity2, offer2);
                }
            }
            else
            {
                offer2.Priority = instance.m_randomizer.Int32(8u);
                offer2.Amount = num26;
                instance2.AddOutgoingOffer(TransferManager.TransferReason.LeaveCity2, offer2);
            }


            return false;
        }

        private static int TickPathfindStatus(ushort buildingID, ref Building data, BuildingAI.PathFindType type)
        {
            return type switch
            {
                BuildingAI.PathFindType.EnteringCargo => TickPathfindStatus(ref data.m_education3, ref data.m_adults),
                BuildingAI.PathFindType.LeavingCargo => TickPathfindStatus(ref data.m_teens, ref data.m_serviceProblemTimer),
                _ => 0,
            };
        }

        private static int TickPathfindStatus(ref byte success, ref byte failure)
        {
            int result = (success << 8) / Mathf.Max(1, success + failure);
            if (success > failure)
            {
                success = (byte)(success + 1 >> 1);
                failure >>= 1;
            }
            else
            {
                success >>= 1;
                failure = (byte)(failure + 1 >> 1);
            }
            return result;
        }
    }
}
