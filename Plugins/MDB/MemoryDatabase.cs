//***************************************************************************************
// Writer: Stylish Esper
//***************************************************************************************

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Esper.MemoryDB
{
    /// <summary>
    /// An in-memory database with support for multiple tables.
    /// </summary>
    public class MemoryDatabase : ScriptableObject
    {
        /// <summary>
        /// A list of all tables.
        /// </summary>
        [SerializeField] private List<Table> tables = new();

        /// <summary>
        /// Gets or creates a table by name.
        /// </summary>
        public Table GetOrCreateTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("tableName cannot be null or empty", nameof(tableName));

            // Ensure tables list is initialized
            if (tables == null)
                tables = new List<Table>();

            // Safely find table
            Table table = null;
            for (int i = 0; i < tables.Count; i++)
            {
                var t = tables[i];
                if (t == null) continue; // skip null entries
                if (t.TableName == tableName)
                {
                    table = t;
                    break;
                }
            }

            // If not found, create a new table
            if (table == null)
            {
                table = new Table(tableName);
                tables.Add(table);
            }

            table.Initialize();
            return table;
        }

        /// <summary>
        /// Inserts or updates a value by key in the specified table.
        /// </summary>
        public void Set<T>(string tableName, string key, T value)
        {
            var table = GetOrCreateTable(tableName);
            table.Set(key, value);
        }

        /// <summary>
        /// Retrieves a value by key from the specified table.
        /// </summary>
        public bool TryGet<T>(string tableName, string key, out T value)
        {
            var table = GetOrCreateTable(tableName);
            return table.TryGet(key, out value);
        }

        /// <summary>
        /// Retrieves the first record in the table where a property equals the given value.
        /// </summary>
        public bool TryGetByProperty<T>(string tableName, string propertyName, string value, out T result)
        {
            var table = GetOrCreateTable(tableName);
            return table.TryGetByProperty(propertyName, value, out result);
        }

        /// <summary>
        /// Deletes a value by key from the specified table.
        /// </summary>
        public bool Delete(string tableName, string key)
        {
            var table = GetOrCreateTable(tableName);
            bool removed = table.Delete(key);
            return removed;
        }

        /// <summary>
        /// Clears all records in the specified table.
        /// </summary>
        public void ClearTable(string tableName)
        {
            // Ensure tables list is initialized
            if (tables == null)
                tables = new List<Table>();

            var table = GetOrCreateTable(tableName);

            // Safety check: table may still be null
            if (table != null)
                table.Clear();
        }

        /// <summary>
        /// Clears all tables in the database.
        /// </summary>
        public void ClearAll()
        {
            foreach (var table in tables)
            {
                table.Clear();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Creates a MemoryDatabase asset if it doesn't exist.
        /// </summary>
        public static MemoryDatabase CreateIfNotExists(string path)
        {
            var db = AssetDatabase.LoadAssetAtPath<MemoryDatabase>(path);
            if (db == null)
            {
                db = CreateInstance<MemoryDatabase>();
                AssetDatabase.CreateAsset(db, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return db;
        }
#endif

        /// <summary>
        /// Represents a single table in the database.
        /// </summary>
        [Serializable]
        public class Table
        {
            /// <summary>
            /// Serialized name.
            /// </summary>
            [SerializeField] private string tableName;

            /// <summary>
            /// Serialized records for persistence.
            /// </summary>
            [SerializeField] private List<Record> records = new();

            /// <summary>
            /// Runtime cache for quick lookup. Not serialized.
            /// </summary>
            [NonSerialized] private Dictionary<string, object> cache = new();

            /// <summary>
            /// Constructor for runtime-created tables.
            /// </summary>
            public Table(string name)
            {
                tableName = name;
                records = records ?? new List<Record>();
            }

            /// <summary>
            /// Table name (read-only).
            /// </summary>
            public string TableName => tableName;

            /// <summary>
            /// Record list (read-only).
            /// </summary>
            public List<Record> Records => records;

            /// <summary>
            /// Initialize runtime cache from serialized records.
            /// </summary>
            public void Initialize()
            {
                if (cache != null) return;
                cache = new Dictionary<string, object>();

                if (records == null)
                    records = new List<Record>();

                for (int i = 0; i < records.Count; i++)
                {
                    var r = records[i];
                    if (r == null || string.IsNullOrEmpty(r.key)) continue;
                    cache[r.key] = r.json;
                }
            }

            /// <summary>
            /// Inserts or updates a value by key in this table.
            /// </summary>
            public void Set<T>(string key, T value)
            {
                Initialize();
                cache[key] = value;
                string json = value.ToJson();
                var existing = records.Find(r => r != null && r.key == key);
                if (existing != null) existing.json = json;
                else records.Add(new Record { key = key, json = json });
            }

            /// <summary>
            /// Retrieves a value by key from this table.
            /// </summary>
            public bool TryGet<T>(string key, out T value)
            {
                Initialize();

                if (cache.TryGetValue(key, out object obj))
                {
                    if (obj is T t)
                    {
                        value = t;
                        return true;
                    }
                    else if (obj is string json)
                    {
                        try
                        {
                            value = json.ToObject<T>();
                            cache[key] = value;
                            return true;
                        }
                        catch
                        {
                            value = default;
                            return false;
                        }
                    }
                }

                value = default;
                return false;
            }


            /// <summary>
            /// Find first record where property equals the given value.
            /// </summary>
            public bool TryGetByProperty<T>(string propertyName, string propertyValue, out T result)
            {
                result = default;
                Initialize();
                if (records == null || cache == null) return false;

                foreach (var kvp in cache.ToList()) // avoid modifying during enumeration
                {
                    object obj = kvp.Value;

                    // Deserialize JSON if needed
                    if (obj is string json)
                    {
                        try
                        {
                            obj = json.ToObject<T>();
                            cache[kvp.Key] = obj;
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (obj == null) continue;

                    var propInfo = obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (propInfo == null) continue;

                    var val = propInfo.GetValue(obj);
                    if (val != null && Convert.ToString(val).ToLowerInvariant() == propertyValue.ToLowerInvariant())
                    {
                        if (obj is T t)
                        {
                            result = t;
                            return true;
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// Deletes a value by key from this table.
            /// </summary>
            public bool Delete(string key)
            {
                Initialize();
                bool removedCache = cache != null && cache.Remove(key);
                var record = records.Find(r => r.key == key);
                bool removedRecord = record != null && records.Remove(record);
                return removedCache || removedRecord;
            }

            /// <summary>
            /// Clears all records in this table.
            /// </summary>
            public void Clear()
            {
                if (cache != null) cache.Clear();
                if (records != null) records.Clear();
            }
        }

        /// <summary>
        /// A single database record.
        /// </summary>
        [Serializable]
        public class Record
        {
            public string key;
            public string json;
        }
    }
}
