using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

//value branded BGMController!
namespace Argus.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SoundController : UdonSharpBehaviour
    {
        [Header("Settings")]
        public bool isNight;

        [Header("Internal")] 
        public AudioSource active;
        public SoundData data;
        public bool isPlaying => active.isPlaying;

        [Header("Status")] public double time;
        public float trackTime;
        public double previousTrackTime;
        public double currentTrackTime;
        public double nextTrackTime;

        [Header("Debugging")] public Text dbText;
        
        //Update for debugging and timer
        private double _updateTimeSync;

        private void Update()
        {
            if (data)
            {
                //Debugging
                if (dbText)
                {
                    string aClip = "null";
                    if (active.clip) aClip = active.clip.name;
                    UpdateTrackTime();
                    UpdateTime(false);
                    /*dbText.text = $"{zone.gameObject.name}\n***Volume***\nActive:{activeVol}\tFade:{fadeVol}\n" +
                                  $"***Track***\nZone:{zone.id}\nActive:{aClip}\nFade:{fClip}\n" +
                                  $"***Time***\nTime:\t{time / 1000d}\nCurrentTrackTime:\t{currentTrackTime}\nnextTrackTime:\t{nextTrackTime}\ntrackTime:\t{trackTime}\n" +
                                  $"ActiveTime:\t{active.time}\nFadeTime:\t{fade.time}" +
                                  $"\nPriority ";
                    for (int i = 0; i < zonePriority.Length; i++)
                    {
                        dbText.text += $" {zonePriority[i]}";
                    }*/

                    if (data.USynced) dbText.text += $"\nUsynced: {data.uSync.timeOffset}";
                }

                if (_updateTimeSync > 0) _updateTimeSync -= Time.deltaTime;
                else
                {
                    UpdateTime(false);
                    if (!isPlaying || data.forceSync)
                    {
                        _SyncTrack();
                    }
                    else
                    {
                        _updateTimeSync = nextTrackTime - time / 1000d;
                    }
                    //Log($"Last Update Sync: {time}@{currentTrackTime} next Sync in: {_updateTimeSync}");
                }
            }
        }

        public void SetActive(SoundData d)
        {
            data = d;
            if(data) _SyncTrack();
        }


        //Sync track to the correct track/time
        public void _SyncTrack()
        {
            var toPlay = _getClip();

            if (data.USynced) CheckUSyncedOffset();
            if ((isPlaying && !data.forceSync) || toPlay == null)
            {
                return;
            }

            if (active.clip != toPlay)
            {
                active.clip = toPlay;

            }

            _SyncActive();

            _updateTimeSync = nextTrackTime - time / 1000d;
        }

        //sync active track
        public void _SyncActive()
        {
            UpdateTime(false);
            active.mute = false;
            active.volume = data.volume;
            UpdateTrackTime();

            if (active.clip)
            {
                if (!(data.syncOnlyStart && trackTime > 0.5f) && data.doSync)
                {
                    active.time = Mathf.Clamp((float) trackTime, 0, active.clip.length - 0.1f);
                }
                else
                {
                    active.time = 0;
                }

                active.loop = data.unityLoop;
                active.Play();
            }

        }


        //***********Time Randomizer***********//

        double SeedTime(double epoch, float offTime)
        {
            int offset = Mathf.RoundToInt(SeedRandom(epoch, 1f) * offTime);
            return epoch + offset;
        }

        float SeedRandom(double seed, float ran)
        {
            float x = Convert.ToSingle((seed % 10000000d) + Math.Floor(seed / 10000000d)) / 100f;
            float o = (Mathf.PerlinNoise(x, data.seed * ran) % 0.01f) * 100f;
            //Log($"SeedRan {o} seed {seed} ran {ran} x{x}");
            return o;
        }


        //***********functions***********//
        public AudioClip _getClip()
        {
            //if (index > zones.Length||index<0) return null;
            UpdateTime(false);
            AudioClip clip;
            if (data.hasNightTrack && isNight)
            {
                clip = _getRandomClip(data.nightClips);
            }
            else
            {
                clip = _getRandomClip(data.defaultClips);
            }

            return clip;
        }

        public AudioClip _getRandomClip(AudioClip[] clips)
        {
            if (clips.Length == 0) return null;
            int indexLength = clips.Length;
            if (indexLength == 0) return null;
            int cIndex = Mathf.RoundToInt(SeedRandom(currentTrackTime, 1.5f) * (indexLength - 1));
            //Debug.Log("cIndex "+cIndex);
            return clips[cIndex];
        }

        //***********time syncing***********//
        long TimeNow()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            return now.ToUnixTimeMilliseconds();
        }

        public void UpdateTime(bool ignoreUSync)
        {
            time = TimeNow();
            if (data.USynced && !ignoreUSync)
            {
                time -= data.uSync.timeOffset;
            }

            float intervals = data.trackIntervals;
            float offTime = data.trackOffset;
            if (data.gapless)
            {
                offTime = 0;
                if ((!isNight || !data.hasNightTrack) && data.defaultClips.Length > 0)
                    intervals = data.defaultClips[0].length;
                else if (isNight && data.nightClips.Length > 0) intervals = data.defaultClips[0].length;
            }

            previousTrackTime = SeedTime((Math.Floor((time / 1000d) / intervals) - 1) * intervals, offTime);
            currentTrackTime = SeedTime(Math.Floor((time / 1000d) / intervals) * intervals, offTime);
            nextTrackTime = SeedTime(Math.Ceiling((time / 1000d) / intervals) * intervals, offTime);
        }

        public void UpdateTrackTime()
        {
            trackTime = Convert.ToSingle(time / 1000d - currentTrackTime);
            if (trackTime < 0) trackTime = Convert.ToSingle(time / 1000d - previousTrackTime);
        }

        //***********udon networking***********//
        public void CheckUSyncedOffset()
        {
            if (data.USynced)
            {
                if (data.uSync.NeedUpdate())
                {
                    UpdateTime(true);
                    var t = TimeNow();
                    double cTime = (currentTrackTime * 1000d);
                    if (cTime < t) cTime = (previousTrackTime * 1000d);
                    bool o = data.uSync.UpdateOffset((float) (t - cTime));
                    if (o) Log("Success Update offset");
                }
                else
                {
                    data.uSync.OwnerUpdateTime();
                }
            }
        }

        public void SetNight(bool night)
        {
            isNight = night;
            _SyncTrack();
        }

        private void Log(string contents)
        {
            Debug.Log($"[<color=green>SoundController</color>] {contents}");
        }
    }
}