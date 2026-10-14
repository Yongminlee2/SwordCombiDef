using UnityEditor;
using UnityEngine;

namespace Swordstellation.Editor
{
    public sealed class GeneratedArtImporter : AssetPostprocessor
    {
        public override uint GetVersion()
        {
            return 2;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art/"))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize =
                assetPath.Contains(
                    "/UI/Battle/StarlightCorridorArena")
                    ? 2048
                    : 1024;

            if (assetPath.Contains("/UI/Battle/ButtonFrame"))
            {
                importer.spriteBorder = new Vector4(
                    220,
                    110,
                    220,
                    110);
            }
            else if (assetPath.Contains(
                         "/UI/Battle/PanelFrameBlueGold"))
            {
                importer.spriteBorder = new Vector4(
                    185,
                    185,
                    185,
                    185);
            }
            else if (assetPath.Contains(
                         "/UI/Battle/EnemyGaugeFrame"))
            {
                importer.spriteBorder = new Vector4(
                    210,
                    60,
                    210,
                    60);
            }
        }
    }
}
