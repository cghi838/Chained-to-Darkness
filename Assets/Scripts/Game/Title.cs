using UnityEngine;

public class Title
{
    public static void Hide(GameObject titleObject)
    {
        if (titleObject != null && titleObject.activeSelf)
        {
            titleObject.SetActive(false);
        }
    }

    public static void Show(GameObject titleObject)
    {
        if (titleObject != null && titleObject.activeSelf == false)
        {
            titleObject.SetActive(true);
        }
    }
}