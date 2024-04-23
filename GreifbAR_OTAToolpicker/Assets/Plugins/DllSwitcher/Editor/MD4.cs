// Decompiled with JetBrains decompiler
// Type: MD4
// Assembly: DllSwitcher, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B4178718-2A83-4686-A7E9-A256F6BFAB35
// Assembly location: F:\Projects\NMY\DllSwitcher.dll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

public class MD4 : HashAlgorithm
{
    private uint _a;
    private uint _b;
    private uint _c;
    private uint _d;
    private uint[] _x;
    private int _bytesProcessed;

    public MD4()
    {
        this._x = new uint[16];
        this.Initialize();
    }

    public override void Initialize()
    {
        this._a = 1732584193U;
        this._b = 4023233417U;
        this._c = 2562383102U;
        this._d = 271733878U;
        this._bytesProcessed = 0;
    }

    protected override void HashCore(byte[] array, int offset, int length)
    {
        this.ProcessMessage(MD4.Bytes(array, offset, length));
    }

    protected override byte[] HashFinal()
    {
        try
        {
            this.ProcessMessage(this.Padding());
            return ((IEnumerable<uint>)new uint[4]
            {
        this._a,
        this._b,
        this._c,
        this._d
            }).SelectMany<uint, byte>((Func<uint, IEnumerable<byte>>)(word => this.Bytes(word))).ToArray<byte>();
        }
        finally
        {
            this.Initialize();
        }
    }

    private void ProcessMessage(IEnumerable<byte> bytes)
    {
        foreach (byte num1 in bytes)
        {
            int num2 = this._bytesProcessed & 63;
            int index = num2 >> 2;
            int num3 = (num2 & 3) << 3;
            this._x[index] = (uint)((int)this._x[index] & ~((int)byte.MaxValue << num3) | (int)num1 << num3);
            if (num2 == 63)
                this.Process16WordBlock();
            ++this._bytesProcessed;
        }
    }

    private static IEnumerable<byte> Bytes(byte[] bytes, int offset, int length)
    {
        for (int i = offset; i < length; ++i)
            yield return bytes[i];
    }

    private IEnumerable<byte> Bytes(uint word)
    {
        yield return (byte)(word & (uint)byte.MaxValue);
        yield return (byte)(word >> 8 & (uint)byte.MaxValue);
        yield return (byte)(word >> 16 & (uint)byte.MaxValue);
        yield return (byte)(word >> 24 & (uint)byte.MaxValue);
    }

    private IEnumerable<byte> Repeat(byte value, int count)
    {
        for (int i = 0; i < count; ++i)
            yield return value;
    }

    private IEnumerable<byte> Padding()
    {
        return this.Repeat((byte)128, 1).Concat<byte>(this.Repeat((byte)0, (this._bytesProcessed + 8 & 2147483584) + 55 - this._bytesProcessed)).Concat<byte>(this.Bytes((uint)(this._bytesProcessed << 3))).Concat<byte>(this.Repeat((byte)0, 4));
    }

    private void Process16WordBlock()
    {
        uint num1 = this._a;
        uint num2 = this._b;
        uint num3 = this._c;
        uint num4 = this._d;
        int[] numArray1 = new int[4] { 0, 4, 8, 12 };
        foreach (int index in numArray1)
        {
            num1 = MD4.Round1Operation(num1, num2, num3, num4, this._x[index], 3);
            num4 = MD4.Round1Operation(num4, num1, num2, num3, this._x[index + 1], 7);
            num3 = MD4.Round1Operation(num3, num4, num1, num2, this._x[index + 2], 11);
            num2 = MD4.Round1Operation(num2, num3, num4, num1, this._x[index + 3], 19);
        }
        int[] numArray2 = new int[4] { 0, 1, 2, 3 };
        foreach (int index in numArray2)
        {
            num1 = MD4.Round2Operation(num1, num2, num3, num4, this._x[index], 3);
            num4 = MD4.Round2Operation(num4, num1, num2, num3, this._x[index + 4], 5);
            num3 = MD4.Round2Operation(num3, num4, num1, num2, this._x[index + 8], 9);
            num2 = MD4.Round2Operation(num2, num3, num4, num1, this._x[index + 12], 13);
        }
        int[] numArray3 = new int[4] { 0, 2, 1, 3 };
        foreach (int index in numArray3)
        {
            num1 = MD4.Round3Operation(num1, num2, num3, num4, this._x[index], 3);
            num4 = MD4.Round3Operation(num4, num1, num2, num3, this._x[index + 8], 9);
            num3 = MD4.Round3Operation(num3, num4, num1, num2, this._x[index + 4], 11);
            num2 = MD4.Round3Operation(num2, num3, num4, num1, this._x[index + 12], 15);
        }
        this._a += num1;
        this._b += num2;
        this._c += num3;
        this._d += num4;
    }

    private static uint ROL(uint value, int numberOfBits)
    {
        return value << numberOfBits | value >> 32 - numberOfBits;
    }

    private static uint Round1Operation(uint a, uint b, uint c, uint d, uint xk, int s)
    {
        return MD4.ROL(a + (uint)((int)b & (int)c | ~(int)b & (int)d) + xk, s);
    }

    private static uint Round2Operation(uint a, uint b, uint c, uint d, uint xk, int s)
    {
        return MD4.ROL((uint)((int)a + ((int)b & (int)c | (int)b & (int)d | (int)c & (int)d) + (int)xk + 1518500249), s);
    }

    private static uint Round3Operation(uint a, uint b, uint c, uint d, uint xk, int s)
    {
        return MD4.ROL((uint)((int)a + ((int)b ^ (int)c ^ (int)d) + (int)xk + 1859775393), s);
    }
}
