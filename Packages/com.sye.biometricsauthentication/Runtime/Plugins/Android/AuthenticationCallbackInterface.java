package sye.biometricsauthentication;
import androidx.biometric.BiometricPrompt;
public interface AuthenticationCallbackInterface {
    public void onAuthenticationError(int errorCode, CharSequence errString);
    public void onAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result);
    public void onAuthenticationFailed();
}