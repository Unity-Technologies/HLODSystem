using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem.CustomUnityCacheClient
{
    /// <summary>
    /// The type of a particular file.
    /// </summary>
    public enum AssetFileType
    {
        Resource = 'r'
    }

    /// <summary>
    /// The result returned by a download operation.
    /// </summary>
    public enum DownloadResult
    {
        Failure = 0,
        FileNotFound = 1,
        Success = 2,
        NotSupportedFileType = 3,
        NotConnectedToServer = 4,
        CacheNotEnabled = 5
    }

    public enum UploadResult
    {
        Failure = 0,
        FileNotFound = 1,
        Success = 2,
        NotSupportedFileType = 3,
        NotConnectedToServer = 4,
        CacheNotEnabled = 5
    }

    /// <summary>
    /// A GUID/Hash pair that uniquely identifies a particular file. For each FileId, the Cache Server can store a separate
    /// binary stream for each AssetFileType.
    /// </summary>
    public struct FileId : IEqualityComparer
    {
        /// <summary>
        /// The guid byte array.
        /// </summary>
        public readonly byte[] guid;

        /// <summary>
        /// The hash code byte array.
        /// </summary>
        public readonly byte[] hash;

        /// <summary>
        /// A structure used to identify a file by guid and hash code.
        /// </summary>
        /// <param name="guid">File GUID.</param>
        /// <param name="hash">File hash code.</param>
        private FileId(byte[] guid, byte[] hash)
        {
            this.guid = guid;
            this.hash = hash;
        }

        /// <summary>
        /// Create a FileId given a string guid and string hash code representation.
        /// </summary>
        /// <param name="guidStr">GUID string representation.</param>
        /// <param name="hashStr">Hash code string representation.</param>
        /// <returns></returns>
        public static FileId From(string guidStr, string hashStr)
        {
            if (guidStr.Length != 32)
                throw new ArgumentException("Length != 32", "guidStr");

            if (hashStr.Length != 32)
                throw new ArgumentException("Length != 32", "hashStr");

            return new FileId(Util.StringToGuid(guidStr), Util.StringToHash(hashStr));
        }

        /// <summary>
        /// Check equality of two objects given their guid and hash code.
        /// </summary>
        /// <param name="x">lhs object.</param>
        /// <param name="y">rhs object.</param>
        /// <returns></returns>
        public new bool Equals(object x, object y)
        {
            var hash1 = (byte[]) x;
            var hash2 = (byte[]) y;

            if (hash1.Length != hash2.Length)
                return false;

            for (var i = 0; i < hash1.Length; i++)
                if (hash1[i] != hash2[i])
                    return false;

            return true;
        }

        /// <summary>
        /// Get the hash code for a specific object.
        /// </summary>
        /// <param name="obj">The object you want the hash code for.</param>
        /// <returns></returns>
        public int GetHashCode(object obj)
        {
            var hc = 17;
            hc = hc * 23 + guid.GetHashCode();
            hc = hc * 23 + hash.GetHashCode();
            return hc;
        }
    }

    /// <summary>
    /// Exception thrown when an upload operation is not properly isolated within a begin/end transaction
    /// </summary>
    public class TransactionIsolationException : Exception
    {
        public TransactionIsolationException(string msg) : base(msg)
        {
        }
    }

    /// <summary>
    /// A client API for uploading and downloading files from a Cache Server
    /// </summary>
    public sealed class CustomCacheClient
    {
        private enum StreamReadState
        {
            Response,
            Size,
            Id
        }

        private const int ProtocolVersion = 254;
        private const string CmdTrxBegin = "ts";
        private const string CmdTrxEnd = "te";
        private const string CmdGet = "g";
        private const string CmdPut = "p";
        private const string CmdQuit = "q";

        private const int ResponseLen = 2;
        private const int SizeLen = 16;
        private const int GuidLen = 16;
        private const int HashLen = 16;
        private const int IdLen = GuidLen + HashLen;
        private const int ReadBufferLen = 4 * 1024;

        private TcpClient m_tcpClient;
        private readonly string m_host;
        private readonly int m_port;
        private NetworkStream m_stream;
        private readonly byte[] m_streamReadBuffer;
        private int m_streamBytesRead;
        private int m_streamBytesNeeded;
        private StreamReadState m_streamReadState = StreamReadState.Response;
        private Stream m_nextWriteStream;
        private bool m_inTrx;

        private CancellationTokenSource mToken;
        public bool CacheEnabled { get; set; }

        private static CustomCacheClient mInstance;
        private static readonly object mLock = new object();

        private CustomCacheClient(string host, int port = 8126)
        {
            m_streamReadBuffer = new byte[ReadBufferLen];
            m_tcpClient = new TcpClient();
            m_host = host;
            m_port = port;

            mToken = new CancellationTokenSource();
            Thread thread = new Thread(() => CheckConnectionStatus(mToken.Token));
            thread.Start();
        }

        /// <summary>
        /// Create an instance of Cache Server client.
        /// Must be called only from GUI. Other callers must call GetInstance() method
        /// and use already created instance.
        /// </summary>
        /// <param name="host">The host name or IP of the Cache Server.</param>
        /// <param name="port">The port number of the Cache Server. Default port is 8126.</param>
        public static CustomCacheClient GetInstance(string host, int port)
        {
            if (null == mInstance || (null != mInstance && (host != mInstance.m_host || port != mInstance.m_port)))
            {
                lock (mLock)
                {
                    //Ensure the previous instance of CheckConnectionStatus() that checks
                    //the connection status periodically is terminated
                    if (mInstance != null)
                        mInstance.mToken.Cancel();

                    mInstance = new CustomCacheClient(host, port);
                    Debug.Log("Initialized a new instance of HLOD Cache Client");
                }
            }

            return mInstance;
        }

        /// <summary>
        /// Get the instance of Cache Server client.
        /// </summary>
        public static CustomCacheClient GetInstance()
        {
            return mInstance;
        }


        /// <summary>
        /// Connects to the Cache Server and sends a protocol version handshake. A TimeoutException is thrown if the connection cannot
        /// be established within <paramref name="timeoutMs"/> milliseconds.
        /// </summary>
        /// <param name="timeoutMs"></param>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="Exception"></exception>
        public void Connect(int timeoutMs)
        {
            if (IsConnected) return;

            m_tcpClient = new TcpClient();

            var client = m_tcpClient;
            var op = client.BeginConnect(m_host, m_port, null, null);

            bool connectionSucceeded = op.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs));

            if (!connectionSucceeded)
                return;

            try
            {
                m_stream = client.GetStream();
            }
            catch
            {
                return;
            }

            SendVersion();
        }


        /// <summary>
        /// Upload from the given stream for the given AssetFileType. Will throw an exception if not preceeded by BeginTransaction.
        /// </summary>
        /// <param name="assetPath">HLOD Asset Path</param>
        /// <param name="compressedTextures">Compressed Textures</param>
        /// <param name="buildTarget">Active Build Target</param>
        /// <exception cref="ArgumentException"></exception>
        public UploadResult PutCachedTextures(string assetPath, List<byte[]> compressedTextures,
            BuildTarget buildTarget)
        {
            if (!CacheEnabled)
                return UploadResult.CacheNotEnabled;

            if (!IsConnected)
                return UploadResult.NotConnectedToServer;

            if (!assetPath.ToLower().EndsWith(".hlod"))
                return UploadResult.NotSupportedFileType;

            string guidStr = AssetDatabase.AssetPathToGUID(assetPath);
            string hashStr = Util.GetHashForBuildTarget(assetPath, buildTarget);

            if (null == hashStr)
                return UploadResult.FileNotFound;

            try
            {
                var fileId = FileId.From(guidStr, hashStr);
                BeginTransaction(fileId);

                using (Stream readStream = new MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(readStream, compressedTextures);

                    if (!readStream.CanRead || !readStream.CanSeek)
                        throw new ArgumentException();
                    readStream.Position = 0;

                    Upload(AssetFileType.Resource, readStream);
                }

                EndTransaction();

                return UploadResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }

            return UploadResult.Failure;
        }

        private void BeginTransaction(FileId fileId)
        {
            m_inTrx = true;
            m_stream.Write(Encoding.ASCII.GetBytes(CmdTrxBegin), 0, 2);
            m_stream.Write(fileId.guid, 0, GuidLen);
            m_stream.Write(fileId.hash, 0, HashLen);
        }

        private void Upload(AssetFileType type, Stream readStream)
        {
            if (!m_inTrx)
                throw new TransactionIsolationException("Upload without BeginTransaction");

            if (!readStream.CanRead || !readStream.CanSeek)
                throw new ArgumentException();

            m_stream.Write(Encoding.ASCII.GetBytes(CmdPut + (char) type), 0, 2);
            m_stream.Write(Util.EncodeInt64(readStream.Length), 0, SizeLen);

            var buf = new byte[ReadBufferLen];
            while (readStream.Position < readStream.Length - 1)
            {
                var len = readStream.Read(buf, 0, ReadBufferLen);
                m_stream.Write(buf, 0, len);
            }
        }

        private void EndTransaction()
        {
            if (!m_inTrx)
                throw new TransactionIsolationException("EndTransaction without BeginTransaction");

            m_inTrx = false;
            m_stream.Write(Encoding.ASCII.GetBytes(CmdTrxEnd), 0, 2);
        }

        /// <summary>
        /// Send a download request to the Cache Server.
        /// </summary>
        /// <param name="assetPath">HLOD Asset Path</param>
        /// <param name="buildTarget">Active Build Target</param>
        /// <param name="compressedTextures">Compressed Textures</param>
        /// <exception cref="ArgumentException"></exception>
        public DownloadResult GetCachedTextures(string assetPath, BuildTarget buildTarget,
            out List<byte[]> compressedTextures)
        {
            compressedTextures = new List<byte[]>();

            if (!CacheEnabled)
                return DownloadResult.CacheNotEnabled;

            if (!IsConnected)
                return DownloadResult.NotConnectedToServer;

            if (!assetPath.ToLower().EndsWith(".hlod"))
                return DownloadResult.NotSupportedFileType;

            string guidStr = AssetDatabase.AssetPathToGUID(assetPath);
            string hashStr = Util.GetHashForBuildTarget(assetPath, buildTarget);

            if (null == hashStr)
                return DownloadResult.FileNotFound;

            try
            {
                var fileId = FileId.From(guidStr, hashStr);
                MemoryStream memoryStream = Download(AssetFileType.Resource, fileId);

                if (null == memoryStream)
                    return DownloadResult.FileNotFound;

                BinaryFormatter bin = new BinaryFormatter();
                memoryStream.Position = 0;
                compressedTextures = (List<byte[]>) bin.Deserialize(memoryStream);

                /*string downloadPath = Util.GetDownloadFilePath(guidStr);
                using (FileStream file = new FileStream(downloadPath, FileMode.Create, FileAccess.Write))
                {
                    memoryStream.Position = 0;
                    memoryStream.WriteTo(file);
                }*/

                memoryStream.Dispose();

                return DownloadResult.Success;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }

            return DownloadResult.Failure;
        }

        private MemoryStream Download(AssetFileType type, FileId downloadItem)
        {
            MemoryStream memoryStream = null;

            try
            {
                m_stream.Write(Encoding.ASCII.GetBytes(CmdGet + (char) type), 0, 2);
                m_stream.Write(downloadItem.guid, 0, GuidLen);
                m_stream.Write(downloadItem.hash, 0, HashLen);

                byte[] cacheHitHeader = new byte[2];
                m_stream.Read(cacheHitHeader, 0, cacheHitHeader.Length);

                if (Util.AssetExistsInServer(cacheHitHeader, ((char) type).ToString()))
                {
                    byte[] itemSizeHeader = new byte[48];
                    m_stream.Read(itemSizeHeader, 0, itemSizeHeader.Length);

                    int itemSize = Util.GetItemSize(itemSizeHeader);

                    if (itemSize > 0)
                    {
                        memoryStream = new MemoryStream(itemSize);

                        int read = -1;
                        int bytesToRead = ReadBufferLen;
                        byte[] buffer = new byte[ReadBufferLen];

                        while ((read = m_stream.Read(buffer, 0, bytesToRead)) > 0)
                        {
                            itemSize -= read;
                            memoryStream.Write(buffer, 0, read);

                            if (itemSize < bytesToRead)
                                bytesToRead = itemSize;

                            if (itemSize <= 0)
                                break;
                        }
                    }
                }
                else
                {
                    //Asset does not exist in Cache Server.
                    //Cleanup the stream for the next request
                    byte[] itemSizeHeader = new byte[32]; //GUID and Hash
                    m_stream.Read(itemSizeHeader, 0, itemSizeHeader.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }

            return memoryStream;
        }

        /// <summary>
        /// Close the connection to the Cache Server. Sends the 'quit' command and closes the network stream.
        /// </summary>
        public void Close()
        {
            if (null == m_tcpClient)
                return;

            if (m_stream != null)
                m_stream.Write(Encoding.ASCII.GetBytes(CmdQuit), 0, 1);

            if (m_tcpClient != null)
                m_tcpClient.Close();

            m_tcpClient = null;
        }

        private void SendVersion()
        {
            var encodedVersion = Util.EncodeInt32(ProtocolVersion, true);
            m_stream.Write(encodedVersion, 0, encodedVersion.Length);

            var versionBuf = new byte[8];
            var pos = 0;
            while (pos < versionBuf.Length - 1)
            {
                pos += m_stream.Read(versionBuf, 0, versionBuf.Length);
            }

            if (Util.ReadUInt32(versionBuf, 0) != ProtocolVersion)
                throw new Exception("Server version mismatch");
        }

        /// <summary>
        /// Checks the Connection Status of TCP Client 
        /// </summary>
        public bool IsConnected
        {
            get
            {
                try
                {
                    if (m_tcpClient != null && m_tcpClient.Client != null && m_tcpClient.Client.Connected)
                    {
                        /* pear to the documentation on Poll:
                         * When passing SelectMode.SelectRead as a parameter to the Poll method it will return 
                         * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                         * -or- true if data is available for reading; 
                         * -or- true if the connection has been closed, reset, or terminated; 
                         * otherwise, returns false
                         */

                        // Detect if client disconnected
                        if (m_tcpClient.Client.Poll(0, SelectMode.SelectRead))
                        {
                            byte[] buff = new byte[1];
                            if (m_tcpClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                            {
                                // Client disconnected
                                return false;
                            }

                            return true;
                        }

                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        private void CheckConnectionStatus(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    return;

                if (mInstance.CacheEnabled && !mInstance.IsConnected)
                {
                    Debug.Log("HLOD Asset Cache Client is disconnected. Reconnecting...");
                    mInstance.Connect(5000);

                    Debug.Log(mInstance.IsConnected ? "\tSuccess" : "\tFailed");
                }

                Thread.Sleep(60000);
            }
        }
    }
}