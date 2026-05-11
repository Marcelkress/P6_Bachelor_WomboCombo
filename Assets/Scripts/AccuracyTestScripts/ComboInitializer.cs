using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace AccuracyTestScripts
{
    public class ComboInitializer : MonoBehaviour
    {
        public class ComboSequence
        {
            public string name;
            public int[] comboValues;
        }

        public enum TutorialType
        {
            NormalControllers,
            CustomControllers
        }

        [Header("Switch Tutorial Type")]
        [Tooltip("Select the tutorial type to display at the start of the game.")]
        [SerializeField] private TutorialType _currentTutorialType;

        [SerializeField] private SpawnedCombo spawnedComboPrefab;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private float delayBetweenCombos = 1f;
        [SerializeField] private CanvasGroup initialStartText;
        [SerializeField] private CanvasGroup finishedTestText;

        // Requirement: startCombo is the ready check and is not counted as a measured combo.
        [SerializeField]
        private ComboSequence startCombo = new ComboSequence
        {
            name = "Start Combo",
            comboValues = new[] { 2, 2 }
        };

        // Requirement: the full visible sequence is 21 prompts: startCombo + 20 measured combos.
        // Requirement: measured combos are always one full combo long, e.g. { 1, 2 }.
        [SerializeField]
        private List<ComboSequence> combos = new List<ComboSequence>
        {
            new ComboSequence { name = "Combo 1", comboValues = new[] { 1, 2 } },
            new ComboSequence { name = "Combo 2", comboValues = new[] { 2, 1 } },
            new ComboSequence { name = "Combo 3", comboValues = new[] { 1, 1 } },
            new ComboSequence { name = "Combo 4", comboValues = new[] { 1, 3 } },
            new ComboSequence { name = "Combo 5", comboValues = new[] { 2, 2 } },
            new ComboSequence { name = "Combo 6", comboValues = new[] { 2, 3 } },
            new ComboSequence { name = "Combo 7", comboValues = new[] { 1, 1 } },
            new ComboSequence { name = "Combo 8", comboValues = new[] { 1, 3 } },
            new ComboSequence { name = "Combo 9", comboValues = new[] { 1, 2 } },
            new ComboSequence { name = "Combo 10", comboValues = new[] { 2, 3 } },
            new ComboSequence { name = "Combo 11", comboValues = new[] { 2, 1 } },
            new ComboSequence { name = "Combo 12", comboValues = new[] { 1, 3 } },
            new ComboSequence { name = "Combo 13", comboValues = new[] { 2, 1 } },
            new ComboSequence { name = "Combo 14", comboValues = new[] { 1, 2 } },
            new ComboSequence { name = "Combo 15", comboValues = new[] { 1, 1 } },
            new ComboSequence { name = "Combo 16", comboValues = new[] { 2, 2 } },
            new ComboSequence { name = "Combo 17", comboValues = new[] { 2, 3 } },
            new ComboSequence { name = "Combo 18", comboValues = new[] { 1, 2 } },
            new ComboSequence { name = "Combo 19", comboValues = new[] { 2, 1 } },
            new ComboSequence { name = "Combo 20", comboValues = new[] { 1, 2 } }
        };

        // global timer starts after startCombo and stops on the final measured input.
        [SerializeField] private float totalRunTime; // er med animations tider fra du laver stat combo til du laver sidste combo

        // each SpawnedCombo reports its own instant solve time.
        [SerializeField] private List<float> comboSolveTimes = new List<float>(); // er ikke med animations tider, kun tiden fra spawn til korrekt input

        private int currentComboIndex;
        private SpawnedCombo activeCombo;
        private bool activeComboIsStartCombo;
        private bool testRunning;
        private float globalStartTime;

        void Start()
        {
            finishedTestText.alpha = 0f;
            initialStartText.alpha = 1f;
            InitializeTest();
        }

        private void InitializeTest()
        {
            currentComboIndex = 0;
            totalRunTime = 0f;
            testRunning = false;
            comboSolveTimes.Clear();

            SpawnStartCombo();
        }

        private void SpawnStartCombo()
        {
            SpawnCombo(startCombo, true);
        }

        private void IncrementAndSpawnCombo(bool finishedStartCombo)
        {

            if (finishedStartCombo) // så den første combo ikke tæller med ellers starter vi test på nummer 2 combo, og så er der kun 19 combos der tæller med i stedet for 20
            {
                SpawnNextCombo();
                return;
            }

            currentComboIndex++;

            if (currentComboIndex < combos.Count)
            {
                SpawnNextCombo();
            }
        }

        private void SpawnNextCombo()
        {
            if (spawnedComboPrefab == null)
            {
                Debug.LogError("ComboInitializer needs a SpawnedCombo prefab.", this);
                return;
            }

            if (combos.Count == 0 || currentComboIndex >= combos.Count)
            {
                Debug.LogWarning("ComboInitializer has no combos to spawn.", this);
                return;
            }

            ComboSequence nextCombo = combos[currentComboIndex];

            SpawnCombo(nextCombo, false);
        }

        private void SpawnCombo(ComboSequence combo, bool isStartCombo)
        {
            if (spawnedComboPrefab == null)
            {
                Debug.LogError("ComboInitializer needs a SpawnedCombo prefab.", this);
                return;
            }

            Transform parent = spawnParent != null ? spawnParent : transform;
            activeCombo = Instantiate(spawnedComboPrefab, parent);
            activeComboIsStartCombo = isStartCombo;
            activeCombo.Initialize(combo.comboValues);
            activeCombo.Solved += HandleComboSolved; // subscriber til Solved event i SpawnedCombo, så vi kan få tiden når en combo er løst
            activeCombo.VisualFinished += HandleComboVisualFinished; // subscriber til VisualFinished event i SpawnedCombo, så vi kan spawne næste combo når den visuelle animation er færdig
        }

        private void HandleComboSolved(SpawnedCombo solvedCombo, float localSolveTime)
        {
            if (solvedCombo != activeCombo)
            {
                return;
            }

            if (activeComboIsStartCombo) // hvis det er start combo start vi testen
            {
                initialStartText.DOFade(0f, 0.5f);
                // completing startCombo starts the global timer.
                testRunning = true;
                globalStartTime = Time.realtimeSinceStartup;
                Debug.Log("Accuracy test started.");
                return;
            }

            comboSolveTimes.Add(localSolveTime); // adder hvor lang tid det tog i en liste så vi kan skrive det i filen til sidst
            Debug.Log($"Combo {currentComboIndex + 1} solved in {localSolveTime:0.000}s");

            // completed correct combo stops the global timer immediately.
            if (testRunning && currentComboIndex >= combos.Count - 1)
            {
                finishedTestText.DOFade(1f, 0.5f);
                testRunning = false;
                totalRunTime = Time.realtimeSinceStartup - globalStartTime;
                Debug.Log($"Accuracy test completed in {totalRunTime:0.000}s");
                WriteResultsFile();
            }
        }

        private void HandleComboVisualFinished(SpawnedCombo finishedCombo)
        {
            if (finishedCombo != activeCombo)
            {
                return;
            }

            finishedCombo.Solved -= HandleComboSolved;
            finishedCombo.VisualFinished -= HandleComboVisualFinished;

            bool finishedStartCombo = activeComboIsStartCombo; // bliver false efter første combo
            Destroy(finishedCombo.gameObject);
            activeCombo = null;

            IncrementAndSpawnCombo(finishedStartCombo); 
        }

        

        private void WriteResultsFile()
        {
            string folderPath = Path.Combine(Application.persistentDataPath, "AccuracyTestResults");
            Directory.CreateDirectory(folderPath);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");


            // Ser hvilken slags controller man har brugt og laver et filnavn ud fra det, mere clean at finde efter
            string filePrefix = _currentTutorialType == TutorialType.NormalControllers
                ? "normal-controllers" // hvis normal controllers er valgt, så er det "normal-controllers"
                : "custom-controllers"; // ellers er det "custom-controllers" hvis custom controllers er valgt

            string filePath = Path.Combine(folderPath, $"{filePrefix}-accuracy-test-{timestamp}.txt");

            using StreamWriter writer = new StreamWriter(filePath);

            if (_currentTutorialType == TutorialType.NormalControllers)
            {
                writer.WriteLine("Controller Type: Normal Controllers");
            }
            else if (_currentTutorialType == TutorialType.CustomControllers)
            {
                writer.WriteLine("Controller Type: Custom Controllers");
            }

            writer.WriteLine("Controller Performance Difference Test");
            writer.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Total Run Time: {totalRunTime:0.000}s");
            writer.WriteLine();

            writer.WriteLine("Start Combo");
            writer.WriteLine("----------------------------------------");
            writer.WriteLine($"{startCombo.name} ({FormatComboValues(startCombo)})");
            writer.WriteLine();

            writer.WriteLine("Measured Combo Times");
            writer.WriteLine("----------------------------------------");

            for (int i = 0; i < comboSolveTimes.Count; i++) // looper over comboSolveTimes og skriver en linje med deres tider osv
            {
                ComboSequence combo = i < combos.Count ? combos[i] : null;
                string comboName = combo != null ? combo.name : $"Combo {i + 1}";
                string comboValues = combo != null ? FormatComboValues(combo) : "Missing Combo";

                writer.WriteLine($"{i + 1:00}. {comboName} ({comboValues}) - {comboSolveTimes[i]:0.000}s");
            }

            Debug.Log($"Accuracy test results saved to: {filePath}");
        }


        // Den laver comboValues int (som fx 1,2) om til en string "1, 2"
        private static string FormatComboValues(ComboSequence combo) 
        {
            return combo == null || combo.comboValues == null
                ? "Missing Combo"
                : string.Join(", ", combo.comboValues);
        }
    }
}
