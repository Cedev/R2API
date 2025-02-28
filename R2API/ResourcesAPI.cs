﻿using MonoMod.RuntimeDetour;
using R2API.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace R2API {

    /// <summary>
    /// Allow to register AssetBundles for Mod Makers.
    /// </summary>
    [R2APISubmodule]
    public static class ResourcesAPI {

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        private static readonly Dictionary<string, IResourceProvider> Providers = new Dictionary<string, IResourceProvider>();

        private static NativeDetour ResourcesLoadDetour;

        private delegate Object d_ResourcesLoad(string path, Type type);

        private static d_ResourcesLoad _origLoad;

        private static NativeDetour ResourcesLoadAsyncDetour;

        private delegate ResourceRequest d_ResourcesAsyncLoad(string path, Type type);

        private static d_ResourcesAsyncLoad _origResourcesLoadAsync;

        private static NativeDetour ResourcesLoadAllDetour;

        private delegate Object[] d_ResourcesLoadAll(string path, Type type);

        private static d_ResourcesLoadAll _origLoadAll;

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void InitHooks() {
            ResourcesLoadDetour = new NativeDetour(
                typeof(Resources).GetMethod("Load", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(Type) }, null),
                typeof(ResourcesAPI).GetMethod(nameof(OnResourcesLoad), BindingFlags.Static | BindingFlags.NonPublic)
            );
            _origLoad = ResourcesLoadDetour.GenerateTrampoline<d_ResourcesLoad>();
            ResourcesLoadDetour.Apply();

            ResourcesLoadAsyncDetour = new NativeDetour(
                typeof(Resources).GetMethod("LoadAsyncInternal", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(Type) }, null),
                typeof(ResourcesAPI).GetMethod(nameof(OnResourcesLoadAsync), BindingFlags.Static | BindingFlags.NonPublic)
            );
            _origResourcesLoadAsync = ResourcesLoadAsyncDetour.GenerateTrampoline<d_ResourcesAsyncLoad>();
            ResourcesLoadAsyncDetour.Apply();

            ResourcesLoadAllDetour = new NativeDetour(
                typeof(Resources).GetMethod("LoadAll", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(Type) }, null),
                typeof(ResourcesAPI).GetMethod(nameof(OnResourcesLoadAll), BindingFlags.Static | BindingFlags.NonPublic)
            );
            _origLoadAll = ResourcesLoadAllDetour.GenerateTrampoline<d_ResourcesLoadAll>();
            ResourcesLoadAllDetour.Apply();
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            ResourcesLoadDetour.Undo();
            ResourcesLoadAsyncDetour.Undo();
            ResourcesLoadAllDetour.Undo();
        }

        /// <summary>
        /// Add an AssetBundleResourcesProvider to the API.
        /// A prefix usually looks like this: "@MyModPrefix"
        /// More information on the R2API Wiki.
        /// </summary>
        /// <param name="provider">assetbundle provider to give, usually made with an AssetBundleResourcesProvider(prefix, assetbundle)</param>
        public static void AddProvider(IResourceProvider? provider) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(ResourcesAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ResourcesAPI)})]");
            }

            if (provider == null) {
                throw new InvalidOperationException($"Given {nameof(IResourceProvider)} is null.");
            }

            if (provider.ModPrefix == null) {
                throw new InvalidOperationException($"Given {nameof(IResourceProvider)}.{nameof(provider.ModPrefix)} is null.");
            }

            if (!provider.ModPrefix.StartsWith("@")) {
                throw new InvalidOperationException($"{nameof(IResourceProvider)}.{nameof(provider.ModPrefix)} value should start with '@' e.g. \"@ModPrefix\"");
            }

            Providers.Add(provider.ModPrefix, provider);
        }

        private static Object OnResourcesLoad(string? path, Type type) {
            if (path != null && path.StartsWith("@")) {
                return ModResourcesLoad(path, type);
            }

            return _origLoad(path, type);
        }

        private static Object ModResourcesLoad(string path, Type type) {
            var split = path.Split(':');
            if (!Providers.TryGetValue(split[0], out var provider)) {
                R2API.Logger.LogError($"Provider `{split[0]}` was not found");
                return null;
            }

            return provider.Load(path, type);
        }

        private static ResourceRequest OnResourcesLoadAsync(string? path, Type type) {
            if (path != null && path.StartsWith("@")) {
                return ModResourcesLoadAsync(path, type);
            }

            return _origResourcesLoadAsync(path, type);
        }

        private static ResourceRequest ModResourcesLoadAsync(string path, Type type) {
            var split = path.Split(':');
            if (!Providers.TryGetValue(split[0], out var provider)) {
                R2API.Logger.LogError($"Provider `{split[0]}` was not found");
                return null;
            }

            return provider.LoadAsync(path, type);
        }

        private static Object[] OnResourcesLoadAll(string? path, Type type) {
            if (path != null && path.StartsWith("@")) {
                return ModResourcesLoadAll(path, type);
            }

            return _origLoadAll(path, type);
        }

        private static Object[] ModResourcesLoadAll(string path, Type type) {
            var split = path.Split(':');
            if (!Providers.TryGetValue(split[0], out var provider)) {
                R2API.Logger.LogError($"Provider `{split[0]}` was not found");
                return Array.Empty<Object>();
            }

            return provider.LoadAll(type);
        }
    }
}
