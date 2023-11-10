using System.Linq;
using UnityEngine;
using VRC.SDK3.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

#if UNITY_EDITOR
    [CustomEditor(typeof(PotionRecipe))]
    public class PotionRecipeEditor : UnityEditor.Editor
    {
        private ElementTypeManager elementTypeManager;

        private DataList excludedProperties = new DataList();

        public override void OnInspectorGUI()
        {
            if (elementTypeManager == null) elementTypeManager = FindObjectOfType<ElementTypeManager>();

            var elementTypes = ItemEditorUtility.GetAllElementTypes().OrderBy(o => o.elementTypeId).ToList();

            serializedObject.Update();

            //List<string> excludedProperties = new List<string>();

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

            string[] excludedPropertiesStrings = new string[excludedProperties.Count];

            for (int i = 0; i < excludedProperties.Count; i++)
            {
                if (excludedProperties.TryGetValue(i, out DataToken value))
                {
                    excludedPropertiesStrings[i] = value.String;
                }
            }

            DrawPropertiesExcluding(serializedObject, excludedPropertiesStrings);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}