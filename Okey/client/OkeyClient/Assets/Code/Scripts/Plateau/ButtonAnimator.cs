using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Vector3 pressedScale = new Vector3(0.95f, 0.95f, 0.95f); // Échelle lorsque pressé
    public Vector3 normalScale = Vector3.one; // Échelle normale
    public float animationSpeed = 9f; // Vitesse de l'animation

    private bool isAutomaticallyAnimating = true; // Contrôle si l'animation automatique est active

    void Start()
    {
        // Commence l'animation de respiration automatique
        StartCoroutine(AutomaticPressAndDecompress());
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StopAllCoroutines(); // Arrête toutes les coroutines en cours pour permettre l'animation de pression
        StartCoroutine(AnimateScale(pressedScale, animationSpeed)); // Anime immédiatement vers l'échelle pressée
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StartCoroutine(AnimateScale(normalScale, animationSpeed)); // Anime immédiatement vers l'échelle normale
        StartCoroutine(DelayBeforeAutomaticAnimation()); // Redémarre l'animation automatique après un délai
    }

    IEnumerator AutomaticPressAndDecompress()
    {
        while (isAutomaticallyAnimating)
        {
            // Anime le bouton pour se compresser
            yield return AnimateScale(pressedScale, animationSpeed / 2);
            // Puis anime le bouton pour se décompresser
            yield return AnimateScale(normalScale, animationSpeed / 2);
        }
    }

    IEnumerator AnimateScale(Vector3 targetScale, float speed)
    {
        float step = 0.0f; // variable de progression de l'animation
        Vector3 startingScale = transform.localScale; // échelle de départ
        while (step < 1.0f)
        {
            step += Time.deltaTime * speed;
            transform.localScale = Vector3.Lerp(startingScale, targetScale, step);
            yield return null;
        }
    }

    IEnumerator DelayBeforeAutomaticAnimation()
    {
        isAutomaticallyAnimating = false;
        yield return new WaitForSeconds(1f); // Délai avant de reprendre l'animation automatique
        isAutomaticallyAnimating = true;
        StartCoroutine(AutomaticPressAndDecompress()); // Redémarre l'animation automatique
    }
}
