using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Читает каталог из PostgreSQL без прямой ссылки на Npgsql в проекте.
/// Положите сборку <b>Npgsql.dll</b> (и зависимости из того же NuGet-пакета) в <c>Assets/Plugins/Npgsql/</c> —
/// см. <c>tools/InstallNpgsqlForUnity.ps1</c> или NuGet For Unity.
/// </summary>
public static class PostgresCatalogLoader
{
    private const string Sql = @"
SELECT
  c.id,
  cc.code AS category_code,
  c.name,
  c.vendor,
  c.model,
  COALESCE(c.description, '') AS description,
  (c.price)::double precision AS price,
  COALESCE(c.power_watts, 0) AS power_watts,
  c.model_tier,
  COALESCE(c.icon_path, '') AS icon_path,
  cpu.socket AS cpu_socket,
  cpu.ram_type AS cpu_ram_type,
  mb.socket AS mb_socket,
  mb.ram_type AS mb_ram_type,
  COALESCE(mb.ram_slots, 0) AS mb_ram_slots,
  COALESCE(mb.max_ram_gb, 0) AS mb_max_ram_gb,
  ram.ram_type AS ram_ram_type,
  COALESCE(gpu.required_psu_w, 0) AS gpu_required_psu_w,
  COALESCE(gpu.tdp_w, 0) AS gpu_tdp_w,
  COALESCE(psu.wattage, 0) AS psu_wattage,
  COALESCE(st.storage_type, '') AS storage_type,
  COALESCE(st.capacity_gb, 0) AS capacity_gb,
  (SELECT string_agg(css.socket, ',' ORDER BY css.socket)
   FROM cooler_socket_support css WHERE css.cooler_id = c.id) AS cooler_sockets
FROM components c
JOIN component_categories cc ON cc.id = c.category_id
LEFT JOIN cpu_specs cpu ON cpu.component_id = c.id
LEFT JOIN motherboard_specs mb ON mb.component_id = c.id
LEFT JOIN ram_specs ram ON ram.component_id = c.id
LEFT JOIN gpu_specs gpu ON gpu.component_id = c.id
LEFT JOIN psu_specs psu ON psu.component_id = c.id
LEFT JOIN storage_specs st ON st.component_id = c.id
WHERE c.is_active IS DISTINCT FROM FALSE
ORDER BY c.id;
";

    public static List<PcComponentData> Load(string connectionString)
    {
        var list = new List<PcComponentData>();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return list;
        }

        Type connectionType = FindNpgsqlConnectionType();
        if (connectionType == null)
        {
            Debug.LogWarning(
                "PostgreSQL: сборка Npgsql не найдена. Запустите tools/InstallNpgsqlForUnity.ps1 " +
                "или установите пакет через NuGet For Unity, затем перезапустите Unity.");
            return list;
        }

        try
        {
            using (IDisposable conn = (IDisposable)Activator.CreateInstance(connectionType, connectionString))
            {
                connectionType.GetMethod("Open", Type.EmptyTypes)?.Invoke(conn, null);
                using (IDisposable cmd = CreateCommand(connectionType, conn, Sql))
                {
                    object reader = cmd.GetType().GetMethod("ExecuteReader", Type.EmptyTypes)?.Invoke(cmd, null);
                    if (reader == null)
                    {
                        return list;
                    }

                    try
                    {
                        MethodInfo read = reader.GetType().GetMethod("Read", Type.EmptyTypes);
                        while (read != null && (bool)read.Invoke(reader, null))
                        {
                            list.Add(MapRow(reader));
                        }
                    }
                    finally
                    {
                        (reader as IDisposable)?.Dispose();
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PostgreSQL: ошибка запроса — {e.GetType().Name}: {e.Message}");
        }

        return list;
    }

    private static Type FindNpgsqlConnectionType()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name != "Npgsql")
            {
                continue;
            }

            Type t = assembly.GetType("Npgsql.NpgsqlConnection");
            if (t != null)
            {
                return t;
            }
        }

