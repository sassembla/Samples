using System;
using UnityEngine;

/*
    data type definition.

    {
        "bundleName": "SampleAssetBundle.unity3d",
        "size": 100,
        "version": 1,
        "crc": 100,
        "resourceNames": "sushi,udon"
    }
*/
[Serializable] public class BundleData {
    [SerializeField] public string bundleName;
    [SerializeField] public int size;
    [SerializeField] public int version;
    [SerializeField] public uint crc;
    [SerializeField] public string[] resourceNames;
    public BundleData (string bundleName, int size, int version, uint crc, string[] resourceNames) {
        this.bundleName = bundleName;
        this.size = size;
        this.version = version;
        this.crc = crc;
        this.resourceNames = resourceNames;
    }
}