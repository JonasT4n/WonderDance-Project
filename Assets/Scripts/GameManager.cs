using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WonderDanceProj
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager singleton;

        [SerializeField] private ConfigSettings config = null;
        
        private BeatMapPlayer beatMapPlayer = new BeatMapPlayer();

        #region Unity BuiltIn Methods
        private async void Awake()
        {
            // Check singleton exists
            if (singleton != null)
            {
                Debug.LogWarning($"Deleted extra object of singleton behaviour");
                Destroy(this);
                return;
            }

            // Set singleton if not exists yet
            singleton = this;
            await DataFileLoader.LoadData();
        }

        private void OnDestroy()
        {
            // Safe release static data
            singleton = null;
            DataFileLoader.SaveData();
            DataFileLoader.ClearLoadedData();
        }
        #endregion
    }

}
