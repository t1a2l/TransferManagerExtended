using ColossalFramework;
using SleepyCommon;
using System;
using System.Collections.Generic;
using System.Text;
using TransferManagerCore.CustomManager;
using UnityEngine;
using static TransferManager;
using static TransferManagerCore.CustomTransferReason;

namespace TransferManagerCore
{
    public class TransferManagerUtils
    {
        public enum OutsideConnectionDirection
        {
            None,
            Both,
            In,
            Out,
        }

        public static string GetDistanceKm(CustomTransferOffer offer1, CustomTransferOffer offer2)
        {
            return (Math.Sqrt(Vector3.SqrMagnitude(offer1.Position - offer2.Position)) * 0.001).ToString("00.000");
        }

        public static string DebugOffer(Reason material, CustomTransferOffer offer, bool bAlign, bool bNode, bool bDistrict)
        {
            StringBuilder stringBuilder = new StringBuilder();

            // Direction
            stringBuilder.Append(offer.IsIncoming() ? "IN  | " : "OUT | ");

            // Describe object
            string sMessage = InstanceHelper.DescribeInstance(offer.m_object, true, true);
            if (bAlign)
            {
                sMessage = SleepyCommon.Utils.PadToWidth(sMessage, 60, false);
            }
            stringBuilder.Append(sMessage);

            // Add object type
            string sType = "";
            if (offer.m_object.Type != InstanceType.Building)
            {
                sType = SleepyCommon.Utils.PadToWidth($" | {offer.m_object.Type}", 14);
            }
            else
            {
                sType = SleepyCommon.Utils.PadToWidth($" | {BuildingTypeHelper.GetBuildingType(offer.m_object.Building)}", 14);
            }
            if (bAlign) sType = SleepyCommon.Utils.PadToWidth(sType, 20);

            // Build string to return
            stringBuilder.Append(sType);
            stringBuilder.Append($" | Priority:{offer.Priority}");
            stringBuilder.Append(offer.Active ? " | Active " : " | Passive");
            stringBuilder.Append($" | Amount:{offer.Amount.ToString("000")}");
            stringBuilder.Append(offer.Unlimited ? "*" : " ");
            stringBuilder.Append($" | Park:{offer.LocalPark.ToString("000")}");

            ushort buildingId = offer.GetBuilding();
            Building building = BuildingManager.instance.m_buildings.m_buffer[buildingId];

            // Add building specific information
            if (buildingId != 0)
            {
                stringBuilder.Append($" | Building:{buildingId.ToString("00000")}");
            }
            else if (bAlign)
            {
                stringBuilder.Append($" | Building:     ");
            }

            if (TransferManagerModes.IsWarehouseMaterial(material))
            {
                // Incoming timer
                if (offer.IsIncoming())
                {
                    if (!offer.IsOutside() && buildingId != 0 && building.m_flags != 0 && building.m_incomingProblemTimer > 0)
                    {
                        stringBuilder.Append($" | IT:{building.m_incomingProblemTimer.ToString("000")}");
                    }
                    else if (bAlign)
                    {
                        stringBuilder.Append($" | IT:   ");
                    }
                }
                else
                {
                    if (!offer.IsOutside() && buildingId != 0 && building.m_flags != 0 && building.m_outgoingProblemTimer > 0)
                    {
                        stringBuilder.Append($" | OT:{SleepyCommon.Utils.PadToWidth(building.m_outgoingProblemTimer.ToString(), 3, true)}");
                    }
                    else if (bAlign)
                    {
                        stringBuilder.Append($" | OT:   ");
                    }
                }

                stringBuilder.Append($" | Transport: {SleepyCommon.Utils.PadToWidth(offer.GetTransportType().ToString(), 6)}");
            }
            else
            {
                switch (material)
                {
                    case CustomTransferReason.Reason.Sick:
                    case CustomTransferReason.Reason.Sick2:
                    case CustomTransferReason.Reason.SickMove:
                        {
                            if (offer.IsOutgoing())
                            {
                                // Add sick timer
                                if (buildingId != 0 && building.m_flags != 0)
                                {
                                    stringBuilder.Append($" | ST:{building.m_healthProblemTimer.ToString("000")}");
                                }
                                else
                                {
                                    stringBuilder.Append($" | ST:   ");
                                }

                                // Add citizen health
                                if (offer.Citizen != 0)
                                {
                                    int iHealth = Singleton<CitizenManager>.instance.m_citizens.m_buffer[offer.Citizen].m_health;
                                    stringBuilder.Append($" | Health:{iHealth.ToString("000")}");
                                }
                                else
                                {
                                    stringBuilder.Append($" | Health:   ");
                                }
                            }
                            
                            break;
                        }
                    case CustomTransferReason.Reason.Dead:
                    case CustomTransferReason.Reason.DeadMove:
                        {
                            if (building.m_flags != 0)
                            {
                                stringBuilder.Append($" | DT:{building.m_deathProblemTimer.ToString("000")}");
                            }
                            else
                            {
                                stringBuilder.Append($" | DT:   ");
                            }
                            break;
                        }
                    case CustomTransferReason.Reason.ChildCare:
                    case CustomTransferReason.Reason.ElderCare:
                        {
                            if (offer.IsIncoming())
                            {
                                if (offer.Citizen != 0)
                                {
                                    Citizen citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[offer.Citizen];
                                    stringBuilder.Append($" | Age: {citizen.m_age.ToString("000")} | Health:{citizen.m_health.ToString("000")}");
                                }
                                else
                                {
                                    stringBuilder.Append($" | Age:     | Health:   ");
                                }
                            }
                            break;
                        }
                    case CustomTransferReason.Reason.Garbage:
                        {
                            if (buildingId != 0 && building.m_flags != 0 && !offer.IsIncoming())
                            {
                                stringBuilder.Append($" | Garbage:{building.m_garbageBuffer.ToString("0000")}"); 
                            }
                            else
                            {
                                stringBuilder.Append($" | Garbage:    ");
                            }
                            break;
                        }
                    case CustomTransferReason.Reason.Worker0:
                    case CustomTransferReason.Reason.Worker1:
                    case CustomTransferReason.Reason.Worker2:
                    case CustomTransferReason.Reason.Worker3:
                        {
                            if (offer.IsIncoming())
                            {
                                if (buildingId != 0 && building.m_flags != 0)
                                {
                                    stringBuilder.Append($" | WT:{building.m_workerProblemTimer.ToString("000")}");
                                }
                                else
                                {
                                    stringBuilder.Append($" | WT:   ");
                                }

                                if (buildingId != 0)
                                {
                                    int iWorkers = BuildingUtils.GetCurrentWorkerCount(buildingId, building, out int worker0, out int worker1, out int worker2, out int worker3);
                                    int iPlaces = BuildingUtils.GetTotalWorkerPlaces(buildingId, building, out int workPlaces0, out int workPlaces1, out int workPlaces2, out int workPlaces3);
                                    float fPercent = ((float)iWorkers / (float)iPlaces) * 100.0f;

                                    // Workers
                                    stringBuilder.Append(SleepyCommon.Utils.PadToWidth($" | Workers:{iWorkers}/{iPlaces} ({fPercent.ToString("00")}%)", 30));

                                    // Worker Levels
                                    stringBuilder.Append(SleepyCommon.Utils.PadToWidth($"| W0:{worker0}/{workPlaces0} W1:{worker1}/{workPlaces1} W2:{worker2}/{workPlaces2} W3:{worker3}/{workPlaces3}", 36));
                                }
                            } 
                            else if (offer.IsOutgoing())                     
                            {
                                if (offer.Citizen != 0)
                                {
                                    Citizen citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[offer.Citizen];
                                    stringBuilder.Append(SleepyCommon.Utils.PadToWidth($" | Education: {citizen.EducationLevel}", 26));
                                }
                            }

                            break;
                        }
                }
            }
            
            // Is it an outside connection
            if (offer.IsOutside())
            {
                stringBuilder.Append(" | Outside ");
            }
            else
            {
                stringBuilder.Append(" | Internal");
            }

            // Force calculation when requested
            if (bNode)
            {
                stringBuilder.Append($" | Node:{offer.GetNearestNode(material).ToString("00000")}");
            }

            // Only add if requested
            if (bDistrict)
            {
                stringBuilder.Append($" | District:{offer.GetDistrict().ToString("000")} | Area:{offer.GetArea().ToString("000")}");

                if (bAlign)
                {
                    // Pad district setting so it aligns
                    stringBuilder.Append($" | DistrictR:{SleepyCommon.Utils.PadToWidth(offer.GetDistrictRestriction(material).ToString(), 24, false)}");
                }
                else
                {
                    stringBuilder.Append($" | DistrictR:{offer.GetDistrictRestriction(material)}");
                }

                // Also add building restrictions
                stringBuilder.Append($" | BuildingR:{offer.GetAllowedBuildingList(material).Count.ToString("00")}");
            }

            // Is it a warehouse
            if (offer.IsWarehouse())
            {
                stringBuilder.Append($" | WarehouseMode: {offer.GetWarehouseMode()}");
                stringBuilder.Append($" | Storage: {(offer.GetWarehouseStoragePercent() * 100.0).ToString("00")}%");
            }

            if (offer.IsOutside())
            {
                stringBuilder.Append($" | OutsideCargoPriorityFactor: {offer.GetEffectiveOutsideCargoPriorityFactor()}");
                stringBuilder.Append($" | OutsideCitizenPriorityFactor: {offer.GetEffectiveOutsideCitizenPriorityFactor()}");
            }

            return stringBuilder.ToString();
        }

