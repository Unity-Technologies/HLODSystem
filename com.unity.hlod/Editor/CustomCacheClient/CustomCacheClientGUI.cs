using System;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem.CustomUnityCacheClient
{
    public class CustomCacheClientGUI : EditorWindow
    {
        private SettingsUtil.CacheServerSettings mCacheServerSettings;
        private bool mToggleCacheEnabled;
        private bool gotIpFromSettings;
        private bool mValidHostAddress = true;
        private bool mConnectedToHost = false;
        private bool mShowMessage = false;
        private bool mSettingsFileWritable = false;
        private string mCacheServerIpAddress = string.Empty;

        [MenuItem("HLOD Utils/Custom Asset Caching")]
        static void Init()
        {
            CustomCacheClientGUI window = (CustomCacheClientGUI) GetWindow(typeof(CustomCacheClientGUI), true,
                "Custom Asset Caching");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        void OnEnable()
        {
            mCacheServerSettings = SettingsUtil.GetCacheServerSettings();
            mToggleCacheEnabled = mCacheServerSettings.enabled;
            mSettingsFileWritable = SettingsUtil.IsSettingsFileWriteable();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            {
                mToggleCacheEnabled = EditorGUILayout.Toggle("Enable Cache", mToggleCacheEnabled);
                mCacheServerSettings.enabled = mToggleCacheEnabled;

                EditorGUI.BeginDisabledGroup(!mToggleCacheEnabled);
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (!gotIpFromSettings)
                        {
                            gotIpFromSettings = true;
                            mCacheServerIpAddress = mCacheServerSettings.host + ":" + mCacheServerSettings.port;
                        }

                        mCacheServerIpAddress =
                            EditorGUILayout.TextField("Default IP Address", mCacheServerIpAddress);

                        if (GUILayout.Button("Check Connection", EditorStyles.miniButton, GUILayout.Width(150)))
                        {
                            mShowMessage = true;
                            mCacheServerSettings.host =
                                SettingsUtil.ValidateIpAddress(mCacheServerIpAddress, ref mCacheServerSettings.port);
                            mValidHostAddress = !string.IsNullOrEmpty(mCacheServerSettings.host);

                            if (mValidHostAddress)
                            {
                                try
                                {
                                    CustomCacheClient.GetInstance(mCacheServerSettings.host, mCacheServerSettings.port);
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
                    }
                    EditorGUILayout.EndHorizontal();

                    if (mShowMessage)
                    {
                        if (!mValidHostAddress)
                        {
                            EditorGUILayout.BeginVertical();
                            EditorGUILayout.HelpBox("Invalid Host Address", MessageType.Error, true);
                            EditorGUILayout.EndVertical();
                        }
                        else if (mValidHostAddress && !mConnectedToHost)
                        {
                            EditorGUILayout.BeginVertical();
                            EditorGUILayout.HelpBox("Connection failed", MessageType.Warning, true);
                            EditorGUILayout.EndVertical();
                        }
                        else if (mValidHostAddress && mConnectedToHost)
                        {
                            EditorGUILayout.BeginVertical();
                            EditorGUILayout.HelpBox("Connection succeeded", MessageType.Info, true);
                            EditorGUILayout.EndVertical();
                        }
                    }

                    if (!mSettingsFileWritable)
                    {
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.HelpBox("Cache Server Settings file is read-only and cannot be changed" +
                                                @" (ProjectSettings\" + SettingsUtil.mCacheServerSettingsFile + ").",
                            MessageType.Warning, true);
                        EditorGUILayout.EndVertical();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();
        }

        private void OnFocus()
        {
            mSettingsFileWritable = SettingsUtil.IsSettingsFileWriteable();
        }

        void OnLostFocus()
        {
            //Continue writing to the Settings File even if mSettingsFileWritable = false
            //as this will result in an Error Message in the log to attract the client's attention.
            SettingsUtil.ApplyCacheServerSettings(mToggleCacheEnabled);

            if (mValidHostAddress)
                SettingsUtil.SetCacheServerSettings(mCacheServerSettings);
        }

        void OnDestroy()
        {
            SettingsUtil.ApplyCacheServerSettings(mToggleCacheEnabled);

            if (mValidHostAddress)
                SettingsUtil.SetCacheServerSettings(mCacheServerSettings);
        }
    }

    public static class SettingsUtil
    {
        public const string mCacheServerSettingsFile = "HLODCacheServerSettings.asset";

        [Serializable]
        public class CacheServerSettings
        {
            public bool enabled;
            public string host;
            public int port;
        }

        /// <summary>
        /// Gets the settings of Cache Server from ProjectSettings Folder
        /// <returns>Cache Server Settings Settings</returns>
        /// </summary>
        public static CacheServerSettings GetCacheServerSettings()
        {
            try
            {
                string filePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ProjectSettings",
                    mCacheServerSettingsFile);

                if (File.Exists(filePath))
                {
                    string dataAsJson = File.ReadAllText(filePath);
                    CacheServerSettings cacheServerSettings = JsonUtility.FromJson<CacheServerSettings>(dataAsJson);

                    return cacheServerSettings;
                }

                return new CacheServerSettings {host = "127.0.0.1", port = 8126};
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Gets the settings of Cache Server from ProjectSettings Folder
        /// <param name="cacheServerSettings">Cache Server Settings</param>
        /// </summary>
        public static void SetCacheServerSettings(CacheServerSettings cacheServerSettings)
        {
            try
            {
                string filePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ProjectSettings",
                    mCacheServerSettingsFile);

                using (var stream = File.CreateText(filePath))
                    stream.Write(EditorJsonUtility.ToJson(cacheServerSettings, true));
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Enables or disables the Cache Client on the fly 
        /// </summary>
        public static void ApplyCacheServerSettings(bool cacheEnabled)
        {
            //Enable/Disable Cache
            CustomCacheClient.GetInstance().CacheEnabled = cacheEnabled;

            if (cacheEnabled)
                CustomCacheClient.GetInstance().Connect(5000);
            else
                CustomCacheClient.GetInstance().Close();
        }

        /// <summary>
        /// Parses the Host and Port number input by user
        /// <param name="ipAddress">Host and Port number separated by ':'</param>
        /// <param name="port">Out parameter that holds the reference to the port number extracted from IpAddress:port String</param>
        /// <returns>IP Address. Port number is returned as a reference</returns>
        /// </summary>
        public static string ValidateIpAddress(string ipAddress, ref int port)
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

        /// <summary>
        /// Check if Settings File can be changed, as it might be locked by VCS  
        /// </summary>
        public static bool IsSettingsFileWriteable()
        {
            string filePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ProjectSettings",
                mCacheServerSettingsFile);

            FileInfo fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
                return true;

            return !fileInfo.IsReadOnly;
        }
    }

    [InitializeOnLoad]
    public class InitCustomCacheClient
    {
        /// <summary>
        /// Creates an instance of Cache Client and Sets the initial settings of it
        /// </summary>
        static InitCustomCacheClient()
        {
            //Get Cache Server Settings
            SettingsUtil.CacheServerSettings
                cacheServerSettings = SettingsUtil.GetCacheServerSettings();
            CustomCacheClient client =
                CustomCacheClient.GetInstance(cacheServerSettings.host, cacheServerSettings.port);

            if (cacheServerSettings.enabled)
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