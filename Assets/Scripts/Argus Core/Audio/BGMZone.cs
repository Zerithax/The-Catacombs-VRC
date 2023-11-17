//#define USEAUDIOLINK
using System;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
#if(USEAUDIOLINK)
using VRCAudioLink;
#endif

namespace Argus.Audio
{
    
#if !COMPILER_UDONSHARP && UNITY_EDITOR
    using UdonSharpEditor;
    using UnityEditor;

    [CustomEditor(typeof(BGMZone))]
    public class BGMZoneEditor : Editor
    {
        private void OnDestroy()
        {
            EndPreview();
        }

        private void EndPreview()
        {
            if (audioPreview != null)
                DestroyImmediate(audioPreview);
        }

        private bool showDefaultInspector;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            BGMZone zone = (BGMZone)target;

            BGMClipStorage clipStorage = zone.GetComponentInChildren<BGMClipStorage>();

            if (!clipStorage&&!Application.isPlaying)
            {
                GameObject temp = new GameObject("ClipStorage");
                temp.transform.SetParent(zone.transform);
                clipStorage = temp.AddComponent<BGMClipStorage>();
                //temp.hideFlags = HideFlags.HideInHierarchy;
                temp.tag = "EditorOnly";
                clipStorage.hasNightTrack = zone.hasNightTrack;
            }

            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.BeginVertical("Helpbox");
            EditorGUILayout.EndVertical();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.label)
                { fontSize = 16, richText = true, fontStyle = FontStyle.Bold };
            GUIStyle boldLabelStyle = new GUIStyle(EditorStyles.label) { richText = true, fontStyle = FontStyle.Bold };
            EditorGUILayout.LabelField("Zone Editor", headerStyle);

            if (!zone.name.StartsWith("BGMFallback"))
            {
                if (zone.BGMCollider == null)
                {
                    zone.BGMCollider = zone.GetComponent<Collider>();
                }

                if (zone.BGMCollider == null || !zone.BGMCollider.isTrigger)
                {
                    EditorGUILayout.HelpBox("A BGMZone needs a collider component set to trigger to function",
                        MessageType.Error);
                    if (zone.BGMCollider != null && !zone.BGMCollider.isTrigger)
                    {
                        if (GUILayout.Button("Mark collider as trigger"))
                        {
                            zone.BGMCollider.isTrigger = true;
                            EditorUtility.SetDirty(zone.BGMCollider);
                        }
                    }
                }
            }

            if (!zone.name.StartsWith("BGMZone_") && !zone.name.StartsWith("BGMFallback"))
            {
                EditorGUILayout.HelpBox(
                    "The name of a BGMZone needs to start with BGMZone_{index}. To fix this, select BGMManager and use Link All button.",
                    MessageType.Error);
            }

            zone.priority =
                EditorGUILayout.IntField(
                    new GUIContent("Zone Priority",
                        "Zone priority when 2 or more zone is active at once (if 2 zone have the same priority level, the latest zone entered will be used)"),
                    zone.priority);

