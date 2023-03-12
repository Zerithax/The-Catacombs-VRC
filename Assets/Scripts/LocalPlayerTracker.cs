
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LocalPlayerTracker : UdonSharpBehaviour
{
    //This class will manage tracking the player's interactions that will eventually be saved with NUSS
    //This class will manage tracking and managing collisions for the player (since OnPlayerTriggerEnter doesn't work in client sim)

    [SerializeField] public VRCPlayerApi localPlayer;
    [SerializeField] private float heightOffset = 1;

    private void Start() { localPlayer = Networking.LocalPlayer; }

    private void Update()
    {
        Vector3 playerPos = localPlayer.GetPosition();
        transform.position = new Vector3(playerPos.x, playerPos.y + heightOffset, playerPos.z);

        transform.rotation = localPlayer.GetRotation();
    }
}
