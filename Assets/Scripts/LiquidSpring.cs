using UdonSharp;
using UnityEngine;

namespace Catacombs.Base
{
    public class LiquidSpring : UdonSharpBehaviour
    {
        [SerializeField] private GameObject liquidToSpawn;
        [SerializeField] private int spawnCountdown;

        private float timer;

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer > spawnCountdown)
            {
                GameObject newDroplet = Instantiate(liquidToSpawn, transform.position, Quaternion.identity, transform);
                newDroplet.transform.position = transform.position;
                timer = 0;
            }
        }
    }
}