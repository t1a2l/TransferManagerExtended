using ColossalFramework;
using ColossalFramework.Math;

namespace TransferManagerCore
{
    public class WarehouseUtils
    {
        public enum WarehouseMode
        {
            None,
            Empty,
            Balanced,
            Fill,
        }

        private enum VanillaWarehouseMode
        {
            Balanced,
            Import,
            Export
        }

        public static ushort GetWarehouseBuildingId(ushort buildingId)
        {
            Building building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            if (building.Info.GetAI() is WarehouseStationAI)
            {
                return building.m_parentBuilding;
            }
            else
            {
                return buildingId;
            }
        }

        public static WarehouseMode GetWarehouseMode(ushort buildingId)
        {
            Building building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            return GetWarehouseMode(building);
        }

        public static WarehouseMode GetWarehouseMode(Building building)
        {
            WarehouseMode mode = WarehouseMode.None;

            if (building.m_flags != 0)
            {
                if ((building.m_flags & Building.Flags.Filling) == Building.Flags.Filling)
                {
                    mode = WarehouseMode.Fill;
                }
                else if ((building.m_flags & Building.Flags.Downgrading) == Building.Flags.Downgrading)
                {
                    mode = WarehouseMode.Empty;
                }
                else
                {
                    mode = WarehouseMode.Balanced;
                }
            }

            return mode;
        }

        public static string GetLocalisedWarehouseMode(WarehouseMode mode)
        {
            switch (mode)
            {
                case WarehouseMode.Balanced:
                    {
                        return ColossalFramework.Globalization.Locale.Get("WAREHOUSEPANEL_MODE", VanillaWarehouseMode.Balanced.ToString());
                    }
                case WarehouseMode.Empty:
                    {
                        return ColossalFramework.Globalization.Locale.Get("WAREHOUSEPANEL_MODE", VanillaWarehouseMode.Export.ToString());
                    }
                case WarehouseMode.Fill:
                    {
                        return ColossalFramework.Globalization.Locale.Get("WAREHOUSEPANEL_MODE", VanillaWarehouseMode.Import.ToString());
                    }
            }
            return string.Empty;
        }

        public static VehicleInfo GetExtendedTransferVehicleService(TransferManager.TransferReason material, ItemClass.Level level, ref Randomizer randomizer)
        {
            ItemClass.Service service = ItemClass.Service.Industrial;
            ItemClass.SubService subService = ItemClass.SubService.None;
            switch (material)
            {
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Anchovy:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Salmon:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Shellfish:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Tuna:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Algae:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Seaweed:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Mussels:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Trout:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.FishMeal:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.FishOil:
                    service = ItemClass.Service.Fishing;
                    break;
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Milk:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.RawHides:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Pork:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Fruits:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Vegetables:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Wool:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Cotton:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.ProcessedVegetableOil:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.LiquidConcentrates:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.ChemicalProducts:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Leather:
                    subService = ItemClass.SubService.IndustrialFarming;
                    break;
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Cows:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.HighlandCows:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Sheep:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Pigs:
                    service = ItemClass.Service.PlayerIndustry;
                    subService = ItemClass.SubService.PlayerIndustryFarming;
                    break;
                case (TransferManager.TransferReason)CustomTransferReason.Reason.FoodProducts:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.BeverageProducts:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.BakedGoods:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.CannedFish:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Furnitures:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.ElectronicProducts:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Tupperware:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Toys:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.PrintedProducts:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.TissuePaper:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Cloths:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Footwear:
                    service = ItemClass.Service.PlayerIndustry;
                    level = ItemClass.Level.Level1;
                    break;
                case (TransferManager.TransferReason)CustomTransferReason.Reason.Cars:
                case (TransferManager.TransferReason)CustomTransferReason.Reason.HouseParts:
                    service = ItemClass.Service.PlayerIndustry;
                    level = ItemClass.Level.Level2;
                    break;
                case (TransferManager.TransferReason)CustomTransferReason.Reason.IndustrialSteel:
                    service = ItemClass.Service.PlayerIndustry;
                    level = ItemClass.Level.Level3;
                    break;
                case (TransferManager.TransferReason)CustomTransferReason.Reason.PetroleumProducts:
                    service = ItemClass.Service.Industrial;
                    subService = ItemClass.SubService.IndustrialOil;
                    level = ItemClass.Level.Level1;
                    break;
                default:
                    return Singleton<VehicleManager>.instance.GetRandomVehicleInfo(ref randomizer, service, subService, level);
            }
            return Singleton<VehicleManager>.instance.GetRandomVehicleInfo(ref randomizer, service, subService, level);
        }
    }
}
