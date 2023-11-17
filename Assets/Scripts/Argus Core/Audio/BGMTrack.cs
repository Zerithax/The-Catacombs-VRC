using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace Argus.Audio
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "BGMTrack", menuName = "Argus/Audio/BGMTrack", order = 1)]
    public class BGMTrack : ScriptableObject
    {
        public string trackName;
        //public float trackLength;
        public AudioClip clip;
        public string clipURL = "";
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public void DrawGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorUtility.SetDirty(this);
            trackName = EditorGUILayout.TextField("Track Name: ", trackName);
            if (GUILayout.Button("Auto",GUILayout.Width(60))) trackName = GenerateName();
        
            EditorGUILayout.EndHorizontal();
            clip= (AudioClip)EditorGUILayout.ObjectField("Audio Clip: ", clip, typeof(AudioClip), false);
        
            clipURL = EditorGUILayout.TextField("Clip URL: ", clipURL);
        }
        
        public string GenerateName()
        {
            return GenerateNameStatic(clip, clipURL);
        }
        
        public static string GenerateNameStatic(AudioClip clip,string clipURL="")
        {
            string n = "";
            if (clip)
            {
                n = clip.name;
            }else if (Utilities.IsValid(clipURL))
            { 
                string[] split = clipURL.ToString().Split('/');
                n = split.Length > 0 ?split[split.Length - 1] : "";
            }

            n = n.Replace('_', ' ');
            return n;
        }
        public static BGMTrack Create(string n)
        {
            BGMTrack asset = ScriptableObject.CreateInstance<BGMTrack>();
            asset.name = n;
            if(!AssetDatabase.IsValidFolder($"Assets/Sounds")) AssetDatabase.CreateFolder("Assets", "Sounds");
            if(!AssetDatabase.IsValidFolder($"Assets/Sounds/BGMTrack/")) AssetDatabase.CreateFolder("Assets/Sounds", "BGMTrack");
            
            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"Assets/Sounds/BGMTrack/{n}.asset");
            Debug.Log($"Creating BGMTrack {n} at {path}");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }
#endif
    }
}