        public static void CheckRoadAccess(CustomTransferReason.Reason material, TransferOffer offer)
        {
            // Update access segment if using path distance but do it in simulation thread so we don't break anything
            if (offer.Building != 0 && PathDistanceTypes.GetDistanceAlgorithm(material) != PathDistanceTypes.PathDistanceAlgorithm.LineOfSight)
            {
                ref Building building = ref BuildingManager.instance.m_buildings.m_buffer[offer.Building];
                if (building.m_accessSegment == 0 &&
                    (building.m_flags & Building.Flags.RoadAccessFailed) == 0 &&
                    (building.m_problems & new Notification.ProblemStruct(Notification.Problem1.RoadNotConnected, Notification.Problem2.NotInPedestrianZone)).IsNone &&
                    building.Info.GetAI() is not OutsideConnectionAI)
                {
                    // See if we can update m_accessSegment.
                    building.Info.m_buildingAI.CheckRoadAccess(offer.Building, ref building);
                    if (building.m_accessSegment == 0)
                    {
                        RoadAccessStorage.AddInstance(new InstanceID { Building = offer.Building });
                    }
                }
            }
        }

        public static OutsideConnectionDirection GetOutsideConnectionDirection(ushort buildingId)
        {
            Building building = BuildingManager.instance.m_buildings.m_buffer[buildingId];

            if ((building.m_flags & Building.Flags.Incoming) != 0 && (building.m_flags & Building.Flags.Outgoing) != 0)
            {
                return OutsideConnectionDirection.Both;
            }
            else if ((building.m_flags & Building.Flags.Incoming) != 0)
            {
                return OutsideConnectionDirection.Out; // Incoming means towards the OC. So OUT from the cities perspective
            }
            else if ((building.m_flags & Building.Flags.Outgoing) != 0)
            {
                return OutsideConnectionDirection.In;  // Outgoing means towards the City. So IN from the cities perspective
            }

            return OutsideConnectionDirection.None;
        }

