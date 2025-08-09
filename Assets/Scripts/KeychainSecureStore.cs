#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

public class KeychainSecureStore : ISecureStore
{
    [DllImport("__Internal")] private static extern bool keychain_set(string key, string val);
    [DllImport("__Internal")] private static extern IntPtr keychain_get(string key);
    [DllImport("__Internal")] private static extern bool keychain_delete(string key);

    public void SetSecret(string key, string value)
    {
        if (!keychain_set(key, value ?? "")) throw new Exception("Keychain set failed");
    }

    public string GetSecret(string key)
    {
        var ptr = keychain_get(key);
        if (ptr == IntPtr.Zero) return null;
        return Marshal.PtrToStringAnsi(ptr);
    }

    public void DeleteSecret(string key)
    {
        keychain_delete(key);
    }
}
#endif
