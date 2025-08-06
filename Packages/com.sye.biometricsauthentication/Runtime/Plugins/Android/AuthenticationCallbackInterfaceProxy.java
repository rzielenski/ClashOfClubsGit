package sye.biometricsauthentication;
import androidx.biometric.BiometricPrompt;
public class AuthenticationCallbackInterfaceProxy extends BiometricPrompt.AuthenticationCallback {
    private AuthenticationCallbackInterface authenticationCallbackInterface;
    public AuthenticationCallbackInterfaceProxy (AuthenticationCallbackInterface authenticationCallbackInterface) {
        this.authenticationCallbackInterface = authenticationCallbackInterface;
    }
    @Override
    public void onAuthenticationError(int errorCode, CharSequence errString) {
        authenticationCallbackInterface.onAuthenticationError(errorCode, errString);
        EmptyFragmentActivity.close();
    }
    @Override
    public void onAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result) {
        authenticationCallbackInterface.onAuthenticationSucceeded(result);
        EmptyFragmentActivity.close();
    }
    @Override
    public void onAuthenticationFailed() {
        authenticationCallbackInterface.onAuthenticationFailed();
        EmptyFragmentActivity.close();
    }
}
