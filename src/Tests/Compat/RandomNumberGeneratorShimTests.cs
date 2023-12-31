using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace RandN.Compat;

public sealed class RandomNumberGeneratorShimTests
{
    /// <summary>
    /// Verifies <see cref="RandomNumberGeneratorShim{TRng}.GetNonZeroBytes(Byte[])"/> method.
    /// </summary>
    [Fact]
    public void NonZeroBytes()
    {
        var rng = new SequenceRng(new UInt32[] { 0, 1, 2, 0, 1, 2, 0, 1 });
        var shim = RandomNumberGeneratorShim.Create(rng);
        var expected = new Byte[]
        {
            1, 2, 1, 2, 1,
            1, 2, 1, 2, 1,
            1, 2, 1, 2, 1,
            1, 2, 1, 2, 1,
            1, 2, 1, 2, 1,
            1, 2, 1, 2, 1,
            1, 2, 1, 2, 1,
        };

        var buffer = new Byte[35];
        shim.GetNonZeroBytes(buffer);
        Assert.DoesNotContain((Byte)0, buffer);
        Assert.Equal(expected, buffer);

#if !NET48
        Array.Fill(buffer, (Byte)0);
        shim.GetNonZeroBytes(buffer.AsSpan());
        Assert.DoesNotContain((Byte)0, buffer);
        Assert.Equal(expected, buffer);
#endif
    }

    /// <summary>
    /// Verifies <see cref="RandomNumberGeneratorShim{TRng}.GetBytes(Byte[])"/> methods.
    /// </summary>
    [Fact]
    public void Bytes()
    {
        var rng = new StepRng(0);
        var shim = RandomNumberGeneratorShim.Create(rng);

        var buffer = new Byte[1024];
        shim.GetBytes(buffer);
        var ints = MemoryMarshal.Cast<Byte, UInt64>(buffer);
        for (Int32 i = 0; i < ints.Length; i++)
            Assert.Equal((UInt64)i, ints[i]);

#if !NET48
        shim.GetBytes(buffer.AsSpan());
        for (Int32 i = 0; i < ints.Length; i++)
            Assert.Equal((UInt64)(i + ints.Length), ints[i]);
#endif

        var maxRng = new StepRng(UInt64.MaxValue) { Increment = 0 };
        var halfRng = new StepRng(0x8080808080808080) { Increment = 0 };
        var maxShim = RandomNumberGeneratorShim.Create(maxRng);
        var halfShim = RandomNumberGeneratorShim.Create(halfRng);

        buffer = new Byte[1024];
        halfShim.GetBytes(buffer, 0, 200);
        maxShim.GetBytes(buffer, 200, 500);
        halfShim.GetBytes(buffer, 700, 0);
        halfShim.GetBytes(buffer, 1023, 1);
        Assert.All(buffer.Take(200), b => Assert.Equal(0x80, b));
        Assert.All(buffer.Skip(200).Take(500), b => Assert.Equal(Byte.MaxValue, b));
        Assert.All(buffer.Skip(700).Take(323), b => Assert.Equal(0, b));
        Assert.All(buffer.Skip(1023), b => Assert.Equal(0x80, b));
    }

    [Fact]
    public void BadArgs()
    {
        var rng = new StepRng(0);
        var shim = RandomNumberGeneratorShim.Create(rng);
        var bytes = new Byte[32];
        Assert.Throws<ArgumentNullException>(() => shim.GetBytes(null!, 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>("offset", () => shim.GetBytes(bytes, -10, 10));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => shim.GetBytes(bytes, 10, -10));
        Assert.Throws<ArgumentException>(() => shim.GetBytes(bytes, 32, 1));
    }

    [Fact]
    public void Disposal()
    {
        var rng = new DisposableRng(new StepRng(10));
        var shim = RandomNumberGeneratorShim.Create(rng);
        shim.Dispose();
        Assert.True(rng.Disposed);
    }

    [Fact]
    public void NonNullable()
    {
        Assert.Throws<ArgumentNullException>(() => RandomNumberGeneratorShim.Create<StepRng>(null!));

        var rng = RandomNumberGeneratorShim.Create(new StepRng(0));
        Assert.Throws<ArgumentNullException>(() => rng.GetBytes(null!));
        Assert.Throws<ArgumentNullException>(() => rng.GetNonZeroBytes(null!));
    }
}
