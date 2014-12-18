using UnityEngine;

using System;
using System.IO;
using System.Collections;

public class Downloader : MonoBehaviour {
	/*
		AssetBundleから取得できるResource名(ファイル自体はjpg)
	*/
	static string bundleResourceName1 = "sushi";
	static string bundleResourceName2 = "udon";

	// AssetBundleのファイル名
	static string bundleName = "SampleAssetBundle.unity3d";

	// AssetBundle化したファイルを置く場所
	static string outputBasePath = "bundlized";

	// ファイルプロトコル
	const string fileProtocolStr = "file://";


	IEnumerator CleanCacheThenLoadAssetBundle() {
		if (!Caching.ready) yield return null;


		// キャッシュをクリア
		Caching.CleanCache();


		// プロジェクトのパスを取得
		var dataPath = Application.dataPath;
		var projectPath = Directory.GetParent(dataPath).ToString();

		var localUrlArray = new string[]{projectPath, outputBasePath, bundleName};
		

		var url = fileProtocolStr + Combine(localUrlArray);
		var version = 1;
		var crc = 0;

		// AssetBundleをファイルプロトコルで取得
		var www = WWW.LoadFromCacheOrDownload(url, version, crc);

		yield return www;

		if (!String.IsNullOrEmpty(www.error)) {
			Debug.Log("www.error:" + www.error);
			yield break;
		}

		var assetBundle = www.assetBundle;

		// loar Resource from AssetBundle
		Texture2D sushiTexture = assetBundle.Load(bundleResourceName1) as Texture2D;
		Debug.Log("sushiTexture:" + sushiTexture.name);

		Texture2D udonTexture = assetBundle.Load(bundleResourceName2) as Texture2D;
		Debug.Log("udonTexture:" + udonTexture.name);


		// キャッシュをクリア
		Caching.CleanCache();

		// assetBundleを開放
		assetBundle.Unload(false);

		www.Dispose();
	}


	// IEnumerator LoadAssetBundle() {
	// 	WWW www = WWW.LoadFromCacheOrDownload(url, 1);
	// 	yield return www;

	// 	Debug.Log("www.error:" + www.error);

	// 	var assetBundle = www.assetBundle;

	// 	if (assetBundle) {
	// 		Debug.Log("hit!");
	// 	} else {
	// 		Debug.Log("failed");
	// 	}
	// 	Debug.Log("name:" + assetBundle.name);

	// 	assetBundle.Unload(false);
	// }




	int cacheState = -1;
	const int CacheWaiting = 0;
	const int CacheReady = 1;
	const int CacheLoad = 2;
	const int CacheDownloading = 3;
	

	void Start () {
		// cacheState = CacheWaiting;

		// 単体で落とす奴
		StartCoroutine(CleanCacheThenLoadAssetBundle());
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
				// var cached = Caching.IsVersionCached(localUrlStr, 1);
				// if (cached) {
				// 	cacheState = CacheLoad;
				// 	StartCoroutine(LoadAssetBundle());
				// } else {
				// 	cacheState = CacheDownloading;
				// 	StartCoroutine(LoadAssetBundle());
				// }
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
				// Debug.Log("cacheState:" + cacheState);
				break;
			}
		}
	}

	private string Combine (string [] paths) {
		string combinedPath = "";
		foreach (var path in paths) {
			combinedPath = Path.Combine(combinedPath, path);
		}
		return combinedPath;
	}



}
