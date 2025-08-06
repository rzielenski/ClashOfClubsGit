# Biometric Authentication Plugin for Unity

## Overview
This plugin allows you to integrate biometric authentication (such as Face ID or Android Biometrics) into your Unity projects. It supports iOS, macOS, Android, and WebGL.

## Supported Platforms
- **iOS**
- **macOS**
- **Android**
- **WebGL**

## How to Use

1. **Call the `Authenticate` Method**:
   - In your script, call the `SyE.BiometricsAuthentication.Biometrics.Authenticate` method, passing in the `onSuccess` and `onFailure` callbacks.

```csharp
using SyE.BiometricsAuthentication;

public class YourScript : MonoBehaviour
{
    void Start()
    {
        Biometrics.Authenticate(
            onSuccess: () => Debug.Log("Authenticated successfully"),
            onFailure: () => Debug.LogError("Authentication failed")
        );
    }
}
```

## Example
- For a complete example, import the sample from the Package Manager and refer to the demo scene. This demo provides a full implementation that can be built for all supported platforms.
- **Example Video**: [Watch on YouTube](https://youtu.be/LNZqCBFtb0g)

## Notes for Each Platform
- **iOS/macOS**: Supports Face ID and Touch ID out-of-the-box.
- **Android**: Automatically sets up Android BiometricPrompt.
- **WebGL**: Supports biometrics depending on browser capabilities.

## Troubleshooting
- **Build Errors**: Ensure your Unity project targets supported platforms and verify your SDK/NDK setup, especially for Android.
- **Authentication Fails**: Confirm biometrics are set up on your test device.
- **Android Issues**: Check the `AndroidManifest.xml` for potential conflicts if using other plugins.

## Links
- **Asset Store Package**: [Biometrics Authentication Plugin](https://assetstore.unity.com/packages/slug/293752)
- **Example Video**: [Watch on YouTube](https://youtu.be/LNZqCBFtb0g)

## Version
- **1.0.2**: Android runtime fixes class not found exceptions
- **1.0.1**: Build bug fixes for Android platform.
- **1.0.0**: Initial release.

## License
This plugin is licensed under the [Standard Unity Asset Store EULA](https://unity.com/legal/as-terms).

## Contact
For questions or support, feel free to reach out at [aqaddora96@gmail.com](mailto:aqaddora96@gmail.com).

---

Thank you for using the `Biometrics Authentication` plugin! We hope it simplifies integrating biometric authentication into your Unity projects.