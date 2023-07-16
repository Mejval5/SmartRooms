using System.Globalization;
using System.IO;
using SmartRooms.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace SmartRooms.Palette
{
    /// <summary>
    /// A tile which can contain two different game objects and switch between them when instantiated.
    /// Creates a sprite which shows the tiles and chances on it.
    /// </summary>
    [ExecuteAlways]
    [CreateAssetMenu(menuName = "SmartRooms/QuantumTile")]
    public class QuantumTile : SmartTile
    {
        [Range(0,100)] [SerializeField] private float _firstGameObjectChance;
        [SerializeField] private GameObject _secondGameObject;
        [SerializeField] private string _previousPath;

        /// <summary>
        /// Gets the tile data for this tile. Randomizes which game object it returns.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="tilemap"></param>
        /// <param name="tileData"></param>
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            base.GetTileData(position, tilemap, ref tileData);

            if (Random.Range(0, 100f) <= _firstGameObjectChance)
            {
                return;
            }

            tileData.gameObject = _secondGameObject;

            if ((flags & TileFlags.InstantiateGameObjectRuntimeOnly) == 0)
            {
                tileData.sprite = null;
            }
        }
        
#if UNITY_EDITOR
        protected void Awake()
        {
            EditorUtility.SetDirty(this);
        }

        public void SetChance(float chance)
        {
            _firstGameObjectChance = chance;
            EditorUtility.SetDirty(this);
        }

        public void FastValidate()
        {
            EditorApplication.delayCall -= LateValidate;
            EditorApplication.delayCall += LateValidate;
        }

        private void LateValidate()
        {
            EditorApplication.delayCall -= LateValidate;
            
            // Attempt to reconnect sprite after moving
            if (string.IsNullOrEmpty(_previousPath) == false && _previousPath != AssetDatabase.GetAssetPath(this))
            {
                AssetDatabase.Refresh();
                if (AssetDatabase.LoadAssetAtPath<QuantumTile>(_previousPath) == null)
                {
                    // If the tile was moved then move the sprite over
                    ReconnectSprite();
                }
                else
                {
                    // If the tile was duplicated then don't move the sprite over and generate it from scratch
                    sprite = null;
                }
                EditorUtility.SetDirty(this);
            }
            
            _previousPath = AssetDatabase.GetAssetPath(this);
            
            // Only generate sprite when something has changed in the tile
            if (EditorUtility.IsDirty(this) == false)
            {
                return;
            }

            // Get texture from the first game object's sprite renderer
            Texture2D one = null;
            if (gameObject != null)
            {
                SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    one = spriteRenderer.sprite.texture;
                }
            }

            // Get texture from the second game object's sprite renderer
            Texture2D two = null;
            if (_secondGameObject != null)
            {
                SpriteRenderer spriteRendererSecond = _secondGameObject.GetComponent<SpriteRenderer>();
            
                if (spriteRendererSecond != null && spriteRendererSecond.sprite != null)
                {
                    two = spriteRendererSecond.sprite.texture;
                }
            }

            // Create a tile texture by combining both textures together
            Texture2D tex = CombineTextures(one, two);

            // Create a sprite for the tile
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2f, tex.width);

            // Serialize the created sprite to the disk
            SerializePNG();
            GetReference();
            AssetDatabase.SaveAssetIfDirty(this);
        }
        
        /// <summary>
        /// Creates a quantum tile texture by combining two textures together and adds text on the final result.
        /// </summary>
        /// <param name="firstTileTexture"></param>
        /// <param name="secondTileTexture"></param>
        /// <returns></returns>
        private Texture2D CombineTextures(Texture2D firstTileTexture, Texture2D secondTileTexture)
        {
            const int res = 128;
            Texture2D finalTexture = new (res, res);
            for (int x = 0; x < res; x++)
            for (int y = 0; y < res; y++)
            {
                int _x, _y;
                
                // Don't draw if it should draw the second one, or first is missing or outside of its bounds
                if (firstTileTexture != null && (y > x || secondTileTexture == null) && x < firstTileTexture.width && y < firstTileTexture.height)
                {
                    _x = (int)(x * firstTileTexture.width / (float) res);
                    _y = (int)(y * firstTileTexture.height / (float) res);
                    finalTexture.SetPixel(x, y, firstTileTexture.GetPixel(_x, _y));
                    continue;
                }

                // Don't draw if second is missing or outside of its bounds
                if (secondTileTexture == null || x >= secondTileTexture.width || y >= secondTileTexture.height)
                {
                    continue;
                }

                _x = (int)(x * secondTileTexture.width / (float) res);
                _y = (int)(y * secondTileTexture.height / (float) res);
                finalTexture.SetPixel(x, y, secondTileTexture.GetPixel(_x, _y));
            }

            int fontSize = SmartTileManager.Instance.DefaultFont.fontSize;

            // Draw percent chance for first tile
            if (firstTileTexture != null)
            {
                string chanceText = Mathf.RoundToInt(_firstGameObjectChance) + "%";
                
                // The font size and text position is important as it could fail if we try to write characters outside of the texture
                const int posX = 10;
                int posY = res - fontSize;
                finalTexture = DrawText(finalTexture, chanceText, posX, posY);
            }

            // Draw percent chance for second tile
            if (secondTileTexture != null)
            {
                string chanceText = Mathf.RoundToInt(100f - _firstGameObjectChance) + "%";
                
                // The font size and text position is important as it could fail if we try to write characters outside of the texture
                int posX = res - fontSize / 3 * 2 - fontSize * chanceText.Length / 2;
                const int posY = 10;
                finalTexture = DrawText(finalTexture, chanceText, posX, posY);
            }

            finalTexture.Apply();
            return finalTexture;
        }

        /// <summary>
        /// Draws text on a texture at position specified.
        /// </summary>
        /// <param name="finalTexture"></param>
        /// <param name="sText"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <returns></returns>
        private static Texture2D DrawText(Texture2D finalTexture, string sText, int posX, int posY)
        {
            // Convert text into char array
            CharacterInfo characterInfo;
            char[] charArray = sText.ToCharArray();
            
            // Get font
            Font myFont = SmartTileManager.Instance.DefaultFont;
            int fontSize = SmartTileManager.Instance.DefaultFont.fontSize;

            // Get font texture
            Material fontMat = myFont.material;
            Texture2D fontTx0 = (Texture2D)fontMat.mainTexture;

            // Create font texture which we can easily read from
            Texture2D fontTx = new(fontTx0.width, fontTx0.height, fontTx0.format, fontTx0.mipmapCount, false);
            Graphics.CopyTexture(fontTx0, fontTx);

            // Determine the size of the text bounding box
            int w1 = 0;
            int posX1 = 0;
            int h1 = 0;
            int wLast = 0;
            foreach (char t in charArray)
            {
                myFont.GetCharacterInfo(t, out characterInfo, fontSize);

                // Get the biggest height
                int h2 = characterInfo.glyphHeight;
                if (h2 > h1)
                {
                    h1 = h2;
                }
                
                // Get the last character's width
                w1 = characterInfo.glyphWidth;
                wLast = characterInfo.advance;
                
                // Advance to next character
                posX1 += wLast;
            }

            // Calculate the size of the text bounding box
            int height = h1;
            int width = w1 + posX1 - wLast;

            // Create black background by writing a block of pixels into the texture
            Color[] color = new Color[height * width];
            for (int i = 0; i < height * width; i++)
            {
                color[i] = Color.black;
            }
            
            finalTexture.SetPixels(posX, posY, width, h1, color);

            // Write the text
            foreach (char character in charArray)
            {
                // Get character info
                myFont.GetCharacterInfo(character, out characterInfo, fontSize);

                // Calculate the character size and position
                int x = (int)(fontTx.width * characterInfo.uvBottomLeft.x);
                int y = (int)(fontTx.height * characterInfo.uvTopLeft.y);
                int w = characterInfo.glyphWidth;
                int h = characterInfo.glyphHeight;

                // Flip the font pixels vertically
                Color[] fontPixels = fontTx.GetPixels(x, y, w, h);
                Color[] finalPixels = new Color[fontPixels.Length];
                for (int j = 0; j < fontPixels.Length; j++)
                {
                    int _x = j % w;
                    int _y = (fontPixels.Length - 1 - j) / w;
                    int a = _x + _y * w;
                    finalPixels[j] = new Color(fontPixels[a].a, fontPixels[a].a, fontPixels[a].a, 1);
                }
                
                finalTexture.SetPixels(posX, posY, w, h, finalPixels);

                // Move to next character
                posX += characterInfo.advance;
            }

            return finalTexture;
        }

        /// <summary>
        /// Saves the generated sprite texture to the disk.
        /// </summary>
        private void SerializePNG()
        {
            Texture2D tex = sprite.texture;
            byte[] exportObj = tex.EncodeToPNG();
            string pngName = AssetDatabase.GetAssetPath(this).Replace(".asset", "_TileTex") + ".png";
            File.WriteAllBytes(pngName, exportObj);
        }

        /// <summary>
        /// Attempts to get a reference to the associated sprite which was previously saved.
        /// </summary>
        private void GetReference()
        {
            AssetDatabase.Refresh();
            string spriteName = AssetDatabase.GetAssetPath(this).Replace(".asset", "_TileTex") + ".png";
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteName);
        }

        /// <summary>
        /// Attempts to move associated sprite to the same location after the tile was moved.
        /// </summary>
        private void ReconnectSprite()
        {
            string spriteName = AssetDatabase.GetAssetPath(sprite);
            string newName = AssetDatabase.GetAssetPath(this).Replace(".asset", "_TileTex") + ".png";
            AssetDatabase.MoveAsset(spriteName, newName);
            AssetDatabase.Refresh();
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(QuantumTile), true)]
    public class QuantumTileEditor : UnityEditor.Editor
    {
        private const float UpdateTimeout = 0.25f;
        private float _secondsSinceLastUpdate = UpdateTimeout * 10f;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            QuantumTile quantumTile = (QuantumTile)target;
            
            // Create chance presets
            GUILayout.BeginHorizontal();
            
            GUILayout.Label("Preset chances", GUILayout.Width((EditorGUIUtility.labelWidth)));

            float[] buttonOptions = { 25f, 50f, 75f, 100f };

            foreach (float buttonOption in buttonOptions)
            {
                if (GUILayout.Button(buttonOption.ToString(CultureInfo.InvariantCulture)))
                {
                    quantumTile.SetChance(buttonOption);
                }
            }
            GUILayout.EndHorizontal();
            
            // Update the tile
            _secondsSinceLastUpdate += Time.unscaledDeltaTime;
            if (_secondsSinceLastUpdate > UpdateTimeout)
            {
                _secondsSinceLastUpdate = 0f;
                quantumTile.FastValidate();
            }
        }
    }
#endif
}
