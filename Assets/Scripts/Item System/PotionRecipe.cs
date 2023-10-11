using System.Linq;
using UnityEditor;
using UnityEngine;
using Boo.Lang;

namespace Argus.ItemSystem.Editor
{
    //This ScriptableObject manages the different combinations of elements needed to convert a container's element into a new type

    [CreateAssetMenu(fileName = "New Potion Recipe", menuName = "Catacombs/PotionRecipe")]
    public class PotionRecipe : ScriptableObject
    {
        [Tooltip("The Element Type this Recipe converts a Container's Liquid into")]
        public ElementTypes potionRecipeId;

        [Tooltip("The Element Type required in liquid form to produce this Recipe (if None, any liquid can be used)")]
        public ElementTypes requiredLiquidType;

        [Tooltip("Whether or not this potion updates the crafted Element's color with the liquid's")]
        public bool updateColor;

        [Tooltip("A list of all Element Types needed to produce this Recipe")]
        public ElementTypes[] requiredElementTypes;
    }

    [CustomEditor(typeof(PotionRecipe))]
    public class PotionRecipeEditor : UnityEditor.Editor
    {
        private ElementTypeManager elementTypeManager;

        public override void OnInspectorGUI()
        {
            if (elementTypeManager == null) elementTypeManager = FindObjectOfType<ElementTypeManager>();

            var elementTypes = ItemEditorUtility.GetAllElementTypes().OrderBy(o => o.elementTypeId).ToList();

            serializedObject.Update();

            List<string> excludedProperties = new List<string>();

            var myBehaviour = target as PotionRecipe;

            if (myBehaviour.requiredLiquidType != ElementTypes.None)
            {
                excludedProperties.Add("updateColor");
                myBehaviour.updateColor = false;
            }
            else
            {
                excludedProperties.Remove("updateColor");
            }

            DrawPropertiesExcluding(serializedObject, excludedProperties.ToArray());

            serializedObject.ApplyModifiedProperties();
        }
    }
}