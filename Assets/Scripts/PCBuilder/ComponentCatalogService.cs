using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

/// <summary>
/// Загрузка каталога: 1) PostgreSQL (если включено и задан connection string), 2) TextAsset, 3) StreamingAssets/pc_catalog.json.
/// </summary>
public class ComponentCatalogService : MonoBehaviour
{
    [Header("PostgreSQL (прямое подключение из Unity)")]
    [SerializeField] private bool loadFromPostgresFirst = true;
    [Tooltip("Строка подключения Npgsql, например: Host=127.0.0.1;Port=5432;Username=builder;Password=secret;Database=pc_builder;SSL Mode=Disable")]
    [SerializeField] private string postgresConnectionString = string.Empty;

    [Header("Fallback: JSON")]
    [SerializeField] private string streamingAssetsFileName = "pc_catalog.json";
    [SerializeField] private TextAsset embeddedCatalogJson;

    private readonly List<PcComponentData> m_Items = new List<PcComponentData>();

    public IReadOnlyList<PcComponentData> Items => m_Items;

    private void Awake()
    {
        Reload();
    }

    public void Reload()
    {
        m_Items.Clear();

        string pgConn = postgresConnectionString;
        string envConn = Environment.GetEnvironmentVariable("PC_BUILDER_POSTGRES");
        if (!string.IsNullOrWhiteSpace(envConn))
        {
            pgConn = envConn;
        }

        if (loadFromPostgresFirst && !string.IsNullOrWhiteSpace(pgConn))
        {
            try
            {
                List<PcComponentData> fromDb = PostgresCatalogLoader.Load(pgConn);
                if (fromDb != null && fromDb.Count > 0)
                {
                    m_Items.AddRange(fromDb);
                    Debug.Log($"ComponentCatalogService: загружено {fromDb.Count} компонентов из PostgreSQL.");
                    return;
                }

                Debug.LogWarning("ComponentCatalogService: PostgreSQL вернул 0 строк — пробуем JSON.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"ComponentCatalogService: ошибка PostgreSQL ({e.GetType().Name}): {e.Message}. Пробуем JSON.");
            }
        }

        if (embeddedCatalogJson != null && !string.IsNullOrWhiteSpace(embeddedCatalogJson.text))
        {
            ParseAndAppend(embeddedCatalogJson.text);
            if (m_Items.Count > 0)
            {
                return;
            }
        }

        string path = Path.Combine(Application.streamingAssetsPath, streamingAssetsFileName);
        if (File.Exists(path))
        {
            try
            {
                string text = File.ReadAllText(path);
                ParseAndAppend(text);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"ComponentCatalogService: файл JSON не прочитан: {e.Message}");
            }
        }
    }

    private void ParseAndAppend(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        json = json.Trim();
        if (json.StartsWith("["))
        {
            List<PcComponentData> list = JsonConvert.DeserializeObject<List<PcComponentData>>(json, settings);
            if (list != null)
            {
                m_Items.AddRange(list);
            }

            return;
        }

        PcCatalogJsonRoot root = JsonConvert.DeserializeObject<PcCatalogJsonRoot>(json, settings);
        if (root?.Components != null)
        {
            m_Items.AddRange(root.Components);
        }
    }
}
