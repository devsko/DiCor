using System;

namespace DiCor
{
    partial record struct Uid
    {
        public static readonly Uid ImplementationClass = new($"{Uid.DiCorOrgRoot}1.{Implementation.Version}");
    }

    public class Implementation
    {
        public static readonly string Version = GetAssemblyVersion();
        public static readonly string VersionName = "DiCor " + Version;

        public static Uid ClassUid
            => Uid.ImplementationClass;

        private static string GetAssemblyVersion()
            => (typeof(Implementation).Assembly.GetName().Version ?? new Version()).ToString();
    }
}
