using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class AssetBundleGenerator {
	/*
		Resourcesから取得できるResource名(ファイル自体はjpg)
	*/
	static string bundleResourceName1 = "sushi";
	static string bundleResourceName2 = "udon";

	// AssetBundleになったときの名前
	static string bundleName = "SampleAssetBundle.unity3d";

	// AssetBundle化したファイルを置く場所
	static string outputBasePath1 = "bundlized1";
	
	// AssetBundle化したファイルと、そのデータが入ったリストを置く場所
	static string outputBasePath2 = "bundlized2";
	
	// リストのファイル名
	static string listName = "list.json";

	
	[MenuItem ("Window/Generate AssetBundle", false, 1)] static void GenerateAssetBundle () {
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


		// AssetBundleそれ自体のファイル名を加えたパス(この場合、PROJECT_FOLDER/outputBasePath1/bundleName)
		var assetBundleOutputPath = Path.Combine(outputBasePath1, bundleName);

		uint crc;

		// assetBundleResource1, assetBundleResource2 を含んだAssetBundleを、assetBundleOutputPathに出力します
		BuildPipeline.BuildAssetBundle(
			assetBundleResource1,
			new UnityEngine.Object[]{assetBundleResource2},
			assetBundleOutputPath,
			out crc,
			BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets,
			targetPlatform
		);

		Debug.Log("crc:" + crc);

		if (File.Exists(assetBundleOutputPath)) {
			Debug.Log("AssetBundle generated:" + assetBundleOutputPath);
		} else {
			Debug.Log("failed to generate AssetBundle.");
		}
		
	}

	
	[MenuItem ("Window/Generate AssetBundle and List", false, 1)] static void GenerateAssetBundleAndList () {
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


		// AssetBundleそれ自体のファイル名を加えたパス(この場合、PROJECT_FOLDER/outputBasePath2/bundleName)
		var assetBundleOutputPath = Path.Combine(outputBasePath2, bundleName);

		uint crc;

		// assetBundleResource1, assetBundleResource2 を含んだAssetBundleを、assetBundleOutputPathに出力します
		BuildPipeline.BuildAssetBundle(
			assetBundleResource1,
			new UnityEngine.Object[]{assetBundleResource2},
			assetBundleOutputPath,
			out crc,
			BuildAssetBundleOptions.None,
			targetPlatform
		);

		Debug.Log("crc:" + crc);

		if (File.Exists(assetBundleOutputPath)) {
			Debug.Log("AssetBundle generated:" + assetBundleOutputPath);
		} else {
			Debug.Log("failed to generate AssetBundle.");
		}


		// リストを作成します。
		// 次のようなJSONで出力するつもりなので、使用するパラメータを取得します。
		/*
		{
			"res_ver": 1,
			"assetBundles": [
				{
					"bundleName": "SampleAssetBundle.unity3d",
					"size": 100,
					"version": 1,
					"crc": 100,
					"resourceNames": ["sushi", "udon"]
				}
			]
		}
		*/
		
		FileInfo fileInfo = new FileInfo(assetBundleOutputPath);
		var size = (int)fileInfo.Length;

		/*
			JSON化するデータを作成
		*/

		// 含まれているassetの名前一覧を用意。
		var bundledAssetNames = new string[]{bundleResourceName1, bundleResourceName2};

		// このデモコードではこのAssetBundleのバージョンを1に指定している。
		var version = 1;
		
		// AssetBundleのデータを用意。
		var bundleData = new BundleData(bundleName, size, version, crc, bundledAssetNames);
		
		/*
			このAssetBundleを入れるリストを作成する。
		*/

		// リストのバージョンを1に指定
		var res_ver = "1";
		
		// 含むAssetBundleのリストを作成
		var assetBundlesList = new List<BundleData>{bundleData};

		// リストデータを作成。
		var listData = new ListData(res_ver, assetBundlesList);
		
		// json化
		var jsonStr = JsonUtility.ToJson(listData);

		// output
		var listOutputPath = Path.Combine(outputBasePath2, listName);
		using (StreamWriter file = new StreamWriter(listOutputPath)) {
			file.WriteLine(jsonStr);
		}

		if (File.Exists(listOutputPath)) {
			Debug.Log("list generated:" + listOutputPath);
		} else {
			Debug.Log("failed to generate list.");
		}
	}


}
