using UnityEngine;

public interface ISecureStore
{
    void SetSecret(string key, string value);
    string GetSecret(string key); // null if missing
    void DeleteSecret(string key);
}

public static class SecureStorage
{
#if UNITY_IOS && !UNITY_EDITOR
    private static ISecureStore impl = new KeychainSecureStore();
#elif UNITY_ANDROID && !UNITY_EDITOR
    private static ISecureStore impl = new KeystoreSecureStore();
#else
    // Editor/dev fallback ONLY. Do not ship using this path.
    private static ISecureStore impl = new PlayerPrefsStore();
#endif

    public static void Set(string key, string value) => impl.SetSecret(key, value);
    public static string Get(string key, string defaultValue = "")
    {
        var v = impl.GetSecret(key);
        return v ?? defaultValue;
    }
    public static void Delete(string key) => impl.DeleteSecret(key);

    private class PlayerPrefsStore : ISecureStore
    {
        public void SetSecret(string key, string value)
        {
            PlayerPrefs.SetString(key, value ?? "");
            PlayerPrefs.Save();
        }
        public string GetSecret(string key)
        {
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
        }
        public void DeleteSecret(string key)
        {
            if (PlayerPrefs.HasKey(key)) PlayerPrefs.DeleteKey(key);
        }
    }
}
