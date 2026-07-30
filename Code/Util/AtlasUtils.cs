using ColossalFramework.UI;
using UnityEngine;
using static TransferManagerCore.CustomTransferReason;

namespace TransferManagerExtended.Util
{
    public static class AtlasUtils
    {
        public static string[] SpriteNames =
        [
            "Algae",
            "Anchovy",
            "BakedGoods",
            "BeverageProducts",
            "CannedFish",
            "Cars",
            "ChemicalProducts",
            "Cloths",
            "Cotton",
            "Cows",
            "ElectronicProducts",
            "FishMeal",
            "FishOil",
            "FoodProducts",
            "Footwear",
            "Fruits",
            "Furnitures",
            "HighlandCows",
            "HouseParts",
            "IndustrialSteel",
            "Leather",
            "LiquidConcentrates",
            "Milk",
            "MixedResources",
            "Mussels",
            "PetroleumProducts",
            "Pigs",
            "Pork",
            "PrintedProducts",
            "ProcessedVegetableOil",
            "RawHides",
            "Salmon",
            "Seaweed",
            "Sheep",
            "Shellfish",
            "Ship",
            "TissuePaper",
            "Toys",
            "Trout",
            "Tuna",
            "Tupperware",
            "Vegetables",
            "Wool"
        ];

        public static void CreateAtlas()
        {
            if (TextureUtils.GetAtlas("IndustriesMeetsSunsetHarborAtlas") == null)
            {
                TextureUtils.InitialiseAtlas("IndustriesMeetsSunsetHarborAtlas");
                for (int i = 0; i < SpriteNames.Length; i++)
                {
                    TextureUtils.AddSpriteToAtlas(new Rect(32 * i, 1, 32, 32), SpriteNames[i], "IndustriesMeetsSunsetHarborAtlas");
                }
            }
        }

        public static UITextureAtlas GetResourceAtlas(Reason reason)
        {
            if (reason != Reason.None)
            {
                if (reason >= Reason.MealsDeliveryLow)
                {
                    return TextureUtils.GetAtlas("IndustriesMeetsSunsetHarborAtlas");
                }
            }
            return TextureUtils.InGameAtlas;
        }

        public static string GetSpriteName(Reason transferReason, bool isStorageBuilding = false)
        {
            if (transferReason < Reason.MealsDeliveryLow)
            {
                return IndustryWorldInfoPanel.ResourceSpriteName((TransferManager.TransferReason)transferReason, isStorageBuilding);
            }

            switch (transferReason)
            {
                case Reason.Algae:
                    return "Algae";
                case Reason.Anchovy:
                    return "Anchovy";
                case Reason.BakedGoods:
                    return "BakedGoods";
                case Reason.BeverageProducts:
                    return "BeverageProducts";
                case Reason.CannedFish:
                    return "CannedFish";
                case Reason.Cars:
                    return "Cars";
                case Reason.ChemicalProducts:
                    return "ChemicalProducts";
                case Reason.Cloths:
                    return "Cloths";
                case Reason.Cotton:
                    return "Cotton";
                case Reason.Cows:
                    return "Cows";
                case Reason.ElectronicProducts:
                    return "ElectronicProducts";
                case Reason.FishMeal:
                    return "FishMeal";
                case Reason.FishOil:
                    return "FishOil";
                case Reason.FoodProducts:
                    return "FoodProducts";
                case Reason.Footwear:
                    return "Footwear";
                case Reason.Fruits:
                    return "Fruits";
                case Reason.Furnitures:
                    return "Furnitures";
                case Reason.HighlandCows:
                    return "HighlandCows";
                case Reason.HouseParts:
                    return "HouseParts";
                case Reason.IndustrialSteel:
                    return "IndustrialSteel";
                case Reason.Leather:
                    return "Leather";
                case Reason.LiquidConcentrates:
                    return "LiquidConcentrates";
                case Reason.Milk:
                    return "Milk";
                case Reason.Mussels:
                    return "Mussels";
                case Reason.PetroleumProducts:
                    return "PetroleumProducts";
                case Reason.Pigs:
                    return "Pigs";
                case Reason.Pork:
                    return "Pork";
                case Reason.PrintedProducts:
                    return "PrintedProducts";
                case Reason.ProcessedVegetableOil:
                    return "ProcessedVegetableOil";
                case Reason.RawHides:
                    return "RawHides";
                case Reason.Salmon:
                    return "Salmon";
                case Reason.Seaweed:
                    return "Seaweed";
                case Reason.Sheep:
                    return "Sheep";
                case Reason.Shellfish:
                    return "Shellfish";
                case Reason.Ship:
                    return "Ship";
                case Reason.TissuePaper:
                    return "TissuePaper";
                case Reason.Toys:
                    return "Toys";
                case Reason.Trout:
                    return "Trout";
                case Reason.Tuna:
                    return "Tuna";
                case Reason.Tupperware:
                    return "Tupperware";
                case Reason.Vegetables:
                    return "Vegetables";
                case Reason.Wool:
                    return "Wool";
                default:
                    return "";
            }
        }
    }
}
