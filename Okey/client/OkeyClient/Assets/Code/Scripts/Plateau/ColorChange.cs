using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    public Color newColor = new Color(243f / 255f, 240f / 255f, 208f / 255f); // Couleur #F3F0D0

    public void ChangeColor()
    {
        ChangeColorRecursive(transform);
    }

    // Cette fonction parcoure récursivement tous les enfants et change leur couleur
    void ChangeColorRecursive(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = newColor;
            }
            // Appel récursif pour traiter tous les descendants
            if (child.childCount > 0)
            {
                ChangeColorRecursive(child);
            }
        }
    }
}
