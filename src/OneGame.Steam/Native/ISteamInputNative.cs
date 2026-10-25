namespace OneGame.Steam.Native;

/// <summary>Digital action state as Steam reports it.</summary>
internal readonly record struct DigitalReading(bool Down, bool Active);

/// <summary>Analog action state as Steam reports it. Y is positive upward.</summary>
internal readonly record struct AnalogReading(float X, float Y, bool Active);

/// <summary>The boundary to Steam Input and the on-screen keyboard: one method per native call, in managed types. The real implementation calls the
/// flat API; tests and the harness supply a fake. A handle of 0 means "not found".</summary>
internal interface ISteamInputNative
{
    /// <summary>True for a stand-in that has no Steam behind it. Only such a stand-in may be driven by the harness's input injection.</summary>
    bool IsFake => false;

    bool Init(string? manifestPath);
    void Shutdown();
    void RunFrame();

    int GetConnectedControllers(Span<ulong> handles);
    int GetInputType(ulong controller);

    ulong GetActionSet(string name);
    ulong GetDigitalAction(string name);
    ulong GetAnalogAction(string name);

    void ActivateActionSet(ulong controller, ulong set);
    ulong GetCurrentActionSet(ulong controller);

    DigitalReading ReadDigital(ulong controller, ulong action);
    AnalogReading ReadAnalog(ulong controller, ulong action);

    int GetDigitalOrigins(ulong controller, ulong set, ulong action, Span<int> origins);
    int GetAnalogOrigins(ulong controller, ulong set, ulong action, Span<int> origins);
    int TranslateOrigin(int destinationInputType, int origin);
    string GlyphPath(int origin, int size, uint flags);
    string OriginName(int origin);

    bool ShowKeyboard(int x, int y, int width, int height);
    bool DismissKeyboard();
}
