using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [SerializeField] Canvas menuCanvas;
    [SerializeField] Image[] menuImages;
    private TextMeshProUGUI[] menuTexts;
    ItemManager itemManager;
    public void Init(ItemManager itemManager)
    {
        this.itemManager = itemManager;
        menuTexts = new TextMeshProUGUI[menuImages.Length];
        for (int i = 0; i < menuImages.Length; i++)
        {
            menuTexts[i] = menuImages[i].GetComponentInChildren<TextMeshProUGUI>();
            menuTexts[i].text = "";
        }
        menuCanvas.enabled = false;
    }
    public void ShowMenu(Transform transform, List<ItemAccess> itemAccesses)
    {
        menuCanvas.enabled = true;
        this.transform.position = transform.position;
        for (int i = 0; i < menuImages.Length; i++)
        {
            if (i >= itemAccesses.Count)
            {
                menuTexts[i].text = "";
                menuImages[i].sprite = null;
            }
            else
            {
                menuTexts[i].text = itemAccesses[i].Num.ToString();
                menuImages[i].sprite = itemManager.GetItemIcon(itemAccesses[i]);
            }
        }
    }
    public void HideMenu()
    {
        menuCanvas.enabled = false;
    }
}
