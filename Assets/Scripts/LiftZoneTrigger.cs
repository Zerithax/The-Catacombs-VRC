using Catacombs.Base;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LiftZoneTrigger : UdonSharpBehaviour
{
    [SerializeField] private ElevatorLift liftSystem;

    public void LiftEntered(LocalPlayerTracker playerTracker)
    {
        liftSystem.LiftEntered(playerTracker);
    }
}
