using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Catacombs.ElementSystem.Runtime;

namespace Argus.ItemSystem.Editor
{
    public static class ItemEditorUtility
    {
        //Refresh all items in the database
        [MenuItem("Argus/Item System/Refresh Database")]
        public static void RefreshItemDatabase()
        {
            AllItems = null;
            GetAllElementTypes();
        }

        private static List<ElementType> AllItems;

        public static List<ElementType> GetAllElementTypes()
        {
            if (AllItems != null) return AllItems.OrderBy(i => i.elementTypeId).ToList();

            string[] assets = AssetDatabase.FindAssets($"t:{nameof(ElementType)}");
            AllItems = assets.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ElementType>).OrderBy(i => i.elementTypeId).ToList();

            return AllItems;
        }
    }
}