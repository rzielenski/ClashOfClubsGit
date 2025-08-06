using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace SyE.BiometricsAuthentication
{
    public class Biometrics
    {
        public delegate void SuccessCallback();
        public delegate void FailureCallback();
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        [DllImport("Biometrics")]
        private static extern bool Authenticate();
#elif UNITY_IOS
        [DllImport("__Internal")]
        private static extern bool Authenticate();
#elif UNITY_WEBGL && !UNITY_EDITOR
        private static SuccessCallback onSuccess;
        private static FailureCallback onFailure;
        [DllImport("__Internal")]
        private static extern void Authenticate(Action<bool> callback);
#endif
        private static System.Threading.SynchronizationContext currentThread;
        public static void Authenticate(SuccessCallback onSuccess, FailureCallback onFailure)
        {
#if UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            bool success = false;
            success = Authenticate();
            if (success)
            {
                onSuccess?.Invoke();
            }
            else
            {
                onFailure?.Invoke();
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            SetupBiometricPromptAndAuthenticate (onSuccess, onFailure);
#elif UNITY_WEBGL && !UNITY_EDITOR
            Biometrics.onSuccess = onSuccess;
            Biometrics.onFailure = onFailure;
            Authenticate(OnAuthenticationResult);
#else
            Debug.LogError("Biometric authentication is not supported on this platform.");
            onFailure?.Invoke();
#endif
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(Action<bool>))]
        private static void OnAuthenticationResult(bool isSuccess)
        {
            if (isSuccess)
                onSuccess?.Invoke();
            else
                onFailure?.Invoke();
        }
#elif UNITY_ANDROID && !UNITY_EDITOR
        private static void SetupBiometricPromptAndAuthenticate(SuccessCallback onSuccess, FailureCallback onFailure)
        {
            currentThread = System.Threading.SynchronizationContext.Current;
            using (AndroidJavaObject promptInfoBuilder = new AndroidJavaObject("androidx.biometric.BiometricPrompt$PromptInfo$Builder"))
            {
                promptInfoBuilder.Call<AndroidJavaObject>("setTitle", Application.productName);
                promptInfoBuilder.Call<AndroidJavaObject>("setSubtitle", "Authenticate with biometrics");
                promptInfoBuilder.Call<AndroidJavaObject>("setNegativeButtonText", "Cancel");
                AndroidJavaObject promptInfo = promptInfoBuilder.Call<AndroidJavaObject> ("build");
                AndroidJavaObject authCallback = new AndroidJavaObject ("sye.biometricsauthentication.AuthenticationCallbackInterfaceProxy",
                     new AuthenticationCallback (onSuccess, onFailure));

                new AndroidJavaClass("sye.biometricsauthentication.EmptyFragmentActivity").CallStatic("startFromUnity",
                    new OnActivityCreatedCallback((activity) => {
                        AndroidJavaObject biometricPrompt = new AndroidJavaObject ("androidx.biometric.BiometricPrompt", activity, authCallback);
                        biometricPrompt.Call("authenticate", promptInfo);
                    }));


            }
        }
        private class OnActivityCreatedCallback : AndroidJavaProxy
        {
            Action<AndroidJavaObject> _callback;
            public OnActivityCreatedCallback(Action<AndroidJavaObject> callback)
                : base("sye.biometricsauthentication.EmptyFragmentActivity$OnActivityCreatedCallback")
            {
                _callback = callback;
            }
            public void onActivityCreated(AndroidJavaObject activity)
            {
                _callback?.Invoke(activity);
            }
        }
        private class AuthenticationCallback : AndroidJavaProxy
        {
            private readonly SuccessCallback onSuccess;
            private readonly FailureCallback onFailure;
            public AuthenticationCallback(SuccessCallback onSuccess, FailureCallback onFailure)
                : base("sye.biometricsauthentication.AuthenticationCallbackInterface")
            {
                this.onSuccess = onSuccess;
                this.onFailure = onFailure;
            }
            public void onAuthenticationSucceeded(AndroidJavaObject result)
            {
                currentThread.Post(_ => onSuccess?.Invoke(), null);
            }
            public void onAuthenticationFailed()
            {
                currentThread.Post (_ => onFailure?.Invoke (), null);
            }
            public void onAuthenticationError(int errorCode, string errorString)
            {
                Debug.LogError("Biometric authentication error: " + errorString);
                currentThread.Post (_ => onFailure?.Invoke (), null);
            }
        }
#endif
    }
}