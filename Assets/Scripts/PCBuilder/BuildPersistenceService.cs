using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using EasyBuildSystem.Features.Runtime.Buildings.Part;

public class BuildPersistenceService : MonoBehaviour
{
    private const string SessionUserPrefsKey = "pc_builder_session_user_id";
    private static int s_SessionUserId = -1;

    [Header("DB")]
    [SerializeField] private string postgresConnectionString = string.Empty;
    [SerializeField] private int defaultUserId = 1;
    [SerializeField] private int currentUserId = -1;

    [Header("Dependencies")]
    [SerializeField] private PCAssemblyState assemblyState;
    [SerializeField] private ComponentCatalogService catalogService;
    [SerializeField] private PcPrefabCatalogMap prefabCatalogMap;

    [Header("Scene")]
    [SerializeField] private Transform loosePlacementRoot;

    public int CurrentUserId => currentUserId > 0 ? currentUserId : s_SessionUserId;
    public bool HasAuthenticatedUser => CurrentUserId > 0;

    private void Awake()
    {
        if (currentUserId > 0)
        {
            s_SessionUserId = currentUserId;
        }
        else if (s_SessionUserId <= 0 && PlayerPrefs.HasKey(SessionUserPrefsKey))
        {
            int restored = PlayerPrefs.GetInt(SessionUserPrefsKey, -1);
            if (restored > 0)
            {
                s_SessionUserId = restored;
                currentUserId = restored;
            }
        }
    }

    public void SetCurrentUserId(int userId)
    {
        currentUserId = userId > 0 ? userId : -1;
        s_SessionUserId = currentUserId;
        if (currentUserId > 0)
        {
            PlayerPrefs.SetInt(SessionUserPrefsKey, currentUserId);
        }
        else
        {
            PlayerPrefs.DeleteKey(SessionUserPrefsKey);
        }
        PlayerPrefs.Save();
    }

    public bool RegisterUser(string username, string password, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(username))
        {
            errorMessage = "Введите логин.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
        {
            errorMessage = "Пароль должен быть не короче 4 символов.";
            return false;
        }

        if (!TryResolveConnectionString(out string connString))
        {
            errorMessage = "Не задана строка подключения к БД.";
            return false;
        }

        Type connectionType = FindNpgsqlConnectionType();
        if (connectionType == null)
        {
            errorMessage = "Npgsql не найден.";
            return false;
        }

        const string sql = @"
INSERT INTO users (username, password_hash)
VALUES (@u, @p)
RETURNING id;";

        try
        {
            using (IDisposable conn = (IDisposable)Activator.CreateInstance(connectionType, connString))
            {
                InvokeInstanceMethod(conn, "Open");
                using (IDisposable cmd = CreateCommand(connectionType, conn, sql))
                {
                    AddParameter(cmd, "@u", username.Trim());
                    AddParameter(cmd, "@p", HashPassword(password));
                    object scalar = InvokeInstanceMethod(cmd, "ExecuteScalar");
                    int userId = Convert.ToInt32(scalar);
                    SetCurrentUserId(userId);
                    return true;
                }
            }
        }
        catch (Exception e)
        {
            errorMessage = "Не удалось зарегистрировать пользователя: " + e.Message;
            Debug.LogWarning($"BuildPersistence RegisterUser: {e.GetType().Name}: {e.Message}\n{e}");
            return false;
        }
    }

    public bool LoginUser(string username, string password, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            errorMessage = "Введите логин и пароль.";
            return false;
        }

        if (!TryResolveConnectionString(out string connString))
        {
            errorMessage = "Не задана строка подключения к БД.";
            return false;
        }

        Type connectionType = FindNpgsqlConnectionType();
        if (connectionType == null)
        {
            errorMessage = "Npgsql не найден.";
            return false;
        }

        const string sql = @"
SELECT id, password_hash
FROM users
WHERE username = @u
LIMIT 1;";

