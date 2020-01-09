using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem.CustomUnityCacheClient
{
    internal static class Util
    {
        /// <summary>
        /// Reverses the hex digits for each byte in a GUID before converting to a string, the same way Unity serializes GUIDs to strings.
        /// </summary>
        /// <returns>Reversed bytes</returns>
        /// <param name="b">Integer whose bytes to be revesed</param>
        private static int ReverseByte(int b)
        {
            return ((b & 0x0F) << 4) | ((b >> 4) & 0x0F);
        }

        /// <summary>
        /// Convert string to HEX byte array.
        /// </summary>
        /// <returns>The to byte array.</returns>
        /// <param name="input">Input String that needs to be reversed</param>
        /// <param name="asGuid">Indicates if the input string is a guid. Based on Cache Server TCP CLient
        /// protocol, only guid bytes need to be reversed. If the param is false, bytes are not reversed.</param>
        private static byte[] StringToByteArray(string input, bool asGuid)
        {
            var bytes = new byte[input.Length / 2];
            for (var i = 0; i < input.Length; i += 2)
            {
                var b = Convert.ToByte(input.Substring(i, 2), 16);
                bytes[i / 2] = asGuid ? (byte) ReverseByte(b) : b;
            }

            return bytes;
        }

        /// <summary>
        /// Convert a hex string to a byte array that represents an Asset Hash
        /// </summary>
        /// <param name="hashStr">32 character hex string</param>
        /// <returns>byte array</returns>
        public static byte[] StringToHash(string hashStr)
        {
            Debug.Assert(hashStr.Length == 32);
            return StringToByteArray(hashStr, false);
        }

        /// <summary>
        /// Convert a hex string to a byte array that represents an Asset GUID
        /// </summary>
        /// <param name="guidStr">32 character hex string</param>
        /// <returns>byte array</returns>
        public static byte[] StringToGuid(string guidStr)
        {
            Debug.Assert(guidStr.Length == 32);
            return StringToByteArray(guidStr, true);
        }

        /// <summary>
        /// Parse an ascii byte array at <paramref name="index"/>start as an int value
        /// </summary>
        /// <param name="bytes">byte array</param>
        /// <param name="index">offset</param>
        /// <returns></returns>
        public static int ReadUInt32(byte[] bytes, int index)
        {
            Debug.Assert(bytes.Length + index >= 8);
            return Int32.Parse(Encoding.ASCII.GetString(bytes, index, 8), NumberStyles.HexNumber);
        }

        /// <summary>
        /// Encode an integer as an ascii byte array
        /// </summary>
        /// <param name="input">integer</param>
        /// <param name="minLength">true ensure the byte array is as short as possible; false to pad to 8 bytes</param>
        /// <returns></returns>
        public static byte[] EncodeInt32(int input, bool minLength = false)
        {
            return Encoding.ASCII.GetBytes(input.ToString(minLength ? "X" : "X8"));
        }

        /// <summary>
        /// Parse a subset of an ascii byte array as a long value
        /// </summary>
        /// <param name="bytes">byte array</param>
        /// <param name="index">offset within <paramref name="bytes"/> to read from</param>
        /// <returns></returns>
        public static long ReadUInt64(byte[] bytes, int index)
        {
            Debug.Assert(bytes.Length + index >= 16);
            return Int64.Parse(Encoding.ASCII.GetString(bytes, index, 16), NumberStyles.HexNumber);
        }

        /// <summary>
        /// Encode a long value into an ascii byte array
        /// </summary>
        /// <param name="input">long value</param>
        /// <returns></returns>
        public static byte[] EncodeInt64(long input)
        {
            return Encoding.ASCII.GetBytes(input.ToString("X16"));
        }

        /// <summary>
        /// Compare two byte arrays for value equality
        /// </summary>
        /// <param name="ar1">first array</param>
        /// <param name="ar2">second array</param>
        /// <returns></returns>
        public static bool ByteArraysAreEqual(byte[] ar1, byte[] ar2)
        {
            return ar1.Length == ar2.Length && ByteArraysAreEqual(ar1, 0, ar2, 0, ar1.Length);
        }

        /// <summary>
        /// Compare two byte arrays for value equality at specific offsets and length
        /// </summary>
        /// <param name="ar1">first array</param>
        /// <param name="start1">offset within first array</param>
        /// <param name="ar2">second array</param>
        /// <param name="start2">offset within second array</param>
        /// <param name="count">number of bytes to compare</param>
        /// <returns></returns>
        public static bool ByteArraysAreEqual(byte[] ar1, int start1, byte[] ar2, int start2, int count)
        {
            Debug.Assert(start1 >= 0 && start2 >= 0 && count >= 0);
            if (start1 + count > ar1.Length)
                return false;

            if (start2 + count > ar2.Length)
                return false;

            for (var i = 0; i < count; i++)
                if (ar1[start1 + i] != ar2[start2 + i])
                    return false;

            return true;
        }

        /// <summary>
        /// Get Hash of the given Asset File based on the Build Target
        /// </summary>
        /// <param name="assetPath">Path of target asset</param>
        /// <param name="buildTarget">Active build target</param>
        /// <param name="textureFormat">Texture Format used for compression</param>
        /// <returns>Platform-dependent Hash String of an asset</returns>
        public static string GetHashForBuildTarget(string assetPath, BuildTarget buildTarget,
            TextureFormat textureFormat)
        {
            if (!File.Exists(assetPath))
                return null;

            using (var md5 = MD5.Create())
            {
                byte[] buildTargetBuffer = BitConverter.GetBytes((int) buildTarget);
                byte[] textureFormatBuffer = BitConverter.GetBytes((int) textureFormat);
                byte[] fileBinary = File.ReadAllBytes(assetPath);
                byte[] projectName = Encoding.ASCII.GetBytes(GetProjectRootFolderName());
                byte[] res = new byte[fileBinary.Length + projectName.Length + buildTargetBuffer.Length +
                                      textureFormatBuffer.Length];

                fileBinary.CopyTo(res, 0);
                projectName.CopyTo(res, fileBinary.Length);
                buildTargetBuffer.CopyTo(res, fileBinary.Length + projectName.Length);
                textureFormatBuffer.CopyTo(res, fileBinary.Length + projectName.Length + textureFormatBuffer.Length);

                var hash = md5.ComputeHash(res);

                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Get Project Root Folder Name.
        /// This makes sure that the same asset used in different projects will have different hashes.
        /// For example, C:/AAA/Asset/MyAsset.asset and C:/BBB/Asset/MyAsset.asset will produce different hashes.
        /// </summary>
        /// <returns>Project Root Folder Name</returns>
        private static string GetProjectRootFolderName()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Directory.GetParent(Application.dataPath).FullName);
            return directoryInfo.Name;
        }

        /// <summary>
        /// Checks if the Asset Exists in the cache by reading the server response stream
        /// </summary>
        /// <param name="header">First 2 bytes of server response stream</param>
        /// <param name="itemType">The type of the item being requested</param>
        public static bool AssetExistsInServer(byte[] header, string itemType)
        {
            try
            {
                string response = Encoding.ASCII.GetString(header, 0, header.Length);

                return response == "+" + itemType;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }

        /// <summary>
        /// Gets the size of the item to download by reading the server response stream
        /// </summary>
        /// <param name="header">Server Response Stream</param>
        /// <returns>Size of the item to be downlaoded</returns>
        public static int GetItemSize(byte[] header)
        {
            int itemSize = 0;

            try
            {
                string strItemSize = Encoding.ASCII.GetString(header, 0, 16);
                itemSize = int.Parse(strItemSize, NumberStyles.HexNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return itemSize;
        }

        /*
        //Used for testing purposes only
        public static string GetUploadFilePath(string guidStr)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string hlodCacheFolder = Path.Combine(projectRoot, "Library", "HLODCache");
            string uploadFolder = Path.Combine(hlodCacheFolder, "Upload");

            if (!Directory.Exists(hlodCacheFolder))
                Directory.CreateDirectory(hlodCacheFolder);

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            return Path.Combine(uploadFolder, guidStr);
        }

        //Used for testing purposes only
        public static string GetDownloadFilePath(string guidStr)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string hlodCacheFolder = Path.Combine(projectRoot, "Library", "HLODCache");
            string downloadFolder = Path.Combine(hlodCacheFolder, "Download");

            if (!Directory.Exists(hlodCacheFolder))
                Directory.CreateDirectory(hlodCacheFolder);

            if (!Directory.Exists(downloadFolder))
                Directory.CreateDirectory(downloadFolder);

            return Path.Combine(downloadFolder, guidStr);
        }*/
    }
}