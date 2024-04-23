// Decompiled with JetBrains decompiler
// Type: FileIDUtil
// Assembly: DllSwitcher, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B4178718-2A83-4686-A7E9-A256F6BFAB35
// Assembly location: F:\Projects\NMY\DllSwitcher.dll

using System;
using System.Security.Cryptography;
using System.Text;

public static class FileIDUtil
{
    public static int Compute(Type t)
    {
        string s = "s\0\0\0" + t.Namespace + t.Name;
        using (HashAlgorithm hashAlgorithm = (HashAlgorithm)new MD4())
        {
            byte[] hash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(s));
            int num = 0;
            for (int index = 3; index >= 0; --index)
                num = num << 8 | (int)hash[index];
            return num;
        }
    }
}
