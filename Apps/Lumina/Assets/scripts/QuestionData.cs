using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SimpleQuestionEntry
{
    public string id; // opcional: si lo dejas vacío se generará uno estable al import
    [TextArea] public string questionText;
    public string[] answers;
    public int correctAnswerIndex;
    [Tooltip("Edad mínima recomendada (0 = cualquiera)")]
    public int minAge; // inclusive, 0 = cualquier edad
    [Tooltip("Edad máxima recomendada (0 = cualquiera)")]
    public int maxAge; // inclusive, 0 = cualquiera
}

[CreateAssetMenu(fileName = "QuestionData", menuName = "Quiz/Question Data")]
public class QuestionData : ScriptableObject
{
    public List<SimpleQuestionEntry> questions = new List<SimpleQuestionEntry>();
}
