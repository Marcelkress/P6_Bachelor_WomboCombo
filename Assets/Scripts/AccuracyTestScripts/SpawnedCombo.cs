using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace AccuracyTestScripts
{
    public class SpawnedCombo : MonoBehaviour
    {
        public int[] comboArray;

        private bool comboSolved;
        private float localStartTime;

        public bool debug = false;

        [SerializeField] private Transform content;
        public Sprite moonImg, starImg, sunImg; // references to the UI images for each button (Square, Circle, Triangle)
        public GameObject inputUIImage; // reference UI image which should be updated to show the combo array (Should spawn multiple)

        [Header("Visual Feedback")]
        public float shakeDuration = 0.2f;
        public Vector3 shakeStrength = new Vector3(12f, 12f, 0f);
        public int shakeVibrato = 10;
        public float shakeRandomness = 90f;
        public float removeDuration = 0.12f;
        public float removeScale = 0.8f;

        private Image[] contentSprite;
        private Sequence comboStepSequence;

        public event Action<SpawnedCombo, float> Solved; // Sender ref til sig selv og en float på tid som ComboInitializer bruger i HandleComboSolved
        public event Action<SpawnedCombo> VisualFinished;

        public void Initialize(int[] combo)
        {
            comboArray = combo;
            comboSolved = false;
            localStartTime = Time.realtimeSinceStartup;
        }

        void Start()
        {
            if (comboArray == null || comboArray.Length != 2)
            {
                Debug.LogError("SpawnedCombo needs a combo array with exactly two values.", this);
                VisualFinished?.Invoke(this); // ComboInitializer er subscribed til dette event
                return;
            }

            InitializeUI();
            
            if (InputManager.instance != null)
            {
                InputManager.instance.PlayerOneEvent.AddListener(PlayerOneUpdate);
            }
        }

        private void OnDisable()
        {
            comboStepSequence?.Kill();

            if (InputManager.instance != null)
            {
                InputManager.instance.PlayerOneEvent.RemoveListener(PlayerOneUpdate);
            }
        }

        public void PlayerOneUpdate()
        {
            PlayerInfoStruct playerOneInfo = InputManager.instance.GetPlayerSymbols(1);
            CompareCombo(playerOneInfo);
        }

        private void CompareCombo(PlayerInfoStruct playerOneInfo)
        {
            if (comboSolved)
            {
                return;
            }

            if (debug)
            {
                Debug.Log("Array symb one: " + comboArray[0]);
                Debug.Log("Array symb two: " + comboArray[1]);
            }

            if (playerOneInfo.symbOne == comboArray[0] && playerOneInfo.symbTwo == comboArray[1])
            {
                SolveAndAnimate();
            }
        }

        public void CheatComboStep()
        {
            if (comboSolved)
            {
                return;
            }
            SolveAndAnimate();
        }

        private void SolveAndAnimate()
        {
            // local timer stops immediately when the correct combo is input.
            comboSolved = true;

            // realtimeSinceStartup skulle være mere accurate end Time.time
            // realTimeSinceStartup er tiden siden spillet startede

            // så hvis der er gået 5.3 seconds siden spillet startede, og vi startede timeren ved 5.0 seconds, 
            // så vil localSolveTime være 0.3 seconds
            float localSolveTime = Time.realtimeSinceStartup - localStartTime; 
            Solved?.Invoke(this, localSolveTime); // ComboInitalizer er subscribed til dette event, 

            AnimateCompletedComboStep();
        }

        private void InitializeUI()
        {
            contentSprite = new Image[2];

            for (int i = 0; i < 2; i++)
            {
                GameObject uiImage = Instantiate(inputUIImage, content);
                Image img = uiImage.GetComponent<Image>();

                switch (comboArray[i])
                {
                    case 1:
                        img.sprite = moonImg;
                        break;
                    case 2:
                        img.sprite = starImg;
                        break;
                    case 3:
                        img.sprite = sunImg;
                        break;
                }

                contentSprite[i] = img;
            }
        }

        private void AnimateCompletedComboStep()
        {
            if (contentSprite == null || contentSprite.Length < 2 || contentSprite[0] == null || contentSprite[1] == null)
            {
                VisualFinished?.Invoke(this); // ComboInitializer er subscribed til dette event
                return;
            }

            Image topImage = contentSprite[0];
            Image bottomImage = contentSprite[1];

            comboStepSequence?.Kill();
            comboStepSequence = DOTween.Sequence();

            comboStepSequence.Join(
                topImage.transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness, false)
            );

            comboStepSequence.Join(
                bottomImage.transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness, false)
            );

            comboStepSequence.Append(topImage.transform.DOScale(removeScale, removeDuration));
            comboStepSequence.Join(bottomImage.transform.DOScale(removeScale, removeDuration));
            comboStepSequence.Join(topImage.DOFade(0f, removeDuration));
            comboStepSequence.Join(bottomImage.DOFade(0f, removeDuration));

            comboStepSequence.OnComplete(() =>
            {
                topImage.gameObject.SetActive(false);
                bottomImage.gameObject.SetActive(false);
                VisualFinished?.Invoke(this); // ComboInitializer er subscribed til dette event
            });
        }

       
    }
}
