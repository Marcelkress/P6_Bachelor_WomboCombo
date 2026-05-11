using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace AccuracyTestScripts
{
    public class SpawnedCombo : MonoBehaviour
    {
        public int[] comboArray;

        private int comboStep = 0;
        private bool comboSolved;
        private bool isAnimatingStep;
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

        public event Action<SpawnedCombo, float> Solved;
        public event Action<SpawnedCombo> VisualFinished;
        public event Action<SpawnedCombo> Completed;

        private PlayerInfoStruct playerOneInfo;

        public void Initialize(int[] combo)
        {
            comboArray = combo;
            comboStep = 0;
            comboSolved = false;
            isAnimatingStep = false;
            localStartTime = Time.realtimeSinceStartup;
        }

        void Start()
        {
            if (!IsValidCombo())
            {
                Debug.LogError("SpawnedCombo needs a combo array with an even amount of values.", this);
                CompleteVisuals();
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
            playerOneInfo = InputManager.instance.GetPlayerSymbols(1);
            CompareCombo();
        }

        private void CompareCombo()
        {
            if (comboSolved || comboStep >= comboArray.Length)
            {
                return;
            }

            if (debug)
            {
                Debug.Log("ComboStep: " + comboStep);
                Debug.Log("Array symb one: " + comboArray[comboStep]);
                Debug.Log("Array symb two: " + comboArray[comboStep + 1]);
            }

            if (playerOneInfo.symbOne == comboArray[comboStep] && playerOneInfo.symbTwo == comboArray[comboStep + 1])
            {
                CompleteCurrentComboStep();
            }
            else
            {

            }
        }

        public void CheatComboStep()
        {
            CompleteCurrentComboStep();
        }

        private void CompleteCurrentComboStep()
        {
            if (comboSolved || isAnimatingStep || comboStep >= comboArray.Length)
            {
                return;
            }

            int top = comboStep;
            int bottom = comboStep + 1;

            comboStep += 2;

            // Requirement: local timer stops immediately when the correct combo is input.
            if (comboStep >= comboArray.Length)
            {
                SolveCombo();
            }

            AnimateCompletedComboStep(top, bottom);
        }

        private void InitializeUI()
        {
            contentSprite = new Image[comboArray.Length];

            for (int i = 0; i < comboArray.Length; i++)
            {
                GameObject uiImage = Instantiate(inputUIImage, content);

                switch (comboArray[i])
                {
                    case 1:
                        uiImage.GetComponent<Image>().sprite = moonImg;
                        break;
                    case 2:
                        uiImage.GetComponent<Image>().sprite = starImg;
                        break;
                    case 3:
                        uiImage.GetComponent<Image>().sprite = sunImg;
                        break;
                }

                contentSprite[i] = uiImage.GetComponent<Image>();
            }
        }

        private void AnimateCompletedComboStep(int top, int bottom)
        {
            if (!CanAnimateComboStep(top, bottom))
            {
                CheckComboCompletion();
                return;
            }

            isAnimatingStep = true;

            Image topImage = contentSprite[top];
            Image bottomImage = contentSprite[bottom];

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
                isAnimatingStep = false;
                CheckComboCompletion();
            });
        }

        private bool CanAnimateComboStep(int top, int bottom)
        {
            return contentSprite != null
                   && top >= 0
                   && bottom >= 0
                   && top < contentSprite.Length
                   && bottom < contentSprite.Length
                   && contentSprite[top] != null
                   && contentSprite[bottom] != null;
        }

        private void CheckComboCompletion()
        {
            if (comboStep >= comboArray.Length)
            {
                CompleteVisuals();
            }
        }

        private bool IsValidCombo()
        {
            return comboArray != null && comboArray.Length > 0 && comboArray.Length % 2 == 0;
        }

        private void SolveCombo()
        {
            if (comboSolved)
            {
                return;
            }

            comboSolved = true;
            float localSolveTime = Time.realtimeSinceStartup - localStartTime;
            Solved?.Invoke(this, localSolveTime);
        }

        private void CompleteVisuals()
        {
            VisualFinished?.Invoke(this);
            Completed?.Invoke(this);
        }
    }
}
