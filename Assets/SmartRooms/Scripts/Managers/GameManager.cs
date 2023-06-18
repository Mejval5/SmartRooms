using System;
using System.Collections;
using System.Collections.Generic;
using SmartRooms.Generator;
using SmartRooms.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SmartRooms.Managers
{
    public class GameManager : MonoBehaviour
    {
        [Header("Player setup")]
        [SerializeField] private SmartLevelGenerator _levelGenerator;
        [SerializeField] private Transform _player;
        [SerializeField] private Transform _playerStart;
        [SerializeField] private ExitLogic _exitLogic;
        

        [Header("UI setup")]
        [SerializeField] private Image _screenBlanker;

        [SerializeField] private float _screenFadeInDelay = 1f;
        [SerializeField] private float _screenFadeInTime = 1f;
        
        
        private void Awake()
        {
            _levelGenerator.LevelGenerated += OnLevelGenerated;
            _exitLogic.PlayerExited += OnPlayerExited;
            
            _screenBlanker.enabled = true;
            _screenBlanker.color = Color.black;
            
            _player.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _levelGenerator.LevelGenerated -= OnLevelGenerated;
            _exitLogic.PlayerExited -= OnPlayerExited;
        }

        private void OnPlayerExited(GameObject player)
        {
            StartCoroutine(ExitSequence());
        }

        private IEnumerator ExitSequence()
        {
            _player.gameObject.SetActive(false);
            
            yield return FadeCoroutine(Color.black);
            
            _levelGenerator.StartGeneration();
        }

        private void OnLevelGenerated()
        {
            _player.position = _playerStart.position;
            _player.gameObject.SetActive(true);

            StartCoroutine(FadeCoroutine(Color.clear));
        }

        private void Start()
        {
            _levelGenerator.StartGeneration();
        }

        private IEnumerator FadeCoroutine(Color targetValue)
        {
            yield return new WaitForSeconds(_screenFadeInDelay);
            
            Color startValue = _screenBlanker.color;
            
            float startTime = Time.time;
            while (startTime + _screenFadeInTime > Time.time)
            {
                float t = (Time.time - startTime) / _screenFadeInTime;
                _screenBlanker.color = Color.Lerp(startValue, targetValue, t);
                yield return null;
            }

            _screenBlanker.color = targetValue;
        }
    } 
}
