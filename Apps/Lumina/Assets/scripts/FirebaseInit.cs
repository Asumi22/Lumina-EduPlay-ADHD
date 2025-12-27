using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

#if !UNITY_WEBGL
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Auth;
#endif

public class FirebaseInit : MonoBehaviour
{
    public static FirebaseInit Instance { get; private set; }

    [Header("Opcional: URL de la base de datos")]
    [SerializeField] public string databaseUrl = "";

#if !UNITY_WEBGL
    private DatabaseReference dbReference;
    public FirebaseAuth Auth { get; private set; }
    public FirebaseUser CurrentUser => Auth?.CurrentUser;
    public DatabaseReference DbReference => dbReference;
#endif

    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    // Solo declaramos la corrutina si NO es WebGL para evitar el aviso amarillo
#if !UNITY_WEBGL
    private Coroutine sessionHeartbeatCoroutine = null;
#endif

    // Propiedades Dummy para WebGL
#if UNITY_WEBGL
    public object CurrentUser => null; 
    public object DbReference => null;
    public object Auth => null;
#endif

    public static FirebaseInit EnsureInstance()
    {
        if (Instance != null) return Instance;

#if UNITY_2023_2_OR_NEWER
        var found = UnityEngine.Object.FindFirstObjectByType<FirebaseInit>();
#else
        var found = UnityEngine.Object.FindObjectOfType<FirebaseInit>();
#endif
        if (found != null) { Instance = found; return Instance; }

        var prefab = Resources.Load<GameObject>("FirebaseManager");
        if (prefab != null)
        {
            var go = GameObject.Instantiate(prefab);
            Instance = go.GetComponent<FirebaseInit>();
            if (Instance == null) Instance = go.AddComponent<FirebaseInit>();
            return Instance;
        }
        var created = new GameObject("FirebaseManager_Auto");
        Instance = created.AddComponent<FirebaseInit>();
        return Instance;
    }

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else if (Instance != this) { Destroy(gameObject); }
    }

    void Start()
    {
#if !UNITY_WEBGL
        if (!isInitialized) InitializeFirebase();
#else
            Debug.Log("[FirebaseInit] WebGL detectado: Firebase desactivado.");
            isInitialized = true; 
#endif
    }

#if !UNITY_WEBGL
    void InitializeFirebase()
    {
        Debug.Log("[FirebaseInit] Inicializando Firebase...");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                dbReference = string.IsNullOrEmpty(databaseUrl) ? FirebaseDatabase.DefaultInstance.RootReference : FirebaseDatabase.DefaultInstance.GetReferenceFromUrl(databaseUrl);
                Auth = FirebaseAuth.DefaultInstance;
                isInitialized = true;
                Auth.StateChanged += Auth_StateChanged;
                Debug.Log("[FirebaseInit] Inicializado ✅");
            }
            else Debug.LogError("[FirebaseInit] Error dependencias: " + task.Result);
        });
    }

    private void Auth_StateChanged(object sender, EventArgs e)
    {
        if (Auth != null && Auth.CurrentUser != null)
        {
            Debug.Log("[FirebaseInit] Usuario conectado: " + Auth.CurrentUser.UserId);
            if (AppBlockerManager.Instance != null) AppBlockerManager.Instance.StartListening(Auth.CurrentUser.UserId);
            StartSessionForCurrentUser();
        }
        else
        {
            Debug.Log("[FirebaseInit] Usuario desconectado.");
            EndSessionForCurrentUser();
        }
    }
