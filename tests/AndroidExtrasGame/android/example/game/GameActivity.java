package example.game;

import android.app.NativeActivity;
import android.os.Bundle;
import android.util.Log;

/** The game's own activity: the engine's native activity with a line in the log when it starts. */
public class GameActivity extends NativeActivity {
    @Override
    protected void onCreate(Bundle state) {
        Log.i("GameActivity", "onCreate in " + getClass().getName());
        super.onCreate(state);
    }
}
