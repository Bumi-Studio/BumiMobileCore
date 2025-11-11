using UnityEngine;
namespace BumiMobile
{
    public abstract class AbstractSkinData : ISkinData
    {
        [SerializeField, UniqueID] string id;
        public string ID => id;
        public int Hash { get; private set; }

        public AbstractSkinDatabase SkinsProvider { get; private set; }
        public bool IsUnlocked { get; private set; }

        public virtual void Init(AbstractSkinDatabase provider)
        {
            Hash = id.GetHashCode();

            SkinsProvider = provider;
        }

        public void Unlock()
        {
            IsUnlocked = true;
        }
    }
}
