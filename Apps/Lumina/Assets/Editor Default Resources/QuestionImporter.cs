#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class QuestionDataImporter : EditorWindow
{
    [TextArea(8, 20)]
    public string bulkText = "";
    public bool overwriteTarget = true;

    [MenuItem("Tools/QuestionData Importer")]
    static void OpenWindow()
    {
        var w = GetWindow<QuestionDataImporter>("QuestionData Importer");
        w.minSize = new Vector2(600, 320);
    }

    void OnGUI()
    {
        GUILayout.Label("Pega el banco aquí (formato tolerante). Separar preguntas con línea en blanco.", EditorStyles.boldLabel);
        bulkText = EditorGUILayout.TextArea(bulkText, GUILayout.Height(220));

        overwriteTarget = EditorGUILayout.ToggleLeft("Sobrescribir QuestionData seleccionado (si no, se añadirán)", overwriteTarget);

        if (GUILayout.Button("Import to Selected QuestionData"))
        {
            if (string.IsNullOrWhiteSpace(bulkText)) { Debug.LogWarning("Bulk vacío."); return; }

            var sel = Selection.activeObject as QuestionData;
            if (sel == null) { Debug.LogError("Selecciona un asset QuestionData en el Project antes de importar."); return; }

            int added = ImportToQuestionData(sel, bulkText, overwriteTarget);
            EditorUtility.SetDirty(sel);
            AssetDatabase.SaveAssets();
            Debug.Log($"Import completo: {added} preguntas importadas en {sel.name}.");
        }
    }

    // Import logic (compatible con formato explicado)
    static int ImportToQuestionData(QuestionData qd, string text, bool overwrite)
    {
        if (overwrite) qd.questions = new List<SimpleQuestionEntry>();

        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        var blocks = Regex.Split(text, @"\n\s*\n");
        int count = 0;

        foreach (var rawBlk in blocks)
        {
            var blk = rawBlk.Trim();
            if (string.IsNullOrEmpty(blk)) continue;
            var lines = blk.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            // Check for age tag in question line: e.g. "(age:4-6)" or "[4-6]"
            string first = lines[0].Trim();
            int minAge = 0, maxAge = 0;
            // extract (age:x-y)
            var m = Regex.Match(first, @"\(?age\s*[:=]\s*(\d+)\s*-\s*(\d+)\)?", RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                // alternative [4-6] at end
                m = Regex.Match(first, @"\[(\d+)\s*-\s*(\d+)\]\s*$");
            }
            if (m.Success)
            {
                int.TryParse(m.Groups[1].Value, out minAge);
                int.TryParse(m.Groups[2].Value, out maxAge);
                // remove tag from question text
                first = Regex.Replace(first, m.Value, "").Trim();
            }

            string questionText = first;
            var options = new List<string>();
            int correctIdx = -1;

            for (int i = 1; i < lines.Length; i++)
            {
                string l = lines[i].Trim();
                if (string.IsNullOrEmpty(l)) continue;
                bool isCorrect = false;
                if (l.StartsWith("*")) { isCorrect = true; l = l.Substring(1).Trim(); }
                if (l.StartsWith("-")) l = l.Substring(1).Trim();
                if (l.EndsWith("(correct)")) { isCorrect = true; l = Regex.Replace(l, @"\(?correct\)?", "", RegexOptions.IgnoreCase).Trim(); }
                if (string.IsNullOrEmpty(l)) continue;
                options.Add(l);
                if (isCorrect) correctIdx = options.Count - 1;
            }

            // allow inline pipe format if only one line
            if (options.Count < 2 && questionText.Contains("|"))
            {
                var parts = questionText.Split('|');
                if (parts.Length >= 2)
                {
                    questionText = parts[0].Trim();
                    for (int k = 1; k < parts.Length; k++)
                    {
                        string opt = parts[k].Trim();
                        bool isC = false;
                        if (opt.StartsWith("*")) { isC = true; opt = opt.Substring(1).Trim(); }
                        if (opt.EndsWith("*")) { isC = true; opt = opt.TrimEnd('*').Trim(); }
                        options.Add(opt);
                        if (isC) correctIdx = options.Count - 1;
                    }
                }
            }

            if (options.Count < 2)
            {
                Debug.LogWarning($"Pregunta ignorada (menos de 2 opciones): {questionText}");
                continue;
            }
            if (correctIdx < 0) correctIdx = 0;

            SimpleQuestionEntry e = new SimpleQuestionEntry();
            e.id = ""; // let QuestionManager generate stable id if empty
            e.questionText = questionText;
            e.answers = options.ToArray();
            e.correctAnswerIndex = Mathf.Clamp(correctIdx, 0, options.Count - 1);
            e.minAge = minAge;
            e.maxAge = maxAge;

            qd.questions.Add(e);
            count++;
        }

        return count;
    }
}
#endif
