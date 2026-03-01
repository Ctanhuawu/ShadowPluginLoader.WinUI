using DryIoc;
using ShadowPluginLoader.Attributes;
using ShadowPluginLoader.WinUI.Args;
using ShadowPluginLoader.WinUI.Enums;
using ShadowPluginLoader.WinUI.Exceptions;
using ShadowPluginLoader.WinUI.Helpers;
using ShadowPluginLoader.WinUI.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ShadowPluginLoader.WinUI.Config;
using Microsoft.Extensions.Logging;

namespace ShadowPluginLoader.WinUI;

public abstract partial class AbstractPluginLoader<TMeta, TAPlugin>
{
    /// <summary>
    /// Logger Print With Prefix
    /// </summary>
    protected virtual string LoggerPrefix => "[PluginLoader] ";

    /// <summary>
    /// Plugin Folder Path
    /// </summary>
    public string PluginFolderPath => BaseSdkConfig.PluginFolderPath;

    /// <summary>
    /// Plugin Folder Path
    /// </summary>
    public string TempFolderPath => BaseSdkConfig.TempFolderPath;

    /// <summary>
    /// 
    /// </summary>
    [Autowired]
    protected BaseSdkConfig BaseSdkConfig { get; }


    /// <summary>
    /// Logger
    /// </summary>
    [Autowired]
    protected ILogger Logger { get; }

    /// <summary>
    /// PluginEventService
    /// </summary>
    [Autowired]
    protected PluginEventService PluginEventService { get; }

    /// <summary>
    /// All Plugins
    /// </summary>
    private readonly Dictionary<string, TAPlugin> _plugins = new();


    /// <summary>
    /// Load Plugin From Type
    /// </summary>
    /// <param name="meta">Plugin MetaData</param>
    protected virtual void LoadPlugin(TMeta meta)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        try
        {
            BeforeLoadPlugin(meta.MainPlugin, meta);
            var instance = LoadMainPlugin(meta.MainPlugin, meta);
            AfterLoadPlugin(meta.MainPlugin, instance, meta);
            _plugins[meta.Id] = instance;
            var enabled = PluginSettingsHelper.GetPluginIsEnabled(meta.Id);
            instance.Loaded();
            PluginEventService.InvokePluginLoaded(this, new PluginEventArgs(meta.Id, PluginStatus.Loaded));
            stopwatch.Stop();
            Logger.LogInformation("{Pre}{ID}({isEnabled}): Load Success! Used: {mi} ms",
                LoggerPrefix, meta.Id, enabled, stopwatch.ElapsedMilliseconds);
            DependencyChecker.LoadedPlugins.TryAdd(meta.DllName, meta.Version);
            if (!enabled) return;
            instance.IsEnabled = enabled;
        }
        catch (Exception e)
        {
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }

            Logger.LogWarning("{Pre}Plugin Load Failed! Used: {mi} ms, Error: {Ex}",
                LoggerPrefix, stopwatch.ElapsedMilliseconds, e);
            throw;
        }
        finally
        {
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="plugin"></param>
    /// <param name="meta"></param>
    protected virtual void BeforeLoadPlugin(Type plugin, TMeta meta)
    {
    }


    /// <summary>
    /// Register Plugin Main
    /// </summary>
    /// <param name="plugin">Plugin Type</param>
    /// <param name="meta">PluginMetaData</param>
    /// <returns>Plugin Instance</returns>
    /// <exception cref="PluginImportException">Can't Register Plugin</exception>
    protected virtual TAPlugin LoadMainPlugin(Type plugin, TMeta meta)
    {
        var instance = DiFactory.Services.Resolve<TAPlugin>(serviceKey: meta.Id);
        if (instance is null) throw new PluginImportException($"{plugin.Name}: Can't Load Plugin");
        Logger.LogInformation("Plugin[{ID}] Main Class Load Success", meta.Id);
        return instance;
    }

    /// <summary>
    /// After Load Plugin
    /// </summary>
    protected virtual void AfterLoadPlugin(Type tPlugin, TAPlugin aPlugin, TMeta meta)
    {
    }
}