using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cubevi_Swizzle
{
    public class UpdateManager : MonoBehaviour
    {
        private static bool applicationIsQuitting = false;
        private static UpdateManager _instance;
        public static UpdateManager Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    return _instance;
                }
                if (_instance == null)
                {
                    GameObject go = new GameObject("UpdateManager");
                    _instance = go.AddComponent<UpdateManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public enum UpdatePriority
        {
            EyeTracking = 0,     // Eye tracking update (Highest priority)
            BatchCamera = 1,     // Camera management (Second priority)
            UI = 2,              // UI update
            Video = 3            // Video playback (Lowest priority)
        }

        private Dictionary<UpdatePriority, List<Action>> updateActions = new Dictionary<UpdatePriority, List<Action>>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 120;

            // Initialize dictionary
            foreach (UpdatePriority priority in Enum.GetValues(typeof(UpdatePriority)))
            {
                updateActions[priority] = new List<Action>();
            }
        }

        private void OnDestroy()
        {
            // Clear all registered update actions
            foreach (UpdatePriority priority in Enum.GetValues(typeof(UpdatePriority)))
            {
                if (updateActions.TryGetValue(priority, out List<Action> actions))
                {
                    actions.Clear();
                }
            }

            applicationIsQuitting = true;
        }

        private void Update()
        {
            // Execute updates in priority order
            foreach (UpdatePriority priority in Enum.GetValues(typeof(UpdatePriority)))
            {
                if (updateActions.TryGetValue(priority, out List<Action> actions))
                {
                    foreach (Action action in actions)
                    {
                        action?.Invoke();
                    }
                }
            }
        }

        public void RegisterUpdate(Action updateAction, UpdatePriority priority)
        {
            if (updateAction != null && updateActions.TryGetValue(priority, out List<Action> actions))
            {
                // Avoid duplicate registration
                if (!actions.Contains(updateAction))
                {
                    actions.Add(updateAction);
                }
            }
        }
        public void UnregisterUpdate(Action updateAction, UpdatePriority priority)
        {
            if (updateAction != null && updateActions.TryGetValue(priority, out List<Action> actions))
            {
                actions.Remove(updateAction);
            }
        }
    }
}
