using System.Linq;
using UdonSharp;
using UnityEngine;
using UnityEditor;
using Argus.ItemSystem.Editor;

namespace Argus.ItemSystem
{
    public class ElementTypeManager : UdonSharpBehaviour
    {
        [Space(5)]
        public bool isInitialized;

        //A list of instantiated ElementData objects containing each ElementType ScriptableObject's Element details
        [Space(5)]
        public ElementData[] elementDataObjs;
        [Space(3)]
        public Transform elementDataParent;

        //A list of instantiated PotionRecipeData objects containing each PotionRecipe ScriptableObject's Potion details
        [Space(5)]
        public PotionRecipeData[] potionRecipeObjs;
        [Space(3)]
        public Transform potionRecipeDataParent;

        private void Start() { SendCustomEventDelayedSeconds(nameof(_CheckInitialized), 1); }

        [RecursiveMethod]
        public void _CheckInitialized()
        {
            if (elementDataObjs[1] != null)
            {
                isInitialized = true;

                for (int i = 1; i < elementDataObjs.Length; i++)
                {
                    Debug.Log($"[{name}] Found Element {i}: {elementDataObjs[i].name}");
                }

                return;
            }

            SendCustomEventDelayedSeconds(nameof(_CheckInitialized), 1);
        }
    }

    [CustomEditor(typeof(ElementTypeManager))]
    public class ElementTypeManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawTopBar();

