using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Catacombs.Base
{
    public class BerryDust : ContainableItem
    {
        public BerryTypes berryDustType;
        public Color berryColor;

        //All of script's content moved to base class (ContainableItem)
    }
}