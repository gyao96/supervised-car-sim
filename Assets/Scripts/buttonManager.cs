using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class buttonManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Color startColor;
    public Color mouseOverColor;

    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponent<Image>().color = mouseOverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponent<Image>().color = startColor;
    }

    public void LoadScene(int index)
    {
        SceneManager.LoadScene(index);
    }

    public void StopResume()
    {
        if (GetComponentInChildren<Text>().text == "RESUME")
            GetComponentInChildren<Text>().text = "STOP";
        else
            GetComponentInChildren<Text>().text = "RESUME";
    }
}
