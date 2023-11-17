
using Argus.Audio;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SoundData : UdonSharpBehaviour
{
    public AudioClip[] defaultClips;
    public bool hasNightTrack;
    public AudioClip[] nightClips;
    
    public bool unityLoop;
    public bool doSync = true;
    public BGMZoneUSynced uSync;
    public float volume = 0.3f;
    public bool USynced => doSync && uSync;
    public bool syncOnlyStart;
    //will only works with only 1 track
    public bool gapless;
    public bool forceSync;
    public float trackIntervals = 30f;
    public float trackOffset = 12f;
        
    public float seed = 1970.0101f;
}
