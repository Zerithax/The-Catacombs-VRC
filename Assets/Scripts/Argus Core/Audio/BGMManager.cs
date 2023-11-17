#define USESAO
//#define USEAUDIOLINK;

using System;
using System.Collections.Generic;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRRefAssist;
#if USEAUDIOLINK
using VRCAudioLink;
#endif

namespace Argus.Audio
{
    
    #if !COMPILER_UDONSHARP && UNITY_EDITOR
        using UdonSharpEditor;
        using UnityEditor;
    
        [CustomEditor(typeof(BGMManager))]
        public class BGMEditor : Editor
        {
            public static bool ForceRemoveClips = false;
            private bool showDefaultInspector;
            
            private bool showZoneList;
            public override void OnInspectorGUI()
            {
                if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
    
                GUIStyle headerStyle = new GUIStyle(EditorStyles.label) {fontSize = 16, richText = true, fontStyle = FontStyle.Bold}; 
                GUIStyle boldLabelStyle = new GUIStyle(EditorStyles.label) {richText = true, fontStyle = FontStyle.Bold}; 
                EditorGUILayout.LabelField("Background Music Editor", headerStyle);
                EditorGUILayout.Space(5);
                
                EditorGUI.BeginChangeCheck();
                
                BGMManager bgmManager = (BGMManager)target;
    
                if (bgmManager.zones == null)
                {
                    bgmManager.zones = new BGMZone[0];
                }
                showZoneList = EditorGUILayout.Foldout(showZoneList, $"Show Zone List {bgmManager.zones.Length}");
                if (showZoneList)
                {
                    for (int i = 0; i < bgmManager.zones.Length; i++)
                    {
                        EditorGUILayout.BeginVertical("Helpbox");
                
                        bgmManager.zones[i] = (BGMZone)EditorGUILayout.ObjectField("Audio Zone: ", bgmManager.zones[i], typeof(BGMZone), true);
                    
                        /*if (bgmManager.zones[i] == null)
                        {
                            EditorGUILayout.EndVertical();
                            continue;
                        }*/
    
                        EditorGUILayout.BeginHorizontal();
                        //int count = bgmManager.zones[i].defaultClips.Length + bgmManager.zones[i].nightClips.Length;
                        EditorGUILayout.LabelField($"Zone {i}");
    
                        GUILayout.FlexibleSpace();
                        if (bgmManager.zones[i] != null)
                        {
                            if (GUILayout.Button("Inspect"))
                            {
                                EditorApplication.ExecuteMenuItem("Window/General/Inspector");
                                Selection.objects = new UnityEngine.Object[] {bgmManager.zones[i].gameObject};
                            }
                        }

                        if (GUILayout.Button("-", GUILayout.Width(30)))
                        {
                            ArrayUtility.RemoveAt(ref bgmManager.zones, i);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                    }
    
                    if (GUILayout.Button("Add Clip"))
                    {
                        ArrayUtility.Add(ref bgmManager.zones, null);
                    }
                }
                if (GUILayout.Button("Scan for zones and fix naming"))
                {
                    ScanAndFixZones.Scan();
                }
    
                
                EditorGUILayout.Space(4);
                
                EditorGUILayout.LabelField("Settings", boldLabelStyle);
                bgmManager.volume = EditorGUILayout.Slider("Volume", bgmManager.volume, 0, 1);
                bgmManager.useFallback = EditorGUILayout.Toggle("Play zoneFallback", bgmManager.useFallback);
                bgmManager.zoneFallback =
                    (BGMZone) EditorGUILayout.ObjectField("ZoneFallback", bgmManager.zoneFallback, typeof(BGMZone), true);
                bgmManager.environmentalController =
                    (SoundController) EditorGUILayout.ObjectField("Environmental Controller", bgmManager.environmentalController, typeof(SoundController), true);

                bgmManager.active =
                    (AudioSource) EditorGUILayout.ObjectField("Active AudioSource", bgmManager.active, typeof(AudioSource), true);
                bgmManager.fade =
                    (AudioSource) EditorGUILayout.ObjectField("Fade AudioSource", bgmManager.fade, typeof(AudioSource), true);
                //bgmManager.vidPlayer = (BGMVideoPlayer[])EditorGUILayout.ObjectField("Fade AudioSource", bgmManager.vidPlayer, typeof(BGMVideoPlayer[]), true);

                if (!bgmManager.zoneFallback)
                {
                    EditorGUILayout.HelpBox("A BGMZone fallback is needed for BGMManager to work",MessageType.Error);
                }
                EditorGUILayout.LabelField("Debug", boldLabelStyle);
                BGMEditor.ForceRemoveClips = EditorGUILayout.Toggle("Force Remove Clips", ForceRemoveClips);
                bgmManager.dbText = (TextMeshPro) EditorGUILayout.ObjectField("Debugging Text", bgmManager.dbText, typeof(TextMeshPro), true);
                EditorGUILayout.LabelField("Default Inspector");
                showDefaultInspector = EditorGUILayout.Foldout(showDefaultInspector, "Show default inspector");
                if (showDefaultInspector)
                    DrawDefaultInspector();
                /*bgmManager.fadeInTime = EditorGUILayout.FloatField("Fadein time",bgmManager.fadeInTime);
                bgmManager.fadeOutTime = EditorGUILayout.FloatField("Fadeout time",bgmManager.fadeOutTime);
                
                //bgmManager.trackIntervals = EditorGUILayout.FloatField(new GUIContent("Track interval", "The time between "), bgmManager.trackIntervals);
                bgmManager.trackOffset = EditorGUILayout.FloatField(
                    new GUIContent("Seed time offset", "The maximum amount of time the seed will offset playback by"),
                    bgmManager.trackOffset);
                bgmManager.bgmSeed = EditorGUILayout.FloatField("BGM Seed", bgmManager.bgmSeed);*/
               
                if (EditorGUI.EndChangeCheck())
                {
                    //bgmManager.ApplyProxyModifications();
                    EditorUtility.SetDirty(bgmManager);
                    if (PrefabUtility.IsPartOfAnyPrefab(bgmManager.gameObject))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(bgmManager);
                    }
                }
            }
            
        }
    
