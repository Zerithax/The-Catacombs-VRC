using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;

namespace Argus.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BGMVideoPlayer : UdonSharpBehaviour
    {
        public BaseVRCVideoPlayer player;
        public AudioSource source;
        float errRetrySec = 3f;
        public VRCUrl url;
        public float timeHolder;
        public PlayerState playerState;
        public int lastPlay;
    
        public Text dbText;

        public float Volume
        {
            get => source.volume;
            set
            {
                source.volume = value;
                source.mute = value <= 0.01f||playerState!=PlayerState.Playing;
            }
        }

        public BGMManager manager;
        private void Start()
        {
            player.Loop = false;
            player.Stop();
            Volume = 0;
            player.EnableAutomaticResync = true;
            _UpdatePlayerState(PlayerState.Stopped);
        }

        private void Update()
        {
            if (dbText)
            {
                dbText.text = $"Playing: {IsPlaying} Mute:{source.mute} time: {player.GetTime()}" +
                              $"\nDuration: {player.GetDuration()}\n";
            }

            if (timeHolder > 0)
                timeHolder += Time.unscaledDeltaTime;
        }

        //|| playerState == PLAYER_STATE_LOADING
        public bool IsPlaying => player.IsPlaying;
        private void _UpdatePlayerState(PlayerState state)
        {
            DebugLog($"_UpdatePlayerState {state}");
            playerState = state;
        }

        public void PlayUrl(VRCUrl iurl,bool retrying = false)
        {
            DebugLog($"PlayUrl {iurl} retrying:{retrying}");
            if (!Utilities.IsValid(iurl))
                return;
            string urlStr = iurl.Get();
            if (urlStr == null || urlStr == "")
                return;
            url = iurl;

            Volume = 0;
            
            _UpdatePlayerState(PlayerState.Loading);
            if(!retrying)_errorCount = 0;
            
            player.LoadURL(iurl);
        }
        
        private int _playTryCount = 0;
        const int playTryMax = 6;
        public void Play()
        {
            if (playerState == PlayerState.Loading)
            {
                return;
            }
            DebugLog("Play called");
            player.Play();
        }

        public void Resume()
        {
            if (playerState == PlayerState.Pause)
            {
                Play();
            }
            else
            {
                DebugLog($"Resume called but not paused state:{playerState}");
                if (playerState == PlayerState.Loading)Reload();
            }
        }
        public void Pause()
        {
            DebugLog("Pause called");
            _UpdatePlayerState(PlayerState.Pause);
            player.Pause();
        }
        public void Stop()
        {
            DebugLog("Stop called");
            Volume = 0;
            _UpdatePlayerState(PlayerState.Stopped);
            url = VRCUrl.Empty;
            player.Stop();
        }

        public void SetTime(float value)
        {
            if (playerState != PlayerState.Playing)
            {
                timeHolder = value;
            }
            else
            {
                DebugLog($"Time set to {value}");
                player.SetTime(Mathf.Clamp(value,0f,player.GetDuration()));
                player.Play();
            }
        }

        public void SetTimeHolder()
        {
            if (timeHolder > 0)
            {
                player.SetTime(timeHolder);
                DebugLog($"Time set to {timeHolder}");
                timeHolder = 0;
            }
        }
        public override void OnVideoReady()
        {
            DebugLog("Video ready");

            player.Play();
        }
        public override void OnVideoStart()
        {
            DebugLog("Video start");
            _UpdatePlayerState(PlayerState.Playing);
            //_OnVideoStart();
            Volume = Volume;
            SetTimeHolder();
        }

        public override void OnVideoEnd()
        {
            DebugLog("Video end");
            _UpdatePlayerState(PlayerState.Stopped);

            Volume = 0;
            player.Pause();
            //_OnVideoEnd();
        }

        public override void OnVideoError(VideoError videoError)
        {
            DebugLog($"Video error: {videoError} url:{url}");
            _UpdatePlayerState(PlayerState.Error);
            if(videoError == VideoError.AccessDenied&&manager) manager._UntrustedUrlWarning();
            //_OnVideoError(videoError);
            SendCustomEventDelayedSeconds(nameof(ErrorRetry),errRetrySec);
        }

        public override void OnVideoLoop()
        {
            DebugLog("Video loop");
            //_OnVideoLoop();
        }

        public override void OnVideoPause()
        {
        
            DebugLog("Video pause");
            _UpdatePlayerState(PlayerState.Pause);
        }

        public override void OnVideoPlay()
        {
            DebugLog("Video play");
            _UpdatePlayerState(PlayerState.Playing);
        }

        private int _errorCount = 0;
        const int errRetryMax = 6;
        public void ErrorRetry()
        {
            if (playerState != PlayerState.Error)
            {
                DebugLog("Error retry called but not in error state");
                return;
            }
            _errorCount++;
            if (_errorCount > errRetryMax)
            {
                DebugLog($"Retrying maxed");
                _errorCount = 0;
                return;
            }
            DebugLog($"Retrying try{_errorCount}");
            Reload();
        }

        public void Reload()
        {
            DebugLog("Reload called");
            VRCUrl tUrl = url; 
            player.Stop();
            PlayUrl(tUrl,true);
        }
        void DebugLog(string contents)
        {
            Debug.Log($"[<color=green>BGMVideo</color>][{gameObject.name}] {contents}");
        }
        
    }
    public enum PlayerState
    {
        Stopped,
        Loading,
        Playing,
        Error,
        Pause
    }
}
