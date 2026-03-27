using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace GridFactory.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music")]
        [SerializeField] private AudioSource kingdomAmbient;
        [SerializeField] private AudioSource voidAmbient;

        [Header("Ambient")]
        [SerializeField] private AudioSource kingdomBackground;
        [SerializeField] private AudioSource voidBackground;

        [Header("SFX")]
        [SerializeField] private AudioSource buildSFX;
        [SerializeField] private AudioSource destroySFX;
        [SerializeField] private AudioSource rotateSFX;
        [SerializeField] private AudioSource buttonClickSFX;
        [SerializeField] private AudioSource modeToggleSFX;

        [Header("Settings")]
        [SerializeField] private float ambientMaxVolume = 0.5f;
        [SerializeField] private float backgroundMaxVolume = 1f;
        [SerializeField] private float fadeDuration = 2f;

        [Header("Trigger")]
        [SerializeField] private Button soundExcludeModeToggleButton;

        private Coroutine _currentAmbientFade;
        private Coroutine _currentBackgroundFade;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            kingdomAmbient.volume = 0;
            kingdomBackground.volume = 0;
            voidAmbient.volume = 0;
            voidBackground.volume = 0;
        }

        private void Start()
        {
            kingdomAmbient.Play();
            kingdomBackground.Play();
            voidAmbient.Play();
            voidBackground.Play();
            StartCoroutine(FadeIn(kingdomBackground, backgroundMaxVolume));
            StartCoroutine(FadeIn(kingdomAmbient, ambientMaxVolume));

            AddListenersToAllButtons();
        }

        private void AddListenersToAllButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Button btn in buttons)
                if (btn != soundExcludeModeToggleButton)
                    btn.onClick.AddListener(() => PlayButtonClickSFX());
        }

        private IEnumerator FadeIn(AudioSource fadeIn, float maxVolume = 1)
        {
            float time = 0f;

            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                float t = time / fadeDuration * 2;

                fadeIn.volume = Mathf.Lerp(0f, maxVolume, t);


                yield return null;
            }

            fadeIn.volume = maxVolume;
        }

        private IEnumerator Crossfade(AudioSource fadeIn, AudioSource fadeOut, float maxVolume = 1)
        {
            float time = 0f;

            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                float t = time / fadeDuration;

                fadeIn.volume = Mathf.Lerp(0f, maxVolume, t);
                fadeOut.volume = Mathf.Lerp(maxVolume, 0f, t);

                yield return null;
            }

            fadeIn.volume = maxVolume;
            fadeOut.volume = 0f;
        }

        public void PlayBuildSFX()
        {
            buildSFX.Play();
        }

        public void PlayDestroySFX()
        {
            destroySFX.Play();
        }

        public void PlayRotateSFX()
        {
            rotateSFX.Play();
        }

        public void PlayButtonClickSFX()
        {
            buttonClickSFX.Play();
        }

        public void PlayModeSwitchSFX()
        {
            modeToggleSFX.Play();
        }

        public void SwitchAmbient(bool intoVoid)
        {
            if (_currentAmbientFade != null)
                StopCoroutine(_currentAmbientFade);
            if (_currentBackgroundFade != null)
                StopCoroutine(_currentBackgroundFade);

            if (intoVoid)
            {
                _currentAmbientFade = StartCoroutine(Crossfade(voidAmbient, kingdomAmbient, ambientMaxVolume));
                _currentBackgroundFade = StartCoroutine(Crossfade(voidBackground, kingdomBackground, backgroundMaxVolume));
            }
            else
            {
                _currentAmbientFade = StartCoroutine(Crossfade(kingdomAmbient, voidAmbient, ambientMaxVolume));
                _currentBackgroundFade = StartCoroutine(Crossfade(kingdomBackground, voidBackground, backgroundMaxVolume));
            }
        }
    }
}