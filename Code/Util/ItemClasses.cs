using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TransferManagerExtended.Util
{
    public static class ItemClasses
    {

        public static readonly ItemClass industrialSteelTransporterVehicle = CreateIndustrialSteelTransporterItemClass("Industrial Steel Transporter Vehicle");

        public static readonly ItemClass carTransporterVehicle = CreateCarTransporterItemClass("Car Transporter Vehicle");

        public static readonly ItemClass modularHomeTransporterVehicle = CreateModularHomeTransporterItemClass("Modular Home Transporter Vehicle");

        public static void Register()
        {
            var dictionary = (Dictionary<string, ItemClass>)typeof(ItemClassCollection).GetField("m_classDict", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            if (!dictionary.ContainsKey(industrialSteelTransporterVehicle.name))
            {
                dictionary.Add(industrialSteelTransporterVehicle.name, industrialSteelTransporterVehicle);
            }
            if (!dictionary.ContainsKey(carTransporterVehicle.name))
            {
                dictionary.Add(carTransporterVehicle.name, carTransporterVehicle);
            }
            if (!dictionary.ContainsKey(modularHomeTransporterVehicle.name))
            {
                dictionary.Add(modularHomeTransporterVehicle.name, modularHomeTransporterVehicle);
            }
        }

        public static void Unregister()
        {
            var dictionary = (Dictionary<string, ItemClass>)typeof(ItemClassCollection).GetField("m_classDict", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            dictionary.Remove(industrialSteelTransporterVehicle.name);
            dictionary.Remove(carTransporterVehicle.name);
            dictionary.Remove(modularHomeTransporterVehicle.name);
        }

        private static ItemClass CreateIndustrialSteelTransporterItemClass(string name)
        {
            var createInstance = ScriptableObject.CreateInstance<ItemClass>();
            createInstance.name = name;
            createInstance.m_level = ItemClass.Level.Level3;
            createInstance.m_service = ItemClass.Service.PlayerIndustry;
            createInstance.m_subService = ItemClass.SubService.None;
            return createInstance;
        }

        private static ItemClass CreateCarTransporterItemClass(string name)
        {
            var createInstance = ScriptableObject.CreateInstance<ItemClass>();
            createInstance.name = name;
            createInstance.m_level = ItemClass.Level.Level4;
            createInstance.m_service = ItemClass.Service.PlayerIndustry;
            createInstance.m_subService = ItemClass.SubService.None;
            return createInstance;
        }

        private static ItemClass CreateModularHomeTransporterItemClass(string name)
        {
            var createInstance = ScriptableObject.CreateInstance<ItemClass>();
            createInstance.name = name;
            createInstance.m_level = ItemClass.Level.Level5;
            createInstance.m_service = ItemClass.Service.PlayerIndustry;
            createInstance.m_subService = ItemClass.SubService.None;
            return createInstance;
        }
    }
}