        try
        {
            using (IDisposable conn = (IDisposable)Activator.CreateInstance(connectionType, connString))
            {
                InvokeInstanceMethod(conn, "Open");
                using (IDisposable cmd = CreateCommand(connectionType, conn, sql))
                {
                    AddParameter(cmd, "@u", username.Trim());
                    object reader = InvokeInstanceMethod(cmd, "ExecuteReader");
                    if (reader == null)
                    {
                        errorMessage = "Не удалось прочитать пользователя.";
                        return false;
                    }

                    try
                    {
                        MethodInfo read = GetZeroArgInstanceMethod(reader.GetType(), "Read");
                        if (read == null || !(bool)read.Invoke(reader, null))
                        {
                            errorMessage = "Пользователь не найден.";
                            return false;
                        }

                        MethodInfo getOrdinal = FindMethod(reader.GetType(), "GetOrdinal", typeof(string));
                        MethodInfo isDbNull = FindMethod(reader.GetType(), "IsDBNull", typeof(int));
                        MethodInfo getInt32 = FindMethod(reader.GetType(), "GetInt32", typeof(int));
                        MethodInfo getString = FindMethod(reader.GetType(), "GetString", typeof(int));
                        MethodInfo getValue = FindMethod(reader.GetType(), "GetValue", typeof(int));

                        int id = ReadInt(reader, getOrdinal, isDbNull, getInt32, getValue, "id");
                        string storedHash = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "password_hash");
                        string incomingHash = HashPassword(password);
                        if (!string.Equals(storedHash, incomingHash, StringComparison.Ordinal))
                        {
                            errorMessage = "Неверный пароль.";
                            return false;
                        }

                        SetCurrentUserId(id);
                        TryUpdateLastLogin(connectionType, conn, id);
                        return true;
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
            errorMessage = "Ошибка логина: " + e.Message;
            Debug.LogWarning($"BuildPersistence LoginUser: {e.GetType().Name}: {e.Message}\n{e}");
            return false;
        }
    }

    public List<BuildListItem> GetBuilds()
    {
        var list = new List<BuildListItem>();
        if (!EnsureAuthenticated("GetBuilds"))
        {
            return list;
        }

        if (!TryResolveConnectionString(out string connString))
        {
            return list;
        }

        Type connectionType = FindNpgsqlConnectionType();
        if (connectionType == null)
        {
            Debug.LogWarning("BuildPersistence: Npgsql не найден.");
            return list;
        }

        const string sql = @"
SELECT b.id, b.name, b.created_at
FROM builds b
WHERE b.user_id = @uid
ORDER BY b.created_at DESC, b.id DESC;";

        try
        {
            using (IDisposable conn = (IDisposable)Activator.CreateInstance(connectionType, connString))
            {
                InvokeInstanceMethod(conn, "Open");
                using (IDisposable cmd = CreateCommand(connectionType, conn, sql))
                {
                    AddParameter(cmd, "@uid", GetEffectiveUserId());
                    object reader = InvokeInstanceMethod(cmd, "ExecuteReader");
                    if (reader == null)
                    {
                        return list;
                    }

                    try
                    {
                        MethodInfo read = GetZeroArgInstanceMethod(reader.GetType(), "Read");
                        MethodInfo getOrdinal = FindMethod(reader.GetType(), "GetOrdinal", typeof(string));
                        MethodInfo isDbNull = FindMethod(reader.GetType(), "IsDBNull", typeof(int));
                        MethodInfo getInt32 = FindMethod(reader.GetType(), "GetInt32", typeof(int));
                        MethodInfo getString = FindMethod(reader.GetType(), "GetString", typeof(int));
                        MethodInfo getDateTime = FindMethod(reader.GetType(), "GetDateTime", typeof(int));
                        MethodInfo getValue = FindMethod(reader.GetType(), "GetValue", typeof(int));

                        while (read != null && (bool)read.Invoke(reader, null))
                        {
                            var item = new BuildListItem
                            {
                                BuildId = ReadInt(reader, getOrdinal, isDbNull, getInt32, getValue, "id"),
                                Name = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "name"),
                                CreatedAt = ReadDateTime(reader, getOrdinal, isDbNull, getDateTime, getValue, "created_at")
                            };
                            list.Add(item);
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
            Debug.LogWarning($"BuildPersistence GetBuilds: {e.GetType().Name}: {e.Message}\n{e}");
        }

        return list;
    }

    public bool SaveCurrentBuild(string buildName)
    {
        if (!EnsureAuthenticated("SaveCurrentBuild"))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(buildName))
        {
            Debug.LogWarning("BuildPersistence: имя сборки пустое.");
            return false;
        }

        if (assemblyState == null) assemblyState = FindFirstObjectByType<PCAssemblyState>();
        if (assemblyState == null)
        {
            Debug.LogWarning("BuildPersistence: PCAssemblyState не найден.");
            return false;
        }

        List<PCComponent> components = assemblyState.GetInstalledComponentsSnapshot();
        if (components.Count == 0)
        {
            Debug.LogWarning("BuildPersistence: нечего сохранять, сборка пустая.");
            return false;
        }

        if (!TryResolveConnectionString(out string connString))
        {
            return false;
        }

        Type connectionType = FindNpgsqlConnectionType();
        if (connectionType == null)
        {
            Debug.LogWarning("BuildPersistence: Npgsql не найден.");
            return false;
        }

        const string insertBuildSql = @"
INSERT INTO builds (user_id, name, is_validated, score)
VALUES (@uid, @name, FALSE, 0)
RETURNING id;";

        const string insertItemSql = @"
INSERT INTO build_items
(build_id, slot_code, component_id, position_x, position_y, position_z, rotation_x, rotation_y, rotation_z)
VALUES
(@bid, @slot, @cid, @px, @py, @pz, @rx, @ry, @rz);";

        try
        {
            using (IDisposable conn = (IDisposable)Activator.CreateInstance(connectionType, connString))
            {
                InvokeInstanceMethod(conn, "Open");

                int buildId;
                using (IDisposable cmd = CreateCommand(connectionType, conn, insertBuildSql))
                {
                    AddParameter(cmd, "@uid", GetEffectiveUserId());
                    AddParameter(cmd, "@name", buildName.Trim());
                    object scalar = InvokeInstanceMethod(cmd, "ExecuteScalar");
                    buildId = Convert.ToInt32(scalar);
                }

                for (int i = 0; i < components.Count; i++)
                {
                    PCComponent c = components[i];
                    if (c == null || c.DatabaseId <= 0)
                    {
                        continue;
                    }

                    string slotCode = c.ParentSlot != null ? c.ParentSlot.SlotType.ToString() : "LOOSE";
                    Vector3 p = c.transform.position;
                    Vector3 r = c.transform.eulerAngles;

                    using (IDisposable itemCmd = CreateCommand(connectionType, conn, insertItemSql))
                    {
                        AddParameter(itemCmd, "@bid", buildId);
                        AddParameter(itemCmd, "@slot", slotCode);
                        AddParameter(itemCmd, "@cid", c.DatabaseId);
                        AddParameter(itemCmd, "@px", p.x);
                        AddParameter(itemCmd, "@py", p.y);
                        AddParameter(itemCmd, "@pz", p.z);
                        AddParameter(itemCmd, "@rx", r.x);
                        AddParameter(itemCmd, "@ry", r.y);
                        AddParameter(itemCmd, "@rz", r.z);
                        InvokeInstanceMethod(itemCmd, "ExecuteNonQuery");
                    }
                }

                Debug.Log($"BuildPersistence: сборка '{buildName}' сохранена (id={buildId}).");
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"BuildPersistence SaveCurrentBuild: {e.GetType().Name}: {e.Message}\n{e}");
            return false;
        }
    }

    public bool DeleteBuild(int buildId)
    {
        if (!EnsureAuthenticated("DeleteBuild"))
        {
            return false;
        }

        if (buildId <= 0)
        {
            return false;
        }

        if (!TryResolveConnectionString(out string connString))
        {
            return false;
        }

        Type connectionType = FindNpgsqlConnectionType();
        if (connectionType == null)
        {
            return false;
        }

        const string sql = "DELETE FROM builds WHERE id=@id AND user_id=@uid;";
        try
        {
            using (IDisposable conn = (IDisposable)Activator.CreateInstance(connectionType, connString))
            {
                InvokeInstanceMethod(conn, "Open");
                using (IDisposable cmd = CreateCommand(connectionType, conn, sql))
                {
                    AddParameter(cmd, "@id", buildId);
                    AddParameter(cmd, "@uid", GetEffectiveUserId());
                    InvokeInstanceMethod(cmd, "ExecuteNonQuery");
                }
            }

            Debug.Log($"BuildPersistence: сборка {buildId} удалена.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"BuildPersistence DeleteBuild: {e.GetType().Name}: {e.Message}\n{e}");
            return false;
        }
    }

    public bool LoadBuild(int buildId)
    {
        if (buildId <= 0)
        {
            return false;
        }

        if (assemblyState == null) assemblyState = FindFirstObjectByType<PCAssemblyState>();
        if (catalogService == null) catalogService = FindFirstObjectByType<ComponentCatalogService>();
        if (assemblyState == null || catalogService == null || prefabCatalogMap == null)
        {
            Debug.LogWarning("BuildPersistence: не назначены зависимости для загрузки сборки.");
            return false;
        }

        if (!TryResolveConnectionString(out string connString))
        {
            return false;
        }

        Type connectionType = FindNpgsqlConnectionType();
        if (connectionType == null)
        {
            return false;
        }

        const string sql = @"
SELECT slot_code, component_id, position_x, position_y, position_z, rotation_x, rotation_y, rotation_z
FROM build_items
WHERE build_id = @bid
ORDER BY id ASC;";

        var items = new List<BuildItemRow>();
        try
        {
            using (IDisposable conn = (IDisposable)Activator.CreateInstance(connectionType, connString))
            {
                InvokeInstanceMethod(conn, "Open");
                using (IDisposable cmd = CreateCommand(connectionType, conn, sql))
                {
                    AddParameter(cmd, "@bid", buildId);
                    object reader = InvokeInstanceMethod(cmd, "ExecuteReader");
                    if (reader == null)
                    {
                        return false;
                    }

                    try
                    {
                        MethodInfo read = GetZeroArgInstanceMethod(reader.GetType(), "Read");
                        MethodInfo getOrdinal = FindMethod(reader.GetType(), "GetOrdinal", typeof(string));
                        MethodInfo isDbNull = FindMethod(reader.GetType(), "IsDBNull", typeof(int));
                        MethodInfo getInt32 = FindMethod(reader.GetType(), "GetInt32", typeof(int));
                        MethodInfo getString = FindMethod(reader.GetType(), "GetString", typeof(int));
                        MethodInfo getValue = FindMethod(reader.GetType(), "GetValue", typeof(int));

                        while (read != null && (bool)read.Invoke(reader, null))
                        {
                            items.Add(new BuildItemRow
                            {
                                SlotCode = ReadString(reader, getOrdinal, isDbNull, getString, getValue, "slot_code"),
                                ComponentId = ReadInt(reader, getOrdinal, isDbNull, getInt32, getValue, "component_id"),
                                Pos = new Vector3(
                                    ReadFloat(reader, getOrdinal, isDbNull, getValue, "position_x"),
                                    ReadFloat(reader, getOrdinal, isDbNull, getValue, "position_y"),
                                    ReadFloat(reader, getOrdinal, isDbNull, getValue, "position_z")),
                                Rot = new Vector3(
                                    ReadFloat(reader, getOrdinal, isDbNull, getValue, "rotation_x"),
                                    ReadFloat(reader, getOrdinal, isDbNull, getValue, "rotation_y"),
                                    ReadFloat(reader, getOrdinal, isDbNull, getValue, "rotation_z"))
                            });
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
            Debug.LogWarning($"BuildPersistence LoadBuild/read: {e.GetType().Name}: {e.Message}\n{e}");
            return false;
        }

        if (items.Count == 0)
        {
            Debug.LogWarning("BuildPersistence: для сборки не найдено деталей.");
            return false;
        }

        ClearCurrentBuild();

        var catalogById = new Dictionary<int, PcComponentData>();
        for (int i = 0; i < catalogService.Items.Count; i++)
        {
            PcComponentData d = (PcComponentData)catalogService.Items[i];
            if (d != null && d.DatabaseId > 0 && !catalogById.ContainsKey(d.DatabaseId))
            {
                catalogById[d.DatabaseId] = d;
            }
        }

        BuildSlot[] allSlots = FindObjectsByType<BuildSlot>(FindObjectsSortMode.None);
        items.Sort((a, b) => PlacementPriority(a.SlotCode).CompareTo(PlacementPriority(b.SlotCode)));

        int placed = 0;
        for (int i = 0; i < items.Count; i++)
        {
            BuildItemRow row = items[i];
            if (!catalogById.TryGetValue(row.ComponentId, out PcComponentData data))
            {
                continue;
            }

            BuildingPart prefab = prefabCatalogMap.GetBuildingPrefab(data.CategoryCode);
            if (prefab == null)
            {
                continue;
            }

            GameObject go = Instantiate(prefab.gameObject, row.Pos, Quaternion.Euler(row.Rot));
            PCComponent component = go.GetComponent<PCComponent>();
            if (component == null)
            {
                component = go.AddComponent<PCComponent>();
            }
            component.ApplyFromData(data);
            ComponentModelBinder.BindVisual(go.transform, prefabCatalogMap, data);

            if (component.GetComponent<PCComponentInfoClickTarget>() == null)
            {
                component.gameObject.AddComponent<PCComponentInfoClickTarget>();
            }

            bool placedIntoSlot = TryPlaceIntoSlot(component, row.SlotCode, allSlots);
            if (!placedIntoSlot)
            {
                if (loosePlacementRoot != null)
                {
                    component.transform.SetParent(loosePlacementRoot, true);
                }

                if (assemblyState != null)
                {
                    assemblyState.RegisterInstalled(component);
                }
            }

            placed++;
        }

        Debug.Log($"BuildPersistence: сборка {buildId} загружена, размещено {placed} деталей.");
        return placed > 0;
    }

    private bool TryPlaceIntoSlot(PCComponent component, string slotCode, BuildSlot[] allSlots)
    {
        if (component == null || string.IsNullOrWhiteSpace(slotCode) || string.Equals(slotCode, "LOOSE", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!Enum.TryParse(slotCode, true, out PCSlotType slotType))
        {
            return false;
        }

        for (int i = 0; i < allSlots.Length; i++)
        {
            BuildSlot slot = allSlots[i];
            if (slot == null || slot.SlotType != slotType || slot.IsOccupied)
            {
                continue;
            }

            if (!slot.CanPlace(component))
            {
                continue;
            }

            return slot.Place(component);
        }

        return false;
    }

    private void ClearCurrentBuild()
    {
        PCComponent[] components = FindObjectsByType<PCComponent>(FindObjectsSortMode.None);
        for (int i = 0; i < components.Length; i++)
        {
            PCComponent c = components[i];
            if (c == null)
            {
                continue;
            }

            if (c.ParentSlot != null)
            {
                c.ParentSlot.Remove();
            }
            else if (assemblyState != null)
            {
                assemblyState.RegisterRemoved(c);
            }

            Destroy(c.gameObject);
        }
    }

    private bool TryResolveConnectionString(out string connectionString)
    {
        string envConn = Environment.GetEnvironmentVariable("PC_BUILDER_POSTGRES");
        connectionString = !string.IsNullOrWhiteSpace(envConn) ? envConn : postgresConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Debug.LogWarning("BuildPersistence: строка подключения PostgreSQL не задана.");
            return false;
        }
        return true;
    }

    private int GetEffectiveUserId()
    {
        return CurrentUserId;
    }

    private bool EnsureAuthenticated(string operation)
    {
        if (CurrentUserId > 0)
        {
            return true;
        }

        Debug.LogWarning($"BuildPersistence {operation}: пользователь не авторизован. " +
                         $"Операция отклонена (defaultUserId={defaultUserId} больше не используется как fallback).");
        return false;
    }

    private static string HashPassword(string password)
    {
        using SHA256 sha = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
        byte[] hash = sha.ComputeHash(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("x2"));
        }
        return sb.ToString();
    }

    private static void TryUpdateLastLogin(Type connectionType, IDisposable conn, int userId)
    {
        const string sql = "UPDATE users SET last_login_at = NOW() WHERE id = @id;";
        try
        {
            using (IDisposable cmd = CreateCommand(connectionType, conn, sql))
            {
                AddParameter(cmd, "@id", userId);
                InvokeInstanceMethod(cmd, "ExecuteNonQuery");
            }
        }
        catch
        {
            // Non-critical
        }
    }

    private static int PlacementPriority(string slotCode)
    {
        if (string.IsNullOrWhiteSpace(slotCode))
        {
            return 1000;
        }

        string s = slotCode.Trim().ToUpperInvariant();
        if (s == "CASESLOT") return 0;
        if (s == "MOTHERBOARDSLOT") return 1;
        if (s == "PSUSLOT") return 2;
        if (s == "CPUSLOT") return 3;
        if (s == "CPUFANSLOT") return 4;
        if (s == "RAMSLOT") return 5;
        if (s == "GPUSLOT") return 6;
        if (s == "STORAGESLOT") return 7;
        if (s == "FANSLOT") return 8;
        return 50;
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
        MethodInfo createCommand = GetZeroArgInstanceMethod(connectionType, "CreateCommand");
        object cmd = createCommand?.Invoke(conn, null);
        if (cmd == null)
        {
            throw new MissingMethodException("NpgsqlConnection.CreateCommand");
        }

        cmd.GetType().GetProperty("CommandText")?.SetValue(cmd, sql);
        return (IDisposable)cmd;
    }

    private static void AddParameter(IDisposable cmd, string name, object value)
    {
        if (cmd is IDbCommand dbCommand)
        {
            IDbDataParameter p = dbCommand.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            dbCommand.Parameters.Add(p);
            return;
        }

        // Reflection fallback (in case provider command does not implement IDbCommand).
        PropertyInfo parametersProperty = cmd.GetType().GetProperty("Parameters", BindingFlags.Instance | BindingFlags.Public);
        object parameters = parametersProperty?.GetValue(cmd);
        if (parameters == null)
        {
            return;
        }

        MethodInfo addWithValue = FindMethod(parameters.GetType(), "AddWithValue", typeof(string), typeof(object));
        if (addWithValue != null)
        {
            addWithValue.Invoke(parameters, new[] { name, value ?? DBNull.Value });
            return;
        }

        MethodInfo add = FindMethod(parameters.GetType(), "Add", typeof(string), typeof(object));
        add?.Invoke(parameters, new[] { name, value ?? DBNull.Value });
    }

    private static object InvokeInstanceMethod(object target, string methodName)
    {
        if (target == null)
        {
            return null;
        }

        MethodInfo m = GetZeroArgInstanceMethod(target.GetType(), methodName);
        return m?.Invoke(target, null);
    }

    private static MethodInfo GetZeroArgInstanceMethod(Type type, string methodName)
    {
        if (type == null || string.IsNullOrWhiteSpace(methodName))
        {
            return null;
        }

        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        MethodInfo candidate = null;
        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo m = methods[i];
            if (!string.Equals(m.Name, methodName, StringComparison.Ordinal))
            {
                continue;
            }

            if (m.IsGenericMethod || m.GetParameters().Length != 0)
            {
                continue;
            }

            if (m.DeclaringType == type)
            {
                return m;
            }

            candidate ??= m;
        }

        return candidate;
    }

    private static MethodInfo FindMethod(Type type, string methodName, params Type[] parameterTypes)
    {
        if (type == null || string.IsNullOrWhiteSpace(methodName))
        {
            return null;
        }

        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        MethodInfo candidate = null;
        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo m = methods[i];
            if (!string.Equals(m.Name, methodName, StringComparison.Ordinal))
            {
                continue;
            }

            ParameterInfo[] p = m.GetParameters();
            if (p.Length != parameterTypes.Length)
            {
                continue;
            }

            bool match = true;
            for (int j = 0; j < p.Length; j++)
            {
                if (p[j].ParameterType != parameterTypes[j])
                {
                    match = false;
                    break;
                }
            }

            if (!match)
            {
                continue;
            }

            if (m.DeclaringType == type)
            {
                return m;
            }

            candidate ??= m;
        }

        return candidate;
    }

    private static int ReadInt(object reader, MethodInfo getOrdinal, MethodInfo isDbNull, MethodInfo getInt32, MethodInfo getValue, string column)
    {
        int ord = (int)getOrdinal.Invoke(reader, new object[] { column });
        if ((bool)isDbNull.Invoke(reader, new object[] { ord }))
        {
            return 0;
        }

        if (getInt32 != null)
        {
            try { return (int)getInt32.Invoke(reader, new object[] { ord }); } catch { }
        }

        object v = getValue.Invoke(reader, new object[] { ord });
        return Convert.ToInt32(v);
    }

    private static float ReadFloat(object reader, MethodInfo getOrdinal, MethodInfo isDbNull, MethodInfo getValue, string column)
    {
        int ord = (int)getOrdinal.Invoke(reader, new object[] { column });
        if ((bool)isDbNull.Invoke(reader, new object[] { ord }))
        {
            return 0f;
        }

        object v = getValue.Invoke(reader, new object[] { ord });
        return Convert.ToSingle(v);
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
            try { return (string)getString.Invoke(reader, new object[] { ord }); } catch { }
        }

        object v = getValue.Invoke(reader, new object[] { ord });
        return v?.ToString() ?? string.Empty;
    }

    private static DateTime ReadDateTime(object reader, MethodInfo getOrdinal, MethodInfo isDbNull, MethodInfo getDateTime, MethodInfo getValue, string column)
    {
        int ord = (int)getOrdinal.Invoke(reader, new object[] { column });
        if ((bool)isDbNull.Invoke(reader, new object[] { ord }))
        {
            return DateTime.MinValue;
        }

        if (getDateTime != null)
        {
            try { return (DateTime)getDateTime.Invoke(reader, new object[] { ord }); } catch { }
        }

        object v = getValue.Invoke(reader, new object[] { ord });
        return Convert.ToDateTime(v);
    }

    private struct BuildItemRow
    {
        public string SlotCode;
        public int ComponentId;
        public Vector3 Pos;
        public Vector3 Rot;
    }
}

[Serializable]
public class BuildListItem
{
    public int BuildId;
    public string Name;
    public DateTime CreatedAt;

    public override string ToString()
    {
        string dt = CreatedAt == DateTime.MinValue ? "-" : CreatedAt.ToString("yyyy-MM-dd HH:mm");
        return $"#{BuildId} {Name} ({dt})";
    }
}
