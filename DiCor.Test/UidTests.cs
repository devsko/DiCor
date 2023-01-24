using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace DiCor.Test
{
    public class UidTests
    {
        [Fact]
        public void Constructor()
        {
            Uid defaultUid = default;
            Uid newUid = new();
            Assert.Throws<ArgumentException>("ascii", () => new Uid(default));
            Assert.Throws<ArgumentException>("ascii", () => new Uid(new AsciiString(""u8, false)));
            Uid emptyUid = new(new AsciiString(""u8, false), false);
            Assert.Throws<ArgumentException>("ascii", () => new Uid(new AsciiString("x"u8, false))) ;
            Uid invalidUid = new(new AsciiString("x"u8, false), false);
            Uid unknownUid = Uid.NewUid();
            Uid retiredUid = new(new AsciiString("1.2.840.10008.1.2.2"u8, false));
            Uid validUid = new(new AsciiString("1.2.840.10008.1.2"u8, false));

            Assert.Equal(defaultUid, newUid);
            Assert.Equal(defaultUid, emptyUid);
            Assert.NotEqual(defaultUid, invalidUid);
            Assert.NotEqual(defaultUid, unknownUid);
            Assert.NotEqual(defaultUid, retiredUid);
            Assert.NotEqual(defaultUid, validUid);

            Assert.False(defaultUid.IsValid);
            Assert.False(newUid.IsValid);
            Assert.False(emptyUid.IsValid);
            Assert.False(invalidUid.IsValid);
            Assert.True(unknownUid.IsValid);
            Assert.True(retiredUid.IsValid);
            Assert.True(validUid.IsValid);

            Assert.False(defaultUid.IsKnown(out _));
            Assert.False(newUid.IsKnown(out _));
            Assert.False(emptyUid.IsKnown(out _));
            Assert.False(invalidUid.IsKnown(out _));
            Assert.False(unknownUid.IsKnown(out _));
            Assert.True(retiredUid.IsKnown(out _));
            Assert.True(validUid.IsKnown(out _));

            Assert.True(retiredUid.GetDetails()!.IsRetired);
            Assert.False(validUid.GetDetails()!.IsRetired);
        }

        [Fact]
        public void ValueIsUnique()
        {
            Uid uid = Uid.ImplicitVRLittleEndian;
            Uid.Details? uidDetails = uid.GetDetails();
            byte[] bytes = uid.Value.Bytes.ToArray();

            byte[] copy = new byte[bytes.Length];
            bytes.CopyTo(copy, 0);
            Uid copyUid = new(new AsciiString(copy));
            Uid.Details? copyDetails = copyUid.GetDetails();

            Assert.NotSame(bytes, copy);
            Assert.NotStrictEqual(bytes, copy);
            Assert.Equal(bytes, copy);

            Assert.Equal(uid, copyUid);
            Assert.StrictEqual(uid, copyUid);
            Assert.True(copyUid.Equals(uid));
            Assert.True(((object)copyUid).Equals(uid));
            Assert.True(copyUid == uid);
            Assert.False(copyUid != uid);

            Assert.NotEqual(bytes.GetHashCode(), copy.GetHashCode());
            Assert.Equal(uid.GetHashCode(), copyUid.GetHashCode());

            Assert.NotNull(uidDetails);
            Assert.NotNull(copyDetails);
            Assert.Same(uidDetails.Name, copyDetails.Name);
        }

        [Fact]
        public void Validation()
        {
            Assert.False(new Uid(new AsciiString(""u8, false), false).IsValid);
            Assert.False(new Uid(new AsciiString("x"u8, false), false).IsValid);
            Assert.False(new Uid(new AsciiString("."u8, false), false).IsValid);
            Assert.False(new Uid(new AsciiString("1..1"u8, false), false).IsValid);
            Assert.False(new Uid(new AsciiString("01"u8, false), false).IsValid);
            Assert.False(new Uid(new AsciiString("01.1.1"u8, false), false).IsValid);
            Assert.False(new Uid(new AsciiString("1.01.1"u8, false), false).IsValid);
            Assert.False(new Uid(new AsciiString("1.1.01"u8, false), false).IsValid);
            Assert.False(new Uid(new AsciiString("1.1."u8, false), false).IsValid);
            Assert.False(new Uid(new AsciiString(".1.1"u8, false), false).IsValid);
            Assert.False(new Uid(new AsciiString(Enumerable.Repeat((byte)1, 65).ToArray()), false).IsValid);

            Assert.True(new Uid(new AsciiString("0"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString("1"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString("2"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString("3"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString("4"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString("5"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString("6"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString("7"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString("8"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString("9"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString("1.10.1"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString("0.0.0"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString("1.10.1"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString("10.10.10.10.10.10.10.10.10.10.10.10.10.10.10.10.10.10.10.10.10"u8, false), false).IsValid);
            Assert.True(new Uid(new AsciiString(Enumerable.Repeat((byte)'1', 64).ToArray(), false), false).IsValid);
        }

        [Fact]
        public void UidDictionary()
        {
            var field = typeof(Uid).GetField("s_dictionary", BindingFlags.Static | BindingFlags.NonPublic)!;
            var dictionary = (FrozenDictionary<Uid, Uid.Details>)field.GetValue(null)!;
            var uids = dictionary.Keys.ToArray();

            var stringDictionary = uids
                .Zip(uids.Select(uid => dictionary[uid]))
                .Select(tuple => new KeyValuePair<string, Uid.Details>(tuple.First.ToString()!, tuple.Second))
                .ToFrozenDictionary(StringComparer.Ordinal);
            var stringUids = uids.Select(uid => uid.ToString()!).ToArray();

            foreach (var uid in uids)
            {
                var details = dictionary[uid];
            }
        }

        [Fact]
        public void HashCode()
        {
            Assert.Equal(0, new Uid().GetHashCode());
            Assert.Equal(757602046, new Uid(new AsciiString(""u8, false), false).GetHashCode());
            Assert.Equal(-1185404109, new Uid(new AsciiString("1"u8, false)).GetHashCode());
            Assert.Equal(780094491, new Uid(new AsciiString("12"u8, false)).GetHashCode());
            Assert.Equal(766700157, new Uid(new AsciiString("123"u8, false)).GetHashCode());
            Assert.Equal(1534540716, new Uid(new AsciiString("1234"u8, false)).GetHashCode());
            Assert.Equal(-986852606, new Uid(new AsciiString("12345"u8, false)).GetHashCode());
            Assert.Equal(-807301808, new Uid(new AsciiString("123456"u8, false)).GetHashCode());
            Assert.Equal(-1953030292, new Uid(new AsciiString("1234567"u8, false)).GetHashCode());
            Assert.Equal(389592624, new Uid(new AsciiString("12345678"u8, false)).GetHashCode());
            Assert.Equal(-1232935784, new Uid(new AsciiString("123456789"u8, false)).GetHashCode());
            Assert.Equal(204578379, new Uid(new AsciiString("1234567890"u8, false)).GetHashCode());
            Assert.Equal(-1309340940, new Uid(new AsciiString("12345678901"u8, false)).GetHashCode());
            Assert.Equal(-2128575101, new Uid(new AsciiString("123456789012"u8, false)).GetHashCode());
            Assert.Equal(-973791476, new Uid(new AsciiString("1234567890123"u8, false)).GetHashCode());
            Assert.Equal(2014814894, new Uid(new AsciiString("12345678901234"u8, false)).GetHashCode());
            Assert.Equal(1854577296, new Uid(new AsciiString("123456789012345"u8, false)).GetHashCode());
            Assert.Equal(-840584849, new Uid(new AsciiString("1234567890123456"u8, false)).GetHashCode());
            Assert.Equal(-844198371, new Uid(new AsciiString("12345678901234567"u8, false)).GetHashCode());
            Assert.Equal(-1911176152, new Uid(new AsciiString("123456789012345678"u8, false)).GetHashCode());
            Assert.Equal(-2029040861, new Uid(new AsciiString("1234567890123456789"u8, false)).GetHashCode());
            Assert.Equal(-57525021, new Uid(new AsciiString("12345678901234567890"u8, false)).GetHashCode());
            Assert.Equal(-2042848156, new Uid(new AsciiString(Enumerable.Repeat((byte)'1', 256 * 256).ToArray(), false), false).GetHashCode());
        }

        [Fact]
        public void ToStringWorks()
        {
            foreach (string s in new string[] {
                "",
                "12345",
                new string('1', 64),
                new string('1', 256 * 256),
            })
            {
                byte[] value = Encoding.ASCII.GetBytes(s);
                string toString = new Uid(new AsciiString(value), false).ToString();
                Assert.Equal('[' + s + ']', toString);
            }
        }

        [Fact]
        public void NewUid()
        {

            Uid uid = Uid.NewUid();
            _ = uid.ToString();

        }
    }
}
