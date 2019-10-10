using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using CustomUnityCacheClient;
using UnityEditor;
using UnityEngine;


namespace CustomUnityCacheClient
{
    public class CustomCacheClientGUI : EditorWindow
    {
        private bool mToggleCacheEnabled;
        private bool gotIpFromSettings;
        private bool mValidHostAddress = true;
        private bool mConnectedToHost = false;

        private bool mShowMessage = false;

        //private bool mIpChanged = false;
        private string mCacheServerIpAddress = string.Empty;

        public const string HLOD_CACHE_ENABLED = "HLODCacheEnabled";
        public const string HLOD_CACHE_SERVER_IP = "HLODCacheServerIP";

        [MenuItem("HLOD Utils/Custom Asset Caching")]
        static void Init()
        {
            CustomCacheClientGUI window = (CustomCacheClientGUI) GetWindow(typeof(CustomCacheClientGUI), true,
                "Custom Asset Caching");
            window.minSize = new Vector2(500, 300);
            window.Show();
        }

        void OnGUI()
        {
            int port;
            string ipAddress;

            GUILayout.BeginVertical();

            bool isCacheEnabled = IsCachingEnabled(out ipAddress, out port);

            mToggleCacheEnabled = EditorGUILayout.Toggle("Cache Textures", isCacheEnabled);
            EditorPrefs.SetBool(HLOD_CACHE_ENABLED, mToggleCacheEnabled);

            EditorGUI.BeginDisabledGroup(!mToggleCacheEnabled);
            GUILayout.BeginHorizontal();

            if (!gotIpFromSettings)
            {
                gotIpFromSettings = true;
                mCacheServerIpAddress = EditorPrefs.GetString(HLOD_CACHE_SERVER_IP);
            }

            mCacheServerIpAddress = EditorGUILayout.TextField("Cache Server IP Address", mCacheServerIpAddress);

            /*if (mCacheServerIpAddress != (ipAddress + ":" + port))
                mIpChanged = true;*/

            if (GUILayout.Button("Test Connection", EditorStyles.miniButton, GUILayout.Width(150)))
            {
                mShowMessage = true;

                ipAddress = ValidateIpAddress(mCacheServerIpAddress, ref port);

                mValidHostAddress = !string.IsNullOrEmpty(ipAddress);

                if (mValidHostAddress)
                {
                    try
                    {
                        CustomCacheClient.GetInstance(ipAddress, port);
                        CustomCacheClient.GetInstance().Connect(5000);
                        mConnectedToHost = CustomCacheClient.GetInstance().IsConnected;
                    }
                    catch
                    {
                        mConnectedToHost = false;
                    }
                }
                else
                {
                    mConnectedToHost = false;
                }
            }

            GUILayout.EndHorizontal();

            if (mShowMessage)
            {
                if (!mValidHostAddress)
                {
                    GUILayout.BeginVertical();
                    EditorGUILayout.HelpBox("Invalid Host Address", MessageType.Error, true);
                    GUILayout.EndVertical();
                }
                else if (mValidHostAddress && !mConnectedToHost)
                {
                    GUILayout.BeginVertical();
                    EditorGUILayout.HelpBox("Connection to Host failed", MessageType.Warning, true);
                    GUILayout.EndVertical();
                }
                else if (mValidHostAddress && mConnectedToHost)
                {
                    GUILayout.BeginVertical();
                    EditorGUILayout.HelpBox("Connection to Host succeeded", MessageType.Info, true);
                    GUILayout.EndVertical();

                    EditorPrefs.SetString(HLOD_CACHE_SERVER_IP, ipAddress + ":" + port.ToString());
                }
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.EndVertical();
            //Debug.Log(CustomCacheClient.GetInstance().CacheEnabled);
        }

        void OnDestroy()
        {
           SetCacheSettings();
        }
        
        void OnLostFocus()
        {
            SetCacheSettings();
        }

        private void SetCacheSettings()
        {
            //Enable/Disable Cache
            CustomCacheClient.GetInstance().CacheEnabled = mToggleCacheEnabled;

            if (mToggleCacheEnabled)
                CustomCacheClient.GetInstance().Connect(5000);
            else
            {
                CustomCacheClient.GetInstance().Close();
            }
        }

        private static string ValidateIpAddress(string ipAddress, ref int port)
        {
            bool isValidPort = true;

            if (ipAddress.Contains(":"))
            {
                isValidPort = Int32.TryParse(ipAddress.Substring(ipAddress.IndexOf(":", StringComparison.Ordinal) + 1),
                    out port);
                ipAddress = ipAddress.Substring(0, ipAddress.IndexOf(":", StringComparison.Ordinal));
            }

            bool isValidIpAddress = IPAddress.TryParse(ipAddress, out _);

            return isValidIpAddress && isValidPort ? ipAddress : null;
        }

        public static bool IsCachingEnabled(out string host, out int port)
        {
            string rawHost = EditorPrefs.GetString(HLOD_CACHE_SERVER_IP);

            Int32.TryParse(rawHost.Substring(rawHost.IndexOf(":", StringComparison.Ordinal) + 1),
                out port);
            host = rawHost.Substring(0, rawHost.IndexOf(":", StringComparison.Ordinal));

            return EditorPrefs.GetBool(HLOD_CACHE_ENABLED);
        }
    }

    [InitializeOnLoad]
    public class InitCustomCacheClient
    {
        /// <summary>
        /// Sets the initial settings of the Cache Client and creates an instance
        /// </summary>
        static InitCustomCacheClient()
        {
            //Create initial keys
            if (!EditorPrefs.HasKey(CustomCacheClientGUI.HLOD_CACHE_ENABLED))
                EditorPrefs.SetBool(CustomCacheClientGUI.HLOD_CACHE_ENABLED, false);

            if (!EditorPrefs.HasKey(CustomCacheClientGUI.HLOD_CACHE_SERVER_IP))
                EditorPrefs.SetString(CustomCacheClientGUI.HLOD_CACHE_SERVER_IP, "127.0.0.1:8126");

            bool isCacheEnabled = CustomCacheClientGUI.IsCachingEnabled(out var ipAddress, out var port);
            CustomCacheClient client = CustomCacheClient.GetInstance(ipAddress, port);

            if (isCacheEnabled)
            {
                client.CacheEnabled = true;
                client.Connect(5000);
                Debug.Log("HLOD Asset Caching is enabled");
            }
            else
            {
                client.CacheEnabled = false;
                Debug.Log("HLOD Asset Caching is enabled");
            }
        }
    }
}