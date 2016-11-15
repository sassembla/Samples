/*
    data type definition.

    {
        "res_ver": 1,
        "assetBundles": [
            {
                "bundleName": "SampleAssetBundle.unity3d",
                "size": 100,
                "version": 1,
                "crc": 100,
                "resourceNames": "sushi,udon"
            }
        ]
    }
*/
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class ListData {
    [SerializeField] public string res_ver;
    [SerializeField] public List<BundleData> assetBundles;
    public ListData (string res_ver, List<BundleData> assetBundles) {
        this.res_ver = res_ver;
        this.assetBundles = assetBundles;
    }
}