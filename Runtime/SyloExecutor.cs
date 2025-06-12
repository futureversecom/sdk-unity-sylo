using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SyloExecutor))]
public class SyloExecutorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Run Debug"))
        {
            (target as SyloExecutor).RunDebug();
        }
    }
}
#endif
public class SyloExecutor : MonoBehaviour
{
    public string debug_did;
    [FormerlySerializedAs("resolverUri")] public string debug_resolverUri;
    [FormerlySerializedAs("accessToken")] public string debug_accessToken;
    
    public void RunDebug()
    {
        StartCoroutine(GetBytesFromDID(debug_did, new DebugAuthDetails(debug_accessToken), bytes => Debug.Log($"Received {bytes.Length} bytes"), Debug.LogException));
    }
    
    private IEnumerator GetBytesFromDID(string did, ISyloAuthDetails authDetails, Action<byte[]> onSuccess, Action<Exception> onError = null)
    {
        if (string.IsNullOrEmpty(did))
        {
            yield break;
        }

        if (!TryParseDID(did, out var futurePassAddress, out var dataId))
        {
            yield break;
        }
        
        string uri = debug_resolverUri; // TODO: We need api to discover sylo/resolvers
        uri += "/api/v1/objects/get/" + futurePassAddress + "/" + dataId + "?authType=access_token";
        
        var webRequest = UnityWebRequest.Get(uri);
        webRequest.SetRequestHeader("Accept", "*/*");
        webRequest.SetRequestHeader("Authorization", "Bearer " + authDetails.GetAccessToken());
        
        yield return webRequest.SendWebRequest();
        Debug.Log("Result: " + webRequest.result);
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(new Exception($"WR Err: Result: {webRequest.result}, Code: {webRequest.responseCode}, Error: {webRequest.error}"));
            yield break;
        }
        
        onSuccess?.Invoke(webRequest.downloadHandler.data);
        Debug.Log(webRequest.downloadHandler.text);
        yield break;
    }
    
    public bool TryParseDID(string did, out string futurePassAddress, out string dataId)
    {
        futurePassAddress = null;
        dataId = null;
        
        // did:sylo-data:futurepassID/data id
        
        var split = did.Split(':');
        if (split == null || split.Length == 0 || split[0] != "did")
        {
            return false;
        }
        
        var dataSplit = split[2].Split('/');
        futurePassAddress = dataSplit[0];
        dataId = dataSplit[1];
        
        return true;
    }
}

public class DebugAuthDetails : ISyloAuthDetails
{
    private readonly string _accessToken;
    public DebugAuthDetails(string accessToken)
    {
        _accessToken = accessToken;
    }
    
    public string GetAccessToken()
    {
        return _accessToken;
    }
}
