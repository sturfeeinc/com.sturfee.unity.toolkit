
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.IO;
using Newtonsoft.Json.Linq;
using System;

public class SturfeeInstaller : EditorWindow
{
    private static string _currentVersion = "3.1";

    private static bool _useDev = false;

    private static bool _isLoading = false;
    private static bool _isLoaded = false;
    private static bool _hasError = false;
    private static bool _showSuccess = true;
    private static string _errorMessage;

    private static AddAndRemoveRequest _request;

    private static string _selectedVersion = "3.1.1";

    private static string[] _activeVersions = new string[]
    {
        "3.1.0",
        "3.1.1"
    };

    private static string[] _sturfeePackages = new string[]
    {
        "com.cesium.unity@1.3.1",
        "https://github.com/BrentM-Sturfee/glTFast.git#aws-req",
        "https://github.com/BrentM-Sturfee/NGeoHash.git",
        "https://github.com/yoshida190/dotween.git#v1.2.632-upm",
        "https://github.com/sturfeeinc/com.sturfee.digital-twin.git#release-",
        "https://github.com/sturfeeinc/com.sturfee.digital-twin.cms.git#release-",
        "https://github.com/sturfeeinc/com.sturfee.digital-twin.hd.git#release-",
        "https://github.com/sturfeeinc/com.sturfee.vps.core.git#release-",
        "https://github.com/sturfeeinc/com.sturfee.vps.networking.git#release-",
        "https://github.com/sturfeeinc/com.sturfee.vps.sdk.git#release-",
        "https://github.com/sturfeeinc/com.sturfee.xrcs.git#release-"
    };

    private static string[] _sturfeeDevPackages = new string[]
    {
        "com.cesium.unity@1.3.1",
        "https://github.com/BrentM-Sturfee/glTFast.git#aws-req",
        "https://github.com/BrentM-Sturfee/NGeoHash.git",
        "https://github.com/yoshida190/dotween.git#v1.2.632-upm",
        "https://github.com/sturfeeinc/com.sturfee.digital-twin.git#3.0-dev",
        "https://github.com/sturfeeinc/com.sturfee.digital-twin.cms.git#3.0-dev",
        "https://github.com/sturfeeinc/com.sturfee.digital-twin.hd.git#3.0-dev",
        "https://github.com/sturfeeinc/com.sturfee.vps.core.git#3.0-dev",
        "https://github.com/sturfeeinc/com.sturfee.vps.networking.git#3.0-dev",
        "https://github.com/sturfeeinc/com.sturfee.vps.sdk.git#3.0-dev",
        "https://github.com/sturfeeinc/com.sturfee.xrcs.git#3.0-dev"
    };

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


    [MenuItem("Sturfee/Version Manager/Install 3.1.0 (OLD)")]
    static void InstallVersionThreeOne()
    {
        _selectedVersion = "3.1.0";
        _isLoading = false;
        _isLoaded = false;
        _hasError = false;
        _showSuccess = true;

        SturfeeInstaller window = ScriptableObject.CreateInstance<SturfeeInstaller>();
        window.titleContent = new GUIContent { text = "Sturfee Toolkit Installer" };

        window.ShowUtility();
    }

    [MenuItem("Sturfee/Version Manager/Install 3.1.1")]
    static void InstallVersionThreeOneOne()
    {
        _selectedVersion = "3.1.1";
        _isLoading = false;
        _isLoaded = false;
        _hasError = false;
        _showSuccess = true;

        SturfeeInstaller window = ScriptableObject.CreateInstance<SturfeeInstaller>();
        window.titleContent = new GUIContent { text = "Sturfee Toolkit Installer" };

        window.ShowUtility();
    }

    public void OnGUI()
    {
        if (!_isLoaded && !_isLoading)
        {
            SetupSdk();
        }

        if (_isLoading)
        {
            //rootVisualElement.Add(new Label("LOADING..."));
            EditorGUILayout.LabelField("Installing...", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Please wait", EditorStyles.wordWrappedLabel);
        }
        else if (_hasError)
        {
            EditorGUILayout.LabelField($"ERROR: {_errorMessage}", EditorStyles.wordWrappedLabel);
        }
        else
        {
            this.Close();
            if (_showSuccess)
            {
                _showSuccess = false;
                EditorUtility.DisplayDialog("Sturfee Unity Toolkit", $"Sturfee SDK {_currentVersion} Installed!", "Ok");

                if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset == null)
                {
                    if (EditorUtility.DisplayDialog("Sturfee Unity Toolkit", $"Sturfee SDK {_currentVersion} requires Unity's Universal Render Pipeline (URP). Please install and configure URP.", "Read More"))
                    {
                        Application.OpenURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@7.1/manual/InstallURPIntoAProject.html");
                    }
                }
            }
        }
    }

    void Update()
    {
        if (_isLoading)
        {
            Repaint();
        }
    }

    private async void SetupSdk()
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

        if (_useDev)
        {
            Debug.Log($"Installing Sturfee DEV Modules...");
            _request = Client.AddAndRemove(_sturfeeDevPackages);
        }
        else
        {
            Debug.Log($"Installing Sturfee Modules version ({_selectedVersion}) ...");
            var versionedPkgs = new List<string>();
            for (var i = 0; i < _sturfeePackages.Length; i++)
            {
                var pkg = _sturfeePackages[i];
                if (pkg.Contains($"#release-"))
                {
                    versionedPkgs.Add($"{pkg}{_selectedVersion}");
                    Debug.Log($"   Adding version pkg: {pkg}{_selectedVersion}");
                }
                else
                {
                    versionedPkgs.Add(pkg);
                }
            }
            _request = Client.AddAndRemove(versionedPkgs.ToArray());
        }
    }

    void Progress()
    {
        if (_request != null && _request.IsCompleted)
        {
            _isLoading = false;
            _isLoaded = true;

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

            EditorApplication.update -= Progress;
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
