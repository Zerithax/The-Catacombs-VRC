using UnityEngine;

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

        [Tooltip("A list of all Element Types needed to produce this Recipe")]
        public ElementTypes[] requiredElementTypes;
    }
}