using System;
using System.Security.Policy;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Argus.Audio
{
    
#if !COMPILER_UDONSHARP && UNITY_EDITOR
    using UdonSharpEditor;
    using UnityEditor;
    
    [CustomEditor(typeof(BGMZoneUSynced))]
    public class BGMZoneUSyncEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            BGMZoneUSynced uSync = (BGMZoneUSynced) target;
                
            EditorGUI.BeginChangeCheck();
                
            uSync.zone = 
                (BGMZone)EditorGUILayout.ObjectField(new GUIContent("BGMZone", "BGMZone associated with this USync object, this should be auto applied by BGMZone"),uSync.zone,typeof(BGMZoneUSynced),true);
            if (!uSync.zone)
            {
                EditorGUILayout.HelpBox("BGMZone is needed to use uSync, this should be auto applied by BGMZone",MessageType.Error);
            }
            else
            {
                uSync.resetOwnerTime =
                    EditorGUILayout.IntField(new GUIContent("Reset Owner Time", "The maximum time before a new player will take owner of this zone (seconds)"), uSync.resetOwnerTime);
        
                // if (!uSync.zone.gapless)
                // {
                //     float minROT = uSync.zone.trackIntervals + uSync.zone.trackOffset;
                //     if (uSync.resetOwnerTime < minROT + 3f)
                //     {
                //         EditorGUILayout.HelpBox($"The minimum value should be at least be trackIntervals + trackOffset + network delay (suggested: {minROT +10f})",MessageType.Warning);
                //     }
                // }
            }
            uSync.dbText =(Text) EditorGUILayout.ObjectField("Debugging Text", uSync.dbText, typeof(Text), true);

                
            if (EditorGUI.EndChangeCheck())
            {
                //uSync.ApplyProxyModifications();
                EditorUtility.SetDirty(uSync);
                if (PrefabUtility.IsPartOfAnyPrefab(uSync.gameObject))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(uSync);
                }
            }
        }
    }
    
#endif

    
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class BGMZoneUSynced : UdonSharpBehaviour
    {
        public BGMZone zone;
        public int resetOwnerTime;
        [UdonSynced] public float timeOffset;
        [UdonSynced] public int lastUpdate=-1;
        [UdonSynced] public bool handOff;
        public int localLastUpdate=-1;
        public float localTimeOffset;

        public Text dbText;

        private void Update()
        {
            if (dbText)
            {
                dbText.text = $"UdonZone {zone.id}\t Owner {Networking.GetOwner(gameObject).displayName}\n" +
                              $"timeOffset\t {timeOffset}" +
                              $"\nlastUpdate\t {lastUpdate}" +
                              $"\nLocal LU\t {localLastUpdate}" +
                              $"\nTime\t {TimeNowSec()}\n" +
                              $"Need Update\t{NeedUpdate()}\n" +
                              $"{lastUpdate} < {TimeNowSec() -(resetOwnerTime)}";
            }
        }

        public void OwnerUpdateTime()
        {
            localLastUpdate = TimeNowSec();
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                lastUpdate = TimeNowSec();
                RequestSerialization();
            }
        }

        public bool UpdateOffset(float offset)
        {
            timeOffset = offset;
            localTimeOffset = offset;
            OwnerUpdateTime();
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                return true;
            }
            else
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                return false;
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                lastUpdate = localLastUpdate;
                timeOffset = localTimeOffset;
                RequestSerialization();
            }
        }

        public bool NeedUpdate()
        {
            return lastUpdate < TimeNowSec() -(resetOwnerTime);
        }
        public int TimeNowSec()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            return Convert.ToInt32(now.ToUnixTimeMilliseconds()/1000);
        }
    }
}