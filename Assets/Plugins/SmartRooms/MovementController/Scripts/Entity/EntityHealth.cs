﻿using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MovementController.Entity
{
    public class EntityHealth : MonoBehaviour
    {
        public GameObject bloodParticles;

        public UnityEvent HealthChangedEvent { get; private set; } = new();

        public int maxHealth;
        [field: SerializeField] public int CurrentHealth { get; private set; }

        [Header("Invulnerability")] public SpriteRenderer spriteRenderer;
        public float invulnerabilityDuration;
        public int numberOfInvulnerabilityFlashes = 10;
        public Color invulnerabilityFlashColor;
        public bool isInvulnerable;

        private void Awake()
        {
            SetHealth(maxHealth);
        }

        public void TakeDamage(int damage)
        {
            if (isInvulnerable)
            {
                return;
            }

            if (bloodParticles != null)
            {
                Instantiate(bloodParticles, transform.position, Quaternion.identity);
            }

            CurrentHealth -= damage;
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }

            HealthChangedEvent?.Invoke();

            if (CurrentHealth <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(InvulnerabilityTime());
            }
        }

        private void SetHealth(int value)
        {
            CurrentHealth = value;
            HealthChangedEvent?.Invoke();
        }

        private void Die()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// TODO: How do we ignore collisions between the player and enemies when this happens?
        /// </summary>
        /// <returns></returns>
        private IEnumerator InvulnerabilityTime()
        {
            isInvulnerable = true;

            Color originalColor = spriteRenderer.color;
            float flashInterval = invulnerabilityDuration / numberOfInvulnerabilityFlashes;
            float _invulnerabilityTimer = 0;

            while (_invulnerabilityTimer < invulnerabilityDuration)
            {
                _invulnerabilityTimer += flashInterval;

                spriteRenderer.color = invulnerabilityFlashColor;
                yield return new WaitForSeconds(flashInterval / 2);
                spriteRenderer.color = originalColor;
                yield return new WaitForSeconds(flashInterval / 2);
            }

            isInvulnerable = false;
        }
    }
}