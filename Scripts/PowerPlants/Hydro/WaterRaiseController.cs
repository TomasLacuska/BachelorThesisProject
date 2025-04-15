#region WaterRiseController Class
/*
 * =========================================================
 * WaterRiseController
 *
 * Controls the water rising animation by smoothly interpolating
 * the water plane's Y position over a specified duration.
 * =========================================================
 */
#endregion

using UnityEngine;
using System.Collections;

public class WaterRiseController : MonoBehaviour
{
    #region Public Methods

    // Animate water rising to targetY over duration seconds.
    public IEnumerator RiseWater(float targetY, float duration)
    {
        float startY = transform.position.y;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newY = Mathf.Lerp(startY, targetY, elapsed / duration);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            yield return null;
        }
        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
    }

    // Starts the water rise coroutine.
    public void StartRising(float targetY, float duration)
    {
        StartCoroutine(RiseWater(targetY, duration));
    }

    #endregion
}
