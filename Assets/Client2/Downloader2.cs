using UnityEngine;

using System;
using System.IO;
using System.Collections;

public class Downloader2 : MonoBehaviour {
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

	// AssetBundleのDownloadを開始する
	void Start () {
		// Downloadを開始する
		StartCoroutine(DownloadAssetBundleThenCache());
	}


	/**
		CacheからResourceの読み込みを行う
	*/
	IEnumerator DownloadAssetBundleThenCache() {
		Debug.Log("start DownloadAssetBundleThenCache");
		
		if (!Caching.ready) yield return null;

		// 毎回Downloadから実行するために、Cacheをすべて消す
		Caching.CleanCache();


		string url = GetFileProtocolUrl();
		int version = 1;
		uint crc = 0;

		// AssetBundleをファイルプロトコルで取得
		var www = WWW.LoadFromCacheOrDownload(url, version, crc);

		yield return www;

		if (!String.IsNullOrEmpty(www.error)) {
			Debug.LogError("DownloadAssetBundleThenCache www.error:" + www.error);
			yield break;
		}
		
		if (!Caching.IsVersionCached(url, version)) {
			// 重量のあるResourceだと、Cacheまでに時間がかかるので、cachedになるまで待つ
			yield return null;
		}
		Debug.Log("cached!!");
		
		var assetBundle = www.assetBundle;

		// assetBundleを開放
		assetBundle.Unload(false);

		www.Dispose();


		// CacheからAssetBundleを読み出す
		StartCoroutine(LoadCachedBundle());
	}


	/**
		CacheからResourceの読み込みを行う
	*/
	IEnumerator LoadCachedBundle() {
		Debug.Log("start LoadCachedBundle");

		string url = GetFileProtocolUrl();
		int version = 1;
		uint crc = 0;

		// AssetBundleをファイルプロトコルで取得
		var www = WWW.LoadFromCacheOrDownload(url, version, crc);

		yield return www;

		if (!String.IsNullOrEmpty(www.error)) {
			Debug.LogError("LoadCachedBundle www.error:" + www.error);
			yield break;
		}

		// wwwからAssetBundleを読み出す
		var assetBundle = www.assetBundle;
		www.Dispose();


		/*
			www.assetBundleからAssetBundleを読み出し、
			AssetBundleからResourceを読み出す。

			AssetBundleに入っているResourceの名前を使用できる。
		*/
		Texture2D sushiTexture = assetBundle.Load(bundleResourceName1) as Texture2D;
		Debug.Log("sushiTexture:" + sushiTexture.name);

		Texture2D udonTexture = assetBundle.Load(bundleResourceName2) as Texture2D;
		Debug.Log("udonTexture:" + udonTexture.name);


		{
			var sushiCube = GameObject.Find("SushiCube");
			var renderer = sushiCube.GetComponent<MeshRenderer>();
			renderer.material.mainTexture = sushiTexture;
		}

		{
			var udonCube = GameObject.Find("UdonCube");
			var renderer = udonCube.GetComponent<MeshRenderer>();
			renderer.material.mainTexture = sushiTexture;
		}
	}



	/**
		Path.CombineのArray版
	*/
	private string CombineAllPath (string [] paths) {
		string combinedPath = "";
		foreach (var path in paths) {
			combinedPath = Path.Combine(combinedPath, path);
		}
		return combinedPath;
	}

	/**
		file://プロトコルのurlを返す
			FULLPATH_OF_PROJECT_FOLDER/bundlize/bundleName
	*/
	private string GetFileProtocolUrl () {
		// プロジェクトのパスを取得
		var dataPath = Application.dataPath;
		var projectPath = Directory.GetParent(dataPath).ToString();

		var localUrlArray = new string[]{projectPath, outputBasePath, bundleName};
		return fileProtocolStr + CombineAllPath(localUrlArray);
	}

}