        public static List<string> GetExtendedTransferReasons()
        {
            return
            [
                "MealsDeliveryLow",
                "MealsDeliveryMedium",
                "MealsDeliveryHigh",
                "Anchovy",
                "Salmon",
                "Shellfish",
                "Tuna",
                "Algae",
                "Seaweed",
                "Mussels",
                "Trout",
                "Milk",
                "RawHides",
                "Pork",
                "Fruits",
                "Vegetables",
                "Wool",
                "Cotton",
                "Cows",
                "HighlandCows",
                "Sheep",
                "Pigs",
                "ProcessedVegetableOil",
                "LiquidConcentrates",
                "FishMeal",
                "FishOil",
                "ChemicalProducts",
                "Leather",
                "FoodProducts",
                "BeverageProducts",
                "BakedGoods",
                "CannedFish",
                "Furnitures",
                "ElectronicProducts",
                "IndustrialSteel",
                "Tupperware",
                "Toys",
                "PrintedProducts",
                "TissuePaper",
                "Cloths",
                "PetroleumProducts",
                "Cars",
                "Footwear",
                "HouseParts",
                "Ship",
                "ConstructionResources",
                "OperationResources",
                "MealsLow",
                "MealsMedium",
                "MealsHigh",
                "PoliceVanCriminalMove",
                "PrisonHelicopterCriminalPickup",
                "PrisonHelicopterCriminalMove",
                "CarRent",
                "CarBuy",
                "CarSell",
                "VehicleFuel",
                "VehicleFuelElectric",
                "VehicleWash",
                "VehicleMinorRepair",
                "VehicleMajorRepair",
                "VehicleOutOfFuel",
                "VehicleBrokenDown"
            ];
        }

