using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RendrixEngine
{
    public static class SceneManager
    {
        private static readonly Dictionary<string, Func<IScene>> sceneFactories = new(); // explicit registration keys / aliases
        private static readonly Dictionary<string, Func<IScene>> sceneNameIndex = new();  // index by IScene.Name
        private static readonly HashSet<Type> registeredSceneTypes = new(); // track auto-registered scene types
        private static readonly List<Func<IScene>> autoLoadSceneFactories = new(); // scenes flagged AutoLoad

        private static IScene? currentScene;

        private class AdditiveSceneRecord
        {
            public IScene Scene { get; }
            public List<SceneNode> TopLevelNodes { get; } = new();
            public AdditiveSceneRecord(IScene scene) => Scene = scene;
        }

        private static readonly Dictionary<IScene, AdditiveSceneRecord> additiveSceneRecords = new();
        private static readonly List<AdditiveSceneRecord> additiveSceneList = new(); // preserve load order

        private static Engine? engineRef;
        private static readonly object sceneLock = new();

        public static void Initialize(Engine engine)
        {
            engineRef = engine;
        }

        // Auto-registration entry point used by SceneBase
        internal static void AutoRegisterSceneType(IScene instance)
        {
            var type = instance.GetType();
            lock (sceneLock)
            {
                if (registeredSceneTypes.Contains(type))
                    return; // already registered
                Func<IScene> factory = () => (IScene)Activator.CreateInstance(type)!;
                registeredSceneTypes.Add(type);
                // If a custom name already present, use it
                string name = instance.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    sceneFactories[name] = factory;
                    sceneNameIndex[name] = factory;
                }
                // Track auto-load scenes
                if (instance.AutoLoad)
                    autoLoadSceneFactories.Add(factory);
            }
        }

        // Register with explicit alias and also index by the scene instance's own Name property
        public static void RegisterScene<T>(string alias) where T : IScene, new()
        {
            Func<IScene> factory = () => new T();
            sceneFactories[alias] = factory;
            IndexSceneName(factory);
            IndexAutoLoad(factory);
        }

        // Convenience: register using the scene's own Name property (instantiate once to read it)
        public static void RegisterScene<T>() where T : IScene, new()
        {
            Func<IScene> factory = () => new T();
            string internalName = factory().Name;
            sceneFactories[internalName] = factory; // also acts as alias
            sceneNameIndex[internalName] = factory;
            IndexAutoLoad(factory);
        }

        public static void RegisterScene(string alias, Func<IScene> factory)
        {
            sceneFactories[alias] = factory;
            IndexSceneName(factory);
            IndexAutoLoad(factory);
        }

        private static void IndexSceneName(Func<IScene> factory)
        {
            try
            {
                var instance = factory();
                if (!string.IsNullOrWhiteSpace(instance.Name))
                    sceneNameIndex[instance.Name] = factory;
            }
            catch
            {
                // Ignore failures; loading will surface errors later.
            }
        }

        private static void IndexAutoLoad(Func<IScene> factory)
        {
            try
            {
                var instance = factory();
                if (instance.AutoLoad)
                    autoLoadSceneFactories.Add(factory);
            }
            catch
            {
                // Ignore failures; loading will surface errors later.
            }
        }

        private static bool TryGetFactory(string name, out Func<IScene> factory)
        {
            if (sceneFactories.TryGetValue(name, out factory)) return true;
            if (sceneNameIndex.TryGetValue(name, out factory)) return true;
            factory = null!;
            return false;
        }

        public static void LoadAutoScenesOnStartup()
        {
            EnsureInitialized();
            // Load first auto-load as primary, rest as additive
            bool primarySet = false;
            foreach (var factory in autoLoadSceneFactories)
            {
                var scene = factory();
                if (!primarySet)
                {
                    LoadScene(scene);
                    primarySet = true;
                }
                else
                {
                    LoadSceneAdditive(scene);
                }
            }
        }

        public static void LoadScene(string name)
        {
            if (!TryGetFactory(name, out var factory))
                throw new ArgumentException($"Scene '{name}' is not registered or indexed by Name property.");
            LoadScene(factory());
        }

        public static void LoadScene(IScene scene)
        {
            EnsureInitialized();
            lock (sceneLock)
            {
                UnloadCurrentSceneInternal();
                UnloadAllAdditiveInternal();
                engineRef!.RootNode.Clear();
                BuildScene(scene, out _);
                currentScene = scene;
                InvokeLifecycle(scene, invokeStart: true);
            }
        }

        public static void LoadSceneAdditive(string name)
        {
            if (!TryGetFactory(name, out var factory))
                throw new ArgumentException($"Scene '{name}' is not registered or indexed by Name property.");
            LoadSceneAdditive(factory());
        }

        public static void LoadSceneAdditive(IScene scene)
        {
            EnsureInitialized();
            lock (sceneLock)
            {
                BuildScene(scene, out var newNodes);
                var record = new AdditiveSceneRecord(scene);
                record.TopLevelNodes.AddRange(newNodes);
                additiveSceneRecords[scene] = record;
                additiveSceneList.Add(record);
                InvokeLifecycle(scene, invokeStart: true);
            }
        }

        public static Task LoadSceneAsync(string name, bool additive = false)
        {
            if (!TryGetFactory(name, out var factory))
                throw new ArgumentException($"Scene '{name}' is not registered or indexed by Name property.");
            return LoadSceneAsync(factory(), additive);
        }

        public static async Task LoadSceneAsync(IScene scene, bool additive = false)
        {
            EnsureInitialized();
            var buildResult = await Task.Run(() => PrepareSceneBuild(scene));
            lock (sceneLock)
            {
                if (!additive)
                {
                    UnloadCurrentSceneInternal();
                    UnloadAllAdditiveInternal();
                    engineRef!.RootNode.Clear();
                    ApplyPreparedBuild(buildResult);
                    currentScene = scene;
                }
                else
                {
                    ApplyPreparedBuild(buildResult);
                    var record = new AdditiveSceneRecord(scene);
                    record.TopLevelNodes.AddRange(buildResult.CreatedTopLevelNodes);
                    additiveSceneRecords[scene] = record;
                    additiveSceneList.Add(record);
                }
                InvokeLifecycle(scene, invokeStart: true);
            }
        }

        public static void UnloadAdditiveScene(IScene scene)
        {
            EnsureInitialized();
            lock (sceneLock)
            {
                if (additiveSceneRecords.TryGetValue(scene, out var record))
                {
                    foreach (var n in record.TopLevelNodes)
                    {
                        if (engineRef!.RootNode.Children.Remove(n))
                            n.Clear();
                    }
                    additiveSceneRecords.Remove(scene);
                    additiveSceneList.Remove(record);
                    InvokeUnload(scene);
                }
            }
        }

        private static void BuildScene(IScene scene, out List<SceneNode> createdTopLevel)
        {
            var root = engineRef!.RootNode;
            int before = root.Children.Count;
            scene.Setup(root);
            int after = root.Children.Count;
            createdTopLevel = new List<SceneNode>(Math.Max(0, after - before));
            for (int i = before; i < after; i++)
                createdTopLevel.Add(root.Children[i]);
        }

        private class PreparedSceneBuild
        {
            public IScene Scene { get; set; } = null!;
            public List<SceneNode> CreatedTopLevelNodes { get; } = new();
        }

        private static PreparedSceneBuild PrepareSceneBuild(IScene scene)
        {
            var tempRoot = new SceneNode("TempRoot");
            scene.Setup(tempRoot);
            var prepared = new PreparedSceneBuild { Scene = scene };
            foreach (var child in tempRoot.Children)
                prepared.CreatedTopLevelNodes.Add(child);
            return prepared;
        }

        private static void ApplyPreparedBuild(PreparedSceneBuild build)
        {
            var realRoot = engineRef!.RootNode;
            foreach (var child in build.CreatedTopLevelNodes)
            {
                child.Transform.Parent = realRoot.Transform;
                realRoot.Children.Add(child);
            }
        }

        private static void InvokeLifecycle(IScene scene, bool invokeStart)
        {
            if (scene is ISceneAwake awake)
                awake.Awake();
            if (invokeStart && scene is ISceneStart start)
                start.Start();
        }

        private static void InvokeUnload(IScene scene)
        {
            if (scene is ISceneUnload unload)
                unload.Unload();
        }

        private static void UnloadCurrentSceneInternal()
        {
            if (currentScene != null)
            {
                InvokeUnload(currentScene);
                currentScene = null;
            }
        }

        private static void UnloadAllAdditiveInternal()
        {
            foreach (var record in additiveSceneList)
            {
                InvokeUnload(record.Scene);
            }
            additiveSceneRecords.Clear();
            additiveSceneList.Clear();
        }

        private static void EnsureInitialized()
        {
            if (engineRef == null)
                throw new InvalidOperationException("SceneManager not initialized. Call SceneManager.Initialize(engine) first.");
        }

        public static IScene? CurrentScene => currentScene;
        public static IReadOnlyList<IScene> AdditiveScenes
        {
            get
            {
                var list = new List<IScene>(additiveSceneList.Count);
                foreach (var r in additiveSceneList)
                    list.Add(r.Scene);
                return list;
            }
        }
    }
}
