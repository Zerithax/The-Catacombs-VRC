using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Catacombs.Base
{
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerMouthTracker : UdonSharpBehaviour
    {
        [SerializeField] private Renderer rend;
        [SerializeField] private SphereCollider mouthTrigger;
        [SerializeField] private bool editorOverride;

        public LocalPlayerTracker playerTracker;

        private void Update()
        {
            transform.SetPositionAndRotation(Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head), Networking.LocalPlayer.GetBoneRotation(HumanBodyBones.Head));
            //transform.SetPositionAndRotation(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation);

            if (Vector3.Angle(Vector3.up, transform.forward) < 45 || editorOverride)
            {
                mouthTrigger.enabled = true;
                rend.enabled = true;
            }
            else
            {
                mouthTrigger.enabled = false;
                rend.enabled = false;
            }
        }
    }
}