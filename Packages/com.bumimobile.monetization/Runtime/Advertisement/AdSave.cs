namespace BumiMobile
{
#if MODULE_SAVE
    public class AdSave : ISaveObject
#else
    public class AdSave
#endif
    {
        public bool IsForcedAdEnabled = true;

#if MODULE_SAVE
        public void Flush()
        {

        }
#endif
    }
}