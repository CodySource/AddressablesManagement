using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Events;

namespace CodySource
{
    public class AddressablesManager : MonoBehaviour
    {

        #region PROPERTIES

        /// <summary>
        /// Auto-Instantiates the addressables manager
        /// </summary>
        public static AddressablesManager instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new GameObject().AddComponent<AddressablesManager>();
                DontDestroyOnLoad(_instance);
                return _instance;
            }
        }
        private static AddressablesManager _instance = null;

        /// <summary>
        /// Invoked whenever an object is instantiated
        /// </summary>
        public static UnityEvent<GameObject> onInstantiate = new UnityEvent<GameObject>();

        /// <summary>
        /// The current unlinked handles
        /// </summary>
        public static List<AsyncOperationHandle<GameObject>> handles => _handles;
        private static List<AsyncOperationHandle<GameObject>> _handles = new List<AsyncOperationHandle<GameObject>>();

        /// <summary>
        /// Tracks all new instances
        /// </summary>
        private static List<int> _newHandleIndexes = new List<int>();

        /// <summary>
        /// Used to remove instances after notification of instantiation
        /// </summary>
        List<int> _remove = new List<int>();

        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Instantiates an addressable gameobject
        /// </summary>
        public static void Instantiate(string pName)
        {
            _instance = _instance ?? instance;
            AsyncOperationHandle<GameObject> _handle = Addressables.InstantiateAsync(pName);
            _handles.Add(_handle);
            _newHandleIndexes.Add(_handles.Count - 1);
        }

        /// <summary>
        /// Instantiates an addressable gameobject and auto-subscribes the provided action to the instantiation event
        /// </summary>
        public static void Instantiate(string pName, UnityAction<GameObject> pOnInstantiated)
        {
            onInstantiate.RemoveListener(_Cleanup);
            onInstantiate.AddListener(pOnInstantiated);
            onInstantiate.AddListener(_Cleanup);
            Instantiate(pName);

            void _Cleanup(GameObject pNull) => onInstantiate.RemoveListener(pOnInstantiated);
        }

        /// <summary>
        /// Destroys an unlinked addressable by passing in a gameobject for comparison
        /// </summary>
        public static void Destroy(string pName) => _DestroyHandle(_handles.Find(h => h.Result.name == pName));

        /// <summary>
        /// Cleans up all handles
        /// </summary>
        public static void Cleanup()
        {
            while (_handles.Count > 0) _DestroyHandle(_handles[0]);
        }

        #endregion

        #region PROTECTED METHODS

        /// <summary>
        /// Destroys a handle
        /// </summary>
        protected static void _DestroyHandle(AsyncOperationHandle<GameObject> pHandle)
        {
            if (!pHandle.IsValid()) return;
            GameObject _obj = pHandle.Result;
            Addressables.ReleaseInstance(_obj);
            _handles.Remove(pHandle);
        }

        #endregion

        #region PRIVATE METHODS

        /// <summary>
        /// Removes all handles / destroys all instantiated objects
        /// </summary>
        private void OnDestroy() => Cleanup();

        /// <summary>
        /// Checks for new instances
        /// </summary>
        private void Update() => _CheckHandlesForNewInstances();

        /// <summary>
        /// Checks for new instances and notifies whenever one is instantiated
        /// </summary>
        private void _CheckHandlesForNewInstances()
        {
            if (_newHandleIndexes.Count == 0) return;
            for (int i = 0; i < _newHandleIndexes.Count; i++)
            {
                int index = _newHandleIndexes[i];
                if (!_handles[index].IsDone) continue;
                _remove.Add(index);
                onInstantiate?.Invoke(_handles[index].Result);
            }
            _remove.ForEach(i => _newHandleIndexes.RemoveAt(i));
            _remove.Clear();
        }

        #endregion

    }
}