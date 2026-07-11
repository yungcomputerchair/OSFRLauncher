using System;
using System.Runtime.InteropServices;

using GitCredentialManager;

using Launcher.Models;

using NLog;

namespace Launcher.Helpers;

public static class CredentialHelper
{
    private const string CredStoreEnv = "GCM_CREDENTIAL_STORE";

    private const string PasswordService = "passwords";

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private static readonly ICredentialStore? _store = CreateStore();

    private static ICredentialStore? CreateStore()
    {
        try
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(CredStoreEnv)))
            {
                string? store = null;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    store = "wincredman";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    store = "keychain";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    // Almost all Linux DEs use credential managers that implement the secret service
                    store = "secretservice";

                if (store is not null)
                    Environment.SetEnvironmentVariable(CredStoreEnv, store);
            }

            return CredentialManager.Create("OSFRLauncher");
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Credential store unavailable.");
            return null;
        }
    }

    public static string? GetPassword(ServerInfo server)
    {
        if (_store is null)
            return null;

        try
        {
            return _store.Get(PasswordService, server.SavePath)?.Password;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to read the password from the OS credential store.");

            return null;
        }
    }

    public static void SavePassword(ServerInfo server, string password)
    {
        if (_store is null)
            return;

        try
        {
            _store.AddOrUpdate(PasswordService, server.SavePath, password);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to write the password to the OS credential store.");
        }
    }

    public static void Clear(ServerInfo server)
    {
        if (_store is null)
            return;

        try
        {
            _store.Remove(PasswordService, server.SavePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to remove the password from the OS credential store.");
        }
    }
}
