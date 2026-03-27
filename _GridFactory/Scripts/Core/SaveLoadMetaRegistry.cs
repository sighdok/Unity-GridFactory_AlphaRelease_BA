using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridFactory.Meta
{
    [CreateAssetMenu(menuName = "GridFactory/SaveLoadMetaRegistry")]
    public class SaveLoadMetaRegistry : ScriptableObject
    {
        [Serializable] public class Entry { public string id; public GameObject prefab; }

        public List<Entry> _machines = new();
        public List<Entry> _conveyors = new();

        private Dictionary<string, GameObject> _m;
        private Dictionary<string, GameObject> _c;

        public GameObject GetMachine(string id)
        {
            Ensure();
            return _m.TryGetValue(id, out var p) ? p : null;
        }

        public GameObject GetConveyor(string id)
        {
            Ensure();
            return _c.TryGetValue(id, out var p) ? p : null;
        }

        private void Ensure()
        {
            if (_m != null)
                return;

            _m = new Dictionary<string, GameObject>();
            foreach (var e in _machines)
                if (!string.IsNullOrEmpty(e.id) && e.prefab != null)
                    _m[e.id] = e.prefab;

            _c = new Dictionary<string, GameObject>();
            foreach (var e in _conveyors)
                if (!string.IsNullOrEmpty(e.id) && e.prefab != null)
                    _c[e.id] = e.prefab;
        }
    }
}
