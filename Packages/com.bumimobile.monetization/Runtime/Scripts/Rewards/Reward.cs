using UnityEngine;

namespace BumiMobile
{
    public abstract class Reward : MonoBehaviour
    {
        public virtual void Init() { }

        public abstract void ApplyReward();

        /// <summary>
        /// Return true if you want to disable offer object.
        /// For example: if you already purchased NoAds as a part of the Pack, you want other NoAds offers to be disabled. 
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckDisableState() { return false; }
    }
}
