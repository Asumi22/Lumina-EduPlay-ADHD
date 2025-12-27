using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AgeScroller: genera botones con edades (por defecto 0..100) dentro de un ScrollRect Content.
/// - Arrastra un prefab de Button con un TMP child a `ageButtonPrefab`.
/// - Asigna el `content` (RectTransform) del Scroll View.
/// - Asigna `selectedAgeText` al TMP_Text donde quieres mostrar la edad seleccionada (y que ya usa AuthUIController).
/// </summary>
public class AgeScroller : MonoBehaviour
{
    [Header("Configuración")]
    public RectTransform content;           // Content del ScrollRect
    public Button ageButtonPrefab;          // Prefab: Button con TMP child (sin listeners)
    public TMP_Text selectedAgeText;        // Texto donde se muestra la edad seleccionada
    public int minAge = 0;                  // edad mínima
    public int maxAge = 100;                // edad máxima
    public bool descending = true;          // mostrar descendente (100,99,...)

    // estado
    private int selectedAge = -1;

    void Start()
    {
        PopulateAges();
    }

    void PopulateAges()
    {
        if (content == null || ageButtonPrefab == null)
        {
            Debug.LogWarning("[AgeScroller] content o ageButtonPrefab no asignado.");
            return;
        }

        // limpiar contenido previo
        foreach (Transform t in content) Destroy(t.gameObject);

        List<int> ages = new List<int>();
        for (int a = minAge; a <= maxAge; a++) ages.Add(a);

        if (descending) ages.Reverse(); // si queremos 100..0

        foreach (int age in ages)
        {
            Button btn = Instantiate(ageButtonPrefab, content);
            btn.onClick.RemoveAllListeners();

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = age.ToString();

            // Capturar el closure
            int closureAge = age;
            btn.onClick.AddListener(() => OnAgeClicked(closureAge));
        }

        // Default selection: si hay al menos un item, seleccionamos el primero de la lista
        if (ages.Count > 0)
        {
            selectedAge = ages[0];
            UpdateSelectedText(selectedAge);
        }

        // Opcional: ajustar Content Size Fitter / Layout Group debería encargarse del tamaño
    }

    void OnAgeClicked(int age)
    {
        selectedAge = age;
        UpdateSelectedText(age);
    }

    void UpdateSelectedText(int age)
    {
        if (selectedAgeText != null)
            selectedAgeText.text = age.ToString();
    }

    // utilidad: permite obtener la edad seleccionada desde otros scripts
    public int GetSelectedAge()
    {
        return selectedAge;
    }
}
