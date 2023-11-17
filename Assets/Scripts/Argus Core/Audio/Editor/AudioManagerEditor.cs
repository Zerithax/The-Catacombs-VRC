using Argus.Audio;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    private bool drawDefaultInspector;
    private AudioManager manager;
    public override void OnInspectorGUI()
    {
        if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
        
        manager = (AudioManager) target;

        serializedObject.Update();
        
        EditorGUI.BeginChangeCheck();
        float sfxVolume = EditorGUILayout.Slider("SFX Volume", manager.sfxVolume, 0, 1);
        if (EditorGUI.EndChangeCheck())
        {
            manager.SfxVolume = sfxVolume;
        }
        
        EditorGUI.BeginChangeCheck();
        float musicVolume = EditorGUILayout.Slider("Music Volume", manager.musicVolume, 0, 1);
        if (EditorGUI.EndChangeCheck())
        {
            manager.MusicVolume = musicVolume;
        }
        
        
        EditorGUI.BeginChangeCheck();
        float ambientVolume = EditorGUILayout.Slider("Ambient Volume", manager.ambientVolume, 0, 1);
        if (EditorGUI.EndChangeCheck())
        {
            manager.AmbientVolume = ambientVolume;
        }
        
        
        
        EditorGUI.BeginChangeCheck();
        int newSize = EditorGUILayout.IntSlider("Pool Size:", manager.poolSize,0,64);
        if (EditorGUI.EndChangeCheck())
        {
            UpdatePoolSize(newSize);
        }

        if (GUILayout.Button($"{(drawDefaultInspector ? "Hide" : "Show")} Default Inspector"))
        {
            drawDefaultInspector = !drawDefaultInspector;
        }
        if(drawDefaultInspector) DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }

    private void UpdatePoolSize(int newSize)
    {
        manager.poolSize = newSize;
        
        //Clear all children
        while (manager.transform.childCount > 0)
        {
            DestroyImmediate(manager.transform.GetChild(0).gameObject);
        }

        ArrayUtility.Clear(ref manager.audioSources);
        ArrayUtility.Clear(ref manager.audioSourceTransforms);
        
        for (int i = 0; i < manager.poolSize; i++)
        {
            Transform newAudioSourceTransform = new GameObject($"SAO AudioSource ({i + 1})",typeof(AudioSource)).transform;
            AudioSource newAudioSource = newAudioSourceTransform.GetComponent<AudioSource>();
            newAudioSource.spatialBlend = 1f;
            
            newAudioSourceTransform.transform.parent = manager.transform;
            
            ArrayUtility.Add(ref manager.audioSourceTransforms,newAudioSourceTransform);
            ArrayUtility.Add(ref manager.audioSources,newAudioSource);
        }
    }
}