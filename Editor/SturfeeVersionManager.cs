using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

[Serializable]
public class SturfeeUpdateData
{
    public string[] Packages;
    public List<SturfeeToolkitVersion> Versions;
}

[Serializable]
public class SturfeeToolkitVersion
{
    public string Version;
    public string Date;
    public bool Required;
    public List<string> UpdateNotes;
}

public class SturfeeVersionManager : EditorWindow
{
    public static string AbsolutePath => Path.Combine(Application.dataPath, "Resources", "Sturfee", "SDK");
    public static string LocalPath => Path.Combine("Assets", "Resources", "Sturfee", "SDK");

    private static string _versionFileName = "SturfeeToolkitVersionInfo.json";

    // Colors
    private Color _sturfeePrimaryColor = new Color(25f / 255.0f, 190f / 255.0f, 200f / 255.0f);
    private Color _sturfeeSecondaryColor = new Color(238f / 255.0f, 66f / 255.0f, 102f / 255.0f);
    private Color _sturfeeErrorColor = new Color(183f / 255.0f, 48f / 255.0f, 48f / 255.0f);
    private Color _sturfeeDarkBackgroundColor = new Color(35f / 255.0f, 35f / 255.0f, 35f / 255.0f);

    private static SturfeeUpdateData _updateData = null;
    private static SturfeeToolkitVersion _currentVersion = null;
    private static SturfeeToolkitVersion _latestVersion = null;
    Vector2 scrollPos;

    private static bool _isLoadingVersions = false;
    private static bool _isInstalling = false;

    private static bool _isLoading = false;
    private static bool _isLoaded = false;
    private static bool _hasError = false;
    private static bool _showSuccess = true;
    private static string _errorMessage;

    private static AddAndRemoveRequest _request;

    private static List<RegistryManager.ScopedRegistry> _scopedRegistries = new List<RegistryManager.ScopedRegistry>
    {
        new RegistryManager.ScopedRegistry
        {
            name = "Cesium",
            url = "https://unity.pkg.cesium.com",
            scopes = new List<string>
            {
                "com.cesium.unity"
            }
        }
    };


    [MenuItem("Sturfee/SDK Version Manager")]
    public static void ShowVersionManager()
    {
        SturfeeVersionManager window = GetWindow<SturfeeVersionManager>();
        GUIContent customTitleContent = new GUIContent("Sturfee Version Manager");//, icon);
        window.titleContent = customTitleContent;
        window.Show();
    }

    SturfeeVersionManager()
    {
        OnLaunch();
    }

    private void OnLaunch()
    {
        LoadLocal();
        CheckForUpdate();
    }

    private static void LoadLocal()
    {
        if (File.Exists(Path.Combine(LocalPath, $"{_versionFileName}")))
        {
            var json = File.ReadAllText(Path.Combine(LocalPath, $"{_versionFileName}"));
            _currentVersion = JsonUtility.FromJson<SturfeeToolkitVersion>(json);
        }
        else
        {
            _currentVersion = null;
        }
    }

    private async void CheckForUpdate()
    {
        Debug.Log("[Sturfee SDK] :: Checking for SDK updates...");

        _isLoadingVersions = true;

        try
        {
            _updateData = await Get<SturfeeUpdateData>($"https://sturfee-public-share.s3.us-west-1.amazonaws.com/UnityToolkitVersionInfo.json");
            if (_updateData != null)
            {
                _updateData.Versions = _updateData.Versions.OrderByDescending(x => new Version(x.Version)).ToList();
                // Debug.Log($"Versions Found: {JsonUtility.ToJson(_updateData)}");
                _latestVersion = _updateData.Versions.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"[Sturfee Toolkit] :: Error checking for updates!");
        }
        finally
        {
            _isLoadingVersions = false;
            Repaint();
        }
    }

    protected virtual void OnGUI()
    {
        LoadLocal();
        if (!_isInstalling)
        {
            ShowMainUi();
        }
        else
        {
            ShowInstallingUi();
        }
    }

    void Update()
    {
        if (_isLoading)
        {
            Repaint();
        }
    }

    private async void InstallSdk()
    {
        EditorApplication.update += Progress;
        _isLoading = true;
        _hasError = false;

        // add scoped registries
        var registryManager = new RegistryManager();
        foreach (var scopedRegistry in _scopedRegistries)
        {
            registryManager.Save(scopedRegistry);
        }

        await System.Threading.Tasks.Task.Delay(1000);

        Debug.Log($"Installing Sturfee Modules version ({_latestVersion.Version}) ...");
        var versionedPkgs = new List<string>();
        for (var i = 0; i < _updateData.Packages.Length; i++)
        {
            var pkg = _updateData.Packages[i];
            if (pkg.Contains($"#release-"))
            {
                versionedPkgs.Add($"{pkg}{_latestVersion.Version}");
                Debug.Log($"   Adding version pkg: {pkg}{_latestVersion.Version}");
            }
            else
            {
                versionedPkgs.Add(pkg);
            }
        }
        _request = Client.AddAndRemove(versionedPkgs.ToArray());
    }

