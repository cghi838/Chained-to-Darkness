using UnityEngine;


public class LoadTitle : MonoBehaviour
{
    public GameObject[] titles; // TutorialTitle1~5
    public int currentIndex = 0; // Index of currently active title

    void Awake()
    {
        // Ensure all titles are hidden
        HideAll();
    }

    void Start()
    {
        if (titles != null && titles.Length > 0)
        {
            if (currentIndex < 0) currentIndex = 0;
            if (currentIndex >= titles.Length) currentIndex = titles.Length - 1;
            // Show the title at the starting index
            ShowOnlyIndex(currentIndex);
        }
    }

    // Show one title by its index and hide all others
    public void ShowOnlyIndex(int index)
    {
        if (titles == null || titles.Length == 0) return;
        if (index < 0 || index >= titles.Length) return;

        int i;
        for (i = 0; i < titles.Length; i++)
        {
            SetActive(titles[i], i == index);
        }
        currentIndex = index;
    }

    // Show one specific GameObject and hide all others in the array
    public void ShowOnly(GameObject target)
    {
        if (titles == null || titles.Length == 0 || target == null) return;

        int found = -1;
        for (int i = 0; i < titles.Length; i++)
        {
            bool on = (titles[i] == target);
            if (on) found = i;
            SetActive(titles[i], on);
        }
        // Update the current index for active title
        currentIndex = found;
    }

    // Move to and show the next title 
    public void Next()
    {
        if (titles == null || titles.Length == 0) return;
        int next = currentIndex + 1;
        if (next >= titles.Length) next = titles.Length - 1;
        ShowOnlyIndex(next);
    }

    // Move to and show the previous title 
    public void Prev()
    {
        if (titles == null || titles.Length == 0) return;
        int prev = currentIndex - 1;
        if (prev < 0) prev = 0; // prev to 0
        ShowOnlyIndex(prev);
    }

    // Disable all GameObjects in the array
    public void HideAll()
    {
        if (titles == null) return;

        for (int i = 0; i < titles.Length; i++)
        {
            SetActive(titles[i], false);
        }
    }

    // Set a GameObject's active state
    static void SetActive(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on)
            go.SetActive(on);
    }
}