#endif

    // --- MÉTODOS PÚBLICOS (Protegidos) ---

    public void SaveClams(int count)
    {
#if !UNITY_WEBGL
        if (!isInitialized || dbReference == null || Auth?.CurrentUser == null) return;
        dbReference.Child("players").Child(Auth.CurrentUser.UserId).Child("clams").SetValueAsync(count);
#endif
    }

    public void LoadClams(Action<int> onLoaded)
    {
#if !UNITY_WEBGL
        if (!isInitialized || dbReference == null || Auth?.CurrentUser == null) { onLoaded?.Invoke(0); return; }
        dbReference.Child("players").Child(Auth.CurrentUser.UserId).Child("clams").GetValueAsync().ContinueWithOnMainThread(t => {
            if (t.IsFaulted) { onLoaded?.Invoke(0); return; }
            int val = 0; if (t.Result.Exists && t.Result.Value != null) int.TryParse(t.Result.Value.ToString(), out val);
            onLoaded?.Invoke(val);
        });
#else
        onLoaded?.Invoke(0);
#endif
    }

    public void RegisterChild(string username, string password, string nombre, int anioNacimiento, Action<bool, string> callback)
    {
#if !UNITY_WEBGL
        if (!isInitialized) { callback?.Invoke(false, "No init"); return; }
        username = username.Trim().ToLower();
        dbReference.Child("usernames").Child(username).GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.Result.Exists) { callback?.Invoke(false, "Usuario no disponible"); return; }
            Auth.CreateUserWithEmailAndPasswordAsync(username + "@lumina.local", password).ContinueWithOnMainThread(authTask => {
                if (authTask.IsFaulted) { callback?.Invoke(false, "Error Auth"); return; }
                string uid = authTask.Result.User.UserId;
                var updates = new Dictionary<string, object>();
                updates[$"usuarios/{uid}/profile"] = new Dictionary<string, object>() { { "nombre", string.IsNullOrEmpty(nombre) ? username : nombre }, { "username", username }, { "role", "child" }, { "anioNacimiento", anioNacimiento }, { "En_linea", 1 }, { "Num_ingresos", 0 } };
                updates[$"usernames/{username}"] = uid;
                dbReference.UpdateChildrenAsync(updates).ContinueWithOnMainThread(t => {
                    MigratePlayerIfNeeded(uid, "player1", true, (ok, m) => callback?.Invoke(true, uid));
                });
            });
        });
#else
        callback?.Invoke(true, "web_dummy_uid");
#endif
    }

    public void SignInChild(string username, string password, Action<bool, string> callback)
    {
#if !UNITY_WEBGL
        if (!isInitialized) { callback?.Invoke(false, "No init"); return; }
        username = username.Trim().ToLower();
        Auth.SignInWithEmailAndPasswordAsync(username + "@lumina.local", password).ContinueWithOnMainThread(task => {
            if (task.IsFaulted) { callback?.Invoke(false, "Error Login"); return; }
            string uid = task.Result.User.UserId;
            dbReference.Child("usuarios").Child(uid).Child("profile").GetValueAsync().ContinueWithOnMainThread(gTask => {
                if (!gTask.IsFaulted && gTask.Result.Exists && gTask.Result.Child("role").Value?.ToString() == "child")
                {
                    long ing = 0; if (gTask.Result.Child("Num_ingresos").Value != null) long.TryParse(gTask.Result.Child("Num_ingresos").Value.ToString(), out ing);
                    dbReference.Child("usuarios").Child(uid).Child("profile").UpdateChildrenAsync(new Dictionary<string, object>() { { "En_linea", 1 }, { "Num_ingresos", ing + 1 } })
                        .ContinueWithOnMainThread(_ => MigratePlayerIfNeeded(uid, "player1", true, (ok, m) => callback?.Invoke(true, uid)));
                }
                else { Auth.SignOut(); callback?.Invoke(false, "Cuenta no válida"); }
            });
        });
#else
        callback?.Invoke(true, "web_dummy_uid");
#endif
    }

    public void SignInUser(string email, string password, Action<bool, string> callback)
    {
#if !UNITY_WEBGL
        if (!isInitialized) { callback?.Invoke(false, "No init"); return; }
        Auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsFaulted) { callback?.Invoke(false, task.Exception?.Message); return; }
            string uid = task.Result.User.UserId;
            dbReference.Child("usuarios").Child(uid).Child("profile").UpdateChildrenAsync(new Dictionary<string, object>() { { "En_linea", 1 } });
            callback?.Invoke(true, uid);
        });
#else
        callback?.Invoke(true, "web_user");
