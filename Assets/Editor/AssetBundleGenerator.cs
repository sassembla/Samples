using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections;

public class AssetBundleGenerator {
	/*
		Resourcesから取得できるResource名(ファイル自体はjpg)
	*/
	static string bundleResourceName1 = "sushi";
	static string bundleResourceName2 = "udon";

	// AssetBundleになったときの名前
	static string bundleName = "SampleAssetBundle.unity3d";

	// AssetBundle化したファイルを置く場所
	static string outputBasePath = "bundlized1";
	
	
	[MenuItem ("Window/Generate AssetBundle", false, 1)]
	static void GenerateAssetBundle () {
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
}
