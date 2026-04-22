using System.Collections;
using UnityEngine;

public class MainMenuScenerySwapper : MonoBehaviour
{
    [SerializeField] private GameObject[] sceneryOptions;
    [SerializeField] private float interval = 9f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
     StartCoroutine(SwapSceneryRoutine());   
    }

    IEnumerator SwapSceneryRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            float randomIndex = Random.Range(0, sceneryOptions.Length);
            for (int i = 0; i < sceneryOptions.Length; i++)
            {
                if (i == randomIndex)
                {
                    sceneryOptions[i].SetActive(true);
                }
                else
                {
                    sceneryOptions[i].SetActive(false);
                }
            }
        }
    }
}
