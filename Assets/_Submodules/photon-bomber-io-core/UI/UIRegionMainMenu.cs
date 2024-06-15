using UnityEngine;
using UnityEngine.UI;

public class UIRegionMainMenu : MonoBehaviour
{
    [SerializeField] private Text textRegion;
    [SerializeField] private GameObject matchMakingObj;

    private void OnEnable()
    {
        if (SimplePhotonNetworkManager.Singleton.isConnectOffline)
        {
            matchMakingObj.SetActive(false);
            textRegion.text = "Offline";
            return;
        }
        matchMakingObj.SetActive(true);
        var uiPhotonNetworking = GetComponentInParent<UIPhotonNetworking>();
        string regionCode = PlayerPrefs.GetString("SAVE_SELECTED_REGION", string.Empty);
        foreach (var region in uiPhotonNetworking.selectableRegions)
        {
            if (region.regionCode.Equals(regionCode))
            {
                textRegion.text = "Server: " + region.title;
                return;
            }
        }
    }
}