#endif
    }

    public void SignOutCurrentUser(Action<bool, string> callback = null)
    {
#if !UNITY_WEBGL
        if (Auth?.CurrentUser == null) { callback?.Invoke(false, "No user"); return; }
        string uid = Auth.CurrentUser.UserId;
        dbReference.Child("usuarios").Child(uid).Child("profile").UpdateChildrenAsync(new Dictionary<string, object>() { { "En_linea", 0 } }).ContinueWithOnMainThread(t => {
            Auth.SignOut(); callback?.Invoke(true, "Desconectado.");
        });
#else
        callback?.Invoke(true, "Bye Web");
#endif
    }

    public void RegisterAndSaveUser(string e, string p, string n, int a, Action<bool, string> c)
    {
#if !UNITY_WEBGL
        Auth.CreateUserWithEmailAndPasswordAsync(e, p).ContinueWithOnMainThread(t => {
            if (!t.IsFaulted)
            {
                var uid = t.Result.User.UserId;
                var d = new Dictionary<string, object>() { { "nombre", n }, { "correo", e }, { "anioNacimiento", a }, { "En_linea", 1 }, { "Num_ingresos", 0 } };
                dbReference.Child("usuarios").Child(uid).Child("profile").SetValueAsync(d).ContinueWithOnMainThread(_ => c?.Invoke(true, uid));
            }
            else c?.Invoke(false, "Err");
        });
#else
        c?.Invoke(true, "web_user"); 
#endif
    }

    public void MigratePlayerIfNeeded(string d, string s, bool del, Action<bool, string> c)
    {
#if !UNITY_WEBGL
        if (!isInitialized) { c?.Invoke(false, "No init"); return; }
        dbReference.Child("players").Child(d).Child("clams").GetValueAsync().ContinueWithOnMainThread(t => {
            if (!t.IsFaulted && t.Result.Exists && int.TryParse(t.Result.Value.ToString(), out int v) && v > 0) { c?.Invoke(false, "Ya tiene datos"); return; }
            dbReference.Child("players").Child(s).Child("clams").GetValueAsync().ContinueWithOnMainThread(src => {
                if (!src.IsFaulted && src.Result.Exists)
                {
                    int val = int.Parse(src.Result.Value.ToString());
                    dbReference.Child("players").Child(d).Child("clams").SetValueAsync(val).ContinueWithOnMainThread(_ => {
                        if (del) dbReference.Child("players").Child(s).RemoveValueAsync();
                        c?.Invoke(true, "Migrado");
                    });
                }
                else c?.Invoke(false, "Nada");
            });
        });
#else
        c?.Invoke(true, "Web Migrated");
#endif
    }

    public void StartSessionForCurrentUser(int heartbeatSeconds = 30)
    {
#if !UNITY_WEBGL
        if (!isInitialized || Auth?.CurrentUser == null) return;
        if (sessionHeartbeatCoroutine != null) return;
        sessionHeartbeatCoroutine = StartCoroutine(SessionHeartbeatCoroutine(Auth.CurrentUser.UserId, heartbeatSeconds));
#endif
    }
    public void EndSessionForCurrentUser()
    {
#if !UNITY_WEBGL
        if (sessionHeartbeatCoroutine != null) { StopCoroutine(sessionHeartbeatCoroutine); sessionHeartbeatCoroutine = null; }
#endif
    }
#if !UNITY_WEBGL
    private IEnumerator SessionHeartbeatCoroutine(string uid, int s)
    {
        while (true) { AddPlaySecondsForToday(uid, s, null); yield return new WaitForSeconds(s); }
    }
#endif
    public void AddPlaySecondsForToday(string uid, int secondsToAdd, Action<bool, string> callback = null)
    {
#if !UNITY_WEBGL
        if (!isInitialized || dbReference == null) return;
        string dateKey = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        dbReference.Child("players").Child(uid).Child("usage").Child(dateKey).RunTransaction(data => {
            long cur = 0; if (data.Value != null) long.TryParse(data.Value.ToString(), out cur);
            data.Value = cur + secondsToAdd;
            return TransactionResult.Success(data);
        });
#endif
    }
}