        try
        {
            Assembly loaded = Assembly.Load("Npgsql");
            return loaded?.GetType("Npgsql.NpgsqlConnection");
        }
        catch
        {
            return null;
        }
    }

    private static IDisposable CreateCommand(Type connectionType, IDisposable conn, string sql)
    {
        object connection = conn;
        MethodInfo createCommand = connectionType.GetMethod("CreateCommand", Type.EmptyTypes);
        if (createCommand == null)
        {
            throw new MissingMethodException("Npgsql.NpgsqlConnection.CreateCommand");
        }

        object cmd = createCommand.Invoke(connection, null);
        cmd.GetType().GetProperty("CommandText")?.SetValue(cmd, sql);
        return (IDisposable)cmd;
    }

    private static PcComponentData MapRow(object reader)
    {
        Type rt = reader.GetType();
        MethodInfo getOrdinal = rt.GetMethod("GetOrdinal", new[] { typeof(string) });
        MethodInfo isDbNull = rt.GetMethod("IsDBNull", new[] { typeof(int) });
        MethodInfo getString = rt.GetMethod("GetString", new[] { typeof(int) });
        MethodInfo getInt32 = rt.GetMethod("GetInt32", new[] { typeof(int) });
        MethodInfo getInt64 = rt.GetMethod("GetInt64", new[] { typeof(int) });
        MethodInfo getValue = rt.GetMethod("GetValue", new[] { typeof(int) });

        string categoryCode = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "category_code");
        var d = new PcComponentData
        {
            DatabaseId = ReadInt(reader, getOrdinal, isDbNull, getInt32, getInt64, getValue, "id"),
            CategoryCode = categoryCode,
            Name = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "name"),
            Vendor = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "vendor"),
            Model = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "model"),
            Description = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "description"),
            Price = (decimal)ReadDouble(reader, getOrdinal, isDbNull, getValue, "price"),
            PowerWatts = ReadInt(reader, getOrdinal, isDbNull, getInt32, getInt64, getValue, "power_watts"),
            ModelTier = Mathf.Clamp(ReadInt(reader, getOrdinal, isDbNull, getInt32, getInt64, getValue, "model_tier"), 1, 3),
            IconPath = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "icon_path")
        };

        string code = (categoryCode ?? string.Empty).Trim().ToUpperInvariant();
        switch (code)
        {
            case "CPU":
                d.Socket = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "cpu_socket");
                d.RamType = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "cpu_ram_type");
                break;
            case "MOTHERBOARD":
                d.Socket = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "mb_socket");
                d.RamType = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "mb_ram_type");
                d.RamSlots = ReadInt(reader, getOrdinal, isDbNull, getInt32, getInt64, getValue, "mb_ram_slots");
                d.MaxRamGb = ReadInt(reader, getOrdinal, isDbNull, getInt32, getInt64, getValue, "mb_max_ram_gb");
                break;
            case "RAM":
                d.RamType = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "ram_ram_type");
                break;
            case "GPU":
                d.RequiredPsuW = ReadInt(reader, getOrdinal, isDbNull, getInt32, getInt64, getValue, "gpu_required_psu_w");
                d.GpuTdpW = ReadInt(reader, getOrdinal, isDbNull, getInt32, getInt64, getValue, "gpu_tdp_w");
                break;
            case "PSU":
                d.PsuWattage = ReadInt(reader, getOrdinal, isDbNull, getInt32, getInt64, getValue, "psu_wattage");
                break;
            case "STORAGE":
                d.StorageType = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "storage_type");
                d.CapacityGb = ReadInt(reader, getOrdinal, isDbNull, getInt32, getInt64, getValue, "capacity_gb");
                break;
            case "CPU_COOLER":
                d.CoolerSocketsCsv = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "cooler_sockets");
                break;
        }

        return d;
    }

    private static int ReadInt(object reader, MethodInfo getOrdinal, MethodInfo isDbNull, MethodInfo getInt32, MethodInfo getInt64, MethodInfo getValue, string column)
    {
        int ord = (int)getOrdinal.Invoke(reader, new object[] { column });
        if ((bool)isDbNull.Invoke(reader, new object[] { ord }))
        {
            return 0;
        }

        if (getInt32 != null)
        {
            try
            {
                return (int)getInt32.Invoke(reader, new object[] { ord });
            }
            catch
            {
                // fall through
            }
        }

        if (getInt64 != null)
        {
            try
            {
                return (int)(long)getInt64.Invoke(reader, new object[] { ord });
            }
            catch
            {
                // fall through
            }
        }

        object v = getValue.Invoke(reader, new object[] { ord });
        return Convert.ToInt32(v);
    }

    private static double ReadDouble(object reader, MethodInfo getOrdinal, MethodInfo isDbNull, MethodInfo getValue, string column)
    {
        int ord = (int)getOrdinal.Invoke(reader, new object[] { column });
        if ((bool)isDbNull.Invoke(reader, new object[] { ord }))
        {
            return 0d;
        }

        object v = getValue.Invoke(reader, new object[] { ord });
        return Convert.ToDouble(v);
    }

    private static string ReadString(object reader, MethodInfo getOrdinal, MethodInfo isDbNull, MethodInfo getString, MethodInfo getValue, string column)
    {
        int ord = (int)getOrdinal.Invoke(reader, new object[] { column });
        if ((bool)isDbNull.Invoke(reader, new object[] { ord }))
        {
            return string.Empty;
        }

        if (getString != null)
        {
            try
            {
                return (string)getString.Invoke(reader, new object[] { ord });
            }
            catch
            {
                // fall through
            }
        }

        object v = getValue.Invoke(reader, new object[] { ord });
        return v?.ToString() ?? string.Empty;
    }
}
