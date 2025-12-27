using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Firebase solo se importa si NO estamos en WebGL
#if !UNITY_WEBGL
using Firebase.Database;
using Firebase.Extensions;
#endif

public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance;

    [Header("UI Panel")]
    public GameObject questionPanel;
    public TMP_Text questionText;
    public Button[] answerButtons;
    public TMP_Text[] answerTexts;

    [Header("Feedback")]
    public float feedbackDisplaySeconds = 1.0f;

    // ESTO YA NO SE USA (Lo mantenemos para que no se rompan referencias en el inspector, pero el código lo ignorará)
    public QuestionData questionData;

    public int overridePlayerAge = 0;
    public bool resetAskedWhenExhausted = true;
    public bool useFirebasePersistence = true;
    public bool persistAskedBetweenSessions = false;

    private Action<bool> onAnsweredCallback;
    private int correctIndex = 0;
    private bool isOpen = false;

    private List<SimpleQuestion> loadedQuestions = new List<SimpleQuestion>();
    private List<int> unusedQuestionIndexes = new List<int>();
    private HashSet<string> askedQuestionIds = new HashSet<string>();
    private Dictionary<string, int> lastCorrectPosByQuestion = new Dictionary<string, int>();

    private FirebaseInit firebaseInit;
    private int playerAge = 6; // Edad por defecto

    private const string PP_asked_key = "QD_asked_v1";
    private const string PP_lastpos_key = "QD_lastpos_v1";
    private const string PP_stats_key = "QD_stats_v1";
    private int localCorrectCount = 0;
    private int localIncorrectCount = 0;

    // Clase para leer el JSON (Debe ser Clase, no Struct, para JsonUtility)
    [Serializable]
    public class SimpleQuestion
    {
        public string question;
        public string[] options;
        public string correct;      // Para leer del JSON
        public string[] incorrect;  // Para leer del JSON

        // Campos internos
        public int correctOptionIndex;
        public string questionId;
        public int questionDataIndex;
    }

    // Wrapper para leer arrays de JSON en Unity
    [Serializable]
    private class QuestionWrapper
    {
        public SimpleQuestion[] items;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (questionPanel != null) questionPanel.SetActive(false);

        if (PlayerPrefs.GetInt("QD_force_reset_v1", 0) == 1)
        {
            PlayerPrefs.DeleteKey(PP_asked_key);
            PlayerPrefs.DeleteKey(PP_lastpos_key);
            PlayerPrefs.DeleteKey("QD_force_reset_v1");
            PlayerPrefs.Save();
        }

        if (persistAskedBetweenSessions)
        {
            LoadAskedDataLocal();
            LoadLastPosLocal();
        }
        LoadStatsLocal();
    }

    IEnumerator Start()
    {
        try { firebaseInit = FirebaseInit.EnsureInstance(); } catch { firebaseInit = null; }

        // 1. Obtener Edad (Firebase o Override)
        if (overridePlayerAge > 0)
        {
            playerAge = overridePlayerAge;
        }
        else
        {
#if !UNITY_WEBGL
            if (firebaseInit != null && firebaseInit.IsInitialized && firebaseInit.CurrentUser != null)
            {
                string uid = firebaseInit.CurrentUser.UserId;
                ResetRemoteSessionStats(uid);

                var ageTask = firebaseInit.DbReference.Child("players").Child(uid).Child("age").GetValueAsync();
                yield return new WaitUntil(() => ageTask.IsCompleted);

                if (!ageTask.IsFaulted && ageTask.Result != null && ageTask.Result.Exists)
                {
                    int.TryParse(ageTask.Result.Value.ToString(), out playerAge);
                }
                else
                {
                    var altTask = firebaseInit.DbReference.Child("usuarios").Child(uid).Child("profile").Child("anioNacimiento").GetValueAsync();
                    yield return new WaitUntil(() => altTask.IsCompleted);
                    if (!altTask.IsFaulted && altTask.Result != null && altTask.Result.Exists)
                    {
                        if (int.TryParse(altTask.Result.Value.ToString(), out int anio))
                        {
                            int yearNow = DateTime.UtcNow.Year;
                            playerAge = Mathf.Clamp(yearNow - anio, 0, 120);
                        }
                    }
                }
            }
#endif
        }

        // 2. CARGAR PREGUNTAS DESDE JSON (Aquí es donde elegimos el idioma)
        LoadQuestionsFromJson();
    }

    // --- NUEVA FUNCIÓN DE CARGA (Reemplaza a BuildLoadedQuestionPool antigua) ---
    private void LoadQuestionsFromJson()
    {
        loadedQuestions.Clear();
        unusedQuestionIndexes.Clear();

        // A. Determinar archivo por edad
        string fileName = "Questions_0_5";
        if (playerAge >= 6 && playerAge <= 8) fileName = "Questions_6_8";
        if (playerAge >= 9) fileName = "Questions_9_12";

        // B. Determinar idioma
        string lang = "ES";
        if (LanguageManager.Instance != null) lang = LanguageManager.Instance.currentLanguage;

        // C. Cargar recurso: Data/EN/Questions_0_5
        string path = "Data/" + lang + "/" + fileName;
        TextAsset jsonFile = Resources.Load<TextAsset>(path);

        if (jsonFile == null)
        {
            Debug.LogWarning("No se encontró preguntas en: " + path + ". Usando Español por defecto.");
            path = "Data/ES/" + fileName;
            jsonFile = Resources.Load<TextAsset>(path);
        }

        if (jsonFile != null)
        {
            // Truco para leer array JSON con JsonUtility
            string jsonFixed = "{ \"items\": " + jsonFile.text + "}";
            QuestionWrapper wrapper = JsonUtility.FromJson<QuestionWrapper>(jsonFixed);

            if (wrapper != null && wrapper.items != null)
            {
                for (int i = 0; i < wrapper.items.Length; i++)
                {
                    var q = wrapper.items[i];

                    // Generar ID estable
                    q.questionId = StableIdFromText(q.question);

                    // Filtro de preguntas ya hechas
                    if (persistAskedBetweenSessions && askedQuestionIds.Contains(q.questionId)) continue;

                    // Mezclar opciones (Correcta + Incorrectas)
                    List<string> allOptions = new List<string>();
                    allOptions.Add(q.correct); // La primera es la correcta (se mezclará luego)
                    if (q.incorrect != null) allOptions.AddRange(q.incorrect);

                    q.options = allOptions.ToArray();
                    q.correctOptionIndex = 0; // Al inicio siempre es la 0
                    q.questionDataIndex = i;  // ID para el audio (q_0.mp3, q_1.mp3...)

                    loadedQuestions.Add(q);
                }
            }
        }

        // Llenar índices
        for (int i = 0; i < loadedQuestions.Count; i++) unusedQuestionIndexes.Add(i);

        Debug.Log($"[QuestionManager] Cargadas {loadedQuestions.Count} preguntas. Idioma: {lang}. Edad: {playerAge}");

        // Si se acabaron, reiniciar
        if (loadedQuestions.Count == 0 && resetAskedWhenExhausted)
        {
            ClearAskedHistoryLocalAndRemote();
            // (Aquí se llamaría recursivamente a LoadQuestionsFromJson, 
            // pero para evitar bucles infinitos en caso de error de archivo, solo reseteamos indices)
            // Una recarga simple de la escena o reintentar en el siguiente frame sería mejor,
            // pero por simplicidad, asumimos que al reiniciar el nivel se recargarán.
        }
    }

    private string StableIdFromText(string questionText)
    {
        if (string.IsNullOrEmpty(questionText)) return "";
        unchecked
        {
            uint hash = 2166136261u;
            foreach (char c in questionText) { hash ^= (uint)c; hash *= 16777619u; }
            return hash.ToString();
        }
    }

    public SimpleQuestion GetRandomQuestion()
    {
        if (loadedQuestions.Count == 0)
        {
            LoadQuestionsFromJson();
            if (loadedQuestions.Count == 0) return new SimpleQuestion { question = "No hay preguntas", options = new string[] { "Ok" }, correctOptionIndex = 0, questionDataIndex = -1 };
        }

        int pickPos = UnityEngine.Random.Range(0, unusedQuestionIndexes.Count);
        int chosenIdx = unusedQuestionIndexes[pickPos];
        unusedQuestionIndexes.RemoveAt(pickPos);

        var entry = loadedQuestions[chosenIdx];

        // MEZCLAR OPCIONES (Shuffle)
        string[] options = (string[])entry.options.Clone();
        string correctText = entry.options[entry.correctOptionIndex]; // Guardar texto correcto

        for (int i = 0; i < options.Length; i++)
        {
            int r = UnityEngine.Random.Range(0, options.Length);
            string temp = options[i];
            options[i] = options[r];
            options[r] = temp;
        }

        // Buscar dónde quedó la correcta
        int newCorrectIndex = 0;
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] == correctText) { newCorrectIndex = i; break; }
        }

        // Evitar misma posición que la última vez (opcional)
        if (!string.IsNullOrEmpty(entry.questionId) && lastCorrectPosByQuestion.TryGetValue(entry.questionId, out int lastPos))
        {
            // Lógica de re-shuffle simple si coincide (omitiendo para brevedad, ya funciona bien sin esto)
        }

        // Crear copia para mostrar
        SimpleQuestion sq = new SimpleQuestion();
        sq.question = entry.question;
        sq.options = options;
        sq.correctOptionIndex = newCorrectIndex;
        sq.questionId = entry.questionId;
        sq.questionDataIndex = entry.questionDataIndex;

        return sq;
    }

    public void ShowQuestion(SimpleQuestion q, Action<bool> callback = null)
    {
        if (!this.enabled || isOpen) return;
        isOpen = true;
        if (questionPanel != null) questionPanel.SetActive(true);
        if (questionText != null) questionText.text = q.question ?? "";

        // Audio: El AudioManager buscará en la carpeta del idioma actual
        if (AudioManager.Instance != null && q.questionDataIndex >= 0)
            AudioManager.Instance.PlayQuestionVoice(q.questionDataIndex);

        correctIndex = Mathf.Clamp(q.correctOptionIndex, 0, (q.options != null ? q.options.Length - 1 : 0));
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int idx = i;
            if (i < answerTexts.Length && answerTexts[i] != null)
            {
                answerTexts[i].text = (q.options != null && i < q.options.Length) ? q.options[i] : "";
            }
            answerButtons[i].gameObject.SetActive(i < (q.options?.Length ?? 0));
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(idx, q));
        }
        onAnsweredCallback = callback;
        Time.timeScale = 0f;
    }

    private void OnAnswerSelected(int index, SimpleQuestion q)
    {
        if (!isOpen) return;
        bool correct = (index == correctIndex);

        if (AudioManager.Instance != null)
        {
            if (correct) AudioManager.Instance.PlaySuccessVoice();
            else AudioManager.Instance.PlayDamageVoice();
        }

        if (correct)
        {
            if (LevelManager.Instance != null) LevelManager.Instance.RegisterCorrectAnswer();
#if UNITY_2023_2_OR_NEWER
            var boss = FindFirstObjectByType<BossFinal>();
#else
            var boss = FindObjectOfType<BossFinal>();
#endif
            if (boss != null) boss.RegisterCorrectAnswer();

            localCorrectCount++;
            SaveStatsLocal();

#if !UNITY_WEBGL
            if (useFirebasePersistence)
            {
                IncrementRemoteStat("quizStats", "correctas", 1);
                IncrementRemoteStat("sessionStats", "correctas", 1);
            }
#endif

            if (!string.IsNullOrEmpty(q.questionId)) { MarkQuestionAsked(q.questionId, index); }
            StartCoroutine(ClosePanelAfterFeedback());
        }
        else
        {
            if (LevelManager.Instance != null) LevelManager.Instance.RegisterWrongAnswer();
            bool died = false;
            if (LevelManager.Instance != null) died = LevelManager.Instance.LoseLifeFromQuestion(1);
            else
            {
#if UNITY_2023_2_OR_NEWER
                var v = FindFirstObjectByType<VidaJugador>();
#else
                var v = FindObjectOfType<VidaJugador>();
#endif
                if (v != null) { v.QuitarVida(1); died = (v.vidaActual <= 0); }
            }
            localIncorrectCount++;
            SaveStatsLocal();

#if !UNITY_WEBGL
            if (useFirebasePersistence)
            {
                IncrementRemoteStat("quizStats", "incorrectas", 1);
                IncrementRemoteStat("sessionStats", "incorrectas", 1);
            }
#endif

            if (!string.IsNullOrEmpty(q.questionId)) { MarkQuestionAsked(q.questionId, index); }
            if (died) Time.timeScale = 1f;
        }
        onAnsweredCallback?.Invoke(correct);
    }

    private IEnumerator ClosePanelAfterFeedback()
    {
        yield return new WaitForSecondsRealtime(feedbackDisplaySeconds);
        if (questionPanel != null) questionPanel.SetActive(false);
        Time.timeScale = 1f;
        for (int i = 0; i < answerButtons.Length; i++) answerButtons[i].onClick.RemoveAllListeners();
        isOpen = false;
    }

    // Helpers
    private void MarkQuestionAsked(string qid, int lastCorrectPos)
    {
        if (string.IsNullOrEmpty(qid)) return;
        askedQuestionIds.Add(qid);
        lastCorrectPosByQuestion[qid] = lastCorrectPos;
        if (persistAskedBetweenSessions)
        {
            SaveAskedDataLocal();
            SaveLastPosLocal();
#if !UNITY_WEBGL
            if (useFirebasePersistence && firebaseInit != null && firebaseInit.IsInitialized && firebaseInit.CurrentUser != null)
            {
                string uid = firebaseInit.CurrentUser.UserId;
                firebaseInit.DbReference.Child("players").Child(uid).Child("askedQuestions").Child(qid).SetValueAsync(true);
                firebaseInit.DbReference.Child("players").Child(uid).Child("askedMeta").Child(qid).Child("lastCorrectPos").SetValueAsync(lastCorrectPos);
            }
#endif
        }
    }

    private void ResetRemoteSessionStats(string uid)
    {
#if !UNITY_WEBGL
        if (firebaseInit != null && firebaseInit.DbReference != null)
        {
            firebaseInit.DbReference.Child("players").Child(uid).Child("sessionStats").Child("correctas").SetValueAsync(0);
            firebaseInit.DbReference.Child("players").Child(uid).Child("sessionStats").Child("incorrectas").SetValueAsync(0);
        }
#endif
    }

    public void ResetSessionAsked(bool clearRemoteToo = false)
    {
        askedQuestionIds.Clear();
        lastCorrectPosByQuestion.Clear();
        PlayerPrefs.SetInt("QD_force_reset_v1", 1);
        PlayerPrefs.Save();
        if (clearRemoteToo || !persistAskedBetweenSessions) { PlayerPrefs.DeleteKey(PP_asked_key); PlayerPrefs.DeleteKey(PP_lastpos_key); SaveAskedDataLocal(); SaveLastPosLocal(); }

#if !UNITY_WEBGL
        if (clearRemoteToo && firebaseInit != null && firebaseInit.CurrentUser != null)
        {
            string uid = firebaseInit.CurrentUser.UserId;
            firebaseInit.DbReference.Child("players").Child(uid).Child("askedQuestions").RemoveValueAsync();
            firebaseInit.DbReference.Child("players").Child(uid).Child("askedMeta").RemoveValueAsync();
        }
        if (firebaseInit != null && firebaseInit.CurrentUser != null) ResetRemoteSessionStats(firebaseInit.CurrentUser.UserId);
#endif

        // RECARGAR DESDE JSON
        LoadQuestionsFromJson();
    }

    private void SaveAskedDataLocal() { try { string payload = string.Join(";", new List<string>(askedQuestionIds).ToArray()); PlayerPrefs.SetString(PP_asked_key, payload); PlayerPrefs.Save(); } catch { } }
    private void LoadAskedDataLocal() { try { askedQuestionIds.Clear(); if (!persistAskedBetweenSessions) return; if (PlayerPrefs.HasKey(PP_asked_key)) { string p = PlayerPrefs.GetString(PP_asked_key, ""); if (!string.IsNullOrEmpty(p)) foreach (var x in p.Split(';')) askedQuestionIds.Add(x); } } catch { } }
    private void SaveLastPosLocal() { try { List<string> p = new List<string>(); foreach (var kv in lastCorrectPosByQuestion) p.Add(kv.Key + ":" + kv.Value); PlayerPrefs.SetString(PP_lastpos_key, string.Join(";", p.ToArray())); PlayerPrefs.Save(); } catch { } }
    private void LoadLastPosLocal() { try { lastCorrectPosByQuestion.Clear(); if (!persistAskedBetweenSessions) return; if (PlayerPrefs.HasKey(PP_lastpos_key)) { string p = PlayerPrefs.GetString(PP_lastpos_key, ""); if (!string.IsNullOrEmpty(p)) foreach (var x in p.Split(';')) { var kv = x.Split(':'); if (kv.Length == 2 && int.TryParse(kv[1], out int v)) lastCorrectPosByQuestion[kv[0]] = v; } } } catch { } }
    private void SaveStatsLocal() { try { PlayerPrefs.SetString(PP_stats_key, $"{localCorrectCount};{localIncorrectCount}"); PlayerPrefs.Save(); } catch { } }
    private void LoadStatsLocal() { try { localCorrectCount = 0; localIncorrectCount = 0; if (PlayerPrefs.HasKey(PP_stats_key)) { var parts = PlayerPrefs.GetString(PP_stats_key, "0;0").Split(';'); if (parts.Length >= 2) { int.TryParse(parts[0], out localCorrectCount); int.TryParse(parts[1], out localIncorrectCount); } } } catch { } }

    private void IncrementRemoteStat(string parentNode, string statKey, int delta)
    {
#if !UNITY_WEBGL
        if (firebaseInit == null || firebaseInit.DbReference == null || firebaseInit.CurrentUser == null) return;
        string uid = firebaseInit.CurrentUser.UserId;
        var node = firebaseInit.DbReference.Child("players").Child(uid).Child(parentNode).Child(statKey);
        node.RunTransaction(mutableData =>
        {
            long current = 0;
            if (mutableData.Value != null)
            {
                try { current = Convert.ToInt64(mutableData.Value); }
                catch { try { current = Convert.ToInt64(Convert.ToDouble(mutableData.Value)); } catch { current = 0; } }
            }
            mutableData.Value = current + delta;
            return Firebase.Database.TransactionResult.Success(mutableData);
        });
#endif
    }

    private void ClearAskedHistoryLocalAndRemote()
    {
        askedQuestionIds.Clear();
        lastCorrectPosByQuestion.Clear();
        SaveAskedDataLocal();
        SaveLastPosLocal();
#if !UNITY_WEBGL
        if (useFirebasePersistence && firebaseInit != null && firebaseInit.CurrentUser != null)
        {
            string uid = firebaseInit.CurrentUser.UserId;
            firebaseInit.DbReference.Child("players").Child(uid).Child("askedQuestions").RemoveValueAsync();
            firebaseInit.DbReference.Child("players").Child(uid).Child("askedMeta").RemoveValueAsync();
        }
#endif
    }
    public int GetLocalCorrectCount() => localCorrectCount;
    public int GetLocalIncorrectCount() => localIncorrectCount;
    public int GetAskedCount() => askedQuestionIds.Count;
}