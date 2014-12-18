using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections;

public class Loader : MonoBehaviour {
	/*
		Resourcesから取得できるResource名(ファイル自体はjpg)
	*/
	static string bundleResourceName1 = "sushi";
	static string bundleResourceName2 = "udon";

	// AssetBundleになったときの名前
	static string bundleName = "SampleAssetBundle.unity3d";

	// AssetBundle化したファイルを置く場所
	static string outputBasePath = "bundlized";

	// AssetBundleをDownloadするためのurl
	static string url = Path.Combine("file:///", bundleName);

	

	
	void GenerateAssetBundle () {

		/*
			assetBundleResource1, assetBundleResource2に、
			ResourcesからResourceを読み出す
		*/
		var assetBundleResource1 = Resources.Load(bundleResourceName1);
		if (assetBundleResource1 != null) {
			Debug.Log("assetBundleResource1:" + assetBundleResource1.name);
		}

		var assetBundleResource2 = Resources.Load(bundleResourceName2);
		if (assetBundleResource2 != null) {
			Debug.Log("assetBundleResource2:" + assetBundleResource2.name);
		}



		// ターゲットプラットフォームの設定(現在のエディタの環境のものを読み込んで使用しています)
		BuildTarget targetPlatform = EditorUserBuildSettings.activeBuildTarget;


		// AssetBundleそれ自体のファイル名を加えたパス(この場合、PROJECT_FOLDER/outputBasePath/bundleName)
		var assetBundleOutputPath = Path.Combine(outputBasePath, bundleName);

		uint crc;

		BuildPipeline.BuildAssetBundle(
			assetBundleResource1,
			new UnityEngine.Object[]{assetBundleResource2},
			assetBundleOutputPath,
			out crc,
			BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets,
			targetPlatform
		);

		Debug.Log("crc:" + crc);
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
				Debug.Log("cacheState:" + cacheState);
				break;
			}
		}
	}



}
