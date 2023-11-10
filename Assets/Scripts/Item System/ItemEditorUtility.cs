#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Catacombs.ElementSystem.Runtime;
using UnityEditor;
#endif

namespace Argus.ItemSystem.Editor
{
#if UNITY_EDITOR
    public static class ItemEditorUtility
    {
        //Refresh all items in the database
        [MenuItem("Argus/Item System/Refresh Database")]
        public static void RefreshItemDatabase()
        {
            AllElements = null;
            GetAllElementTypes();

            AllPotions = null;
            GetAllPotionRecipes();
        }

        public static void RefreshElementTypes()
        {
            AllElements = null;
            GetAllElementTypes();
        }

        public static void RefreshPotionRecipes()
        {
            AllPotions = null;
            GetAllPotionRecipes();
        }

        private static List<ElementType> AllElements;

        public static List<ElementType> GetAllElementTypes()
        {
            if (AllElements != null) return AllElements.OrderBy(i => i.elementTypeId).ToList();

            string[] assets = AssetDatabase.FindAssets($"t:{nameof(ElementType)}");
            AllElements = assets.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ElementType>).OrderBy(i => i.elementTypeId).ToList();

            return AllElements;
        }

        private static List<PotionRecipe> AllPotions;

        public static List<PotionRecipe> GetAllPotionRecipes()
        {
            if (AllPotions != null) return AllPotions.OrderBy(i => i.potionRecipeId).ToList();

            string[] assets = AssetDatabase.FindAssets($"t:{nameof(PotionRecipe)}");
            AllPotions = assets.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<PotionRecipe>).OrderBy(i => i.potionRecipeId).ToList();

            return AllPotions;
        }
    }
#endif
}