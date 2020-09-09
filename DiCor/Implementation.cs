using System;

namespace DiCor
{
    public class Implementation
    {
        public static readonly string Version = GetAssemblyVersion();

        public static readonly Uid ClassUid = Uid.ImplementationClass;
        public static readonly string VersionName = "DiCor " + Version;

        private static string GetAssemblyVersion()
            => (typeof(Implementation).Assembly.GetName().Version ?? new Version()).ToString();
    }

    partial struct Uid
    {
        public static readonly Uid ImplementationClass = new($"{ThisOrgRoot}.1.{Implementation.Version}", "Implementation Class UID", UidType.Other);
    }
}
