using System.Linq;
using VRC.SDKBase;
using VRC.Udon;
using UdonSharp;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Catacombs.ElementSystem.Runtime;
using Argus.ItemSystem.Editor;


namespace Argus.ItemSystem
{
    public enum ElementTypes
    {
        None = 0,
        Linkberry = 1,
        Arieberry = 2,
        Blueberry = 3,
        Water = 4,
        Oil = 5,
        TestType = 6
    }

    public class ElementTypeManager : UdonSharpBehaviour
    {
        //A list of instantiated ElementData objects containing each ElementType ScriptableObject's Element details
        public ElementData[] elementTypeData;
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
                //selectedType = GUILayout.Toolbar(selectedType, typeNames, EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();

                ElementTypeManager elmtTypeManager = FindObjectOfType<ElementTypeManager>();
                using (new EditorGUI.DisabledScope(!elmtTypeManager))
                {
                    if (GUILayout.Button("Generate Element Types", EditorStyles.toolbarButton))
                    {
                        var elementTypes = ItemEditorUtility.GetAllElementTypes();

                        int id = 0;
                        for (int i = 0; i < elementTypes.Count; i++)
                        {
                            if ((int)elementTypes[i].elementTypeId == id++)
                            {
                                continue;
                            }

                            Debug.LogError($"Element ID: {i} is missing! Last found element: {elementTypes[i].elementTypeId.ToString()}.", this);
                            return;
                        }

                        elementTypes = elementTypes.OrderBy(o => o.elementTypeId).ToList();

                        //var itemPrefabs = items.Select(i => i.elementPrefab).ToList();

                        //elementTypeManager.elementTypePrefabs = itemPrefabs.ToArray();

                        //var elementPrefabs = ItemEditorUtility.GetAllItems();
                        //foreach (var elementPrefab in elementPrefabs )
                        //{
                        //}

                        //Remove old elementTypes
                        while (elmtTypeManager.transform.childCount > 0) DestroyImmediate(elmtTypeManager.transform.GetChild(0).gameObject);

                        //Generate a GameObject, AddComponent<ElementData>(), then fill it out with the ElementType data
                        ElementData[] elmtTypeDataObjs = new ElementData[elementTypes.Count];

                        for (int i = 0; i < elementTypes.Count; i++)
                        {
                            GameObject newElementData = new GameObject();
                            newElementData.transform.SetParent(elmtTypeManager.transform, true);
                            newElementData.SetActive(false);
                            elmtTypeDataObjs[i] = newElementData.AddComponent<ElementData>();
                            newElementData.name = elementTypes[i].elementTypeId.ToString();

                            elmtTypeDataObjs[i].elementTypeId = elementTypes[i].elementTypeId;
                            elmtTypeDataObjs[i].elementColor = elementTypes[i].elementColor;
                            elmtTypeDataObjs[i].canDespawn = elementTypes[i].canDespawn;
                            elmtTypeDataObjs[i].killVelocity = elementTypes[i].killVelocity;
                            elmtTypeDataObjs[i].despawnTime = elementTypes[i].despawnTime;
                            elmtTypeDataObjs[i].canCreatePrecipitate = elementTypes[i].canCreatePrecipitate;
                            elmtTypeDataObjs[i].elementPrecipitateType = elementTypes[i].elementPrecipitateType;
                            elmtTypeDataObjs[i].ElementPrecipitatePrefab = elementTypes[i].ElementPrecipitatePrefab;
                            elmtTypeDataObjs[i].shrinkSpeed = elementTypes[i].shrinkSpeed;
                            elmtTypeDataObjs[i].elementPrecipitateAmount = elementTypes[i].elementPrecipitateAmount;
                            elmtTypeDataObjs[i].scaleVelocity = elementTypes[i].scaleVelocity;
                            elmtTypeDataObjs[i].targetVelocity = elementTypes[i].targetVelocity;
                            elmtTypeDataObjs[i].momentumScale = elementTypes[i].momentumScale;
                            elmtTypeDataObjs[i].canCreateSpawners = elementTypes[i].canCreateSpawner;
                            elmtTypeDataObjs[i].ElementSpawnerPrefab = elementTypes[i].ElementSpawnerPrefab;
                            elmtTypeDataObjs[i].elementSpawnerGrowthPeriod = elementTypes[i].elementSpawnerGrowTime;
                            elmtTypeDataObjs[i].BaseElementPrefab = elementTypes[i].BaseElementPrefab;
                            elmtTypeDataObjs[i].elementSpawnTime = elementTypes[i].elementSpawnTime;
                            elmtTypeDataObjs[i].seedGrowChance = elementTypes[i].seedGrowChance;

                            Debug.Log($"[{name}] Created {newElementData.name}");
                        }


                        elmtTypeManager.elementTypeData = elmtTypeDataObjs;

                        //EditorUtility.SetDirty(elementTypeManager);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}