        public static string GetTransferReasonName(int transferInt)
        {
            string s = transferInt switch
            {
                150 => "MealsDeliveryLow",
                151 => "MealsDeliveryMedium",
                152 => "MealsDeliveryHigh",
                153 => "Anchovy",
                154 => "Salmon",
                155 => "Shellfish",
                156 => "Tuna",
                157 => "Algae",
                158 => "Seaweed",
                159 => "Mussels",
                160 => "Trout",
                161 => "Milk",
                162 => "RawHides",
                163 => "Pork",
                164 => "Fruits",
                165 => "Vegetables",
                166 => "Wool",
                167 => "Cotton",
                168 => "Cows",
                169 => "HighlandCows",
                170 => "Sheep",
                171 => "Pigs",
                172 => "ProcessedVegetableOil",
                173 => "LiquidConcentrates",
                174 => "FishMeal",
                175 => "FishOil",
                176 => "ChemicalProducts",
                177 => "Leather",
                178 => "FoodProducts",
                179 => "BeverageProducts",
                180 => "BakedGoods",
                181 => "CannedFish",
                182 => "Furnitures",
                183 => "ElectronicProducts",
                184 => "IndustrialSteel",
                185 => "Tupperware",
                186 => "Toys",
                187 => "PrintedProducts",
                188 => "TissuePaper",
                189 => "Cloths",
                190 => "PetroleumProducts",
                191 => "Cars",
                192 => "Footwear",
                193 => "HouseParts",
                194 => "Ship",
                195 => "ConstructionResources",
                196 => "OperationResources",
                220 => "MealsLow",
                221 => "MealsMedium",
                222 => "MealsHigh",
                223 => "PoliceVanCriminalMove",
                224 => "PrisonHelicopterCriminalPickup",
                225 => "PrisonHelicopterCriminalMove",
                226 => "CarRent",
                227 => "CarBuy",
                228 => "CarSell",
                229 => "VehicleFuel",
                230 => "VehicleFuelElectric",
                231 => "VehicleWash",
                232 => "VehicleMinorRepair",
                233 => "VehicleMajorRepair",
                234 => "VehicleOutOfFuel",
                235 => "VehicleBrokenDown",
                _ => ""
            };

            return s;
        }

        public static string GetTransferReasonDescription(int transferInt)
        {
            string s = transferInt switch
            {
                153 => "Anchovy is gathered by Anchovy fish harbor.",
                154 => "Salmon is gathered by Salmon fish harbor.",
                155 => "Shellfish is gathered by Shellfish fish harbor.",
                156 => "Tuna is gathered by Tuna fish harbor.",
                157 => "Algae is gathered by Algae fish farm.",
                158 => "Seaweed is gathered by Seaweed fish farm.",
                159 => "Mussels is gathered by Mussel fish farm.",
                160 => "Trout is gathered by Trout fish farm.",
                161 => "Milk is produced by Milking Parlours.",
                162 => "RawHides are produced by Slaughterhouses to create Leather.",
                163 => "Pork is produced by Slaughterhouses.",
                164 => "Fruits are produced by Fruit Fields.",
                165 => "Vegetables are produced by Potatoes Fields, Corn Fields and Greeenhouses.",
                166 => "Wool is produced from Sheep in Animal Pastures.",
                167 => "Cotton is produced by Cotton Fields.",
                172 => "Processed Vegetable Oil is produced in a Vegetable Oil Mill and require Crops and Vegetables.",
                173 => "Liquid Concentrates is produced in a Pressing Plant and require Fruits and Vegetables.",
                174 => "Fish Meal is produced in a Fish Meal Factory from raw fish. Used as input for Fish Hatcheries.",
                175 => "Fish Oil is produced in a Fish Meal Factory as a byproduct of fish processing.",
                176 => "Chemical Products are produced in a Chemical Plant and require Processed Vegetable Oil, Petroleum and Metals.",
                177 => "Leather is produced in a Tannery and require Raw Hides and Chemical Products.",
                178 => "FoodProducts are produced in a Food Factory and require Red Meat/Pork, Flour, Milk, Processed Vegetable Oil, Vegetables/Fruits, Paper and Plastics.",
                179 => "BeverageProducts are produced in a Beverage Factory and require Liquid Concentrates/Milk, Crops, Glass and Plastics.",
                180 => "BakedGoods are produced in a Bakery and require Flour, Milk and Fruits.",
                181 => "CannedFish is produced in a Seafood Factory and require Salmon/Tuna/Trout, Processed Vegetable Oil, Algae/Seaweed, Plastics and Metals.",
                182 => "Furnitures are produced in a Furniture Factory and require Planed Timber, Leather/Cotton, Chemical Products and Paper.",
                183 => "ElectronicProducts are produced in a Electronics Factory and require Metals, Glass and Plastics.",
                184 => "IndustrialSteel is produced in a Industrial Steel Plant and require Metals.",
                185 => "Tupperware is produced in a Household Plastic Factory and require Chemical Products, Processed Vegetable Oil and Plastics.",
                186 => "Toys are produced in a Toy Factory and require Planed Timber, Cotton/Wool, Chemical Products and Plastics.",
                187 => "PrintedProducts are produced in a Printing Press and require Paper, Chemical Products, Processed Vegetable Oil and Plastics.",
                188 => "TissuePaper is produced in a Soft Paper Factory and require Cotton, Paper, Chemical Products and Plastics.",
                189 => "Cloths are produced in a Clothing Factory and require Cotton/Wool, Leather and Plastics/Paper.",
                190 => "PetroleumProducts are produced in a Petroleum Refinery and require Metals, Patroleum and Plastics.",
                191 => "Cars are produced in a Car Factory and require Metals, Leather, Plastics, Chemical Products and Glass.",
                192 => "Footwear is produced in a Sneaker Factory and require Planed Timber, Cotton/Leather, Plastics and Chemical Products.",
                193 => "HouseParts are produced in a Modular House Factory and require Chemical Products, Metals/Planed Timber, Paper/Plastics and Glass.",
                194 => "Ship is produced in a Shipyard and require Planed Timber/Metals, Plastics/Glass, Chemical Products and Leather / Cotton.",
                _ => ""
            };

            return s;
        }