    void Progress()
    {
        if (_request != null && _request.IsCompleted)
        {
            _isLoading = false;
            _isLoaded = true;
            _isInstalling = false;

            if (_request.Status == StatusCode.Success)
            {
                Debug.Log($"Done Installing Sturfee Modules!");

                Repaint();
            }

            else if (_request.Status >= StatusCode.Failure)
            {
                Debug.Log(_request.Error.message);
                _errorMessage = _request.Error.message;
                _hasError = true;
                _showSuccess = false;
            }

            string dir = LocalPath;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(LocalPath, $"{_versionFileName}"), JsonUtility.ToJson(_currentVersion));
            Repaint();

            EditorApplication.update -= Progress;
        }
    }

    protected virtual void ShowMainUi()
    {
        //var boxStyle = new GUIStyle(GUI.skin.label);
        //boxStyle.normal.background = EditorGUIUtility.whiteTexture;

        var primaryButtonStyle = new GUIStyle(GUI.skin.button);//.label);
        primaryButtonStyle.normal.textColor = Color.white;// _sturfeePrimaryColor;
        primaryButtonStyle.normal.background = MakeTex(2, 2, _sturfeePrimaryColor);

        var secondaryButtonStyle = new GUIStyle(GUI.skin.button);//.label);
        secondaryButtonStyle.normal.textColor = Color.white;// _sturfeePrimaryColor;
        secondaryButtonStyle.normal.background = MakeTex(2, 2, _sturfeeSecondaryColor);

        var headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.normal.textColor = Color.white;
        headerStyle.fontSize = 18;
        headerStyle.hover.textColor = Color.white;

        var labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = Color.white;
        labelStyle.hover.textColor = Color.white;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox); // boxStyle);
        {
            

            GUILayout.Label("Version Info", headerStyle);
            EditorGUILayout.Space();

            if (_isLoadingVersions)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Loading versions data...", labelStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
                return;
            }

            if (_currentVersion != null)
            {
                EditorGUILayout.Space();
                GUILayout.Label($"Current Version: {_currentVersion.Version}", labelStyle);
                EditorGUILayout.Space();
                GUILayout.Label($"Latest Version: {_latestVersion.Version}", labelStyle);
                EditorGUILayout.Space();

                EditorGUILayout.Space();

                if ($"{_currentVersion.Version}" != $"{_latestVersion.Version}")
                {
                    GUILayout.Label("An update is available!", headerStyle);
                    if (GUILayout.Button("Update", secondaryButtonStyle, GUILayout.Height(25)))
                    {
                        Debug.Log($"Updating to version {_latestVersion.Version}...");
                        string dir = LocalPath;
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        File.WriteAllText(Path.Combine(LocalPath, $"{_versionFileName}"), JsonUtility.ToJson(_latestVersion));
                    }

                    EditorGUILayout.Space();
                }
            }

            if (GUILayout.Button("Check for Updates", primaryButtonStyle, GUILayout.Height(25)))
            {
                Debug.Log($"Checking for updates...");
                CheckForUpdate();
            }
            EditorGUILayout.Space();

            if (_currentVersion == null)
            {
                EditorGUILayout.Space();
                GUILayout.Label("No version info found. Please install the latest version.", headerStyle);
                if (GUILayout.Button($"Install (version {_latestVersion.Version})", secondaryButtonStyle, GUILayout.Height(25)))
                {
                    Debug.Log($"Installing version {_latestVersion.Version}...");
                    _isInstalling = true;
                    _currentVersion = _latestVersion;
                    InstallSdk();
                }
                EditorGUILayout.Space();
            }

        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        if (!_isInstalling && _currentVersion != null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox); // boxStyle);
            {
                GUILayout.Label($"Release Notes ({_currentVersion.Version})", headerStyle);
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                scrollPos =
                EditorGUILayout.BeginScrollView(scrollPos);
                foreach (var note in _currentVersion.UpdateNotes)
                {
                    GUILayout.Label($"- {note}", labelStyle);
                }
                EditorGUILayout.EndScrollView();


                EditorGUILayout.Space();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }

    protected virtual void ShowInstallingUi()
    {
        if (_isLoading)
        {
            EditorGUILayout.LabelField("Installing...", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Please wait", EditorStyles.wordWrappedLabel);
        }
        else if (_hasError)
        {
            EditorGUILayout.LabelField($"ERROR: {_errorMessage}", EditorStyles.wordWrappedLabel);
        }
        else
        {
            //this.Close();
            if (_showSuccess)
            {
                _showSuccess = false;
                EditorUtility.DisplayDialog("Sturfee Unity Toolkit", $"Sturfee SDK {_currentVersion.Version} Installed!", "Ok");

                if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset == null)
                {
                    if (EditorUtility.DisplayDialog("Sturfee Unity Toolkit", $"Sturfee SDK {_currentVersion.Version} requires Unity's Universal Render Pipeline (URP). Please install and configure URP.", "Read More"))
                    {
                        Application.OpenURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@7.1/manual/InstallURPIntoAProject.html");
                    }
                }
            }
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    public static async Task<T> Get<T>(string url)
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.ContentType = "application/json; charset=utf-8";

        try
        {
            var response = await request.GetResponseAsync() as HttpWebResponse;
            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
            {
                Debug.LogError($"ERROR:: API => {response.StatusCode} - {response.StatusDescription}");
                //Debug.LogError(response);
                throw new Exception(response.StatusDescription);
            }
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string jsonResponse = reader.ReadToEnd();
            var result = JsonUtility.FromJson<T>(jsonResponse);
            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            throw;
        }
    }

}

