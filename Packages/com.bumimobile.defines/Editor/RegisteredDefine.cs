namespace BumiMobile
{
    public class RegisteredDefine
    {
        public string Define { get; private set; }
        public string AssemblyType { get; private set; }

        public RegisteredDefine(string define, string assemblyType)
        {
            Define = define;
            AssemblyType = assemblyType;
        }

        public RegisteredDefine(DefineAttribute defineAttribute)
        {
            Define = defineAttribute.Define;
            AssemblyType = defineAttribute.AssemblyType;
        }
    }
}