    public static class ScanAndFixZones
        {
            [RunOnBuild()]
            public static void Scan()
            {
                GameObject[] rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
    
                List<BGMZone> zones = new List<BGMZone>();
                List<BGMManager> managers = new List<BGMManager>();
                foreach (GameObject obj in rootGameObjects)
                {
                    zones.AddRange(obj.GetComponentsInChildren<BGMZone>());
                    managers.AddRange(obj.GetComponentsInChildren<BGMManager>());
                }

                if (managers.Count <= 0)
                {
                    Debug.LogWarning("No BGMManager found in scene, aborting scan");
                    return;
                }
                if(managers.Count > 1)
                {
                    Debug.LogWarning("Multiple BGMManagers found in scene, there should only be one. Using first found");
                }
                BGMManager bgmManager = managers[0];
                if (!bgmManager.zoneFallback.name.StartsWith("BGMFallback"))
                    bgmManager.zoneFallback.name = "BGMFallback";
                
                for (int index = 0; index < zones.Count; index++)
                {
                    if (zones[index].gameObject.name.StartsWith("BGMFallback"))
                    {
                        zones.RemoveAt(index);
                        break;
                    }
                }

                for (int index = 0; index < zones.Count; index++)
                {
                    GameObject zone = zones[index].gameObject;
                    string[] sp = zone.name.Split('_');
                    zone.name = $"BGMZone_{index}";
                    if (sp.Length > 2)
                    {
                        for (int j = 2; j < sp.Length; j++)
                            zone.name += $"_{sp[j]}";
                    }
                }
    
                bgmManager.zones = zones.ToArray();
                Debug.Log($"Scanned and fixed {zones.Count} zones");
            }
         }
    #endif

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)][Singleton]
    public class BGMManager : UdonSharpBehaviour
    {
        #region SINGLETONS

        [SerializeField, HideInInspector] private AudioManager audioManager;

        #endregion
        
        [Header("Sources")] public BGMZone[] zones;
        public SoundController environmentalController;

        [Header("Settings")] public float volume = 0.3f;
        public bool isNight;
        public bool useFallback;
        

        [Header("Internal")] public float activeVol;
        public float fadeVol;
        public float fadeInTimer;
        public float fadeOutTimer;
        public bool TempMute => inCombat;
        public bool inCombat;
        
        public int[] zonePriority;
        public AudioSource active;
        public AudioSource fade;
        public BGMZone zone;
        public BGMZone zoneFallback;
#if(USEAUDIOLINK)
        public AudioLink audioLink;
#endif
        //Video player
        public BGMVideoPlayer[] vidPlayer;
        //public int[] vidPlayerState;
        public int activePlayerIndex = -1;
        public int fadePlayerIndex = -1;
        public BGMVideoPlayer ActivePlayer => ActivePlayerValid ? vidPlayer[activePlayerIndex] : null;
        public BGMVideoPlayer FadePlayer => FadePlayerValid ? vidPlayer[fadePlayerIndex] : null;
        public bool ActivePlayerValid => activePlayerIndex > -1 && activePlayerIndex < vidPlayer.Length;
        public bool FadePlayerValid => fadePlayerIndex > -1 && fadePlayerIndex < vidPlayer.Length;
        public VRCUrl ActiveUrl
        { get {
                if (ActivePlayerValid) return ActivePlayer.url;
                else return VRCUrl.Empty; }
        }
        public VRCUrl FadeUrl
        { get {
                if (FadePlayerValid) return FadePlayer.url;
                else return VRCUrl.Empty; }
        }
        
        public bool isPlaying
        {
            get
            {
                bool vidPlaying=false;
                if (ActivePlayerValid)
                {
                        if(ActivePlayer.IsPlaying)
                        {
                            vidPlaying = true;
                        }
                }
                return active.isPlaying||vidPlaying;
            }
        }

        [Header("Status")] public double time;
        public float trackTime;
        public double previousTrackTime;
        public double currentTrackTime;
        public double nextTrackTime;

        [Header("Debugging")] public TextMeshPro dbText;

        private void Start()
        {
            
            
            zonePriority = new int[zones.Length];
            zone = zoneFallback;
            zoneFallback.id = -1;
            //_setTimer();
            foreach (var p in vidPlayer)
            {
                p.manager = this;
            }
            if (audioManager) audioManager._SubscribeMusicVolume(this,nameof(_MusicVolumeChanged));
            _MusicVolumeChanged();
        }

        private float musicVolume;
        public void _MusicVolumeChanged()
        {
            if (audioManager) musicVolume = audioManager.MusicVolume;
            else
                musicVolume = 1;/*
            active.volume = activeVol * volume * musicVolume;
            fade.volume = fadeVol * volume * musicVolume;*/
            UpdateFade();
        }

        public void Mute(bool mute)
        {
            inCombat = mute;
        }

        //Update for debugging and timer
        private double _updateTimeSync;

        private void Update()
        {
            //Debugging
            if (dbText)
            {
                string aClip = "null";
                string fClip = "null";
                if (active.clip) aClip = active.clip.name;
                if (fade.clip) fClip = fade.clip.name;
                //UpdateTrackTime();
                //UpdateTime(false);
                
                dbText.text = $"{zone.gameObject.name}\n***Volume***\nActive:{activeVol:0.00}\tFade:{fadeVol:0.00}\n" +
                              $"***Track***\nZone:{zone.id}\nActive:{aClip}\nFade:{fClip}\nIsPlaying: {isPlaying} aPlaying:{active.isPlaying}\n" +
                              $"ActPlayer:{activePlayerIndex} FadePlayer:{fadePlayerIndex}\n";
                for (int i = 0; i < vidPlayer.Length; i++)
                {
                    var vidStr = "null";
                    if (Utilities.IsValid(vidPlayer[i].url))
                        vidStr = vidPlayer[i].url.ToString()
                            .Substring(Math.Max(0, vidPlayer[i].url.ToString().Length - 15));
                    dbText.text += $"VidID{i}: PS:{vidPlayer[i].playerState} Lp:{vidPlayer[i].lastPlay} " +
                                   $"P:{vidPlayer[i].IsPlaying} M:{vidPlayer[i].source.mute} V:{vidPlayer[i].source.volume:0.00}\n{vidPlayer[i].player.GetTime():.0}/{vidPlayer[i].player.GetDuration():.0} URL:{vidStr}\n";
                }
                dbText.text += $"***Time***\nTime:\t{TimeNow() / 1000d}\nCurrentTrackTime:\t{currentTrackTime}\nnextTrackTime:\t{nextTrackTime}\ntrackTime:\t{trackTime}\n" +
                                            $"ActiveTime:\t{active.time}\nFadeTime:\t{fade.time}" +
                                            $"\nPriority ";
                for (int i = 0; i < zonePriority.Length; i++)
                {
                    dbText.text += $" {zonePriority[i]}";
                }
                if (zone.USynced) dbText.text += $"\nUsynced: {zone.uSync.timeOffset}";
            }
            if((TempMute? activeVol>0:activeVol<1)||fadeVol>0){ UpdateFade();}
            if (_updateTimeSync > 0) _updateTimeSync -= Time.deltaTime;
            else
            {
                UpdateTime(false);
                if (!isPlaying||zone.forceSync)
                {
                    _SyncTrack();
                }
                _updateTimeSync = nextTrackTime - time / 1000d;
            }

            #region TPDebug
            /*var localPlayer = Networking.LocalPlayer;
            if (Input.GetKeyDown(KeyCode.J))
            {
                int zonI = zone.id;
                ZoneExit(zonI);
                ZoneEnter(Mathf.Clamp(zonI - 1, 0, zones.Length - 1));
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                int zonI = zone.id;
                ZoneExit(zonI);
                ZoneEnter(Mathf.Clamp(zonI + 1, 0, zones.Length - 1));
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                int zonI = zone.id;
                ZoneExit(zonI);
                ZoneEnter(UnityEngine.Random.Range(0, zones.Length));
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                int zonI = zone.id;
                if(ActivePlayerValid) ActivePlayer.Play();
                /*
                ZoneExit(zonI);
                ZoneEnter(zonI);#1#
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                if(ActivePlayerValid) ActivePlayer.Reload();
            }*/
            #endregion
        }

        public void UpdateFade()
        {
            //if(activeVol>=1&&fadeVol<=0) return;
            bool activeLoading = false;
            if (ActivePlayerValid)
            {
                activeLoading = ActivePlayer.playerState == PlayerState.Loading;
            }

            if ((!zone.singleFade || (zone.singleFade && fadeVol == 0)) && (active.clip || !activeLoading))
            {
                if (!TempMute)
                {
                    activeVol = Mathf.Clamp(activeVol + (Time.deltaTime / fadeInTimer), 0, 1);
                }
                else
                {
                    activeVol = Mathf.Clamp(activeVol - (Time.deltaTime / zone.fadeOutTime), 0, 1);
                }
                
            }
                
            var volA =activeVol * volume * musicVolume;
            active.volume = volA;
            if (ActivePlayerValid) ActivePlayer.Volume = volA;
            

            if(fade.clip||FadePlayerValid)
                fadeVol = Mathf.Clamp(fadeVol - (Time.deltaTime / fadeOutTimer), 0, 1);

            var volF = fadeVol * volume * musicVolume;
            fade.volume = volF;
            if (FadePlayerValid) FadePlayer.Volume = volF;
            
            if (fadeVol <= 0)
            {
                fade.mute = true;
                fade.volume = 0f;
                fade.loop = false;
                fade.Stop();
                if (FadePlayerValid)
                { 
                    FadePlayer.Pause();
                    fadePlayerIndex = -1;
                }
            }
        }
        
        //***********Set Tracks***********//

        //set BGM Zone
        public void SetActive(int index)
        {
            if (index < 0 || index >= zones.Length)
            {
                Log($"Incorrect/Fallback zone index ({index})");
                zone = zoneFallback;
                zone.id = -1;
            }
            else
            {
                if (index != zone.id)
                {
                    Log($"Switching BGM track to {index}");
                    zone = zones[index];
                    zone.id = index;
                }
                else
                {
                    Log($"BGM track {index} is already active");
                    return;
                }
            }
            _SyncTrack();
        }

        //Sync track to the correct track/time
        public void _SyncTrack()
        { 
#if(USEAUDIOLINK)
            if(audioLink) audioLink.transform.parent.gameObject.SetActive(false);
#endif
            //check for fallback
            if (!useFallback && zone.id < 0)
            {
                if (environmentalController) environmentalController.SetActive(null);
                return;
            }
            
#if(USEAUDIOLINK)
            if (zone.audioLink)
            {
                audioLink = zone.audioLink;
                audioLink.transform.parent.gameObject.SetActive(true);;
            }
#endif
            //get clips
            var toPlay = _getClip();
            var toPlayUrl = _getClipURL();
            var urlStr = toPlayUrl.ToString();
            if(environmentalController) environmentalController.SetActive(zone.environmentalData);

            if(zone.USynced) CheckUSyncedOffset();
            if ((isPlaying && !zone.forceSync))
            {
                //Log("SyncTrack quit: isPlaying");
                return;
            }

            if (toPlay == null&&urlStr=="")
            {
                //Log("SyncTrack quit: url and audio null");
                return;
            }

            if ((toPlay == fade.clip&&toPlay!=null&& fade.isPlaying)||(toPlayUrl.Equals(FadeUrl)&&!toPlayUrl.Equals(VRCUrl.Empty)&&FadePlayer.IsPlaying) )
            {
                
                //Log("SyncTrack quit: cancel fade out");
                FadeOutCancel();
                return;
            }

            FadeOutActive();
            if (active.clip != toPlay)
            {
                active.clip = toPlay;
                active.volume = 0f;
                activeVol = 0.01f;
#if(USEAUDIOLINK)
                if (audioLink) audioLink.audioSource = active;
#endif
            }

            if (!urlStr.Equals("")&&active.clip==null)
            {
                PlayUrl(toPlayUrl);
            }
            
            fadeInTimer = zone.fadeInTime;
            _SyncActive();
            
        }

        //sync active track position
        public void _SyncActive()
        {
            UpdateTime(false);
            active.mute = false;
            UpdateTrackTime();

            if (active.clip)
            {
                if (!(zone.syncOnlyStart && trackTime > 0.5f) && zone.doSync)
                {
                    active.time = Mathf.Clamp(trackTime, 0, active.clip.length - 0.1f);
                }
                else
                {
                    active.time = 0;
                }


                active.loop = zone.unityLoop;
                active.Play();
            }

            if (ActivePlayerValid)
            {
                if (!(zone.syncOnlyStart && trackTime > 0.5f) && zone.doSync)
                {
                    ActivePlayer.SetTime(trackTime);
                }
                else
                {
                    ActivePlayer.SetTime(0);
                }
            }
            
            UnloadActivePlayer();
        }

        private int _latestPlay;
        public int PlayUrl(VRCUrl toPlayUrl)
        {
            //check for cached player
            for (int i = 0; i < vidPlayer.Length; i++)
            {
                if (toPlayUrl.Equals(vidPlayer[i].url))
                {
                    //Log($"Zone{zone.id} URL: {toPlayUrl}");
                    Log($"Using cached player {i}");
                    if(activePlayerIndex != i) activeVol = 0.01f;
                    SetActivePlayer(i);
                    ActivePlayer.Resume();
                    
#if(USEAUDIOLINK)
                    if (audioLink) audioLink.audioSource = vidPlayer[i].source;
#endif
                    ActivePlayer.lastPlay = _latestPlay+1;
                    _latestPlay+=1;
                    return i;
                }    
            }
            //check for playable player
            int toPlayer = -1;
            int lowestPlayer = Int32.MaxValue;
            for (int i = 0; i < vidPlayer.Length; i++)
            {
                //play on empty url player
                if (!Utilities.IsValid( vidPlayer[i].url))
                {
                    toPlayer = i;
                    break;
                }
                //find the oldest 
                else if (i!=activePlayerIndex&&i!=fadePlayerIndex&&vidPlayer[i].lastPlay<lowestPlayer)
                {
                    toPlayer = i;
                    lowestPlayer = vidPlayer[i].lastPlay;
                }
            }

            if (toPlayer < 0)
            {
                Log("NO PLAYABLE URL PLAYER FOUND!");
                return -1;
            }
            //Log($"Zone{zone.id} URL: {toPlayUrl}");
            SetActivePlayer(toPlayer);
            ActivePlayer.PlayUrl(toPlayUrl);
            activeVol = 0.01f;
#if(USEAUDIOLINK)
            if (audioLink) audioLink.audioSource = vidPlayer[toPlayer].source;
#endif
            ActivePlayer.lastPlay = _latestPlay+1;
            _latestPlay+=1;
            
            //fade out any active player
            //UnloadActivePlayer(toPlayer);
            return toPlayer;
        }

        public void UnloadActivePlayer()
        {
            for (int i = 0; i < vidPlayer.Length; i++)
            {
                if(i==activePlayerIndex||i==fadePlayerIndex) continue;
                if (vidPlayer[i].IsPlaying||
                    (vidPlayer[i].playerState!=PlayerState.Stopped&&vidPlayer[i].playerState!=PlayerState.Pause))
                {
                    LogError($"PLAYER {i} SHOULD NOT BE PLAYING");
                    vidPlayer[i].Pause();
                }
            }
        }

        void SetActivePlayer(int player)
        {
            if(activePlayerIndex == player) return;
            if (ActivePlayerValid)
            {
                FadeOutActive();
            }
            activePlayerIndex = player;
        }

        //***********Zone Priority***********//
        //I'm still mad at wonder egg priority ending
        private bool _priorityRequested;
        public void ZoneEnter(int index)
        {
            Log($"Zone entered!");

            if (index < 0 || index >= zones.Length)
            {
                //Log($"Zone {index} is invalid!");
            }
            else
            {
                if (zonePriority[index] > 0)
                {
                    Log($"Zone {index} is already active");
                    return;
                }

                int priorityRange = zones[index].priority * 100;
                int basePriority = priorityRange +1;
                //make sure no conflict of priority
                for (int i = 0; i < zonePriority.Length; i++)
                {
                    var check = zonePriority[i];
                    if (check > priorityRange && check < priorityRange + 100 && check >= basePriority)
                    {
                        basePriority = check + 1;
                    }
                }
                zonePriority[index] = basePriority;
                Log($"Zone {index} entered with priority level {zonePriority[index]}");
                PriorityZoneStart();
            }
        }
        public void ZoneExit(int index)
        {
            if (index < 0 || index >= zonePriority.Length)
            {
                Log($"Zone {index} exit invalid");
            }
            else
            {
                zonePriority[index] = 0;
                if (index == zone.id)
                {
                    FadeOutActive();
                } 
                Log($"Zone {index} exited");
            }

            PriorityZoneStart();
        }

        public void PriorityZoneStart()
        {
            if (!_priorityRequested)
            {
                _priorityRequested = true;
                SendCustomEventDelayedSeconds(nameof(PriorityZoneUpdate),.5f);
            }
        }
        public void PriorityZoneUpdate()
        {
            _priorityRequested = false;
            int highestCheck = 0;
            int pZone = -1;
            for (int i = 0; i < zonePriority.Length; i++)
            {
                if (zonePriority[i] > highestCheck)
                {
                    pZone = i;
                    highestCheck = zonePriority[i];
                }
            }

            if (pZone != zone.id)
            {
                FadeOutActive();
            } 
            SetActive(pZone);
            Log($"PriorityZone set to {pZone}");
        }
        
        //***********Fading***********//
        public void FadeOutActive()
        {
            if (isPlaying)
            {
                fadeOutTimer = zone.fadeOutTime;
                fade.mute = false;
                fade.clip = active.clip;
                fade.time = active.time;
                fadeVol = activeVol;
                if(fadeVol <=0) fadeVol = 0.01f;
                
                fade.loop = active.loop;
                active.clip = null;
                active.loop = false;
                fade.Play();
                
                if (FadePlayerValid)
                {
                    FadePlayer.Pause();
                    fadePlayerIndex = -1;
                }

                if (ActivePlayerValid)
                {
                    fadePlayerIndex = activePlayerIndex;
                    activePlayerIndex = -1;
                }
            }
            else
            {
                active.clip = null;
            }
        }

        public void FadeOutCustom(float speed)
        {
            FadeOutActive();
            fadeOutTimer = speed;
        }

        //***********Fading (safe)***********//
        public void FadeOutID(int index)
        {
            if (index == zone.id)
            {
                FadeOutActive();
                SetActive(-1);
            }
        }

        public void FadeOutIDCustom(int index, float speed)
        {
            if (index == zone.id)
            {
                FadeOutCustom(speed);
                SetActive(-1);
            }
            
        }

        //you know what? fuck you *unfade you fade out*
        public void FadeOutCancel()
        {
            if (fade.isPlaying)
            {
                active.mute = false;
                active.clip = fade.clip;
                active.time = fade.time;
                activeVol = fadeVol;
                active.loop = fade.loop;
                
                active.Play();

                fade.clip = null;
                fade.mute = true;
                fade.volume = 0f;
                fade.loop = false;
                fade.Stop();
            }
            if (ActivePlayerValid)
            {
                ActivePlayer.Pause();
                activePlayerIndex = -1;
            }
            if (FadePlayerValid)
            {
                activePlayerIndex = fadePlayerIndex;
                fadePlayerIndex = -1;
            }
        }

        //***********Time Randomizer***********//
        
        double SeedTime(double epoch,float offTime)
        {
            int offset = Mathf.RoundToInt(SeedRandom(epoch, 1f) * offTime);
            return epoch + offset;
        }

        float SeedRandom(double seed, float ran)
        {
            int x = Mathf.RoundToInt(Convert.ToSingle((seed % 10000000d) + Math.Floor(seed / 10000000d)) / 100f);
            //float o = (Mathf.PerlinNoise(x, zone.bgmSeed * ran) % 0.01f) * 100f;
            
            UnityEngine.Random.InitState(x+Mathf.RoundToInt(zone.bgmSeed * ran));
            float o = UnityEngine.Random.Range(0, 100f);
            //UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
            //Log($"SeedRan {o} seed {seed} ran {ran} x{x}");
            return o/100f;
        }


        //***********functions***********//
        public AudioClip _getClip()
        {
            //if (index > zones.Length||index<0) return null;
            UpdateTime(false);
            AudioClip clip;
            if (zone.hasNightTrack && isNight)
            {
                clip = _getRandomClip( zone.nightClips);
            }
            else
            {
                clip = _getRandomClip(zone.defaultClips);
            }
            return clip;
        }
        public VRCUrl _getClipURL()
        {
            //if (index > zones.Length||index<0) return null;
            UpdateTime(false);
            VRCUrl clip;
            if (zone.hasNightTrack && isNight)
            {
                clip = _getRandomUrl(zone.nightClipsURL);
            }
            else
            {
                Debug.Log($"Getting {zone.id} default clip {zone.defaultClipsURL.Length} {zone.defaultClips.Length}");
                clip = _getRandomUrl(zone.defaultClipsURL);
            }
            return clip;
        }

        public AudioClip _getRandomClip(AudioClip[] clips)
        {
            if (clips == null) return null;
            if (clips.Length == 0) return null;
            int cIndex = _getRandomClipIndex(clips.Length);
            return  clips[cIndex];
        }

        public VRCUrl _getRandomUrl(VRCUrl[] clips)
        {
            if (clips == null) return VRCUrl.Empty;
            if (clips.Length == 0) return VRCUrl.Empty;
            int cIndex = _getRandomClipIndex(clips.Length);
            return  clips[cIndex];
        }
        public int _getRandomClipIndex(int indexLength)
        {
            if (indexLength == 0) return 0;
            int cIndex = Mathf.RoundToInt(SeedRandom(currentTrackTime, 1.5f) * (indexLength - 1));
            return  cIndex;
        }

    //***********time syncing***********//
        long TimeNow()
        {
            DateTimeOffset now = Networking.GetNetworkDateTime();
            return now.ToUnixTimeMilliseconds();
        }

        public void UpdateTime(bool ignoreUSync)
        {
            time = TimeNow();
            if (zone.USynced&&!ignoreUSync)
            {
                time -= zone.uSync.timeOffset;
            }
            float intervals = zone.trackIntervals;
            float offTime = zone.trackOffset;
            //if (zone.gapless) { offTime = 0; if ((!isNight||!zone.hasNightTrack) && zone.defaultClips.Length > 0) intervals = zone.defaultClips[0].length;else if (isNight && zone.nightClips.Length > 0) intervals = zone.defaultClips[0].length; }
            previousTrackTime = SeedTime((Math.Floor((time / 1000d) / intervals) - 1) * intervals,offTime);
            currentTrackTime = SeedTime(Math.Floor((time / 1000d) / intervals) * intervals,offTime);
            nextTrackTime = SeedTime(Math.Ceiling((time / 1000d) / intervals) * intervals,offTime);
        }

        public void UpdateTrackTime()
        {
            trackTime = Convert.ToSingle(time / 1000d - currentTrackTime);
            if (trackTime < 0) trackTime = Convert.ToSingle(time / 1000d - previousTrackTime);
        }
    //***********udon networking***********//
        public void CheckUSyncedOffset()
        {
            if (zone.USynced)
            {
                if (zone.uSync.NeedUpdate())
                {
                    UpdateTime(true);
                    var t = TimeNow();
                    double cTime = (currentTrackTime* 1000d);
                    if (cTime < t) cTime = (previousTrackTime* 1000d);
                    bool o = zone.uSync.UpdateOffset((float)(t-cTime  ));
                    if(o) Log("Success Update offset");
                }
                else
                {
                    zone.uSync.OwnerUpdateTime();
                }
            }
        }


        //Debug

        

#if (USESAO)

        public bool uUrlDismissed;
        public void _UntrustedUrlWarning()
        {
            /*
            if(uUrlDismissed||tempNotification) return;
            
            tempNotification = notificationHandler._SetupPrompt("Untrusted URL not allowed", $"Untrusted URl is disabled, please enable it in the VRChat settings page", this,
                nameof(_UntrustedUrlAccept), nameof(_UntrustedUrlReject));
            */
        }

        public void _UntrustedUrlReject()
        {
            /*
            tempNotification = null;
            uUrlDismissed = true;
            */
        }
        public void _UntrustedUrlAccept()
        {
            /*
            tempNotification = null;
            */
        }
#else
        public void _UntrustedUrlWarning() { }
#endif
            public void dbPlay0()
        {
            SetActive(0);
        }

        public void dbPlay1()
        {
            SetActive(1);
        }

        public void ToggleNight()
        {
            isNight = !isNight;
            _SyncTrack();
        }
        public void SetNight(bool night)
        {
            isNight = night;
            _SyncTrack();
        }

        private void Log(string contents)
        {
            Debug.Log($"[<color=green>BGMManager</color>] {contents}");
        }private void LogError(string contents)
        {
            Debug.LogError($"[<color=green>BGMManager</color>] {contents}");
        }
    }
}