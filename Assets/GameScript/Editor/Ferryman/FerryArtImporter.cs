using UnityEditor;
using UnityEngine;

namespace Hotfix.EditorTools
{
	/// <summary>
	/// 过河美术导入规则：像素风的整图切帧素材，禁止压缩/缩放，否则帧矩形会错位
	/// </summary>
	public class FerryArtImporter : AssetPostprocessor
	{
		private const string EntityRoot = "Assets/GameRes/Ferryman/Entity/";

		private void OnPreprocessTexture()
		{
			if (assetPath.StartsWith(EntityRoot) == false)
				return;

			TextureImporter importer = assetImporter as TextureImporter;
			if (importer == null)
				return;

			importer.textureType = TextureImporterType.Default;
			importer.textureShape = TextureImporterShape.Texture2D;
			importer.mipmapEnabled = false;
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.filterMode = FilterMode.Point;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.maxTextureSize = 4096;
			importer.alphaIsTransparency = true;
			importer.sRGBTexture = true;
			importer.npotScale = TextureImporterNPOTScale.None;
		}

		[MenuItem("Tools/Ferryman/2. 重导过河美术", false, 2)]
		public static void ReimportAll()
		{
			string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { EntityRoot.TrimEnd('/') });
			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
			}
			Debug.Log($"过河美术重导完成，共 {guids.Length} 张");
		}
	}
}
