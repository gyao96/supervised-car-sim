using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetect : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject panel;

    // Update is called once per frame
    void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "obstacle")
        {
            Debug.Log("Collide");
            Time.timeScale = 0f;
            panel.SetActive(true);
        }
    }
}
