using Kitchen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KitchenInGameChat.MessageWindow
{
    public static class PreferenceViewRegistry
    {
        private static Dictionary<Type, ViewType> _viewTypeCache = new Dictionary<Type, ViewType>();
        private static Dictionary<ViewType, Type> _typeCache => _viewTypeCache.ToDictionary(item => item.Value, item => item.Key);
        private static Dictionary<ViewType, GameObject> _prefabs = new Dictionary<ViewType, GameObject>();

        private static bool _requireInit = true;
        internal static GameObject _prefabContainer;

        public static bool TryGetPrefab(ViewType viewType, out GameObject prefab)
        {
            prefab = GetPrefab(viewType);
            return prefab != null;
        }

        public static GameObject GetPrefab(ViewType viewType)
        {
            if (!_prefabs.TryGetValue(viewType, out GameObject gameObject))
            {
                return null;
            }
            return gameObject;
        }

        public static bool TryRegister<T>(out ViewType viewType, bool warn_if_already_registered = true) where T : GenericWindowPreferenceView
        {
            bool success = TryRegister(typeof(T), out viewType, warn_if_already_registered);
            Main.LogInfo($"TryRegister = {success}");
            return success;
        }

        internal static bool TryRegister(Type type, out ViewType viewType, bool warn_if_already_registered = true)
        {
            viewType = (ViewType)int.MinValue;
            if (!type.IsSubclassOf(typeof(GenericWindowPreferenceView)))
            {
                LogError($"{type} is invalid. Type must inherit {typeof(GenericWindowPreferenceView)}");
                return false;
            }

            Init();

            if (_viewTypeCache.TryGetValue(type, out viewType))
            {
                if (warn_if_already_registered)
                    LogWarning($"{type} already registered!");
                return true;
            }

            GameObject windowPrefViewObj = new GameObject(type.Name);
            GenericWindowPreferenceView windowPrefView = (GenericWindowPreferenceView)windowPrefViewObj.AddComponent(type);
            viewType = windowPrefView.Type;

            if (_typeCache.ContainsKey(viewType))
            {
                LogWarning($"ViewType KEY COLLISION between {type} and {_typeCache[viewType]}");
                UnityEngine.Object.Destroy(windowPrefViewObj);
                return true;
            }
            windowPrefViewObj.transform.SetParent(_prefabContainer.transform);

            _viewTypeCache.Add(type, viewType);
            _prefabs.Add(viewType, windowPrefViewObj);
            return true;
        }

        private static void Init()
        {
            if (_requireInit)
            {
                _prefabContainer = new GameObject("PrefViewReg Prefab Container");
                UnityEngine.Object.DontDestroyOnLoad(_prefabContainer);
                _prefabContainer.SetActive(false);
            }
            _requireInit = false;
        }

        private static void LogWarning(string message)
        {
            Debug.LogWarning($"*[In-Game Chat] PreferenceViewRegistry - {message}");
        }
        private static void LogError(string message)
        {
            Debug.LogError($"*[In-Game Chat] PreferenceViewRegistry - {message}");
        }
    }
}
