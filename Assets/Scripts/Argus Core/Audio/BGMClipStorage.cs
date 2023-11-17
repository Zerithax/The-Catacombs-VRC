using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

#if UNITY_EDITOR && !COMPILER_UDONSHARP

using System;
using System.Collections;
using System.Collections.Generic;
using Argus.Audio;
using UdonSharpEditor;
using UnityEditor.Rendering.PostProcessing;
using VRC.SDKBase.Editor.BuildPipeline;

#endif

namespace Argus.Audio
{
    public class BGMClipStorage : MonoBehaviour
    {
        /*public AudioClip[] defaultClips;
        public VRCUrl[] defaultClipsURL;
        public AudioClip[] nightClips;
        public VRCUrl[] nightClipsURL;*/

        public BGMTrack[] defaultTracks = new BGMTrack[0];
        public bool hasNightTrack;
        public BGMTrack[] nightTracks= new BGMTrack[0];

    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    /*
    [CustomEditor(typeof(BGMClipStorage))]

    public class BGMClipStorageEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            var t = (BGMClipStorage)target;
            DrawDefaultInspector();
            if (t.defaultTracks.Length <= 0||t.nightTracks.Length <= 0)
            {
                if (GUILayout.Button("Migrate to scriptable object"))
                {
                    t.defaultTracks = new BGMTrack[t.defaultClips.Length];
                    for (int i = 0; i < t.defaultClips.Length; i++)
                    {
                        var cl = t.defaultClips[i];
                        var clu = t.defaultClipsURL[i];
                        if(cl==null&&clu==null) continue;
                        string n = BGMTrack.GenerateNameStatic(cl, clu);
                        var track = BGMTrack.Create(n);
                        track.clip = cl;
                        track.clipURL = clu;
                        t.defaultTracks[i] = track;
                    }
                    t.nightTracks = new BGMTrack[t.nightClips.Length];
                    for (int i = 0; i < t.nightClips.Length; i++)
                    {
                        var cl = t.nightClips[i];
                        var clu = t.nightClipsURL[i];
                        if(cl==null&&clu==null) continue;
                        string n = BGMTrack.GenerateNameStatic(cl, clu);
                        var track = BGMTrack.Create(n);
                        track.clip = cl;
                        track.clipURL = clu;
                        t.nightTracks[i] = track;
                    }
                }
            }
            
        }
    }*/

    public static class SyncClipsOnBuild
    {
        //[RunOnBuild]
        public static void OnBuildRequested()
        {
            GameObject[] rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            List<BGMZone> zones = new List<BGMZone>();

            foreach (GameObject obj in rootGameObjects)
            {
                zones.AddRange(obj.GetComponentsInChildren<BGMZone>());
            }

            foreach (BGMZone zone in zones)
            {
                try
                {
                    zone.SyncClips();

                    if (PrefabUtility.IsPartOfAnyPrefab(zone))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(zone);
                    }
                    EditorUtility.SetDirty(zone);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            Debug.Log($"<color=lightblue>[BGMZones]</color> Synced clip data for {zones.Count} zones");
        }
    }
#endif
}
