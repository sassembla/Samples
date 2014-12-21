using UnityEngine;

public class FileCache {
	public static string LoadSavedLocalList () {
		return "{\"res_ver\":0, \"assetBundles\":[]}";
	}

	public static string UpdateList (string data) {
		Debug.Log("UpdateList:" + data);
		// 起動するたび消すのが面倒くさいので特に何もしません、保存したことにして、そのまま入力データを返します。
		return data;
	}
}