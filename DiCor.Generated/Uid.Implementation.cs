﻿using System;

namespace DiCor
{
    partial struct Uid
    {
        public static readonly Uid ImplementationClass = new($"{DiCorOrgRoot}1.{Implementation.Version}", "Implementation Class UID", UidType.Other);
    }

    public class Implementation
    {
        public static readonly string Version = GetAssemblyVersion();
        public static readonly string VersionName = "DiCor " + Version;

        public static Uid ClassUid
        {
            get => Uid.ImplementationClass;
        }

        private static string GetAssemblyVersion()
            => (typeof(Implementation).Assembly.GetName().Version ?? new Version()).ToString();
    }
}