            DrawDefaultInspector();
        }

        private void DrawTopBar()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                ElementTypeManager elementTypeManager = FindObjectOfType<ElementTypeManager>();
                using (new EditorGUI.DisabledScope(!elementTypeManager))
                {
                    #region Generate Element Types

                    if (GUILayout.Button("Generate Element Types", EditorStyles.toolbarButton))
                    {
                        var elementTypes = ItemEditorUtility.GetAllElementTypes();

                        for (int i = 0; i < elementTypes.Count; i++)
                        {
                            if ((int)elementTypes[i].elementTypeId == i + 1)
                            {
                                continue;
                            }

                            Debug.LogError($"Element ID: {i} is missing! Last found element: {elementTypes[i - 1].elementTypeId:G}.", this);
                            return;
                        }

                        elementTypes = elementTypes.OrderBy(o => o.elementTypeId).ToList();

                        //Programmatically, we want the array's count to match the number of enums, which means we need to account for the enum's None field (0)
                        elementTypes.Insert(0, null);

                        //Remove old elementTypes
                        while (elementTypeManager.elementDataParent.transform.childCount > 0) DestroyImmediate(elementTypeManager.elementDataParent.transform.GetChild(0).gameObject);

                        ElementData[] elementDataObjs = new ElementData[elementTypes.Count];

                        //Make sure to start at 1 so we ignore the None field added in line 87
                        for (int i = 1; i < elementTypes.Count; i++)
                        {
                            //Generate a GameObject, AddComponent<ElementData>(), then fill it out with the ElementType data
                            GameObject newElementData = new GameObject();

                            EditorUtility.SetDirty(newElementData);

                            newElementData.transform.SetParent(elementTypeManager.elementDataParent.transform, true);
                            elementDataObjs[i] = newElementData.AddComponent<ElementData>();
                            newElementData.name = $"{elementTypes[i].elementTypeId:G} Element";

                            //Element info
                            elementDataObjs[i].elementTypeId = elementTypes[i].elementTypeId;
                            elementDataObjs[i].elementColor = elementTypes[i].elementColor;

                            //Despawn settings
                            if (elementTypes[i].canDespawn)
                            {
                                elementDataObjs[i].canDespawn = elementTypes[i].canDespawn;
                                elementDataObjs[i].despawnTime = elementTypes[i].despawnTime;
                                elementDataObjs[i].killVelocity = elementTypes[i].killVelocity;
                            }

                            //Base Element settings
                            if (elementTypes[i].elementIsSpawnedByElementSpawner)
                            {
                                elementDataObjs[i].baseElementSpawnTime = elementTypes[i].baseElementSpawnTime;
                                elementDataObjs[i].baseElementMesh = elementTypes[i].baseElementMesh;
                                elementDataObjs[i].BaseElementCollisionPrefab = elementTypes[i].BaseElementCollisionPrefab;
                                elementDataObjs[i].pickupColliderRadius = elementTypes[i].pickupColliderRadius;
                                elementDataObjs[i].rbMass = elementTypes[i].rbMass;
                                elementDataObjs[i].rbDrag = elementTypes[i].rbDrag;
                                elementDataObjs[i].rbAngularDrag = elementTypes[i].rbAngularDrag;

                                //Seed Pod settings
                                if (elementTypes[i].elementCanBecomeSeedPod)
                                {
                                    elementDataObjs[i].elementCanBecomeSeedPod = elementTypes[i].elementCanBecomeSeedPod;
                                    elementDataObjs[i].seedPodSpawnChance = elementTypes[i].seedPodSpawnChance;
                                    elementDataObjs[i].seedPodElementType = elementTypes[i].seedPodElementType;
                                    elementDataObjs[i].ElementLeavesPrefab = elementTypes[i].ElementLeavesPrefab;
                                }

                                //Grown Object Settings 1
                                if (elementTypes[i].elementIsSeedPod)
                                {
                                    elementDataObjs[i].SeedPodPrefab = elementTypes[i].SeedPodPrefab;
                                    elementDataObjs[i].seedPodPosOffset = elementTypes[i].seedPodPosOffset;
                                }

                                if (elementTypes[i].elementIsSpawnedByElementSpawner || (elementTypes[i].elementIsSeedPod && elementTypes[i].canPlantManually))
                                {
                                    elementDataObjs[i].canPlantManually = elementTypes[i].canPlantManually;
                                    elementDataObjs[i].GrownObjectAnimator = elementTypes[i].GrownObjectAnimator;
                                    elementDataObjs[i].colliderYPos = elementTypes[i].colliderYPos;
                                    elementDataObjs[i].colliderRadius = elementTypes[i].colliderRadius;
                                    elementDataObjs[i].colliderHeight = elementTypes[i].colliderHeight;
                                    elementDataObjs[i].GrownObjectGrowthPrefabs = elementTypes[i].GrownObjectGrowthPrefabs;
                                    elementDataObjs[i].grownObjectGrowTime = elementTypes[i].grownObjectGrowTime;
                                    elementDataObjs[i].ElementSpawnTransforms = elementTypes[i].ElementSpawnTransforms;
                                }
                            }

                            //Grown Object settings 2
                            if (elementTypes[i].elementCanSpawnGrownObject)
                            {
                                elementDataObjs[i].grownObjectType = elementTypes[i].grownObjectType;
                                elementDataObjs[i].grownObjectElement = elementTypes[i].grownObjectElement;

                                if (elementTypes[i].grownObjectType == GrownObjectType.GrowableLink)
                                {
                                    elementDataObjs[i].canPlantManually = elementTypes[i].canPlantManually;
                                    elementDataObjs[i].GrownObjectAnimator = elementTypes[i].GrownObjectAnimator;
                                    elementDataObjs[i].colliderYPos = elementTypes[i].colliderYPos;
                                    elementDataObjs[i].colliderRadius = elementTypes[i].colliderRadius;
                                    elementDataObjs[i].colliderHeight = elementTypes[i].colliderHeight;
                                    elementDataObjs[i].GrownObjectGrowthPrefabs = elementTypes[i].GrownObjectGrowthPrefabs;
                                    elementDataObjs[i].grownObjectGrowTime = elementTypes[i].grownObjectGrowTime;
                                    elementDataObjs[i].ElementSpawnTransforms = elementTypes[i].ElementSpawnTransforms;
                                }
                            }

                            //Precipitate settings
                            if (elementTypes[i].elementHasPrecipitateForm)
                            {
                                elementDataObjs[i].elementHasPrecipitateForm = elementTypes[i].elementHasPrecipitateForm;
                                elementDataObjs[i].elementPrecipitateType = elementTypes[i].elementPrecipitateType;
                                elementDataObjs[i].minimumVelocity = elementTypes[i].minimumVelocity;
                                elementDataObjs[i].maximumVelocity = elementTypes[i].maximumVelocity;
                                elementDataObjs[i].velocityMultiplier = elementTypes[i].velocityMultiplier;

                                //BaseElement Precipitation settings
                                if (elementTypes[i].elementIsSpawnedByElementSpawner)
                                {
                                    elementDataObjs[i].shrinkSpeed = elementTypes[i].shrinkSpeed;
                                    elementDataObjs[i].elementPrecipitateAmount = elementTypes[i].elementPrecipitateAmount;
                                    elementDataObjs[i].elementPrecipitateSpawnChance = elementTypes[i].elementPrecipitateSpawnChance;
                                }
                            }

                            //Effect settings
                            if (elementTypes[i].elementHasUsableEffect)
                            {
                                elementDataObjs[i].elementHasUsableEffect = elementTypes[i].elementHasUsableEffect;
                                elementDataObjs[i].elementEffectPrimingTrigger = elementTypes[i].elementEffectPrimingTrigger;
                                elementDataObjs[i].effectPrimingThreshold = elementTypes[i].effectPrimingThreshold;
                                elementDataObjs[i].effectUseTrigger = elementTypes[i].effectUseTrigger;
                                elementDataObjs[i].ingestedEffect = elementTypes[i].ingestedEffect;
                                elementDataObjs[i].ingestedEffectStrength = elementTypes[i].ingestedEffectStrength;
                                elementDataObjs[i].ingestedEffectDuration = elementTypes[i].ingestedEffectDuration;
                            }

                            Debug.Log($"Created {newElementData.name}");
                        }

                        //elementTypeManager.elementDataParent.gameObject.SetActive(false);
                        
                        int enumElementTypes = System.Enum.GetNames(typeof(ElementTypes)).Length - 1;

                        if (elementDataObjs.Length < enumElementTypes)
                        {
                            Debug.LogError($"Only {elementDataObjs.Length} ElementData SO's found while {enumElementTypes} total ElementTypes defined in Enum!");
                        }

                        elementTypeManager.elementDataObjs = elementDataObjs;

                        AssetDatabase.SaveAssets();
                    }
                    #endregion

                    #region Generate Potion Recipes

                    if (GUILayout.Button("Generate Potion Types", EditorStyles.toolbarButton))
                    {
                        var potionRecipes = ItemEditorUtility.GetAllPotionRecipes();

                        potionRecipes = potionRecipes.OrderBy(o => o.potionRecipeId).ToList();

                        //Programmatically, we want the array's count to match the number of enums, which means we need to account for the enum's None field (0)
                        potionRecipes.Insert(0, null);

                        //Remove old elementTypes
                        while (elementTypeManager.potionRecipeDataParent.transform.childCount > 0) DestroyImmediate(elementTypeManager.potionRecipeDataParent.transform.GetChild(0).gameObject);

                        PotionRecipeData[] potionDataObjs = new PotionRecipeData[potionRecipes.Count];

                        //Make sure to start at 1 so we ignore the None field added in line 158
                        for (int i = 1; i < potionRecipes.Count; i++)
                        {
                            if ((int)potionRecipes[i].potionRecipeId > elementTypeManager.elementDataObjs.Length || elementTypeManager.elementDataObjs[(int)potionRecipes[i].potionRecipeId] == null)
                            {
                                Debug.LogError($"No Element Type set up for new Potion's Element. Go do that first!");
                                continue;
                            }

                            //Generate a GameObject, AddComponent<PotionRecipeData>(), then fill it out with the PotionRecipe data
                            GameObject newPotionData = new GameObject();
                            newPotionData.transform.SetParent(elementTypeManager.potionRecipeDataParent.transform, true);
                            potionDataObjs[i] = newPotionData.AddComponent<PotionRecipeData>();
                            newPotionData.name = $"{potionRecipes[i].potionRecipeId:G} Potion";

                            potionDataObjs[i].potionColor = elementTypeManager.elementDataObjs[(int)potionRecipes[i].potionRecipeId].elementColor;

                            potionDataObjs[i].potionElementType = potionRecipes[i].potionRecipeId;
                            potionDataObjs[i].requiredLiquidType = potionRecipes[i].requiredLiquidType;
                            potionDataObjs[i].updateColor = potionRecipes[i].updateColor;
                            potionDataObjs[i].requiredElementTypes = potionRecipes[i].requiredElementTypes;

                            Debug.Log($"Created {newPotionData.name}");
                        }

                        //elementTypeManager.potionRecipeDataParent.gameObject.SetActive(false);

                        elementTypeManager.potionRecipeObjs = potionDataObjs;

                        AssetDatabase.SaveAssets();
                    }
                    #endregion
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}