            EditorGUILayout.Space(5);
            DrawAudioClipList(ref clipStorage.defaultTracks);
            clipStorage.hasNightTrack =
                EditorGUILayout.Toggle(
                    new GUIContent("Enable Night Track",
                        "Enable nighttime track switching on this zone (can be left empty for no track)"),
                    clipStorage.hasNightTrack);
            if (clipStorage.hasNightTrack)
            {
                EditorGUILayout.Space(5);
                DrawAudioClipList(ref clipStorage.nightTracks);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Sync with Clip Storage")) zone.SyncClips();
            if (GUILayout.Button("Delete Clip Storage")) DestroyImmediate(clipStorage.gameObject);
            EditorGUILayout.EndHorizontal();

            zone.environmentalData =
                (SoundData)EditorGUILayout.ObjectField(new GUIContent("Environmental Sound", ""),
                    zone.environmentalData, typeof(SoundData), true);
            
#if(USEAUDIOLINK)
            zone.audioLink =
                (AudioLink)EditorGUILayout.ObjectField(new GUIContent("AudioLink", ""),
                    zone.audioLink, typeof(AudioLink), true);
#endif
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Settings", boldLabelStyle);
            zone.fadeInTime =
                EditorGUILayout.FloatField(new GUIContent("FadeIn Time", "How long will the track fades in (seconds)"),
                    zone.fadeInTime);
            zone.fadeOutTime =
                EditorGUILayout.FloatField(
                    new GUIContent("FadeOut Time", "How long will the track fades out (seconds)"), zone.fadeOutTime);
            zone.singleFade =
                EditorGUILayout.Toggle(new GUIContent("Single Fade", "Track will not fade in and out at the same time"),
                    zone.singleFade);
            zone.unityLoop =
                EditorGUILayout.Toggle(new GUIContent("Unity Loop", "Loop using audio source loop (for wav loop)"),
                    zone.unityLoop);


            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Syncing", boldLabelStyle);
            zone.doSync =
                EditorGUILayout.Toggle(
                    new GUIContent("UTC Sync",
                        "This will use UTC Time in combination with the seed to sync audio playback across clients (without actually using any networking)"),
                    zone.doSync);
            if (zone.doSync)
            {
                if (zone.unityLoop)
                {
                    EditorGUILayout.HelpBox("Sync does not support unity loop, it should be disabled",
                        MessageType.Warning);
                }

                zone.uSync =
                    (BGMZoneUSynced)EditorGUILayout.ObjectField(
                        new GUIContent("Udon Sync", "Sync between players using Udon"), zone.uSync,
                        typeof(BGMZoneUSynced), true);
                if (zone.uSync)
                {
                    zone.uSync.zone = zone;
                }

                zone.syncOnlyStart =
                    EditorGUILayout.Toggle(
                        new GUIContent("Play from Start",
                            "Track will always start from the beginning regardless of synced time"),
                        zone.syncOnlyStart);

            }

            // zone.gapless = 
            //     EditorGUILayout.Toggle(new GUIContent("Gapless Playback", "Track will played without silence gap in between"), zone.gapless);
            // if (zone.gapless && (zone.defaultClips.Length > 1 || zone.nightClips.Length > 1))
            // {
            //     EditorGUILayout.HelpBox("Having more than 1 track will cause gapless to breaks",MessageType.Warning);
            // }
            //
            // if (!zone.gapless)
            // {
            //  
            zone.trackIntervals =
                EditorGUILayout.FloatField(
                    new GUIContent("Track Intervals", "The minimum time between tracks (seconds)"),
                    zone.trackIntervals);
            zone.trackOffset =
                EditorGUILayout.FloatField(
                    new GUIContent("Track Offset",
                        "The maximum amount of time the seed will offset playback by (seconds)"), zone.trackOffset);
            if (zone.trackOffset > zone.trackIntervals)
            {
                EditorGUILayout.HelpBox("Track offset should not be larger than the track intervals",
                    MessageType.Warning);
            }

            // }
            zone.forceSync =
                EditorGUILayout.Toggle(new GUIContent("Force Sync", "Force sync and play track even if it's locally playing"),
                    zone.forceSync);
            zone.bgmSeed =
                EditorGUILayout.FloatField(new GUIContent("Seed", "Seed used in randomization"), zone.bgmSeed);
            if (GUILayout.Button("Force Sync"))
            {
                //zone.UpdateProxy();
                zone.SyncClips();
                //zone.ApplyProxyModifications();
            }
            showDefaultInspector = EditorGUILayout.Foldout(showDefaultInspector, "Show default inspector");
            if (showDefaultInspector)
                DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck())
            {
                //zone.ApplyProxyModifications();
                EditorUtility.SetDirty(zone);
                if (PrefabUtility.IsPartOfAnyPrefab(zone.gameObject))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(zone);
                }
            }
        }

        private AudioClip preview;
        private string newName;
        private void DrawAudioClipList(ref BGMTrack[] tracks)
        {
            GUIStyle boldLabelStyle = new GUIStyle(EditorStyles.label) { richText = true, fontStyle = FontStyle.Bold };

            if (tracks == null)
            {
                tracks = Array.Empty<BGMTrack>();
            }
            if (tracks.Length == 0)
            {
                EditorGUILayout.HelpBox("This list is empty.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < tracks.Length; i++)
                {
                    EditorGUILayout.BeginVertical("Helpbox");
                    if (tracks[i] != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        //if (GUILayout.Button("Show")) EditorGUIUtility.PingObject(tracks[i]);
                        if (tracks[i].clip != null && preview == tracks[i].clip)
                        {
                            if (GUILayout.Button("End Preview"))
                            {
                                preview = null;
                                EndPreview();
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Preview"))
                            {
                                preview = tracks[i].clip;
                            }
                        }

                        if (GUILayout.Button("-", GUILayout.Width(30)))
                        {
                            if(preview == tracks[i].clip) EndPreview();
                    
                            ArrayUtility.RemoveAt(ref tracks, i);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            continue;
                        }

                        EditorGUILayout.EndHorizontal();

                        tracks[i] = (BGMTrack)EditorGUILayout.ObjectField(new GUIContent("Track", ""),
                            tracks[i], typeof(BGMTrack),false);
                        tracks[i].DrawGUI();

                        if (tracks[i].clip == preview)
                        {
                            DrawPreviewGUI(preview);
                        }
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("-", GUILayout.Width(30)))
                        {
                            ArrayUtility.RemoveAt(ref tracks, i);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            continue;
                        }

                        EditorGUILayout.EndHorizontal();
                        tracks[i] = (BGMTrack)EditorGUILayout.ObjectField(new GUIContent("Import", ""),
                            tracks[i], typeof(BGMTrack),false);
                        EditorGUILayout.HelpBox(
                            "This track data is missing, please create a new track or select one",
                            MessageType.Warning);
                    
                        EditorGUILayout.BeginHorizontal();
                        newName = EditorGUILayout.TextField("Create New", newName);
                        if (GUILayout.Button("Create", GUILayout.Width(90)))
                        {
                            tracks[i] = BGMTrack.Create(newName);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField(name, boldLabelStyle);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                ArrayUtility.Add(ref tracks, null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private Editor audioPreview;
        private GUIStyle audioPreviewBG;
        private float zoomLevel;
        private Vector2 previewScrollPos;

        private AudioClip inspectorClip;
        
        private void DrawPreviewGUI(AudioClip previewClip)
        {
            if (previewClip == null)
            {
                EndPreview();
                return;
            }
            
            GUILayout.FlexibleSpace();

            if (audioPreview == null || inspectorClip != previewClip)
            {
                audioPreview = CreateEditor(previewClip);
                inspectorClip = previewClip;
            }

            if (audioPreviewBG == null)
            {
                audioPreviewBG = new GUIStyle();
                audioPreviewBG.normal.background = Texture2D.blackTexture;
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(inspectorClip.name);
            GUILayout.FlexibleSpace();
            audioPreview.OnPreviewSettings();
            EditorGUILayout.EndHorizontal();

            audioPreview.OnPreviewGUI(GUILayoutUtility.GetRect(256, 100), audioPreviewBG);
        }
        
        private int GetSamplePosition()
        {
            var audioUtil = Assembly.GetAssembly(typeof(UnityEditor.Editor)).GetType("UnityEditor.AudioUtil");
            // ReSharper disable once PossibleNullReferenceException
            return (int) audioUtil?.GetMethod("GetClipSamplePosition")?
                .Invoke(null, new object[] {inspectorClip});
        }
    }
#endif
    
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BGMZone : UdonSharpBehaviour
    {
        public Collider BGMCollider;
        public int priority;
        
        public SoundData environmentalData;
        public float fadeInTime = 2;
        public float fadeOutTime = 2;
        public bool singleFade;

        public AudioClip[] defaultClips;
        public VRCUrl[] defaultClipsURL;
        public bool hasNightTrack;
        public AudioClip[] nightClips;
        public VRCUrl[] nightClipsURL;
        
        public bool unityLoop;
        public bool doSync = true;
        public BGMZoneUSynced uSync;
#if(USEAUDIOLINK)
        public VRCAudioLink.AudioLink audioLink;
#endif
        public bool USynced => doSync && uSync;
        public bool syncOnlyStart;
        //will only works with only 1 track
        public bool forceSync;
        public float trackIntervals = 300f;
        public float trackOffset = 120f;
        
        public float bgmSeed = 2022.1106f;

        public int id;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        /*
        [MenuItem("SAO/Audio/Toggle force clip removal")]
        public static void ToggleRemoveClips()
        {
            bool newState = !EditorPrefs.GetBool("BGMManagerRemoveClips");
            EditorPrefs.SetBool("BGMManagerRemoveClips", newState);

            EditorUtility.DisplayDialog("BGMManager", $"Force clip removal was {(newState ? "enabled" : "disabled")}.",
                "Ok");
        }
        */

        public void SyncClips()
        {
            BGMClipStorage clipStorage = GetComponentInChildren<BGMClipStorage>();
            
            hasNightTrack = clipStorage.hasNightTrack;

            int dl = clipStorage.defaultTracks.Length;
            defaultClips = new AudioClip[dl];
            defaultClipsURL = new VRCUrl[dl];
            int nl = clipStorage.nightTracks.Length;
            nightClips = new AudioClip[nl];
            nightClipsURL = new VRCUrl[nl];

            for (int i = 0; i < dl; i++)
            {
                defaultClips[i] = clipStorage.defaultTracks[i].clip;
                defaultClipsURL[i] = new VRCUrl( clipStorage.defaultTracks[i].clipURL);
            }
            for (int i = 0; i < nl; i++)
            {
                nightClips[i] = clipStorage.nightTracks[i].clip;
                nightClipsURL[i] = new VRCUrl(clipStorage.nightTracks[i].clipURL);
            }
            

            bool removeclips = false;
            removeclips = BGMEditor.ForceRemoveClips;
#if UNITY_ANDROID
            removeclips = true;
#endif
            if (!removeclips) return;
            
            for (int index = 0; index < defaultClipsURL.Length; index++)
            {
                VRCUrl url = defaultClipsURL[index];
                if (url != null && !url.Equals(VRCUrl.Empty))
                {
                    defaultClips[index] = null;
                }
                else
                {
                    Debug.LogWarning($"Clip {defaultClips[index].name} is missing URL, please re add it", this);
                }
            }
            
            for (int index = 0; index < nightClipsURL.Length; index++)
            {
                VRCUrl url = nightClipsURL[index];
                if (url != null && !url.Equals(VRCUrl.Empty))
                {
                    nightClips[index] = null;
                }
                else
                {
                    Debug.LogWarning($"Clip {nightClips[index].name} is missing URL, please re add it", this);
                }
            }

        }
#endif
    }
}
