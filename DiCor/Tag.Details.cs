﻿using System.Diagnostics.CodeAnalysis;

namespace DiCor
{
    public partial struct Tag
    {
        public bool IsKnown([NotNullWhen(true)] out Details? details)
        {
            details = GetDetails();
            return details is not null;
        }

        public partial Details? GetDetails();

        public sealed class Details
        {
            public string? Name { get; }

            public VM VM { get; }

            public bool IsRetired { get; }

            public VR SingleVR { get; }

            public VR[]? MultipleVRs { get; }

            internal Details(string? name, VM vm, VR vr, bool isRetired = false)
            {
                Name = name;
                VM = vm;
                SingleVR = vr;
                IsRetired = isRetired;
            }

            internal Details(string? name, VM vm, VR[] multipleVRs, bool isRetired = false)
            {
                Name = name;
                VM = vm;
                MultipleVRs = multipleVRs;
                IsRetired = isRetired;
            }

            public bool TryGetCompatibleVR<T>(bool isQuery, out VR vr)
            {
                if (MultipleVRs is not null)
                {
                    foreach (VR test in MultipleVRs)
                    {
                        if (test.IsCompatible<T>(isQuery))
                        {
                            vr = test;
                            return true;
                        }
                    }
                }
                else if (SingleVR.IsCompatible<T>(isQuery))
                {
                    vr = SingleVR;
                    return true;
                }

                vr = default;
                return false;
            }
        }

        private readonly struct TemplateTagPart
        {
            public ushort Value { get; }
            public ushort Mask { get; }

            public TemplateTagPart(ushort value, ushort mask)
            {
                Value = value;
                Mask = mask;
            }

            public bool IsGroup
                => (~Mask & Value) != 0;

            public bool IsMatch(ref Tag tag)
            {
                bool isGroup = IsGroup;
                ushort part = isGroup ? tag.Group : tag.Element;
                ushort search = (ushort)(part & Mask);
                if (search == (Value & Mask))
                {
                    tag = isGroup ? new Tag(search, tag.Element) : new Tag(tag.Group, search);
                    return true;
                }

                return false;
            }
        }
    }
}