public class RegistryManager
{
    private string manifest = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");

    public List<ScopedRegistry> registries
    {
        get; private set;
    }

    public RegistryManager()
    {
        this.registries = new List<ScopedRegistry>();

        JObject manifestJSON = JObject.Parse(File.ReadAllText(manifest));

        JArray Jregistries = (JArray)manifestJSON["scopedRegistries"];
        if (Jregistries != null)
        {
            foreach (var JRegistry in Jregistries)
            {
                registries.Add(LoadRegistry((JObject)JRegistry));
            }
        }
        else
        {
            Debug.Log("No scoped registries set");
        }
    }

    public void Save(ScopedRegistry registry)
    {
        JObject manifestJSON = JObject.Parse(File.ReadAllText(manifest));

        JToken manifestRegistry = GetOrCreateScopedRegistry(registry, manifestJSON);

        write(manifestJSON);
    }

    public void Remove(ScopedRegistry registry)
    {
        JObject manifestJSON = JObject.Parse(File.ReadAllText(manifest));
        JArray Jregistries = (JArray)manifestJSON["scopedRegistries"];

        foreach (var JRegistryElement in Jregistries)
        {
            if (JRegistryElement["name"] != null && JRegistryElement["url"] != null &&
            JRegistryElement["name"].Value<string>().Equals(registry.name, StringComparison.Ordinal) &&
            JRegistryElement["url"].Value<string>().Equals(registry.url, StringComparison.Ordinal))
            {
                JRegistryElement.Remove();
                break;
            }
        }

        write(manifestJSON);
    }

    private ScopedRegistry LoadRegistry(JObject Jregistry)
    {
        ScopedRegistry registry = new ScopedRegistry();
        registry.name = (string)Jregistry["name"];
        registry.url = (string)Jregistry["url"];

        List<string> scopes = new List<string>();
        foreach (var scope in (JArray)Jregistry["scopes"])
        {
            scopes.Add((string)scope);
        }
        registry.scopes = new List<string>(scopes);

        return registry;
    }

    private void UpdateScope(ScopedRegistry registry, JToken registryElement)
    {
        JArray scopes = new JArray();
        foreach (var scope in registry.scopes)
        {
            scopes.Add(scope);
        }
        registryElement["scopes"] = scopes;
    }

    private JToken GetOrCreateScopedRegistry(ScopedRegistry registry, JObject manifestJSON)
    {
        JArray Jregistries = (JArray)manifestJSON["scopedRegistries"];
        if (Jregistries == null)
        {
            Jregistries = new JArray();
            manifestJSON["scopedRegistries"] = Jregistries;
        }

        foreach (var JRegistryElement in Jregistries)
        {
            if (JRegistryElement["name"] != null && JRegistryElement["url"] != null &&
                String.Equals(JRegistryElement["name"].Value<string>(), registry.name, StringComparison.Ordinal) &&
                String.Equals(JRegistryElement["url"].Value<string>(), registry.url, StringComparison.Ordinal))
            {
                UpdateScope(registry, JRegistryElement);
                return JRegistryElement;
            }
        }

        JObject JRegistry = new JObject();
        JRegistry["name"] = registry.name;
        JRegistry["url"] = registry.url;
        UpdateScope(registry, JRegistry);
        Jregistries.Add(JRegistry);

        return JRegistry;
    }



    private void write(JObject manifestJSON)
    {
        File.WriteAllText(manifest, manifestJSON.ToString());
        AssetDatabase.Refresh();
    }



    [System.Serializable]
    public class ScopedRegistry
    {
        public string name;
        public string url;
        public List<string> scopes = new List<string>();

        public bool auth;

        public string token;

        public override string ToString()
        {
            return JsonUtility.ToJson(this, true);
        }

        public bool isValidCredential()
        {

            if (string.IsNullOrEmpty(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                return false;
            }


            if (auth)
            {
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }
            }

            return true;
        }

        public bool isValid()
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (scopes.Count < 1)
            {
                return false;
            }

            scopes.RemoveAll(string.IsNullOrEmpty);

            foreach (string scope in scopes)
            {
                if (Uri.CheckHostName(scope) != UriHostNameType.Dns)
                {
                    Debug.LogWarning("Invalid scope " + scope);
                    return false;
                }
            }


            return isValidCredential();


        }
    }
}

