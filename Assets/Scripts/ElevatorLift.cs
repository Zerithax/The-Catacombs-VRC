using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using Catacombs.Base;

public class ElevatorLift : UdonSharpBehaviour
{
    [Header("Lift Settings")]
    [SerializeField] private GameObject liftObject;
    [SerializeField] private Collider liftTrigger;
    [SerializeField] private GameObject safetyNet;
    [SerializeField] private float liftMinHeight;
    [SerializeField] private float liftMaxHeight;
    [SerializeField] private float liftCycleTime = 10f;
    [SerializeField] private float playerResponseDelay = 3f;
    [UdonSynced] private bool elevatorLerping;
    [UdonSynced] private bool reachedSafetyNet;

    [SerializeField] private bool editorOverride;

    [Header("Station")]
    [SerializeField] private bool useStation;
    [SerializeField] private VRCStation liftStation;

    private float liftStartHeight;
    private float elevatorCallTime;
    private float elevatorPlayerCollisionTime;

    [Header("Steam Engine Animations")]
    [SerializeField] private GameObject spoolRope;
    [SerializeField] private float ropeStartingX;
    [SerializeField] private float ropeMaximumX;
    [SerializeField] private int rotateAmount;
    [SerializeField] private GameObject spool;
    [SerializeField] private GameObject pulley;

    private void Start() { if (liftStation == null) useStation = false; liftStartHeight = liftMinHeight; }

    public override void OnPlayerJoined(VRCPlayerApi player) { InitElevator(); }

    public void InitElevator()
    {
        if (liftObject.transform.localPosition.y == liftMaxHeight) return;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        liftStartHeight = liftObject.transform.localPosition.y;
        elevatorLerping = true;
        reachedSafetyNet = false;
        safetyNet.SetActive(true);
        liftTrigger.enabled = false;

        elevatorCallTime = Time.time;
    }

    private void Update()
    {
        if (elevatorLerping)
        {
            //Smoothly animate scalar from 0 - 1
            float animationScalar = Mathf.SmoothStep(0, 1, (Time.time - (!reachedSafetyNet ? elevatorCallTime : elevatorPlayerCollisionTime)) / liftCycleTime);

            //Elevator is on its way UP to the net
            if (!reachedSafetyNet)
            {
                liftObject.transform.localPosition = new Vector3(0, (Mathf.Abs(liftStartHeight) + Mathf.Abs(liftMaxHeight)) * animationScalar + liftStartHeight, 0);

                spoolRope.transform.localPosition = new Vector3((Mathf.Abs(ropeStartingX) + Mathf.Abs(ropeMaximumX)) * (1 - animationScalar) + ropeMaximumX, spoolRope.transform.localPosition.y, spoolRope.transform.localPosition.z);

                spool.transform.localRotation = Quaternion.Euler(new Vector3(360 * -rotateAmount * animationScalar, 0, 90));
                pulley.transform.localRotation = Quaternion.Euler(new Vector3(360 * -rotateAmount * animationScalar, 0, 90));

                //When lift reaches maxHeight, prep to lerp down
                if (liftObject.transform.localPosition.y == liftMaxHeight)
                {
                    //Wait for trigger enter before lowering
                    elevatorLerping = false;

                    //Next time elevatorLerping == true, lerp down instead
                    reachedSafetyNet = true;
                    safetyNet.SetActive(false);

                    liftTrigger.enabled = true;
                }
            }
            //Elevator is on its way DOWN to the cave
            else
            {
                //Account for the delay
                if (Time.time > elevatorPlayerCollisionTime)
                {
                    liftObject.transform.localPosition = new Vector3(0, (Mathf.Abs(liftStartHeight) + Mathf.Abs(liftMaxHeight)) * (1 - animationScalar) + liftStartHeight, 0);

                    spoolRope.transform.localPosition = new Vector3((Mathf.Abs(ropeStartingX) + Mathf.Abs(ropeMaximumX)) * animationScalar + ropeMaximumX, spoolRope.transform.localPosition.y, spoolRope.transform.localPosition.z);

                    spool.transform.localRotation = Quaternion.Euler(new Vector3(360 * rotateAmount * animationScalar, 0, 90));
                    pulley.transform.localRotation = Quaternion.Euler(new Vector3(360 * rotateAmount * animationScalar, 0, 90));

                    //When lift reaches minHeight, stop lerping
                    if (liftObject.transform.localPosition.y == liftMinHeight)
                    {
                        elevatorLerping = false;
                        liftTrigger.enabled = true;
                    }
                }
            }
        }
    }

    public void LiftEntered(LocalPlayerTracker playerTracker)
    {
        Debug.Log($"[{name}] Entered!");

        //If the elevator isn't actively running, activate it and save the collision time
        if (!elevatorLerping)
        {
            elevatorLerping = true;
            elevatorPlayerCollisionTime = Time.time + playerResponseDelay;
        }

        if (useStation)
        {
            Vector3 enterPos = playerTracker.Owner.GetPosition();
            Quaternion enterRot = playerTracker.Owner.GetRotation();

            liftStation.UseStation(playerTracker.Owner);
            playerTracker.Owner.TeleportTo(enterPos, enterRot);
        }

        if (editorOverride) playerTracker.Owner.TeleportTo(new Vector3(transform.position.x, liftMinHeight, transform.position.z) + transform.forward, transform.rotation);
    }

    private void OnTriggerExit(Collider other)
    {
        LocalPlayerTracker playerTracker = other.GetComponent<LocalPlayerTracker>();
        if (playerTracker != null)
        {
            if (useStation)
            {
                Debug.Log($"[{name}] Exited!");

                Vector3 enterPos = playerTracker.Owner.GetPosition();
                Quaternion enterRot = playerTracker.Owner.GetRotation();

                liftStation.ExitStation(playerTracker.Owner);
                playerTracker.Owner.TeleportTo(enterPos, enterRot);
            }
        }
    }
}
