using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SmartRooms.Utils
{
    public static class SmartScriptableObjectUtils
    {
#if UNITY_EDITOR
        [MenuItem("Assets/Create/SmartRooms/CreateRuleTiles", false, 1)]
        private static void OnContextMenuClicked()
        {
            CreateRuleTiles();
        }

        /// <summary>
        /// Creates a rule tiles. When a sprite or multiple sprites are selected then the one rule tile for each sprite is generated.
        /// </summary>
        private static void CreateRuleTiles()
        {
            string stylePath = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());

            if (Selection.activeObject is Texture2D)
            {
                int len = stylePath.LastIndexOf("/", StringComparison.Ordinal) + 1;
                string filePathWithName = stylePath[..len];

                List<Sprite> sprites = new();
                foreach (string guid in Selection.assetGUIDs)
                {
                    string pathToObject = AssetDatabase.GUIDToAssetPath(guid);
                    sprites.AddRange(AssetDatabase.LoadAllAssetRepresentationsAtPath(pathToObject).Select(item => (Sprite)item));
                }

                foreach (Sprite sprite in sprites)
                {
                    CreateSmartTileForSprite(sprite, filePathWithName);
                }
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Creates a rule tiles with target sprite.
        /// </summary>
        private static void CreateSmartTileForSprite(Sprite sprite, string filePathWithName)
        {
            RuleTile tileAsset = ScriptableObject.CreateInstance<RuleTile>();
            tileAsset.m_DefaultSprite = sprite;

            string tilePath = filePathWithName + sprite.name + "RuleTile" + ".asset";
            string tileUniquePath = AssetDatabase.GenerateUniqueAssetPath(tilePath);
            AssetDatabase.CreateAsset(tileAsset, tileUniquePath);
        }
#endif
    }
}