using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DiCor
{
    public class Implementation
    {
        private static readonly string s_version = GetAssemblyVersion() ;

        public static readonly Uid ClassUid = new Uid($"{Uid.ThisOrgRoot}.1.{s_version}", "Implementation Class UID", UidType.Other);
        public static readonly string VersionName = "DiCor " + s_version.ToString();

        private static string GetAssemblyVersion()
            => (Assembly.GetExecutingAssembly().GetName().Version ?? new Version()).ToString();
    }
}
