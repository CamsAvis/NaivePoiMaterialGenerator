#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CreatePoiMaterialGenerator
{
	static string filename = "Assets/Scripts/Editor/CreatePoiMaterialMenu.cs";
	static string foldername = "Assets/Scripts/Editor";
	static bool enabled = false;

	public static List<Shader> GetPoiyomiShaders()
	{
		List<string> poiShaders = ShaderUtil.GetAllShaderInfo()
					.Select(info => info.name)
					.Where(name => !name.StartsWith("Hidden/") && name.IndexOf("poiyomi", StringComparison.CurrentCultureIgnoreCase) != -1)
					.ToList();

		return poiShaders.Select(name => Shader.Find(name)).ToList();
	}

	// add a top bar menu item to toggle this effect
	[MenuItem("Cam/Tools/Retrieve Poi Shader")]
	private static void RetrivePoiShaders()
	{
		Retrieve();
		// Your code to turn the Poi Shader Detection on
		Debug.Log("Retrieved Poi Shaders and generated script");
	}


	// add a top bar menu item to toggle this effect
	[MenuItem("Cam/Tools/Toggle Poi Shader Detection/On")]
	private static void TogglePoiShaderOn()
	{
		enabled = true;
		Retrieve();
		// Your code to turn the Poi Shader Detection on
		Debug.Log("Poi Shader Detection On");
	}

	[MenuItem("Cam/Tools/Toggle Poi Shader Detection/Off")]
	private static void TogglePoiShaderOff()
	{
		enabled = false;
		// Your code to turn the Poi Shader Detection off
		Debug.Log("Poi Shader Detection Off");
	}

	// This will update the checkbox state based on the enabled value
	[MenuItem("Cam/Tools/Toggle Poi Shader Detection/On", true)]
	private static bool IsPoiShaderOn()
	{
		return enabled; // Return the current state to show whether it's checked or not
	}

	[MenuItem("Cam/Tools/Toggle Poi Shader Detection/Off", true)]
	private static bool IsPoiShaderOff()
	{
		return !enabled; // Inverse of the On state
	}


	[InitializeOnLoadMethod]
	private static void OnLoad() {
		if (enabled) {
			Retrieve();
		}
	}

	private static void Retrieve() { 
		if (!System.IO.Directory.Exists(foldername)) {
			System.IO.Directory.CreateDirectory(foldername);
		}

		Debug.Log("Generating Poi Material Menu");
		List<Shader> PoiyomiShaders = GetPoiyomiShaders();
		Debug.Log("Found " + PoiyomiShaders.Count + " Poiyomi Shaders");

		string GeneratedCode = @"
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public static class CreatePoiMaterialMenu {
	static Dictionary<string,Shader> PoiyomiShaders = new Dictionary<string, Shader>();

	[InitializeOnLoadMethod]
	private static void OnLoad() => LoadPoiyomiShaders();

	public static void LoadPoiyomiShaders()
	{
		List<string> poiShaders = ShaderUtil.GetAllShaderInfo()
					.Select(info => info.name)
					.Where(name => !name.StartsWith(""Hidden/"") && name.IndexOf(""poiyomi"", StringComparison.CurrentCultureIgnoreCase) != -1)
					.ToList();
		
		foreach(string shaderName in poiShaders) {
			Shader shader = Shader.Find(shaderName);
			PoiyomiShaders.Add(shaderName, shader);
		}
	}

	public static string GeneratePath() {
		// Get the currently selected asset's path
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);

		// If the selected item is a file, use its parent directory
		if (!string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path))
		{
			// If it's a folder, use its path
			path = AssetDatabase.GenerateUniqueAssetPath($""{path}/NewPoiMaterial.mat"");
		}
		else if (!string.IsNullOrEmpty(path))
		{
			// If it's a file, get the parent directory of the file
			string directory = System.IO.Path.GetDirectoryName(path);
			path = AssetDatabase.GenerateUniqueAssetPath($""{directory}/NewPoiMaterial.mat"");
		}

		return path;
	}

	### GENERATED CODE ###

}
#endif
		";

		int i = 0;
		string code = "";
		foreach(Shader shader in PoiyomiShaders) {
			string shaderName = shader.name;
			string menuPath = shaderName.Replace(".poiyomi/", "");

			code += $@"
[MenuItem(""Assets/Create/Material (Poiyomi)/{menuPath}"", priority=301)]
private static void CreatePoiMaterial{i}()
{{
	Shader shader;
	PoiyomiShaders.TryGetValue(""{shaderName}"", out shader);

	if(shader == null) return;

	Material material = new Material(shader);

	// get the currently clicked asset database location
	string path = AssetDatabase.GetAssetPath(Selection.activeObject);

	path = GeneratePath();

	AssetDatabase.CreateAsset(material, path);
	AssetDatabase.ImportAsset(path);
	AssetDatabase.SaveAssets();
	AssetDatabase.Refresh();

	EditorUtility.FocusProjectWindow();
	Selection.activeObject = material;
}}
			
";
			i++;
		}

		GeneratedCode = GeneratedCode.Replace("### GENERATED CODE ###", $"/* ### GENERATED CODE ### */\r\n\r\n{code}");

		// write generated code to a text file in the project
		System.IO.File.WriteAllText(filename, GeneratedCode);
		AssetDatabase.Refresh();

		// request script reload
		AssetDatabase.ImportAsset(filename);
	}
}
#endif