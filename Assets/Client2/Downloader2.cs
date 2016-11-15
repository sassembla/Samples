using UnityEngine;

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Downloader2 : MonoBehaviour {
	/*
		AssetBundleから取得できるリソース名(ファイル自体はjpg)
	*/
	const string bundleResourceName1 = "sushi";
	const string bundleResourceName2 = "udon";
	
	// AssetBundle化したファイルを置く場所
	const string outputBasePath = "bundlized2";

	// ファイルプロトコル
	const string fileProtocolStr = "file://";

	// リストのファイル名
	const string listName = "list.json";



	/*
		既に一度そのパーツがロードされた事が有るassetBundleのキャッシュ
		assetBundleName : assetBundleObject
	*/
	private static Dictionary<string, AssetBundle> onMemoryAssetBundleDict = new Dictionary<string, AssetBundle>();

	/*
		assetBundleに含まれているresourceの名称 - assetBundleの名称 の辞書
		assetBundled resourceName : assetBundleName
	*/
	private static Dictionary<string, string> bundledResourceNamesDict;

	/*
		bundleName : AssetBundleの情報 が入っている辞書
	*/
	private static Dictionary<string, BundleData> assetBundlesDict;


	/*
		assetBundleをcacheからloadする際に、現在loading中のassetBundleNameを保持するリスト
	*/
	private static List<string> cachedAssetBundleLoadingList = new List<string>();



	// リストのダウンロードを開始する
	void Start () {

		
		// 毎回ダウンロードから実行するために、キャッシュをすべて消す
		Caching.CleanCache();

		StartCoroutine(DownloadList());
	}


	/**
		リストのダウンロードを行う。

		・リストを取得(以降取得したリストを remoteリスト　と呼ぶ)
		・現在クライアント内に保存されている リスト(以降localリストと呼ぶ)と、remoteリストの res_ver を比較
		・res_ver が上がっている場合、localリストに載っていないAssetBundleと、remoteリストで更新されているAssetBundleの取得を開始する
	*/
	IEnumerator DownloadList() {
		Debug.Log("start DownloadList");
		
		if (!Caching.ready) yield return null;

		string url = GetFileProtocolUrlForList();
		
		var www = new WWW(url);

		yield return www;

		if (!String.IsNullOrEmpty(www.error)) {
			Debug.LogError("DownloadList www.error:" + www.error);
			yield break;
		}

		var remoteListDataStr = www.text;

		// 取得したリストの情報を、"remoteから取得したデータ"として扱う
		var remoteListData = JsonUtility.FromJson<ListData>(remoteListDataStr);

		string remote_res_ver = remoteListData.res_ver;

		// localに保存してあるリストを読み出す
		var localSavedListStr = FileCache.LoadSavedLocalList();
		var localListData = JsonUtility.FromJson<ListData>(localSavedListStr);


		string local_res_ver = localListData.res_ver;

		// res_verに差があったら、内容を確認、ダウンロードすべきAssetBundleがあったらダウンロードする。
		if (local_res_ver != remote_res_ver) {
			Debug.Log("updated!");

			var remoteAssetBundlesInfoList = remoteListData.assetBundles;
			var localAssetBundlesInfoList = localListData.assetBundles;

			var localAssetBundlesDict = GetContainedAssetBundleDictionary(localAssetBundlesInfoList);
			var remoteAssetBundlesDict = GetContainedAssetBundleDictionary(remoteAssetBundlesInfoList);
			

			// remoteにあってlocalに無い bundleName を列挙
			var localContainedBundlesNames = localAssetBundlesDict.Values.Select(bundleData => bundleData.bundleName).ToList();
			var remoteContainedBundlesNames = remoteAssetBundlesDict.Values.Select(bundleData => bundleData.bundleName).ToList();

			var notContainedBundleNamesInLocal = remoteContainedBundlesNames.Except(localContainedBundlesNames).ToList();
			

			// remote, localの両方に含まれていて、 version が異なる bundleName を列挙
			var bothContainedButVersionChanged = GetVersionGainedBundleNames(localAssetBundlesDict, remoteAssetBundlesDict);

			// 名前を元に、ダウンロードすべきAssetBundleの取得を行う。
			var shouldDownloadAssetBundleNames = notContainedBundleNamesInLocal.Concat(bothContainedButVersionChanged).ToList();

			var loadingAssetBundleNames = new List<string>(shouldDownloadAssetBundleNames);

			Action<string> completed = (string cacheCompletedBundleName) => {
				if (loadingAssetBundleNames.Contains(cacheCompletedBundleName)) loadingAssetBundleNames.Remove(cacheCompletedBundleName);
				if (!loadingAssetBundleNames.Any()) {
					Debug.Log("download completed.");
					AllAssetBundleCached(remoteListDataStr);
				}
			};

			foreach (var bundleName in shouldDownloadAssetBundleNames) {
				var bundleData = remoteAssetBundlesDict[bundleName];
				StartCoroutine(DownloadAssetBundleThenCache2(bundleData, completed));
			}
		}
	}


	IEnumerator DownloadAssetBundleThenCache2 (BundleData bundleData, Action<string> completed) {
		
		var bundleName = bundleData.bundleName;
		string url = GetFileProtocolUrlForAssetBundle(bundleName);
		int version = bundleData.version;
		uint crc = bundleData.crc;

		// AssetBundleをファイルプロトコルで取得
		var www = WWW.LoadFromCacheOrDownload(url, version, crc);

		yield return www;

		if (!String.IsNullOrEmpty(www.error)) {
			Debug.LogError("DownloadAssetBundleThenCache2 www.error:" + www.error);
			yield break;
		}
		
		while (!Caching.IsVersionCached(url, version)) {
			// 重量のあるリソースだと、キャッシュまでに時間がかかるので、cachedになるまで待つ
			yield return null;
		}
		var assetBundle = www.assetBundle;

		// assetBundleを開放
		assetBundle.Unload(false);

		www.Dispose();

		completed(bundleName);
	}

	/**
		すべてのダウンロードすべきAssetBundleのダウンロードが完了し、キャッシュされたら呼ばれる。
	*/
	private void AllAssetBundleCached (string remoteListDataStr) {
		// 取得したリスト remoteリスト の内容をすべてキャッシュし終わったので、 remoteリスト をクライアント内に保存する。
		var saved = FileCache.UpdateList(remoteListDataStr);
		

		/*
			新しくなったリストを使って、AssetBundleからリソースを取得する
		*/
		var newLocalListDataStr = saved;
		
		var newLocalListData = JsonUtility.FromJson<ListData>(newLocalListDataStr);
		var newLocalAssetBundlesInfoList = newLocalListData.assetBundles;

		{
			// AssetBundle名 : AssetBundle情報 の辞書を更新
			assetBundlesDict = GetContainedAssetBundleDictionary(newLocalAssetBundlesInfoList);

			// resourceName : bundleName 辞書の更新
			bundledResourceNamesDict = GetAssetbundledResourceNamesDict(assetBundlesDict);
		}
		
		// AssetBundleに含まれている resourceName から、AssetBundleを取得してみます。
		StartCoroutine(LoadCachedBundle2(
			bundleResourceName1,
			(string loadSucceededResourceName, Texture2D t) => {
				var sushiCube = GameObject.Find("SushiCube");
				var renderer = sushiCube.GetComponent<MeshRenderer>();
				renderer.material.mainTexture = t;
			},
			(string loadFailedResourceName, string reason) => {
				Debug.LogError("loadFailedResourceName:"+loadFailedResourceName + " /reason:" + reason);
			}
		));

		StartCoroutine(LoadCachedBundle2(
			bundleResourceName2,
			(string loadSucceededResourceName, Texture2D t) => {
				var udonCube = GameObject.Find("UdonCube");
				var renderer = udonCube.GetComponent<MeshRenderer>();
				renderer.material.mainTexture = t;
			},
			(string loadFailedResourceName, string reason) => {
				Debug.LogError("loadFailedResourceName:"+loadFailedResourceName + " /reason:" + reason);
			}
		));
	}


	/**
		キャッシュからリソースの読み込みを行う
	*/
	IEnumerator LoadCachedBundle2 <T> (
		string resourceName,
		System.Action<string, T> succeeded,
		System.Action<string, string> failed) where T : UnityEngine.Object {

		Debug.Log("start LoadCachedBundle2 for Resource:" + resourceName);

		/*
			resourceName がリスト由来の情報に含まれている場合
		*/		
		if (bundledResourceNamesDict.ContainsKey(resourceName)) {
			var bundleName = bundledResourceNamesDict[resourceName];

			/*
				要求されたリソースが入っているAssetBundleが、
				現在取得中のAssetBundleだったら、現在先行している取得が終わるまで待つ。
			*/
			while (cachedAssetBundleLoadingList.Contains(bundleName)) {
				Debug.Log("belonging assetBundle is loading. assetBundleName:" + bundleName + " resourceName:" + resourceName);
				yield return null;
			}

			
			Debug.Log("loading from cache already done or failed. assetBundleName:" + bundleName + " resourceName:" + resourceName);

			/*
				メモリ内に展開されていれば読み出す。
			*/
			if (onMemoryAssetBundleDict.ContainsKey(bundleName)) {
				var assetBundle1 = onMemoryAssetBundleDict[bundleName];
				var loadedResource1 = (T)assetBundle1.LoadAsset(resourceName, typeof(T));

				if (loadedResource1 == null) {
					failed(resourceName, "resouce-is-null-in-assetBundle-1:" + bundleName);
					yield break;
				}

				Debug.Log("load from cache done resourceName:" + resourceName);
				succeeded(resourceName, loadedResource1);
				yield break;
			}

			/*
				assetBundleがまだキャッシュからロードされていない場合、
				キャッシュからロードを行い、リソースを取り出して返す。

				読み出すための情報はすべてリストから取得できる。
			*/
			string url = GetFileProtocolUrlForAssetBundle(bundleName);
			int version = assetBundlesDict[bundleName].version;
			uint crc = assetBundlesDict[bundleName].crc;

			var isCached = Caching.IsVersionCached(url, version);
			if (!isCached) {
				failed(resourceName, "assetBundle-not-cached:" + url + " maybe expired.");
				yield break;
			}

			/* 
				loading control block.
				
				ひとつのAssetBundleに入っているリソースを取得中、おなじAssetBundleに入っている別リソースを取得しにくると、
	
				"Cannot load cached AssetBundle. A file of the same name is already loaded from another AssetBundle. "
				
				というエラーが出るので、読み出し中のAssetBundleであれば、 bundleName を cachedAssetBundleLoadingList に入れて避ける必要がある。
				読み出しが完了したら bundleName を cachedAssetBundleLoadingList から消す。
			*/
			{
				// 読み込みが完了するまで読み込み中リストに入れる
				cachedAssetBundleLoadingList.Add(bundleName);
				
				// chacheから読み出す
				WWW www = WWW.LoadFromCacheOrDownload(url, version, crc);
				yield return www;

				if (!String.IsNullOrEmpty(www.error)) {
					if (cachedAssetBundleLoadingList.Contains(bundleName)) cachedAssetBundleLoadingList.Remove(bundleName);
					failed(resourceName, "www-error:" + www.error);
				}
				
				// 読み込みが完了したので読み込み中リストから外す
				if (cachedAssetBundleLoadingList.Contains(bundleName)) cachedAssetBundleLoadingList.Remove(bundleName);

				var assetBundle2 = www.assetBundle;
				
				// memory cache
				onMemoryAssetBundleDict[bundleName] = assetBundle2;


				var loadedResource2 = (T)assetBundle2.LoadAsset(resourceName, typeof(T));
				
				if (loadedResource2 == null) {
					if (cachedAssetBundleLoadingList.Contains(bundleName)) cachedAssetBundleLoadingList.Remove(bundleName);
					failed(resourceName, "resouce-is-null-in-assetBundle-2:" + bundleName);
					yield break;
				}

				succeeded(resourceName, loadedResource2);
				yield break;
			}
		}

		/*
			リストの情報に resourceName が含まれていないため、
		*/
		failed(resourceName , "selected resourceName is not contained in 'bundledResourceNamesDict'");
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
		listName で終わる、file://プロトコルのurlを返す
			FULLPATH_OF_PROJECT_FOLDER/outputBasePath/listName
	*/
	private string GetFileProtocolUrlForList () {
		// プロジェクトのパスを取得
		var dataPath = Application.dataPath;
		var projectPath = Directory.GetParent(dataPath).ToString();

		var localUrlArray = new string[]{projectPath, outputBasePath, listName};
		return fileProtocolStr + CombineAllPath(localUrlArray);
	}

	/**
		assetBundleFileName で終わる、file://プロトコルのurlを返す
			FULLPATH_OF_PROJECT_FOLDER/outputBasePath/assetBundleFileName
	*/
	private string GetFileProtocolUrlForAssetBundle (string bundleName) {
		// プロジェクトのパスを取得
		var dataPath = Application.dataPath;
		var projectPath = Directory.GetParent(dataPath).ToString();

		var localUrlArray = new string[]{projectPath, outputBasePath, bundleName};
		return fileProtocolStr + CombineAllPath(localUrlArray);
	}

	/**
		リストからAssetBundle名の列挙されたListを返す
	*/
	private Dictionary<string, BundleData> GetContainedAssetBundleDictionary (List<BundleData> sourceList) {
		var resultDict = new Dictionary<string, BundleData>();

		foreach (var bundleData in sourceList) {
			var bundleName = bundleData.bundleName;
			resultDict[bundleName] = bundleData;
		}

		return resultDict;
	}

	/**
		version の更新があったAssetBundleの bundleName をListで返す
	*/
	private List<string> GetVersionGainedBundleNames (Dictionary<string, BundleData> local, Dictionary<string, BundleData> remote) {
		var versionGainedBundleNameList = new List<string>();
		var localContainedBundlesNames = local.Values.Select(bundleData => bundleData.bundleName).ToList();
		
		foreach (var assetBundleName in localContainedBundlesNames) {
			if (!remote.Keys.Contains(assetBundleName)) continue;

			var localVersion = local[assetBundleName].version;
			var remoteVersion = remote[assetBundleName].version;
			if (localVersion < remoteVersion) {
				versionGainedBundleNameList.Add(assetBundleName);
			}
		}

		return versionGainedBundleNameList;
	}

	/**
		{AssetBundle : resourcenames} x N から、
		resourceName : 所属しているAssetBundle の辞書を返す
	*/
	private static Dictionary<string, string> GetAssetbundledResourceNamesDict (Dictionary<string, BundleData> assetBundleList) {
		Dictionary<string, string> dict = new Dictionary<string, string>();

		foreach (var assetBundleName in assetBundleList.Keys) {
			foreach (var resourceName in assetBundleList[assetBundleName].resourceNames) {
				dict[resourceName] = assetBundleName;
			}
		}

		return dict;
	}
}
