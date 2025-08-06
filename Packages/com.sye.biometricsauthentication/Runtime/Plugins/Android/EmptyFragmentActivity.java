package sye.biometricsauthentication;
import androidx.fragment.app.FragmentActivity;
import android.os.Bundle ;
import android.content.Intent;
public class EmptyFragmentActivity extends FragmentActivity {
    private static OnActivityCreatedCallback onActivityCreatedCallback;
    private static EmptyFragmentActivity instance;
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        instance = this;
        onActivityCreatedCallback.onActivityCreated(this);
    }
    public static void startFromUnity (OnActivityCreatedCallback callback) {
        onActivityCreatedCallback = callback;
        Intent myIntent = new Intent(com.unity3d.player.UnityPlayer.currentActivity, EmptyFragmentActivity.class);
        com.unity3d.player.UnityPlayer.currentActivity.startActivity(myIntent);
    }
    public static void close () {
        instance.finish();
    }
    public interface OnActivityCreatedCallback {
        public void onActivityCreated (FragmentActivity fa);
    }        
}