        public static int GetResourcePrice(Reason material, ItemClass.Service sourceService = ItemClass.Service.None)
        {
            if ((int)material < (int)Reason.MealsDeliveryLow)
            {
                return IndustryBuildingAI.GetResourcePrice((TransferReason)material, sourceService);
            }

            int value = material switch
            {
                // ── Raw agricultural (= Grain tier 200) ──────────────────
                Reason.Fruits => 200,
                Reason.Vegetables => 200,
                Reason.Cotton => 200,

                // ── Live animals (= Ore tier 300) ────────────────────────
                Reason.Cows => 300,
                Reason.HighlandCows => 300,
                Reason.Sheep => 300,
                Reason.Pigs => 300,

                // ── Raw animal products (= Oil tier 400) ─────────────────
                Reason.Milk => 400,
                Reason.RawHides => 400,
                Reason.Wool => 400,

                // ── Fish variants (= Fish tier 600) ──────────────────────
                Reason.Anchovy => 600,
                Reason.Salmon => 600,
                Reason.Shellfish => 600,
                Reason.Tuna => 600,
                Reason.Algae => 600,
                Reason.Seaweed => 600,
                Reason.Mussels => 600,
                Reason.Trout => 600,

                // ── Tier 1 processed (= AnimalProducts/Paper tier 1500) ──
                Reason.Pork => 1500,
                Reason.ProcessedVegetableOil => 1500,
                Reason.FoodProducts => 1500,
                Reason.BeverageProducts => 1500,
                Reason.BakedGoods => 1500,
                Reason.TissuePaper => 1500,
                Reason.PrintedProducts => 1500,
                Reason.Tupperware => 1500,

                // ── Tier 2 processed (= Glass/Metals tier 2250) ──────────
                Reason.IndustrialSteel => 2250,
                Reason.HouseParts => 2250,
                Reason.CannedFish => 2000,
                Reason.Leather => 2000,
                Reason.LiquidConcentrates => 2000,
                Reason.FishMeal => 2000,
                Reason.FishOil => 2000,
                Reason.Cloths => 2500,
                Reason.Footwear => 2500,

                // ── Tier 3 processed (= Petroleum/Plastics tier 3000) ────
                Reason.ChemicalProducts => 3000,
                Reason.PetroleumProducts => 3000,
                Reason.Furnitures => 3000,
                Reason.Toys => 3000,

                // ── High value (= LuxuryProducts tier 10000) ─────────────
                Reason.ElectronicProducts => 5000,
                Reason.Cars => 8000,
                Reason.Ship => 10000,

                _ => 0
            };

            return UniqueFacultyAI.IncreaseByBonus(UniqueFacultyAI.FacultyBonus.Science, value);
        }
    }
}

