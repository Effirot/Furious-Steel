using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleDisolver : MonoBehaviour
{
    [SerializeField]
    private float value;
    
    [SerializeField]
    private float startAfter = 3;

#if !UNITY_SERVER || UNITY_EDITOR
    private void Start()
    {
        StartCoroutine(DisolveProcess());
    }

    private IEnumerator DisolveProcess()
    {
        yield return new WaitForSeconds(startAfter);
        var endOfFrame = new WaitForEndOfFrame();

        while (transform.localScale.magnitude > 0.05f)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.zero, value * 10 * Time.deltaTime);

            yield return endOfFrame;
        }
    }
#endif
}