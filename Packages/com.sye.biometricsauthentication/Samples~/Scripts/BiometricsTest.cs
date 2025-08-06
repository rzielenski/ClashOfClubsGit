using UnityEngine;
using UnityEngine.UI;
using SyE.BiometricsAuthentication;
namespace SyE.BiometricsAuthentication.Demo {
    public class BiometricsTest : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text text;

        public void AuthenticateButton()
        {
            Biometrics.Authenticate(
                onSuccess: () => {
                    text.text = "Authentication succeeded.";
                    backgroundImage.color = Color.green;
                    Debug.Log("Authentication succeeded.");
                },
                onFailure: () => {
                    text.text = "Authentication failed.";
                    backgroundImage.color = Color.red;
                    Debug.LogError("Authentication failed.");
                }
            );
        }
    }
}
