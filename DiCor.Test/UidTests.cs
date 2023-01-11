using System;
using Xunit;

namespace DiCor.Test
{
    public class UidTests
    {
        [Fact]
        public void UidConstructor()
        {
            Uid defaultUid = default;
            Uid newUid = new();
            Assert.Throws<ArgumentException>("value", () => new Uid(null));
            Uid nullUid = new(null, false);
            Assert.Throws<ArgumentException>("value", () => new Uid(""u8));
            Uid emptyUid = new(""u8, false);
            Assert.Throws<ArgumentException>("value", () => new Uid("x"u8));
            Uid invalidUid = new("x"u8, false);
            Uid unknownUid = Uid.NewUid();
            Uid retiredUid = new("1.2.840.10008.1.2.2"u8);
            Uid validUid = new("1.2.840.10008.1.2"u8);

            Assert.Equal(defaultUid, newUid);
            Assert.Equal(defaultUid, nullUid);
            Assert.Equal(defaultUid, emptyUid);
            Assert.NotEqual(defaultUid, invalidUid);
            Assert.NotEqual(defaultUid, unknownUid);
            Assert.NotEqual(defaultUid, retiredUid);
            Assert.NotEqual(defaultUid, validUid);

            Assert.False(defaultUid.IsValid);
            Assert.False(newUid.IsValid);
            Assert.False(nullUid.IsValid);
            Assert.False(emptyUid.IsValid);
            Assert.False(invalidUid.IsValid);
            Assert.True(unknownUid.IsValid);
            Assert.True(retiredUid.IsValid);
            Assert.True(validUid.IsValid);

            Assert.False(defaultUid.IsKnown);
            Assert.False(newUid.IsKnown);
            Assert.False(nullUid.IsKnown);
            Assert.False(emptyUid.IsKnown);
            Assert.False(invalidUid.IsKnown);
            Assert.False(unknownUid.IsKnown);
            Assert.True(retiredUid.IsKnown);
            Assert.True(validUid.IsKnown);

            Assert.True(retiredUid.GetDetails()!.Value.IsRetired);
            Assert.False(validUid.GetDetails()!.Value.IsRetired);
        }

        [Fact]
        public void UidValueIsUnique()
        {
            Uid uid = Uid.ImplicitVRLittleEndian;
            Uid.Details? uidDetails = uid.GetDetails();
            byte[] bytes = uid.Value;

            byte[] copy = new byte[bytes.Length];
            bytes.CopyTo(copy, 0);
            Uid copyUid = new(copy);
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
            Assert.Same(uidDetails.Value.Name, copyDetails.Value.Name);
        }

        [Fact]
        public void GenerateNewUid()
        {

            Uid uid = Uid.NewUid();
            _ = uid.ToString();

        }
    }
}
