namespace BumiMobile
{
    [System.Serializable]
    public struct SoundKey
    {
        public string Group;
        public string Id;

        public SoundKey(string group, string id)
        {
            Group = group;
            Id = id;
        }

        public override string ToString() => $"{Group}/{Id}";
    }
}