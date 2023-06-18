using System;
using SmartRooms.Variables;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SmartRooms.Tiles
{
    /// <summary>
    /// Randomly swap the Sprite Renderer's sprite for another sprite on Awake. Can be configured to also swap on OnEnable.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class RandomSpriteSwapper : MonoBehaviour
    {
        [SerializeField] private SpriteListVariable _swappableSprites;
        [SerializeField] private bool _swapOnEnable;
        
        // Internals
        private SpriteRenderer _spriteRenderer;
        
        protected void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            DoRandomSpriteSwap();
        }

        protected void OnEnable()
        {
            if (_swapOnEnable)
            {
                DoRandomSpriteSwap();
            }
        }

        /// <summary>
        /// Randomly swap the Sprite Renderer's sprite for another sprite
        /// </summary>
        private void DoRandomSpriteSwap()
        {
            if (_swappableSprites == null || _swappableSprites.Value.Count == 0)
            {
                return;
            }

            // Set random sprite to the Sprite Renderer
            _spriteRenderer.sprite = _swappableSprites.Value[Random.Range(0, _swappableSprites.Value.Count)];
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(RandomSpriteSwapper), true)]
    [CanEditMultipleObjects]
    internal class SpriteSwapperEditor: UnityEditor.Editor
    {
    }
#endif
}
