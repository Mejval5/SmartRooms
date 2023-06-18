using UnityEditor;
using UnityEngine;

namespace SmartRooms.Editor
{
    /// <summary>
    /// Manager for holding data and logic which handles the Smart Rooms package in the editor.
    /// </summary>
    public class SmartTileManager : ScriptableObject
    {
        [field: SerializeField] public Font DefaultFont { get; private set; }
        
        // Instance handling
        private static SmartTileManager _instance;
        public static SmartTileManager Instance
        {
            get
            {
                // Get instance of the manager if possible
                if (_instance != null)
                {
                    return _instance;
                }
                
                // Load an instance of the manager if possible
                _instance = Resources.Load<SmartTileManager>("SmartTileManager");

                if (_instance != null)
                {
                    return _instance;
                }

                // Create a manager scriptable object in the Resources folder
                SmartTileManager smartTileManager = CreateInstance<SmartTileManager>();
                string uniquePath = AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/SmartTileManager.asset");
                AssetDatabase.CreateAsset(smartTileManager, uniquePath);
                _instance = smartTileManager;

                return _instance;
            }
        }
    }
}

