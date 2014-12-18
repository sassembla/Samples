using UnityEngine;
using System;
using System.IO;
using System.Collections;

public class Loader : MonoBehaviour {

	static string bundleResourceName1 = "";
	static string bundleName = "SampleAssetBundle.unity3d";
	static string url = Path.Combine("file:///AssetRails/temp/Resources/bundlize/", bundleName);

	void GenerateAssetBundle () {
		// var assetBundleResource = Resources.bu
	}



	IEnumerator CleanCacheThenLoadAssetBundle() {
		if (!Caching.ready) yield return null;

		Debug.Log("cache ready. clean cache.");
		Caching.CleanCache();

		Debug.Log("cache cleaned. get AssetBundle from:" + url);

		WWW www = WWW.LoadFromCacheOrDownload(url, 1);
		yield return www;

		Debug.Log("www.error:" + www.error);

		var assetBundle = www.assetBundle;

		if (assetBundle) {
			Debug.Log("hit!");
		} else {
			Debug.Log("failed");
		}
		Debug.Log("name:" + assetBundle.name);

		assetBundle.Unload(false);
	}


	IEnumerator LoadAssetBundle() {
		WWW www = WWW.LoadFromCacheOrDownload(url, 1);
		yield return www;

		Debug.Log("www.error:" + www.error);

		var assetBundle = www.assetBundle;

		if (assetBundle) {
			Debug.Log("hit!");
		} else {
			Debug.Log("failed");
		}
		Debug.Log("name:" + assetBundle.name);

		assetBundle.Unload(false);
	}




	int cacheState = -1;
	const int CacheWaiting = 0;
	const int CacheReady = 1;
	const int CacheLoad = 2;
	const int CacheDownloading = 3;
	
	void Start () {
		// cacheState = CacheWaiting;

		// 単体でResourceから作るやつ
		GenerateAssetBundle();

		// 単体で落とす奴
		// StartCoroutine(CleanCacheThenLoadAssetBundle());
	}

	// Update is called once per frame
	void Update () {
		switch (cacheState) {
			case CacheWaiting: {
				Debug.Log("waiting...");
				if (Caching.ready) {
					cacheState = CacheReady;
				}
				break;
			}
			case CacheReady: {
				var cached = Caching.IsVersionCached(url, 1);
				if (cached) {
					cacheState = CacheLoad;
					StartCoroutine(LoadAssetBundle());
				} else {
					cacheState = CacheDownloading;
					StartCoroutine(LoadAssetBundle());
				}
				break;
			}
			case CacheLoad: {
				Debug.Log("loading Resource from cache...");
				break;
			}
			case CacheDownloading: {
				Debug.Log("downloading...");
				break;
			}

			default: {
				break;
			}
		}
	}



}
