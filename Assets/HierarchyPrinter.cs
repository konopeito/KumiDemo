using UnityEngine;

public class HierarchyPrinter : MonoBehaviour
{
    void Start()
    {
        PrintHierarchy(transform, 0);
    }

    void PrintHierarchy(Transform parent, int indent)
    {
        string prefix = new string('-', indent * 2);
        Debug.Log(prefix + parent.name);
        foreach (Transform child in parent)
        {
            PrintHierarchy(child, indent + 1);
        }
    }
}
