using UnityEngine;

using System;
using System.IO;
using System.Collections;

public class Downloader : MonoBehaviour {
	/*
		AssetBundleから取得できるリソース名(ファイル自体はjpg)
	*/
	const string bundleResourceName1 = "sushi";
	const string bundleResourceName2 = "udon";

	// AssetBundleのファイル名
	const string constBundleName = "SampleAssetBundle.unity3d";

	// AssetBundle化したファイルを置く場所
	const string outputBasePath = "bundlized1";

	// ファイルプロトコル
	const string fileProtocolStr = "file://";

	// AssetBundleのダウンロードを開始する
	void Start () {
		StartCoroutine(DownloadAssetBundleThenCache());
	}


	/**
		AssetBundleのダウンロードとキャッシュを行う
	*/
	IEnumerator DownloadAssetBundleThenCache () {
		Debug.Log("start DownloadAssetBundleThenCache");
		
		if (!Caching.ready) yield return null;

		// 毎回ダウンロードから実行するために、キャッシュをすべて消す
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
		
		while (!Caching.IsVersionCached(url, version)) {
			// 重量のあるリソースだと、キャッシュ完了までに時間がかかるので、cachedになるまで待つ
			yield return null;
		}
		Debug.Log("cached!!");
		
		var assetBundle = www.assetBundle;

		// assetBundleを開放
		assetBundle.Unload(false);

		www.Dispose();


		// キャッシュからAssetBundleを読み出す
		StartCoroutine(LoadCachedBundle());
	}


	/**
		キャッシュからリソースの読み込みを行う
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
			AssetBundleからリソースを読み出す。

			読み出しにはAssetBundleに入っているリソースの名前を使用できる。
		*/
		Texture2D sushiTexture = assetBundle.LoadAsset(bundleResourceName1) as Texture2D;
		Debug.Log("sushiTexture:" + sushiTexture.name);

		Texture2D udonTexture = assetBundle.LoadAsset(bundleResourceName2) as Texture2D;
		Debug.Log("udonTexture:" + udonTexture.name);


		{
			var sushiCube = GameObject.Find("SushiCube");
			var renderer = sushiCube.GetComponent<MeshRenderer>();
			renderer.material.mainTexture = sushiTexture;
		}

		{
			var udonCube = GameObject.Find("UdonCube");
			var renderer = udonCube.GetComponent<MeshRenderer>();
			renderer.material.mainTexture = udonTexture;
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
			FULLPATH_OF_PROJECT_FOLDER/outputBasePath/bundleName
	*/
	private string GetFileProtocolUrl () {
		// プロジェクトのパスを取得
		var dataPath = Application.dataPath;
		var projectPath = Directory.GetParent(dataPath).ToString();

		var localUrlArray = new string[]{projectPath, outputBasePath, constBundleName};
		return fileProtocolStr + CombineAllPath(localUrlArray);
	}

}
