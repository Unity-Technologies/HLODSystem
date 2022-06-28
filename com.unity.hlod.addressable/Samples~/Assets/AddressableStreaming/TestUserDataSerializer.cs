using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestUserDataSerializer : Unity.HLODSystem.Serializer.UserDataSerializerBase
{
#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    static void Register()
    {
        Unity.HLODSystem.Serializer.UserDataSerializerTypes.RegisterType(typeof(TestUserDataSerializer));
    }
#endif

    protected override void SerializeUserData(GameObject gameObject, HLODUserData data)
    {
        var test = gameObject.GetComponent<TestSerialize>();
        if (test == null)
            return;

        data.IntDatas.AddData("Test", test.Value);
    }

    protected override void DeserializeUserData(GameObject gameObject, HLODUserData data)
    {
        if (data.IntDatas.HasData("Test") == false)
            return;

        var test = gameObject.AddComponent<TestSerialize>();
        test.Value = data.IntDatas.GetData("Test");
    }
}