using System.Collections;
using UnityEngine;
using UnityEngine.Android;

public class LocationData : MonoBehaviour
{
    public static LocationData Instance { get; private set; }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }

    // Public method to get distance to a course
    public void GetCourseDist(float lat, float lon, System.Action<float> callback)
    {
        GetUserLocation(location =>
        {
            if (location != Vector2.zero)
            {
                float distance = CalculateDistance(lat, lon, location.x, location.y);
                callback?.Invoke(distance);
            }
            else
            {
                Debug.LogWarning("Failed to get user location.");
                callback?.Invoke(-1f); // Indicate failure
            }
        });
    }

    // Public method to get user location, runs coroutine
    public void GetUserLocation(System.Action<Vector2> callback)
    {
        StartCoroutine(GetLatLonUsingGPS((success, location) =>
        {
            if (success)
            {
                callback?.Invoke(location);
            }
            else
            {
                callback?.Invoke(new Vector2(0, 0));
            }
        }));
    }

    // Coroutine to fetch GPS coordinates
    private IEnumerator GetLatLonUsingGPS(System.Action<bool, Vector2> callback)
    {
        // Check if location permission is granted
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("Requesting location permission...");
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(1f); // Wait for user response
        }

        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("Location permission denied.");
            callback?.Invoke(false, new Vector2(0, 0));
            yield break;
        }

        // Start location service
        Input.location.Start();
        int maxWait = 10; // Wait up to 10 seconds for GPS
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1f);
            maxWait--;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Failed to initialize location service.");
            callback?.Invoke(false, new Vector2(0, 0));
            yield break;
        }
        else if (Input.location.status == LocationServiceStatus.Running)
        {
            // Retrieve location
            float latitude = Input.location.lastData.latitude;
            float longitude = Input.location.lastData.longitude;
            Vector2 location = new Vector2(latitude, longitude);
            Debug.Log($"Got location: {latitude}, {longitude}");
            callback?.Invoke(true, location);
        }

        // Stop location service to save battery
        Input.location.Stop();
    }

    // Simple Euclidean distance calculation (in kilometers, approximate)
    private float CalculateDistance(float lat1, float lon1, float lat2, float lon2)
    {
        // Approximate conversion: 1 degree ≈ 111 km for latitude, adjusted for longitude
        float latDiff = (lat2 - lat1) * 111f; // Latitude difference in km
        float lonDiff = (lon2 - lon1) * 111f * Mathf.Cos(lat1 * Mathf.Deg2Rad); // Adjust longitude for latitude
        float distance = Mathf.Sqrt(latDiff * latDiff + lonDiff * lonDiff); // Euclidean distance in km
        return distance; // Returns distance in kilometers
    }
}