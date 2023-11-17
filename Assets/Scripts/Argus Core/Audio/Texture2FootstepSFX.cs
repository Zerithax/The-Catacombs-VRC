#if UNITY_EDITOR && !COMPILER_UDONSHARP

using System;
using System.Collections.Generic;
using System.Linq;
using Argus.Audio;
using UnityEditor;
using UnityEngine;
using VRRefAssist;

[CreateAssetMenu(menuName = "Argus/Audio/Footstep SFX")]
public class Texture2FootstepSFX : ScriptableObject
{
    [SerializeField] public int defaultSFXIndex;
    [SerializeField] public List<FootstepSFX> footstepSFXs = new List<FootstepSFX>();
    [SerializeField] public List<FootstepSFXClips> footstepSFXClips = new List<FootstepSFXClips>();

    [Serializable]
    public class FootstepSFX
    {
        public Texture2D texture;
        public int clipsIndex = 0;
    }

    [Serializable]
    public class FootstepSFXClips
    {
        public string name = "";
        public List<AudioClip> audioClips = new List<AudioClip>();
        public List<AudioClip> landClips = new List<AudioClip>();
    }

    [RunOnBuild]
    public static void PushFootstepSFXToAudioManager()
    {
        //Find the Texture2FootstepSFX asset
        var texture2FootstepSFX = AssetDatabase.FindAssets("t:Texture2FootstepSFX")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<Texture2FootstepSFX>)
            .FirstOrDefault();

        var audioManager = FindObjectOfType<AudioManager>();

        audioManager.footstepSFXNames = texture2FootstepSFX.footstepSFXClips.Select(x => x.name).ToArray();
        audioManager.footstepSFXTextures = texture2FootstepSFX.footstepSFXs.Select(sfx => sfx.texture).ToArray();
        audioManager.footstepSFXIndices = texture2FootstepSFX.footstepSFXs.Select(sfx => sfx.clipsIndex).ToArray();
        audioManager.footstepSFXs = texture2FootstepSFX.footstepSFXClips.Select(sfx => sfx.audioClips.ToArray()).ToArray();
        audioManager.landSFXs = texture2FootstepSFX.footstepSFXClips.Select(sfx => sfx.landClips.ToArray()).ToArray();
        audioManager.defaultFootstepSFXIndex = texture2FootstepSFX.defaultSFXIndex;
        //audioManager.footstepSFXs = texture2FootstepSFX.footstepSFXs.Select(sfx => sfx.audioClips.ToArray()).ToArray();

        EditorUtility.SetDirty(audioManager);
    }
}

[CustomEditor(typeof(Texture2FootstepSFX))]
public class Texture2FootstepSFXEditor : Editor
{
    private string terrainName = "Floor 1 Terrain";

    private bool showClips = false;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        Texture2FootstepSFX texture2FootstepSFX = (Texture2FootstepSFX) target;

        texture2FootstepSFX.defaultSFXIndex = EditorGUILayout.Popup("Default SFX Set", texture2FootstepSFX.defaultSFXIndex, texture2FootstepSFX.footstepSFXs.Select(x => x.texture != null ? x.texture.name : null).ToArray());

        //Iterate and display every texture and its audio clips
        foreach (Texture2FootstepSFX.FootstepSFX sfx in texture2FootstepSFX.footstepSFXs)
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                sfx.texture = (Texture2D) EditorGUILayout.ObjectField(sfx.texture, typeof(Texture2D), false, GUILayout.Width(64), GUILayout.Height(64));

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Texture: " + sfx.texture.name);

                    sfx.clipsIndex = EditorGUILayout.Popup(sfx.clipsIndex, texture2FootstepSFX.footstepSFXClips.Select(x => x.name).ToArray());
                }

                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                {
                    texture2FootstepSFX.footstepSFXs.Remove(sfx);
                    GUIUtility.ExitGUI();
                }
            }
        }

        if (GUILayout.Button("Add Texture"))
        {
            texture2FootstepSFX.footstepSFXs.Add(new Texture2FootstepSFX.FootstepSFX());
        }

        //mainTerrain = (Terrain) EditorGUILayout.ObjectField("Terrain", null, typeof(Terrain), true);
        //terrainGameObject = (GameObject) EditorGUILayout.ObjectField("Terrain GameObject", null, typeof(GameObject), true);


        if (GUILayout.Button("Pull textures from terrain"))
        {
            var terrainTextures = GameObject.Find(terrainName).GetComponent<Terrain>().terrainData.terrainLayers.Select(l => l.diffuseTexture).ToList();

            for (int i = 0; i < terrainTextures.Count; i++)
            {
                if (texture2FootstepSFX.footstepSFXs.Count <= i)
                    texture2FootstepSFX.footstepSFXs.Add(new Texture2FootstepSFX.FootstepSFX());

                if (texture2FootstepSFX.footstepSFXs[i].texture == null)
                {
                    texture2FootstepSFX.footstepSFXs[i].texture = terrainTextures[i];
                }
                else if (texture2FootstepSFX.footstepSFXs[i].texture != terrainTextures[i])
                {
                    texture2FootstepSFX.footstepSFXs.Insert(i, new Texture2FootstepSFX.FootstepSFX {texture = texture2FootstepSFX.footstepSFXs[i].texture, clipsIndex = texture2FootstepSFX.footstepSFXs[i].clipsIndex});
                    texture2FootstepSFX.footstepSFXs[i].texture = terrainTextures[i];
                }
            }
        }

        if (GUILayout.Button("Push to AudioManager"))
        {
            Texture2FootstepSFX.PushFootstepSFXToAudioManager();
        }

        showClips = EditorGUILayout.Foldout(showClips, "Audio Clips");

        if (showClips)
        {
            foreach (var clips in texture2FootstepSFX.footstepSFXClips)
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        clips.name = EditorGUILayout.TextField("Name", clips.name);

                        if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false)))
                        {
                            texture2FootstepSFX.footstepSFXClips.Remove(clips);
                            GUIUtility.ExitGUI();
                        }
                    }

                    EditorGUILayout.LabelField("Footstep Clips");
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        for (var i = 0; i < clips.audioClips.Count; i++)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                clips.audioClips[i] = (AudioClip) EditorGUILayout.ObjectField(clips.audioClips[i], typeof(AudioClip), false);

                                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                                {
                                    clips.audioClips.Remove(clips.audioClips[i]);
                                    GUIUtility.ExitGUI();
                                }
                            }
                        }
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Add Clip", GUILayout.ExpandWidth(false)))
                        {
                            clips.audioClips.Add(null);
                        }
                    }

                    EditorGUILayout.LabelField("Land Clips");
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        for (var i = 0; i < clips.landClips.Count; i++)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                clips.landClips[i] = (AudioClip) EditorGUILayout.ObjectField(clips.landClips[i], typeof(AudioClip), false);

                                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                                {
                                    clips.landClips.Remove(clips.landClips[i]);
                                    GUIUtility.ExitGUI();
                                }
                            }
                        }
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Add Clip", GUILayout.ExpandWidth(false)))
                        {
                            clips.landClips.Add(null);
                        }
                    }
                }
            }

            if (GUILayout.Button("Add Clip Set", GUILayout.ExpandWidth(false)))
            {
                texture2FootstepSFX.footstepSFXClips.Add(new Texture2FootstepSFX.FootstepSFXClips());
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif