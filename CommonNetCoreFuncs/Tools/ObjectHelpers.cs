using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace CommonNetCoreFuncs.Tools
{
    public static class ObjectHelpers
    {
        private static List<Type> refTypes = new() 
        {
            typeof(object),
            typeof(string),
            typeof(Enum),
            typeof(List<bool>),
            typeof(List<byte>),
            typeof(List<sbyte>),
            typeof(List<char>),
            typeof(List<decimal>),
            typeof(List<double>),
            typeof(List<float>),
            typeof(List<int>),
            typeof(List<uint>),
            typeof(List<nint>),
            typeof(List<nuint>),
            typeof(List<long>),
            typeof(List<ulong>),
            typeof(List<short>),
            typeof(List<ushort>),
            typeof(List<object>),
            typeof(List<string>)
        };

        public static void CopyPropertiesTo<T, TU>(this T source, TU dest)
        {
            var sourceProps = typeof(T).GetProperties().Where(x => x.CanRead).ToList();
            var destProps = typeof(TU).GetProperties().Where(x => x.CanWrite).ToList();

            foreach (var sourceProp in sourceProps)
            {
                if (destProps.Any(x => x.Name == sourceProp.Name))
                {
                    var p = destProps.FirstOrDefault(x => x.Name == sourceProp.Name);
                    if (p != null)
                    {
                        p.SetValue(dest, sourceProp.GetValue(source, null), null);
                    }
                }
            }
        }

        public static IEnumerable<T> SetValue<T>(this IEnumerable<T> items, Action<T> updateMethod)
        {
            foreach (T item in items)
            {
                updateMethod(item);
            }
            return items;
        }

        //public static void TruncCircularRefs<T>(this T source, List<string> hashes)
        //{

        //    using SHA256 sha256Hash = SHA256.Create();
        //    string hash = source.GetHashString(sha256Hash);

        //    var sourceProps = typeof(T).GetProperties().Where(x => x.CanRead).ToList();
        //    foreach (var sourceProp in sourceProps)
        //    {
        //        Type type = sourceProp.PropertyType;
        //        if (type.IsPublic)
        //        {
        //            if (!(sourceProp == null || type.IsPrimitive || refTypes.Contains(type) || (sourceProp.IsNullable() && (Nullable.GetUnderlyingType(type) ?? typeof(string)).IsPrimitive)))
        //            {
        //                hash = sourceProp.GetValue(source, null).GetHashString(sha256Hash);
        //                if (hashes.Contains(hash))
        //                {
        //                    sourceProp.SetValue(sourceProp, null);
        //                }
        //                else
        //                {
        //                    hashes.Add(hash);
        //                    sourceProp.TruncCircularRefs(hashes);
        //                }
        //            }
        //        }
        //    }
        //}

        //public static bool IsNullable<T>(this T obj)
        //{
        //    if (obj == null) return true; // obvious
        //    Type type = typeof(T);
        //    if (!type.IsValueType) return true; // ref-type
        //    if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
        //    return false; // value-type
        //}

        //public static string GetHashString<T>(this T source, SHA256 sha256Hash)
        //{
        //    try
        //    {
        //        if (source == null)
        //        {
        //            return null;
        //        }
        //        //DataContractAttribute dataContractAttribute = new()
        //        //{
        //        //    IsReference = true
        //        //};

        //        Type[] knownTypes = { typeof(T) };
        //        DataContractSerializerSettings dataContractSerializerSettings = new()
        //        {
        //            PreserveObjectReferences = true,
        //            KnownTypes = knownTypes
        //        };

        //        DataContractSerializer serializer = new(typeof(T), dataContractSerializerSettings );
        //        using MemoryStream ms = new();
        //        serializer.WriteObject(ms, source);
        //        return Convert.ToBase64String(sha256Hash.ComputeHash(ms.ToArray()));
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //    return null;
        //}

        //public static string GetHashStringFromBytes(this byte[] bytes, StringBuilder builder = null)
        //{
        //    if (builder == null)
        //    {
        //        builder = new();
        //    }
        //    for (int i = 0; i < bytes.Length; i++)
        //    {
        //        builder.Append(bytes[i].ToString("x2"));
        //    }
        //    return builder.ToString();
        //}
    }
}